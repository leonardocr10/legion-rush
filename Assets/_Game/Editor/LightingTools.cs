using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LegionRush.EditorTools
{
    public static class LightingTools
    {
        private const string ProfileFolder = "Assets/_Game/ScriptableObjects/Lighting";
        private const string SettingsFolder = "Assets/_Game/Settings";

        [MenuItem("Legion Rush/Lighting/Create World Lighting Profiles")]
        private static void CreateWorldProfiles()
        {
            EnsureFolder(ProfileFolder);
            int created = 0;
            for (int i = 0; i < WorldLightingProfile.WorldCount; i++)
            {
                var id = (WorldId)i;
                string path = $"{ProfileFolder}/WorldLighting_{id}.asset";
                if (AssetDatabase.LoadAssetAtPath<WorldLightingProfile>(path) != null) continue;
                AssetDatabase.CreateAsset(WorldLightingProfile.CreatePreset(id), path);
                created++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[LegionRush] {created} perfis de iluminação criados em {ProfileFolder}. Arraste-os para GameBootstrap > World Profiles.");
        }

        // Ajusta o URP Asset ativo para mobile. Usa SerializedObject porque parte dos campos não tem setter público.
        [MenuItem("Legion Rush/Lighting/Apply Recommended URP Settings")]
        private static void ApplyRecommendedUrpSettings()
        {
            var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset == null)
            {
                EditorUtility.DisplayDialog("Legion Rush", "Nenhum URP Asset ativo em Graphics/Quality Settings.", "OK");
                return;
            }

            var serialized = new SerializedObject(asset);
            var report = new List<string>();
            SetBool(serialized, "m_UseSRPBatcher", true, report);
            SetBool(serialized, "m_SupportsDynamicBatching", false, report);
            SetBool(serialized, "m_MainLightShadowsSupported", true, report);
            SetInt(serialized, "m_MainLightShadowmapResolution", 2048, report);
            SetInt(serialized, "m_ShadowCascadeCount", 1, report);
            SetFloat(serialized, "m_ShadowDistance", 40f, report);
            SetBool(serialized, "m_SoftShadowsSupported", true, report);
            SetInt(serialized, "m_AdditionalLightsPerObjectLimit", 2, report);
            SetBool(serialized, "m_AdditionalLightShadowsSupported", false, report);
            SetBool(serialized, "m_SupportsHDR", true, report);
            SetInt(serialized, "m_ColorGradingMode", 0, report);
            SetInt(serialized, "m_ColorGradingLutSize", 16, report);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LegionRush] URP Asset '{asset.name}' ajustado:\n{string.Join("\n", report)}");
        }

        // Para cenas de mundo montadas no editor (Etapa 10). A pista gerada em runtime usa a luz ambiente SH.
        [MenuItem("Legion Rush/Lighting/Create Light Probe Grid From Selection")]
        private static void CreateLightProbeGrid()
        {
            GameObject selected = Selection.activeGameObject;
            Renderer[] renderers = selected != null
                ? selected.GetComponentsInChildren<Renderer>()
                : Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            if (renderers.Length == 0)
            {
                EditorUtility.DisplayDialog("Legion Rush", "Selecione o objeto raiz do cenário (com renderers).", "OK");
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            float spacing = 4f;
            float[] heights = { 0.5f, 2.5f };
            while ((bounds.size.x / spacing + 1f) * (bounds.size.z / spacing + 1f) * heights.Length > 4096f) spacing *= 1.5f;

            var positions = new List<Vector3>();
            for (float x = bounds.min.x; x <= bounds.max.x + 0.01f; x += spacing)
            {
                for (float z = bounds.min.z; z <= bounds.max.z + 0.01f; z += spacing)
                {
                    foreach (float h in heights) positions.Add(new Vector3(x, bounds.min.y + h, z));
                }
            }

            var go = new GameObject("LR_LightProbeGrid");
            Undo.RegisterCreatedObjectUndo(go, "Create Light Probe Grid");
            var group = go.AddComponent<LightProbeGroup>();
            group.probePositions = positions.ToArray();
            Selection.activeGameObject = go;
            Debug.Log($"[LegionRush] {positions.Count} light probes criados (espaçamento {spacing:0.#}m).");
        }

        [MenuItem("Legion Rush/Lighting/Apply Mobile Lightmap Settings")]
        private static void ApplyMobileLightmapSettings()
        {
            if (!Lightmapping.TryGetLightingSettings(out LightingSettings settings) || settings == null)
            {
                EnsureFolder(SettingsFolder);
                settings = new LightingSettings { name = "LR_MobileLighting" };
                AssetDatabase.CreateAsset(settings, SettingsFolder + "/LR_MobileLighting.lighting");
                Lightmapping.lightingSettings = settings;
            }

            settings.bakedGI = true;
            settings.realtimeGI = false;
            settings.mixedBakeMode = MixedLightingMode.Subtractive;
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            settings.lightmapResolution = 12f;
            settings.lightmapMaxSize = 1024;
            settings.lightmapPadding = 2;
            settings.directionalityMode = LightmapsMode.NonDirectional;
            settings.ao = true;
            settings.aoMaxDistance = 1.5f;
            settings.lightmapCompression = LightmapCompression.NormalQuality;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[LegionRush] Lightmap mobile: Baked GI + Subtractive, 12 texels/m, 1024px, não-direcional, AO baked.");
        }

        private static void SetBool(SerializedObject so, string name, bool value, List<string> report)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null) { report.Add($"  ? {name} não encontrado nesta versão do URP"); return; }
            p.boolValue = value;
            report.Add($"  {name} = {value}");
        }

        private static void SetInt(SerializedObject so, string name, int value, List<string> report)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null) { report.Add($"  ? {name} não encontrado nesta versão do URP"); return; }
            p.intValue = value;
            report.Add($"  {name} = {value}");
        }

        private static void SetFloat(SerializedObject so, string name, float value, List<string> report)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null) { report.Add($"  ? {name} não encontrado nesta versão do URP"); return; }
            p.floatValue = value;
            report.Add($"  {name} = {value}");
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
