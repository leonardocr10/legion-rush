using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LegionRush.EditorTools
{
    // Importação automática: SFX curtos = Decompress On Load + ADPCM + mono; music_* = Streaming + Vorbis.
    public class AudioImportProcessor : AssetPostprocessor
    {
        public const string AudioRoot = "Assets/_Game/Audio";
        private static bool forceReapply;

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioRoot + "/", StringComparison.OrdinalIgnoreCase)) return;
            var importer = (AudioImporter)assetImporter;
            if (!importer.importSettingsMissing && !forceReapply) return;

            bool isMusic = Path.GetFileName(assetPath).StartsWith("music_", StringComparison.OrdinalIgnoreCase);
            AudioImporterSampleSettings sample = importer.defaultSampleSettings;
            sample.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            if (isMusic)
            {
                sample.loadType = AudioClipLoadType.Streaming;
                sample.compressionFormat = AudioCompressionFormat.Vorbis;
                sample.quality = 0.6f;
                importer.forceToMono = false;
                importer.loadInBackground = true;
            }
            else
            {
                sample.loadType = AudioClipLoadType.DecompressOnLoad;
                sample.compressionFormat = AudioCompressionFormat.ADPCM;
                importer.forceToMono = true;
                importer.loadInBackground = false;
            }
            importer.defaultSampleSettings = sample;
        }

        [MenuItem("Legion Rush/Audio/Reapply Import Settings")]
        private static void ReapplyImportSettings()
        {
            if (!AssetDatabase.IsValidFolder(AudioRoot))
            {
                EditorUtility.DisplayDialog("Legion Rush", $"Pasta {AudioRoot} não encontrada.", "OK");
                return;
            }
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot });
            forceReapply = true;
            try
            {
                foreach (string guid in guids)
                    AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid),
                        ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                forceReapply = false;
            }
            Debug.Log($"[LegionRush] Configurações de importação reaplicadas em {guids.Length} clipes.");
        }
    }

    public static class AudioLibraryTools
    {
        private const string LibraryFolder = "Assets/_Game/ScriptableObjects/Audio";

        [MenuItem("Legion Rush/Audio/Create Audio Library Asset")]
        private static void CreateLibraryAsset()
        {
            EnsureFolder(LibraryFolder);
            string path = AssetDatabase.GenerateUniqueAssetPath(LibraryFolder + "/AudioLibrary.asset");
            AudioLibrary library = AudioLibrary.CreateDefault();
            var missingSounds = new List<string>();
            var missingMusic = new List<string>();
            library.ResolveClips(missingSounds, missingMusic);
            AssetDatabase.CreateAsset(library, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = library;
            Debug.Log($"[LegionRush] AudioLibrary criada em {path}. {Report(missingSounds, missingMusic)}");
        }

        [MenuItem("Legion Rush/Audio/Validate Audio Files")]
        private static void ValidateAudioFiles()
        {
            AudioLibrary library = AudioLibrary.CreateDefault();
            var missingSounds = new List<string>();
            var missingMusic = new List<string>();
            library.ResolveClips(missingSounds, missingMusic);
            UnityEngine.Object.DestroyImmediate(library);
            string report = Report(missingSounds, missingMusic);
            Debug.Log("[LegionRush] " + report);
            EditorUtility.DisplayDialog("Legion Rush — Áudio", report, "OK");
        }

        private static string Report(List<string> missingSounds, List<string> missingMusic)
        {
            string sounds = missingSounds.Count == 0 ? "Todos os efeitos encontrados." : "Efeitos ausentes: " + string.Join(", ", missingSounds);
            string music = missingMusic.Count == 0 ? string.Empty : "\nTrilhas opcionais ausentes: " + string.Join(", ", missingMusic);
            return sounds + music;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
