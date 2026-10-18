using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace LegionRush
{
    // Salvamento local robusto em Application.persistentDataPath:
    // envelope JSON versionado com SHA-256, gravação atômica (temporário → substituição), backup .bak,
    // recuperação automática, migração de versões, salvamento automático e exportação/importação manual.
    public static class SaveManager
    {
        private const string Folder = "save";
        private const string FileName = "legion_save.json";
        private const string ExportPrefix = "LR1:";
        private const float AutosaveDelay = 2f;

        [Serializable]
        private class Envelope
        {
            public int version;
            public string checksum;
            public string payload;
        }

        public static SaveData Data { get; private set; }
        public static bool IsLoaded => Data != null;
        public static string LastStatus { get; private set; } = string.Empty;
        public static event Action Saved;
        public static event Action Loaded;

        private static bool dirty;
        private static float dirtyTime;
        private static SaveDriver driver;

        private static string SaveFolder => Path.Combine(Application.persistentDataPath, Folder);
        private static string PrimaryPath => Path.Combine(SaveFolder, FileName);
        private static string BackupPath => PrimaryPath + ".bak";
        private static string TempPath => PrimaryPath + ".tmp";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Data = null;
            dirty = false;
            driver = null;
        }

        public static SaveData Load()
        {
            EnsureDriver();
            System.IO.Directory.CreateDirectory(SaveFolder);
            if (TryRead(PrimaryPath, out SaveData data, out string error))
            {
                LastStatus = "Progresso carregado.";
            }
            else if (TryRead(BackupPath, out data, out string backupError))
            {
                LastStatus = "Arquivo principal inválido; progresso recuperado do backup.";
                GameLog.Warn($"Save principal inválido ({error}); usando backup.");
                Quarantine(PrimaryPath);
            }
            else
            {
                bool hadFiles = File.Exists(PrimaryPath) || File.Exists(BackupPath);
                data = SaveData.CreateNew();
                LastStatus = hadFiles ? "Saves corrompidos; novo progresso criado (cópia preservada)." : "Novo progresso criado.";
                if (hadFiles)
                {
                    GameLog.Error($"Save e backup inválidos ({error}; {backupError}). Arquivos movidos para .corrupt.");
                    Quarantine(PrimaryPath);
                    Quarantine(BackupPath);
                }
            }
            Data = data;
            Save();
            GameLog.Info($"Save: {LastStatus} Ouro {Data.wallet.gold}, gemas {Data.wallet.gems}, fase máx. {Data.progress.highestUnlocked}.");
            Loaded?.Invoke();
            return Data;
        }

        public static SaveData Current
        {
            get
            {
                if (Data == null) Load();
                return Data;
            }
        }

        // Marca alterações; o driver grava após AutosaveDelay sem novas mudanças (ou ao pausar/fechar o app).
        public static void MarkDirty()
        {
            dirty = true;
            dirtyTime = Time.unscaledTime;
            EnsureDriver();
        }

        public static bool Save()
        {
            if (Data == null) return false;
            try
            {
                Data.updatedUtcTicks = DateTime.UtcNow.Ticks;
                string json = SerializeEnvelope(Data);
                System.IO.Directory.CreateDirectory(SaveFolder);
                using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (!TryRead(TempPath, out _, out string verifyError)) throw new IOException("verificação falhou: " + verifyError);

                if (File.Exists(PrimaryPath))
                {
                    try
                    {
                        File.Replace(TempPath, PrimaryPath, BackupPath);
                    }
                    catch (Exception)
                    {
                        File.Copy(PrimaryPath, BackupPath, true);
                        File.Delete(PrimaryPath);
                        File.Move(TempPath, PrimaryPath);
                    }
                }
                else
                {
                    File.Move(TempPath, PrimaryPath);
                }
                dirty = false;
                Saved?.Invoke();
                return true;
            }
            catch (Exception exception)
            {
                LastStatus = "Falha ao salvar: " + exception.Message;
                GameLog.Error("Falha ao salvar: " + exception);
                return false;
            }
        }

        public static string Export()
        {
            if (Data == null) return string.Empty;
            string json = SerializeEnvelope(Data);
            return ExportPrefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        public static bool Import(string code, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(code) || !code.Trim().StartsWith(ExportPrefix, StringComparison.Ordinal))
            {
                message = "Código inválido.";
                return false;
            }
            try
            {
                string json = Encoding.UTF8.GetString(Convert.FromBase64String(code.Trim().Substring(ExportPrefix.Length)));
                if (!TryParse(json, out SaveData imported, out string error))
                {
                    message = "Código corrompido: " + error;
                    return false;
                }
                if (File.Exists(PrimaryPath)) File.Copy(PrimaryPath, PrimaryPath + ".before_import", true);
                Data = imported;
                Save();
                Loaded?.Invoke();
                message = "Progresso importado.";
                return true;
            }
            catch (Exception exception)
            {
                message = "Falha ao importar: " + exception.Message;
                return false;
            }
        }

        public static void ResetProgress()
        {
            if (File.Exists(PrimaryPath)) File.Copy(PrimaryPath, PrimaryPath + ".before_reset", true);
            Data = SaveData.CreateNew();
            Save();
            Loaded?.Invoke();
        }

        internal static void TickAutosave()
        {
            if (dirty && Time.unscaledTime - dirtyTime >= AutosaveDelay) Save();
        }

        internal static void FlushIfDirty()
        {
            if (dirty) Save();
        }

        private static string SerializeEnvelope(SaveData data)
        {
            string payload = JsonUtility.ToJson(data);
            var envelope = new Envelope { version = data.version, checksum = Hash(payload), payload = payload };
            return JsonUtility.ToJson(envelope, true);
        }

        private static bool TryRead(string path, out SaveData data, out string error)
        {
            data = null;
            error = string.Empty;
            if (!File.Exists(path))
            {
                error = "inexistente";
                return false;
            }
            try
            {
                return TryParse(File.ReadAllText(path, Encoding.UTF8), out data, out error);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static bool TryParse(string json, out SaveData data, out string error)
        {
            data = null;
            error = string.Empty;
            Envelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<Envelope>(json);
            }
            catch (Exception exception)
            {
                error = "JSON inválido: " + exception.Message;
                return false;
            }
            if (envelope == null || string.IsNullOrEmpty(envelope.payload))
            {
                error = "envelope vazio";
                return false;
            }
            if (!string.Equals(envelope.checksum, Hash(envelope.payload), StringComparison.Ordinal))
            {
                error = "checksum não confere";
                return false;
            }
            if (envelope.version > SaveData.CurrentVersion)
            {
                error = $"versão {envelope.version} mais nova que o jogo ({SaveData.CurrentVersion})";
                return false;
            }

            var parsed = SaveData.CreateNew();
            try
            {
                JsonUtility.FromJsonOverwrite(envelope.payload, parsed);
            }
            catch (Exception exception)
            {
                error = "dados inválidos: " + exception.Message;
                return false;
            }
            data = envelope.version < SaveData.CurrentVersion ? SaveMigrations.Migrate(parsed, envelope.version) : parsed;
            data.Normalize();
            return true;
        }

        private static string Hash(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var builder = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        private static void Quarantine(string path)
        {
            try
            {
                if (File.Exists(path)) File.Copy(path, path + "." + DateTime.UtcNow.Ticks + ".corrupt", true);
            }
            catch (Exception exception)
            {
                GameLog.Warn("Não foi possível preservar arquivo corrompido: " + exception.Message);
            }
        }

        private static void EnsureDriver()
        {
            if (driver != null) return;
            var go = new GameObject("LR_SaveDriver");
            UnityEngine.Object.DontDestroyOnLoad(go);
            driver = go.AddComponent<SaveDriver>();
        }
    }

    // Grava automaticamente: após mudanças, ao pausar (app em segundo plano) e ao fechar.
    public class SaveDriver : MonoBehaviour
    {
        private void Update() => SaveManager.TickAutosave();

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveManager.FlushIfDirty();
        }

        private void OnApplicationQuit() => SaveManager.FlushIfDirty();
    }
}
