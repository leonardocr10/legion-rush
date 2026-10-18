using System;
using UnityEngine;

namespace LegionRush
{
    [Serializable]
    public class AudioSettingsData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public float master = 1f;
        public float music = 0.8f;
        public float sfx = 1f;
        public float ui = 1f;
        public bool masterMuted;
        public bool musicMuted;
        public bool sfxMuted;
        public bool uiMuted;

        public float GetVolume(AudioBus bus) => bus switch
        {
            AudioBus.Music => music,
            AudioBus.Sfx => sfx,
            AudioBus.Ui => ui,
            _ => master
        };

        public void SetVolume(AudioBus bus, float value)
        {
            value = Clean(value, 1f);
            switch (bus)
            {
                case AudioBus.Music: music = value; break;
                case AudioBus.Sfx: sfx = value; break;
                case AudioBus.Ui: ui = value; break;
                default: master = value; break;
            }
        }

        public bool IsMuted(AudioBus bus) => bus switch
        {
            AudioBus.Music => musicMuted,
            AudioBus.Sfx => sfxMuted,
            AudioBus.Ui => uiMuted,
            _ => masterMuted
        };

        public void SetMuted(AudioBus bus, bool muted)
        {
            switch (bus)
            {
                case AudioBus.Music: musicMuted = muted; break;
                case AudioBus.Sfx: sfxMuted = muted; break;
                case AudioBus.Ui: uiMuted = muted; break;
                default: masterMuted = muted; break;
            }
        }

        public void Sanitize()
        {
            version = CurrentVersion;
            master = Clean(master, 1f);
            music = Clean(music, 0.8f);
            sfx = Clean(sfx, 1f);
            ui = Clean(ui, 1f);
        }

        private static float Clean(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return fallback;
            return Mathf.Clamp01(value);
        }
    }

    public static class AudioSettingsStore
    {
        private const string Key = "LR_AudioSettings";

        public static AudioSettingsData Load()
        {
            var data = new AudioSettingsData();
            if (!PlayerPrefs.HasKey(Key)) return data;

            string json = PlayerPrefs.GetString(Key, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                GameLog.Warn("Configurações de áudio vazias; usando padrões.");
                return data;
            }
            try
            {
                JsonUtility.FromJsonOverwrite(json, data);
            }
            catch (Exception exception)
            {
                GameLog.Warn($"Configurações de áudio corrompidas ({exception.Message}); restaurando padrões.");
                data = new AudioSettingsData();
                Save(data);
                return data;
            }
            if (data.version > AudioSettingsData.CurrentVersion)
                GameLog.Warn($"Configurações de áudio de versão futura ({data.version}); lendo campos conhecidos.");
            data.Sanitize();
            return data;
        }

        public static void Save(AudioSettingsData data)
        {
            if (data == null) return;
            data.Sanitize();
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
