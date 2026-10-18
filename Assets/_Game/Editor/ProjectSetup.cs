using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

#pragma warning disable 0618 // FindObjectsSortMode: obsoleto apenas em versões mais novas do Unity 6
namespace LegionRush.EditorTools
{
    // Configura o projeto do zero (URP, cena, Android) e gera o APK.
    // Usado pelo menu e pelo GitHub Actions (game-ci) via -executeMethod.
    public static class ProjectSetup
    {
        private const string SettingsFolder = "Assets/_Game/Settings";
        private const string ScenesFolder = "Assets/_Game/Scenes";
        private const string ScenePath = ScenesFolder + "/Main.unity";
        private const string UrpAssetPath = SettingsFolder + "/LR_URP.asset";
        private const string RendererPath = SettingsFolder + "/LR_URP_Renderer.asset";
        private const string PostProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";
        private const string ToonShaderPath = "Assets/_Game/Shaders/LegionToon.shader";
        private const string GlowShaderPath = "Assets/_Game/Shaders/LegionGlow.shader";
        private const string DefaultApkPath = "Builds/Android/LegionRush3D.apk";

        [MenuItem("Legion Rush/Setup/Configure Project (URP + Scene + Android)")]
        public static void ConfigureProject()
        {
            EnsureFolder(SettingsFolder);
            EnsureFolder(ScenesFolder);
            EnsureUrp();
            EnsureScene();
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            Debug.Log("[LegionRush] Projeto configurado: URP ativo, cena Main na build, Android IL2CPP ARM64 retrato.");
        }

        [MenuItem("Legion Rush/Setup/Build Android APK")]
        public static void BuildAndroid()
        {
            try
            {
                ConfigureProject();
                string output = GetArgument("-customBuildPath") ?? DefaultApkPath;
                if (!output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase)) output = Path.Combine(output, "LegionRush3D.apk");
                string directory = Path.GetDirectoryName(output);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                EditorUserBuildSettings.buildAppBundle = false;

                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = output,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                if (summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"[LegionRush] Build falhou: {summary.result}, {summary.totalErrors} erros.");
                    ExitIfBatch(1);
                    return;
                }
                Debug.Log($"[LegionRush] APK gerado: {output} ({summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime.TotalMinutes:0.0} min).");
                ExitIfBatch(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("[LegionRush] Build interrompido: " + exception);
                ExitIfBatch(1);
            }
        }

        private static void EnsureUrp()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (asset == null)
            {
                var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
                if (rendererData == null)
                {
                    rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(rendererData, RendererPath);
                }
                if (rendererData.postProcessData == null)
                    rendererData.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(PostProcessDataPath);
                if (rendererData.postProcessData == null)
                    Debug.LogWarning("[LegionRush] PostProcessData padrão não encontrado; pós-processamento pode ficar inativo.");
                EditorUtility.SetDirty(rendererData);

                asset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(asset, UrpAssetPath);
            }

            asset.supportsHDR = true;
            asset.msaaSampleCount = 2;
            asset.renderScale = 1f;
            asset.shadowDistance = 40f;
            asset.shadowCascadeCount = 1;
            EditorUtility.SetDirty(asset);

            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(current, false);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureScene()
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            if (Camera.main == null)
            {
                var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cameraGo.AddComponent<Camera>();
                cameraGo.AddComponent<AudioListener>();
                cameraGo.AddComponent<UniversalAdditionalCameraData>();
            }

            bool hasSun = false;
            foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                hasSun |= light.type == LightType.Directional;
            if (!hasSun)
            {
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
            }

            var bootstrap = UnityEngine.Object.FindAnyObjectByType<LegionRush.GameBootstrap>();
            if (bootstrap == null) bootstrap = new GameObject("Bootstrap").AddComponent<LegionRush.GameBootstrap>();
            var serialized = new SerializedObject(bootstrap);
            AssignObject(serialized, "toonShader", AssetDatabase.LoadAssetAtPath<Shader>(ToonShaderPath));
            AssignObject(serialized, "glowShader", AssetDatabase.LoadAssetAtPath<Shader>(GlowShaderPath));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "LegionRush";
            PlayerSettings.productName = "Legion Rush 3D";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.legionrush.game");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
        }

        private static void AssignObject(SerializedObject serialized, string property, UnityEngine.Object value)
        {
            SerializedProperty p = serialized.FindProperty(property);
            if (p == null)
            {
                Debug.LogWarning($"[LegionRush] Campo '{property}' não encontrado no GameBootstrap.");
                return;
            }
            if (value == null) Debug.LogWarning($"[LegionRush] Asset para '{property}' não encontrado.");
            p.objectReferenceValue = value;
        }

        private static string GetArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }
            return null;
        }

        private static void ExitIfBatch(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
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
