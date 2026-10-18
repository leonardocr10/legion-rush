using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LegionRush.Environments.EditorTools
{
    // Renderiza vistas da pista do Reino Congelado em PNG (mesma câmera do jogo, 540×960) sem entrar em Play:
    // serve para comparar o cenário com as referências e iterar rápido.
    // Batch: Unity.exe -batchmode -projectPath . -executeMethod LegionRush.Environments.EditorTools.EnvPreviewCapture.CaptureBatch -captureDir <pasta>
    public static class EnvPreviewCapture
    {
        private const int Width = 540;
        private const int Height = 960;

        [MenuItem("Legion Rush/Ambiente/Capturar prévia do Reino Congelado")]
        public static void CaptureMenu()
        {
            string dir = Path.GetFullPath("Builds/EnvPreview");
            Capture(dir);
            EditorUtility.RevealInFinder(dir);
        }

        public static void CaptureBatch()
        {
            int code = 0;
            try
            {
                Capture(GetArgument("-captureDir") ?? Path.GetFullPath("Builds/EnvPreview"));
            }
            catch (Exception exception)
            {
                Debug.LogError("[LegionRush] Captura falhou: " + exception);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        private static void Capture(string dir)
        {
            Directory.CreateDirectory(dir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MaterialLibrary.PreferredToonShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Game/Shaders/LegionToon.shader");
            MaterialLibrary.PreferredGlowShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Game/Shaders/LegionGlow.shader");

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            UniversalAdditionalCameraData data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.renderShadows = true;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 140f; // igual ao GameBootstrap
            cam.allowHDR = true;
            cam.allowMSAA = true;
            float aspect = Width / (float)Height;
            cam.fieldOfView = Mathf.Clamp(2f * Mathf.Atan(Mathf.Tan(23f * Mathf.Deg2Rad) / aspect) * Mathf.Rad2Deg, 40f, 75f);

            var lighting = new GameObject("Lighting").AddComponent<WorldLightingController>();
            lighting.Initialize(sun, cam, null);
            WorldLightingProfile profile = WorldLightingProfile.CreatePreset(WorldId.FrozenKingdom);
            lighting.Apply(profile);

            LevelConfig config = LevelConfig.CreateDefault();
            config.Sanitize();
            float hw = config.trackHalfWidth;
            var root = new GameObject("Track");
            var track = root.AddComponent<FrozenKingdomTrack>();
            track.Build(config.trackLength, hw, 1, profile, null, cam);

            Transform actors = new GameObject("Actors").transform;
            SpawnArmy(actors, 12f);
            SpawnArmy(actors, 98f);
            SpawnArmy(actors, 203f);
            SpawnEnemies(actors, 116f, 18);
            foreach (GateChoiceRow row in config.choiceRows)
            {
                try
                {
                    var gate = new GameObject("Gate").AddComponent<GateRowController>();
                    gate.transform.SetParent(actors, false);
                    gate.Initialize(row, hw);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[LegionRush] Portal não montado na prévia: " + exception.Message);
                }
            }
            foreach (ParticleSystem ps in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                ps.Simulate(1.5f, true, true);

            float L = config.trackLength;
            Shot(cam, dir, "01_inicio", GameView(12f, 1f, 2f));
            Shot(cam, dir, "02_meio_portal", GameView(64f, 1f, 2f));
            Shot(cam, dir, "03_meio_exercito", GameView(98f, 1f, 2.4f));
            Shot(cam, dir, "04_reta_final", GameView(203f, 1f, 2.4f));
            Shot(cam, dir, "05_vitoria", GameView(L + 1f, 0.7f, 2.4f));
            Shot(cam, dir, "06_castelo_heroico", (new Vector3(0f, 2.6f, L + 6f), new Vector3(0f, 9f, L + 40f)));
            Shot(cam, dir, "07_lateral", (new Vector3(-26f, 22f, 70f), new Vector3(0f, 0f, 110f)));
            foreach (EnvGateDoors doors in UnityEngine.Object.FindObjectsByType<EnvGateDoors>(FindObjectsSortMode.None)) doors.SetOpenAmount(1f);
            Shot(cam, dir, "09_portao_aberto", (new Vector3(0f, 5f, L + 14f), new Vector3(0f, 6f, L + 40f)));
            Shot(cam, dir, "08_vista_geral", (new Vector3(0f, 26f, L - 70f), new Vector3(0f, 4f, L + 30f)));
            Debug.Log($"[LegionRush] Prévia do cenário salva em {dir}");
        }

        // Miniaturas de todos os prefabs do Reino Congelado (conferência visual do kit), uma PNG por prefab.
        // Batch: -executeMethod LegionRush.Environments.EditorTools.EnvPreviewCapture.CapturePrefabsBatch -captureDir <pasta>
        public static void CapturePrefabsBatch()
        {
            int code = 0;
            try
            {
                string outDir = GetArgument("-captureDir") ?? Path.GetFullPath("Builds/EnvPreview/Prefabs");
                Directory.CreateDirectory(outDir);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                var cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.68f, 0.85f);
                cam.fieldOfView = 35f;
                cam.farClipPlane = 600f;
                var lighting = new GameObject("Lighting").AddComponent<WorldLightingController>();
                lighting.Initialize(sun, cam, null);
                WorldLightingProfile profile = WorldLightingProfile.CreatePreset(WorldId.FrozenKingdom);
                lighting.Apply(profile);
                RenderSettings.fog = false;
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/LegionRush/Worlds/FrozenKingdom" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    GameObject instance = UnityEngine.Object.Instantiate(prefab);
                    var bounds = new Bounds(instance.transform.position, Vector3.one * 0.5f);
                    foreach (Renderer r in instance.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
                    float radius = bounds.extents.magnitude;
                    float distance = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
                    Vector3 dir = Quaternion.Euler(28f, 215f, 0f) * Vector3.forward;
                    cam.transform.SetPositionAndRotation(bounds.center - dir * distance, Quaternion.LookRotation(dir));
                    RenderPng(cam, Path.Combine(outDir, prefab.name), 320, 320);
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("[LegionRush] Miniaturas falharam: " + exception);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        private static void RenderPng(Camera cam, string pathNoExt, int width, int height)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            cam.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            cam.targetTexture = null;
            File.WriteAllBytes(pathNoExt + ".png", texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }

        // Mesma fórmula do ArmyCameraController (altura 10, distância 8,5, olha 7 m à frente).
        private static (Vector3, Vector3) GameView(float z, float zoom, float radius)
        {
            var position = new Vector3(0f, (10f + radius * 0.9f) * zoom, z - (8.5f + radius * 0.6f) * zoom);
            var focus = new Vector3(0f, 0f, z + 7f * zoom);
            return (position, focus);
        }

        private static void Shot(Camera cam, string dir, string name, (Vector3 position, Vector3 focus) view)
        {
            cam.transform.SetPositionAndRotation(view.position, Quaternion.LookRotation(view.focus - view.position));
            foreach (EnvSkyBackdrop sky in UnityEngine.Object.FindObjectsByType<EnvSkyBackdrop>(FindObjectsSortMode.None)) sky.Follow();
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            cam.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            cam.targetTexture = null;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }

        private static void SpawnArmy(Transform parent, float z)
        {
            GameObject commander = CharacterFactory.CreateCommander(parent);
            commander.transform.position = new Vector3(0f, 0f, z);
            var rng = new System.Random((int)z);
            Color tunic = new Color(0.2f, 0.42f, 1f);
            for (int i = 0; i < 28; i++)
            {
                float a = i * 2.39996f;
                float r = 0.75f * Mathf.Sqrt(i + 1);
                var unit = (UnitClass)(i % 6);
                GameObject soldier = CharacterFactory.CreateSoldier(unit, parent, tunic);
                soldier.transform.position = new Vector3(Mathf.Cos(a) * r, 0f, z - 1.2f + Mathf.Sin(a) * r * 0.8f);
                soldier.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 20f - 10f, 0f);
            }
        }

        private static void SpawnEnemies(Transform parent, float z, int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject enemy = CharacterFactory.CreateEnemy(parent, new Color(0.75f, 0.15f, 0.15f));
                enemy.transform.SetPositionAndRotation(new Vector3((i % 6 - 2.5f) * 1.1f, 0f, z + (i / 6) * 1.1f), Quaternion.Euler(0f, 180f, 0f));
            }
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
    }
}
