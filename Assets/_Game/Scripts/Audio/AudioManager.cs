using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace LegionRush
{
    // Canais independentes (Música, SFX, UI), limite de vozes, agregação de pedidos por quadro
    // e persistência. Todo o roteamento passa por SoundId: trocar arquivos não altera a lógica.
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        private const float SaveDelay = 0.6f;
        private const float StealMinAge = 0.06f;
        private const float CullMargin = 0.35f;
        private const float FarDepthStart = 55f;
        private const float FarDepthEnd = 100f;
        private const float CrowdGainPerDoubling = 0.18f;

        private struct Voice
        {
            public AudioSource Source;
            public int Sound;
            public AudioBus Bus;
            public float BaseVolume;
            public float StartTime;
            public int Priority;
        }

        public static AudioManager Instance => instance;
        private static AudioManager instance;

        public event Action SettingsChanged;
        public MusicTrack CurrentTrack { get; private set; } = MusicTrack.None;
        public bool IsReady => initialized;

        [SerializeField, Tooltip("Usado apenas quando o AudioManager é colocado manualmente na cena.")]
        private AudioLibrary sceneLibrary;

        private readonly float[] busGain = new float[4];
        private readonly AudioSource[] musicSources = new AudioSource[2];
        private readonly float[] musicFade = new float[2];
        private readonly float[] musicFadeTarget = new float[2];
        private readonly float[] musicTrackVolume = new float[2];
        private readonly HashSet<MusicTrack> reportedTracks = new HashSet<MusicTrack>();

        private AudioLibrary library;
        private AudioSettingsData settings;
        private SoundDef[] defs;
        private MusicDef[] musicDefs;
        private Voice[] sfxVoices;
        private Voice[] uiVoices;
        private float[] lastPlayTime;
        private int[] pendingCount;
        private int[] pendingPositioned;
        private Vector3[] pendingPosition;
        private int[] activeCount;
        private int[] lastVariant;
        private bool anyPending;
        private bool initialized;
        private int activeMusic;
        private float musicFadeSpeed = 1f;
        private float duckLevel = 1f;
        private float duckTarget = 1f;
        private float duckTimer;
        private float saveTimer = -1f;
        private Camera listenerCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        public static AudioManager EnsureInstance(AudioLibrary source)
        {
            if (instance != null) return instance;
            var go = new GameObject("LR_AudioManager");
            DontDestroyOnLoad(go);
            AudioManager manager = go.AddComponent<AudioManager>();
            manager.Initialize(source);
            return manager;
        }

        public static void EnsureSingleListener()
        {
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            int enabledCount = 0;
            for (int i = 0; i < listeners.Length; i++)
            {
                if (!listeners[i].isActiveAndEnabled) continue;
                enabledCount++;
                if (enabledCount <= 1) continue;
                listeners[i].enabled = false;
                GameLog.Warn($"AudioListener extra desativado em '{listeners[i].name}'.");
            }
            if (instance != null) instance.listenerCamera = null;
            if (enabledCount > 0) return;

            Camera cam = Camera.main;
            if (cam == null)
            {
                GameLog.Warn("Nenhuma câmera principal para receber o AudioListener.");
                return;
            }
            AudioListener listener = cam.GetComponent<AudioListener>();
            if (listener == null) cam.gameObject.AddComponent<AudioListener>();
            else listener.enabled = true;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                GameLog.Warn("AudioManager duplicado removido.");
                Destroy(gameObject);
                return;
            }
            instance = this;
            AudioListener.pause = false;
        }

        private void Start()
        {
            if (instance != this || initialized) return;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
            Initialize(sceneLibrary);
        }

        private void Initialize(AudioLibrary source)
        {
            if (initialized) return;
            library = source != null ? Instantiate(source) : AudioLibrary.CreateDefault();
            library.name = "LR_RuntimeAudioLibrary";
            library.Sanitize();
            var missingSounds = new List<string>();
            var missingMusic = new List<string>();
            library.ResolveClips(missingSounds, missingMusic);

            defs = library.sounds.ToArray();
            int count = defs.Length;
            lastPlayTime = new float[count];
            pendingCount = new int[count];
            pendingPositioned = new int[count];
            pendingPosition = new Vector3[count];
            activeCount = new int[count];
            lastVariant = new int[count];
            for (int i = 0; i < count; i++)
            {
                lastPlayTime[i] = -1000f;
                lastVariant[i] = -1;
            }

            musicDefs = new MusicDef[Enum.GetValues(typeof(MusicTrack)).Length];
            foreach (MusicDef def in library.music) musicDefs[(int)def.track] = def;

            settings = AudioSettingsStore.Load();
            RecomputeBusGains();

            sfxVoices = CreateVoices("SFX", library.sfxVoices, library.sfxGroup, false);
            uiVoices = CreateVoices("UI", library.uiVoices, library.uiGroup, true);
            Transform musicRoot = CreateChild("Music");
            for (int i = 0; i < musicSources.Length; i++)
            {
                AudioSource source = CreateSource(musicRoot, library.musicGroup, true);
                source.loop = true;
                source.priority = 0;
                musicSources[i] = source;
            }

            initialized = true;
            if (missingSounds.Count > 0)
                GameLog.Warn($"Áudio: {missingSounds.Count} sons sem arquivo: {string.Join(", ", missingSounds)}");
            if (missingMusic.Count > 0)
                GameLog.Info($"Áudio: trilhas opcionais ausentes (a trilha atual continua): {string.Join(", ", missingMusic)}");
            GameLog.Info($"AudioManager pronto: {count - missingSounds.Count}/{count} sons, {library.sfxVoices} vozes SFX, {library.uiVoices} vozes UI.");
        }

        // ---------- Efeitos ----------

        public void Request(SoundId id) => Enqueue((int)id, false, default);
        public void Request(SoundId id, Vector3 position) => Enqueue((int)id, true, position);

        public void StopAllSfx()
        {
            if (!initialized) return;
            for (int i = 0; i < sfxVoices.Length; i++)
            {
                if (sfxVoices[i].Sound < 0) continue;
                sfxVoices[i].Source.Stop();
                sfxVoices[i].Sound = -1;
            }
            for (int i = 0; i < defs.Length; i++)
            {
                if (defs[i].bus == AudioBus.Ui) continue;
                pendingCount[i] = 0;
                pendingPositioned[i] = 0;
                pendingPosition[i] = Vector3.zero;
            }
            duckTarget = 1f;
            duckTimer = 0f;
        }

        private void Enqueue(int index, bool positioned, Vector3 position)
        {
            if (!initialized || (uint)index >= (uint)defs.Length) return;
            pendingCount[index]++;
            if (positioned)
            {
                pendingPosition[index] += position;
                pendingPositioned[index]++;
            }
            anyPending = true;
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            float now = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            Array.Clear(activeCount, 0, activeCount.Length);
            RefreshVoices(sfxVoices);
            RefreshVoices(uiVoices);
            if (anyPending) FlushPending(now);
            UpdateMusic(dt);

            if (saveTimer >= 0f)
            {
                saveTimer -= dt;
                if (saveTimer < 0f) FlushSave(true);
            }
        }

        private void RefreshVoices(Voice[] pool)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].Sound < 0) continue;
                if (!pool[i].Source.isPlaying)
                {
                    pool[i].Sound = -1;
                    continue;
                }
                activeCount[pool[i].Sound]++;
            }
        }

        private void FlushPending(float now)
        {
            anyPending = false;
            for (int i = 0; i < defs.Length; i++)
            {
                int requests = pendingCount[i];
                if (requests == 0) continue;
                SoundDef def = defs[i];
                if (now - lastPlayTime[i] < def.minInterval)
                {
                    anyPending = true;
                    continue;
                }

                int positioned = pendingPositioned[i];
                Vector3 position = positioned > 0 ? pendingPosition[i] / positioned : Vector3.zero;
                pendingCount[i] = 0;
                pendingPositioned[i] = 0;
                pendingPosition[i] = Vector3.zero;
                if (def.clips.Length == 0) continue;

                float pan = 0f;
                float attenuation = 1f;
                if (positioned > 0 && !ComputeSpatial(position, def.stereoPan, out pan, out attenuation)) continue;
                StartVoice(i, def, requests, pan, attenuation, now);
            }
        }

        private bool ComputeSpatial(Vector3 world, bool usePan, out float pan, out float attenuation)
        {
            pan = 0f;
            attenuation = 1f;
            if (listenerCamera == null) listenerCamera = Camera.main;
            if (listenerCamera == null) return true;

            Vector3 viewport = listenerCamera.WorldToViewportPoint(world);
            if (viewport.z <= 0f) return false;
            if (viewport.x < -CullMargin || viewport.x > 1f + CullMargin ||
                viewport.y < -CullMargin || viewport.y > 1f + CullMargin) return false;
            if (usePan) pan = Mathf.Clamp((viewport.x - 0.5f) * 2f, -1f, 1f) * library.panStrength;
            attenuation = 1f - Mathf.InverseLerp(FarDepthStart, FarDepthEnd, viewport.z) * 0.85f;
            return true;
        }

        private void StartVoice(int soundIndex, SoundDef def, int requests, float pan, float attenuation, float now)
        {
            Voice[] pool = def.bus == AudioBus.Ui ? uiVoices : sfxVoices;
            int slot = activeCount[soundIndex] >= def.maxVoices
                ? FindOldestOfSound(pool, soundIndex, now)
                : FindFreeOrSteal(pool, def.priority, now);
            if (slot < 0) return;

            ref Voice voice = ref pool[slot];
            if (voice.Sound >= 0)
            {
                activeCount[voice.Sound] = Mathf.Max(0, activeCount[voice.Sound] - 1);
                voice.Source.Stop();
            }

            AudioClip clip = PickClip(soundIndex, def);
            float crowd = Mathf.Min(library.maxCrowdGain, 1f + CrowdGainPerDoubling * Mathf.Log(requests, 2f));
            AudioSource source = voice.Source;
            voice.Sound = soundIndex;
            voice.Bus = def.bus;
            voice.BaseVolume = def.volume * crowd * attenuation;
            voice.StartTime = now;
            voice.Priority = def.priority;

            source.clip = clip;
            source.pitch = UnityEngine.Random.Range(def.pitchRange.x, def.pitchRange.y);
            source.panStereo = pan;
            source.priority = def.priority;
            source.volume = voice.BaseVolume * busGain[(int)def.bus];
            source.Play();

            activeCount[soundIndex]++;
            lastPlayTime[soundIndex] = now;
            if (def.duckMusic > 0f)
                Duck(def.duckMusic, def.duckSeconds > 0f ? def.duckSeconds : clip.length / Mathf.Max(0.1f, source.pitch));
        }

        private static int FindOldestOfSound(Voice[] pool, int soundIndex, float now)
        {
            int oldest = -1;
            float oldestTime = float.MaxValue;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].Sound != soundIndex || pool[i].StartTime >= oldestTime) continue;
                oldest = i;
                oldestTime = pool[i].StartTime;
            }
            return oldest >= 0 && now - oldestTime >= StealMinAge ? oldest : -1;
        }

        private static int FindFreeOrSteal(Voice[] pool, int priority, float now)
        {
            int candidate = -1;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].Sound < 0) return i;
                if (pool[i].Priority < priority || now - pool[i].StartTime < StealMinAge) continue;
                if (candidate < 0 ||
                    pool[i].Priority > pool[candidate].Priority ||
                    (pool[i].Priority == pool[candidate].Priority && pool[i].StartTime < pool[candidate].StartTime))
                    candidate = i;
            }
            return candidate;
        }

        private AudioClip PickClip(int soundIndex, SoundDef def)
        {
            AudioClip[] clips = def.clips;
            if (clips.Length == 1) return clips[0];
            int pick;
            if (lastVariant[soundIndex] < 0)
            {
                pick = UnityEngine.Random.Range(0, clips.Length);
            }
            else
            {
                pick = UnityEngine.Random.Range(0, clips.Length - 1);
                if (pick >= lastVariant[soundIndex]) pick++;
            }
            lastVariant[soundIndex] = pick;
            return clips[pick];
        }

        // ---------- Música ----------

        public void PlayMusic(MusicTrack track, bool restartIfSame = false)
        {
            if (!initialized) return;
            if (track == MusicTrack.None)
            {
                StopMusic(library.musicCrossfade);
                return;
            }

            MusicDef def = (int)track < musicDefs.Length ? musicDefs[(int)track] : null;
            if (def == null || def.clip == null)
            {
                if (reportedTracks.Add(track)) GameLog.Info($"Música '{track}' sem arquivo; mantendo a trilha atual.");
                return;
            }

            AudioSource current = musicSources[activeMusic];
            if (!restartIfSame && current.isPlaying && current.clip == def.clip && musicFadeTarget[activeMusic] > 0f)
            {
                CurrentTrack = track;
                musicTrackVolume[activeMusic] = def.volume;
                return;
            }

            int next = 1 - activeMusic;
            AudioSource source = musicSources[next];
            source.Stop();
            source.clip = def.clip;
            source.loop = def.loop;
            source.time = 0f;
            musicFade[next] = 0f;
            musicFadeTarget[next] = 1f;
            musicTrackVolume[next] = def.volume;
            musicFadeTarget[activeMusic] = 0f;
            musicFadeSpeed = 1f / Mathf.Max(0.01f, library.musicCrossfade);
            source.volume = 0f;
            source.Play();
            activeMusic = next;
            CurrentTrack = track;
        }

        public void StopMusic(float fadeSeconds)
        {
            if (!initialized) return;
            musicFadeTarget[0] = 0f;
            musicFadeTarget[1] = 0f;
            musicFadeSpeed = 1f / Mathf.Max(0.01f, fadeSeconds);
            CurrentTrack = MusicTrack.None;
        }

        public void Duck(float amount, float seconds)
        {
            duckTarget = Mathf.Min(duckTarget, 1f - Mathf.Clamp01(amount));
            duckTimer = Mathf.Max(duckTimer, Mathf.Max(0f, seconds));
        }

        private void UpdateMusic(float dt)
        {
            if (duckTimer > 0f)
            {
                duckTimer -= dt;
                if (duckTimer <= 0f) duckTarget = 1f;
            }
            duckLevel = Mathf.MoveTowards(duckLevel, duckTarget, dt * (duckLevel > duckTarget ? 5f : 0.7f));

            float gain = busGain[(int)AudioBus.Music];
            for (int i = 0; i < musicSources.Length; i++)
            {
                AudioSource source = musicSources[i];
                musicFade[i] = Mathf.MoveTowards(musicFade[i], musicFadeTarget[i], dt * musicFadeSpeed);
                source.volume = musicTrackVolume[i] * musicFade[i] * duckLevel * gain;
                if (musicFade[i] <= 0f && musicFadeTarget[i] <= 0f && source.isPlaying) source.Stop();
            }
        }

        // ---------- Configurações ----------

        public float GetVolume(AudioBus bus) => settings != null ? settings.GetVolume(bus) : 1f;
        public bool IsMuted(AudioBus bus) => settings != null && settings.IsMuted(bus);

        public void SetVolume(AudioBus bus, float value)
        {
            if (settings == null) return;
            value = Mathf.Clamp01(value);
            if (Mathf.Abs(settings.GetVolume(bus) - value) < 0.0001f) return;
            settings.SetVolume(bus, value);
            OnSettingsChanged();
        }

        public void SetMuted(AudioBus bus, bool muted)
        {
            if (settings == null || settings.IsMuted(bus) == muted) return;
            settings.SetMuted(bus, muted);
            OnSettingsChanged();
        }

        public void ResetSettings()
        {
            settings = new AudioSettingsData();
            OnSettingsChanged();
        }

        private void OnSettingsChanged()
        {
            RecomputeBusGains();
            ApplyVoiceVolumes(sfxVoices);
            ApplyVoiceVolumes(uiVoices);
            saveTimer = SaveDelay;
            SettingsChanged?.Invoke();
        }

        private void RecomputeBusGains()
        {
            float master = settings.masterMuted ? 0f : Perceptual(settings.master);
            busGain[(int)AudioBus.Master] = master;
            busGain[(int)AudioBus.Music] = settings.musicMuted ? 0f : master * Perceptual(settings.music);
            busGain[(int)AudioBus.Sfx] = settings.sfxMuted ? 0f : master * Perceptual(settings.sfx);
            busGain[(int)AudioBus.Ui] = settings.uiMuted ? 0f : master * Perceptual(settings.ui);
        }

        private void ApplyVoiceVolumes(Voice[] pool)
        {
            if (pool == null) return;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].Sound >= 0) pool[i].Source.volume = pool[i].BaseVolume * busGain[(int)pool[i].Bus];
            }
        }

        private static float Perceptual(float linear) => linear * linear;

        private void FlushSave(bool force)
        {
            if (settings == null || (!force && saveTimer < 0f)) return;
            saveTimer = -1f;
            AudioSettingsStore.Save(settings);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) FlushSave(false);
        }

        private void OnApplicationQuit() => FlushSave(false);

        private void OnDestroy()
        {
            if (instance == this)
            {
                FlushSave(false);
                instance = null;
            }
            if (library != null) Destroy(library);
        }

        // ---------- Construção ----------

        private Transform CreateChild(string label)
        {
            var child = new GameObject(label).transform;
            child.SetParent(transform, false);
            return child;
        }

        private Voice[] CreateVoices(string label, int count, AudioMixerGroup group, bool ignoreListenerPause)
        {
            Transform root = CreateChild(label);
            var voices = new Voice[count];
            for (int i = 0; i < count; i++)
                voices[i] = new Voice { Source = CreateSource(root, group, ignoreListenerPause), Sound = -1 };
            return voices;
        }

        private static AudioSource CreateSource(Transform parent, AudioMixerGroup group, bool ignoreListenerPause)
        {
            AudioSource source = parent.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.reverbZoneMix = 0f;
            source.volume = 0f;
            source.outputAudioMixerGroup = group;
            source.ignoreListenerPause = ignoreListenerPause;
            return source;
        }
    }

    // Fachada estática e segura: funciona mesmo se o AudioManager ainda não existir.
    public static class GameAudio
    {
        public static void Play(SoundId id)
        {
            AudioManager manager = AudioManager.Instance;
            if (manager != null) manager.Request(id);
        }

        public static void PlayAt(SoundId id, Vector3 position)
        {
            AudioManager manager = AudioManager.Instance;
            if (manager != null) manager.Request(id, position);
        }

        public static void PlayMusic(MusicTrack track)
        {
            AudioManager manager = AudioManager.Instance;
            if (manager != null) manager.PlayMusic(track);
        }

        public static void StopMusic(float fadeSeconds = 1f)
        {
            AudioManager manager = AudioManager.Instance;
            if (manager != null) manager.StopMusic(fadeSeconds);
        }

        public static void DuckMusic(float amount, float seconds)
        {
            AudioManager manager = AudioManager.Instance;
            if (manager != null) manager.Duck(amount, seconds);
        }

        public static void StopAllSfx()
        {
            AudioManager manager = AudioManager.Instance;
            if (manager != null) manager.StopAllSfx();
        }

        // Pausa efeitos de gameplay; música e UI continuam (ignoreListenerPause).
        public static void SetGameplayPaused(bool paused) => AudioListener.pause = paused;
    }
}
