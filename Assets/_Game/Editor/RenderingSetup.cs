using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LegionRush.EditorTools
{
    // Recursos de renderização do renderer URP do jogo: oclusão de ambiente (SSAO) leve, para assentar tropas,
    // muros e props no chão como na arte de referência. Idempotente: só cria o que falta.
    public static class RenderingSetup
    {
        private const string RendererPath = "Assets/_Game/Settings/LR_URP_Renderer.asset";

        [MenuItem("Legion Rush/Setup/Ensure SSAO")]
        public static void EnsureSsao()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null) return;
            foreach (ScriptableRendererFeature existing in rendererData.rendererFeatures)
                if (existing is ScreenSpaceAmbientOcclusion) return;

            var ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            ssao.name = "SSAO";
            AssetDatabase.AddObjectToAsset(ssao, rendererData);

            // Barato o bastante para celular/WebGL: meia resolução, poucas amostras, raio de mundo pequeno.
            var so = new SerializedObject(ssao);
            SerializedProperty s = so.FindProperty("m_Settings");
            s.FindPropertyRelative("Downsample").boolValue = true;
            s.FindPropertyRelative("Intensity").floatValue = 1.1f;
            s.FindPropertyRelative("Radius").floatValue = 0.35f;
            s.FindPropertyRelative("DirectLightingStrength").floatValue = 0.35f;
            s.FindPropertyRelative("Falloff").floatValue = 60f;
            s.FindPropertyRelative("Samples").enumValueIndex = 2; // Low
            so.ApplyModifiedPropertiesWithoutUndo();

            var data = new SerializedObject(rendererData);
            SerializedProperty features = data.FindProperty("m_RendererFeatures");
            SerializedProperty map = data.FindProperty("m_RendererFeatureMap");
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = ssao;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out _, out long localId);
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            data.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            Debug.Log("[LegionRush] SSAO adicionado ao renderer URP.");
        }
    }
}
