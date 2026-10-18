using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace LegionRush
{
    public enum AudioBus { Master = 0, Music = 1, Sfx = 2, Ui = 3 }

    public enum SoundId
    {
        ArrowShot, ArrowImpact, SwordSwing, SwordHit, MagicCast, FireExplosion, IceBurst, HealingSpell,
        PortalPositive, PortalNegative, CoinPickup, GemPickup, ChestOpen, BossRoar, BossStomp,
        Victory, Defeat, UiClick, UiConfirm, UiBack
    }

    public enum MusicTrack { None = 0, Menu = 1, Battle = 2, Boss = 3 }

    [Serializable]
    public class SoundDef
    {
        public SoundId id;
        public AudioBus bus = AudioBus.Sfx;
        [Tooltip("Nome base do arquivo em Resources/<pasta>. Variações opcionais: nome_2, nome_3...")]
        public string fileName;
        [Tooltip("Clipes explícitos. Vazio = carrega pelo nome do arquivo.")]
        public AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float volume = 0.7f;
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
        [Min(1)] public int maxVoices = 3;
        [Min(0f)] public float minInterval = 0.05f;
        [Tooltip("0 = mais importante. Sons importantes roubam vozes de sons menos importantes.")]
        [Range(0, 255)] public int priority = 128;
        public bool stereoPan = true;
        [Range(0f, 1f)] public float duckMusic;
        [Tooltip("0 = usa a duração do clipe.")]
        [Min(0f)] public float duckSeconds;
    }

    [Serializable]
    public class MusicDef
    {
        public MusicTrack track;
        public string fileName;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.55f;
        public bool loop = true;
    }

    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Legion Rush/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        public const string DefaultFolder = "LegionAudio";

        [Tooltip("Pasta dentro de qualquer diretório Resources. Ex.: Assets/_Game/Audio/Resources/LegionAudio")]
        public string resourcesFolder = DefaultFolder;
        [Range(8, 64)] public int sfxVoices = 24;
        [Range(2, 8)] public int uiVoices = 4;
        [Min(0f)] public float musicCrossfade = 1.2f;
        [Range(0f, 1f)] public float panStrength = 0.6f;
        [Range(1f, 2f)] public float maxCrowdGain = 1.6f;
        [Header("Roteamento opcional para AudioMixer")]
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup uiGroup;
        public List<SoundDef> sounds = new List<SoundDef>();
        public List<MusicDef> music = new List<MusicDef>();

        public static AudioLibrary CreateDefault()
        {
            var library = CreateInstance<AudioLibrary>();
            library.name = "AudioLibrary_Default";
            library.Sanitize();
            return library;
        }

        public void Sanitize()
        {
            resourcesFolder = string.IsNullOrWhiteSpace(resourcesFolder) ? DefaultFolder : resourcesFolder.Trim().Trim('/');
            sfxVoices = Mathf.Clamp(sfxVoices, 8, 64);
            uiVoices = Mathf.Clamp(uiVoices, 2, 8);
            musicCrossfade = Mathf.Max(0f, musicCrossfade);
            panStrength = Mathf.Clamp01(panStrength);
            maxCrowdGain = Mathf.Clamp(maxCrowdGain, 1f, 2f);

            var soundIds = (SoundId[])Enum.GetValues(typeof(SoundId));
            var orderedSounds = new SoundDef[soundIds.Length];
            if (sounds != null)
            {
                foreach (SoundDef def in sounds)
                {
                    if (def == null) continue;
                    int index = (int)def.id;
                    if (index < 0 || index >= orderedSounds.Length) continue;
                    if (orderedSounds[index] != null)
                    {
                        GameLog.Warn($"AudioLibrary: som '{def.id}' duplicado; usando o primeiro.");
                        continue;
                    }
                    orderedSounds[index] = def;
                }
            }
            for (int i = 0; i < orderedSounds.Length; i++)
            {
                SoundDef def = orderedSounds[i] ?? DefaultSound(soundIds[i]);
                SanitizeSound(def);
                orderedSounds[i] = def;
            }
            sounds = new List<SoundDef>(orderedSounds);

            var tracks = (MusicTrack[])Enum.GetValues(typeof(MusicTrack));
            var orderedMusic = new MusicDef[tracks.Length];
            if (music != null)
            {
                foreach (MusicDef def in music)
                {
                    if (def == null || def.track == MusicTrack.None) continue;
                    int index = (int)def.track;
                    if (index < 0 || index >= orderedMusic.Length || orderedMusic[index] != null) continue;
                    orderedMusic[index] = def;
                }
            }
            music = new List<MusicDef>();
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i] == MusicTrack.None) continue;
                MusicDef def = orderedMusic[(int)tracks[i]] ?? DefaultMusic(tracks[i]);
                if (string.IsNullOrWhiteSpace(def.fileName)) def.fileName = DefaultMusic(def.track).fileName;
                def.volume = Mathf.Clamp01(def.volume);
                music.Add(def);
            }
        }

        public void ResolveClips(List<string> missingSounds, List<string> missingMusic)
        {
            AudioClip[] all = Resources.LoadAll<AudioClip>(resourcesFolder);
            var byName = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
            foreach (AudioClip clip in all)
            {
                if (clip != null && !byName.ContainsKey(clip.name)) byName.Add(clip.name, clip);
            }

            var variants = new List<AudioClip>(4);
            foreach (SoundDef def in sounds)
            {
                variants.Clear();
                foreach (AudioClip clip in def.clips)
                {
                    if (clip != null) variants.Add(clip);
                }
                if (variants.Count == 0) CollectVariants(byName, def.fileName, variants);
                def.clips = variants.ToArray();
                if (def.clips.Length == 0) missingSounds?.Add($"{def.id} ({def.fileName}.wav)");
            }

            foreach (MusicDef def in music)
            {
                if (def.clip == null && byName.TryGetValue(def.fileName, out AudioClip clip)) def.clip = clip;
                if (def.clip == null) missingMusic?.Add($"{def.track} ({def.fileName}.wav)");
            }
        }

        private static void CollectVariants(Dictionary<string, AudioClip> byName, string baseName, List<AudioClip> output)
        {
            if (byName.TryGetValue(baseName, out AudioClip first)) output.Add(first);
            for (int i = 2; i <= 32; i++)
            {
                if (!byName.TryGetValue($"{baseName}_{i}", out AudioClip variant)) break;
                output.Add(variant);
            }
        }

        private static void SanitizeSound(SoundDef def)
        {
            if (def.bus == AudioBus.Master) def.bus = AudioBus.Sfx;
            if (string.IsNullOrWhiteSpace(def.fileName)) def.fileName = DefaultSound(def.id).fileName;
            def.clips ??= Array.Empty<AudioClip>();
            def.volume = Mathf.Clamp01(def.volume);
            float minPitch = Mathf.Clamp(def.pitchRange.x, 0.1f, 3f);
            def.pitchRange = new Vector2(minPitch, Mathf.Clamp(def.pitchRange.y, minPitch, 3f));
            def.maxVoices = Mathf.Max(1, def.maxVoices);
            def.minInterval = Mathf.Max(0f, def.minInterval);
            def.priority = Mathf.Clamp(def.priority, 0, 255);
            def.duckMusic = Mathf.Clamp01(def.duckMusic);
            def.duckSeconds = Mathf.Max(0f, def.duckSeconds);
        }

        private static MusicDef DefaultMusic(MusicTrack track)
        {
            return track switch
            {
                MusicTrack.Menu => new MusicDef { track = track, fileName = "music_main_menu_loop", volume = 0.55f, loop = true },
                MusicTrack.Battle => new MusicDef { track = track, fileName = "music_battle_loop", volume = 0.5f, loop = true },
                MusicTrack.Boss => new MusicDef { track = track, fileName = "music_boss_loop", volume = 0.6f, loop = true },
                _ => new MusicDef { track = track, fileName = "music_none", volume = 0f, loop = false }
            };
        }

        private static SoundDef DefaultSound(SoundId id)
        {
            const AudioBus Sfx = AudioBus.Sfx;
            const AudioBus Ui = AudioBus.Ui;
            return id switch
            {
                SoundId.ArrowShot => Def(id, "arrow_shot", Sfx, 0.45f, 0.9f, 1.1f, 4, 0.045f, 160, true),
                SoundId.ArrowImpact => Def(id, "arrow_impact", Sfx, 0.4f, 0.9f, 1.12f, 4, 0.05f, 170, true),
                SoundId.SwordSwing => Def(id, "sword_swing", Sfx, 0.42f, 0.92f, 1.08f, 3, 0.06f, 165, true),
                SoundId.SwordHit => Def(id, "sword_hit", Sfx, 0.5f, 0.9f, 1.1f, 4, 0.05f, 150, true),
                SoundId.MagicCast => Def(id, "magic_cast", Sfx, 0.6f, 0.95f, 1.05f, 3, 0.08f, 120, true),
                SoundId.FireExplosion => Def(id, "fire_explosion", Sfx, 0.75f, 0.95f, 1.05f, 3, 0.1f, 90, true, 0.2f, 0.5f),
                SoundId.IceBurst => Def(id, "ice_burst", Sfx, 0.7f, 0.95f, 1.05f, 3, 0.1f, 90, true, 0.15f, 0.4f),
                SoundId.HealingSpell => Def(id, "healing_spell", Sfx, 0.6f, 0.98f, 1.02f, 2, 0.2f, 110, true),
                SoundId.PortalPositive => Def(id, "portal_positive", Sfx, 0.8f, 1f, 1f, 2, 0.1f, 40, false),
                SoundId.PortalNegative => Def(id, "portal_negative", Sfx, 0.8f, 1f, 1f, 2, 0.1f, 40, false),
                SoundId.CoinPickup => Def(id, "coin_pickup", Sfx, 0.4f, 0.95f, 1.15f, 3, 0.07f, 140, true),
                SoundId.GemPickup => Def(id, "gem_pickup", Sfx, 0.6f, 0.98f, 1.05f, 2, 0.08f, 100, true),
                SoundId.ChestOpen => Def(id, "chest_open", Sfx, 0.85f, 1f, 1f, 1, 0.2f, 30, false, 0.3f, 0f),
                SoundId.BossRoar => Def(id, "boss_roar", Sfx, 0.95f, 0.97f, 1.03f, 1, 0.5f, 10, false, 0.5f, 0f),
                SoundId.BossStomp => Def(id, "boss_stomp", Sfx, 0.9f, 0.95f, 1.05f, 2, 0.15f, 20, true, 0.25f, 0.5f),
                SoundId.Victory => Def(id, "victory", Sfx, 0.9f, 1f, 1f, 1, 1f, 0, false, 0.75f, 0f),
                SoundId.Defeat => Def(id, "defeat", Sfx, 0.9f, 1f, 1f, 1, 1f, 0, false, 0.75f, 0f),
                SoundId.UiClick => Def(id, "ui_click", Ui, 0.7f, 0.98f, 1.02f, 2, 0.03f, 50, false),
                SoundId.UiConfirm => Def(id, "ui_confirm", Ui, 0.8f, 1f, 1f, 2, 0.05f, 40, false),
                SoundId.UiBack => Def(id, "ui_back", Ui, 0.7f, 1f, 1f, 2, 0.05f, 50, false),
                _ => Def(id, id.ToString().ToLowerInvariant(), Sfx, 0.6f, 1f, 1f, 2, 0.05f, 128, true)
            };
        }

        private static SoundDef Def(SoundId id, string file, AudioBus bus, float volume, float pitchMin, float pitchMax,
            int maxVoices, float minInterval, int priority, bool pan, float duck = 0f, float duckSeconds = 0f)
        {
            return new SoundDef
            {
                id = id,
                fileName = file,
                bus = bus,
                clips = Array.Empty<AudioClip>(),
                volume = volume,
                pitchRange = new Vector2(pitchMin, pitchMax),
                maxVoices = maxVoices,
                minInterval = minInterval,
                priority = priority,
                stereoPan = pan,
                duckMusic = duck,
                duckSeconds = duckSeconds
            };
        }
    }
}
