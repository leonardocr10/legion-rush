using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using LegionRush.Environments;

namespace LegionRush.Characters.EditorTools
{
    // Capturas reais dos prefabs no Unity (URP + LegionToon) para comparar com as folhas de referência:
    // frente, lado, costas, perspectiva, quadros de cada animação e uma cena de jogo com o exército.
    // Batch: -executeMethod LegionRush.Characters.EditorTools.CharacterPreviewCapture.CaptureBatch -captureDir <pasta> [-only Warrior]
    public static class CharacterPreviewCapture
    {
        [MenuItem("Legion Rush/Personagens/Capturar prévias")]
        public static void CaptureMenu()
        {
            string dir = Path.GetFullPath("Builds/CharacterUnity");
            Capture(dir, null);
            EditorUtility.RevealInFinder(dir);
        }

        public static void CaptureBatch()
        {
            int code = 0;
            try
            {
                Capture(Arg("-captureDir") ?? Path.GetFullPath("Builds/CharacterUnity"), Arg("-only"));
            }
            catch (Exception exception)
            {
                Debug.LogError("[LegionRush] Captura de personagens falhou: " + exception);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        private static void Capture(string dir, string only)
        {
            Directory.CreateDirectory(dir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MaterialLibrary.PreferredToonShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Game/Shaders/LegionToon.shader");
            MaterialLibrary.PreferredGlowShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Game/Shaders/LegionGlow.shader");
            Camera cam = SetupStudio(out Light sun);

            foreach (var entry in CharacterPrefabBuilder.Roster)
            {
                if (only != null && !string.Equals(only, entry.model, StringComparison.OrdinalIgnoreCase)) continue;
                string folder = $"{CharacterPrefabBuilder.Root}/{(entry.enemy ? "Enemies" : "Allies")}/{entry.model}";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/Prefabs/{entry.prefab}.prefab");
                if (prefab == null) continue;
                string outDir = Path.Combine(dir, entry.model);
                Directory.CreateDirectory(outDir);
                CaptureTurnaround(cam, prefab, outDir);
                CaptureAnimations(cam, prefab, outDir, folder);
            }
            CaptureGameplay(dir, only);
            Debug.Log("[LegionRush] Capturas de personagens em " + dir);
        }

        private static Camera SetupStudio(out Light sun)
        {
            sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.intensity = 1.25f;
            sun.transform.rotation = Quaternion.Euler(42f, 150f, 0f);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.15f, 0.32f);
            cam.orthographic = true;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 50f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.48f, 0.58f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.24f, 0.28f);
            RenderSettings.fog = false;
            return cam;
        }

        private static GameObject Spawn(GameObject prefab)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = Vector3.zero;
            return go;
        }

        private static void Sample(GameObject go, string clipName, float normalizedTime)
        {
            Animator animator = go.GetComponentInChildren<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null) return;
            AnimationClip clip = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name == clipName);
            if (clip == null) return;
            clip.SampleAnimation(animator.gameObject, clip.length * normalizedTime);
        }

        private static void CaptureTurnaround(Camera cam, GameObject prefab, string outDir)
        {
            GameObject go = Spawn(prefab);
            Sample(go, "Idle", 0f);
            Bounds bounds = Bounds(go);
            float h = Mathf.Max(bounds.size.y, bounds.size.x) * 1.12f;
            cam.orthographic = true;
            cam.orthographicSize = h * 0.5f;
            var shots = new List<Texture2D>();
            foreach (float yaw in new[] { 0f, 90f, 180f })
            {
                // yaw 0: câmera na frente do personagem (+Z), olhando para trás
                Vector3 dir = Quaternion.Euler(4f, 180f + yaw, 0f) * Vector3.forward;
                cam.transform.SetPositionAndRotation(bounds.center - dir * 10f, Quaternion.LookRotation(dir));
                shots.Add(Render(cam, 420, 560));
            }
            cam.orthographic = false;
            cam.fieldOfView = 30f;
            Vector3 pdir = Quaternion.Euler(18f, 180f + 35f, 0f) * Vector3.forward;
            float dist = h / (2f * Mathf.Tan(15f * Mathf.Deg2Rad)) * 1.05f;
            cam.transform.SetPositionAndRotation(bounds.center - pdir * dist, Quaternion.LookRotation(pdir));
            shots.Add(Render(cam, 420, 560));
            Save(Combine(shots), Path.Combine(outDir, "turnaround.png"));
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static void CaptureAnimations(Camera cam, GameObject prefab, string outDir, string folder)
        {
            GameObject go = Spawn(prefab);
            Animator animator = go.GetComponentInChildren<Animator>();
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
            Sample(go, "Idle", 0f);
            Bounds bounds = Bounds(go);
            float h = bounds.size.y * 1.5f;
            cam.orthographic = true;
            cam.orthographicSize = h * 0.5f;
            Vector3 dir = Quaternion.Euler(10f, 180f + 35f, 0f) * Vector3.forward;
            var rows = new List<Texture2D>();
            foreach (AnimationClip clip in clips)
            {
                var frames = new List<Texture2D>();
                for (int i = 0; i < 6; i++)
                {
                    clip.SampleAnimation(animator.gameObject, clip.length * i / 5f);
                    cam.transform.SetPositionAndRotation(bounds.center + Vector3.up * 0.1f - dir * 10f, Quaternion.LookRotation(dir));
                    frames.Add(Render(cam, 170, 210));
                }
                rows.Add(Combine(frames));
            }
            Save(Stack(rows), Path.Combine(outDir, "animations.png"));
            File.WriteAllText(Path.Combine(outDir, "animations.txt"), string.Join("\n", clips.Select(c => $"{c.name} {c.length:0.00}s loop={c.isLooping}")));
            UnityEngine.Object.DestroyImmediate(go);
        }

        // Cena de jogo: chão do Reino Congelado, câmera do jogo, exército com os prefabs da biblioteca.
        private static void CaptureGameplay(string dir, string only)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 140f;
            cam.fieldOfView = 2f * Mathf.Atan(Mathf.Tan(23f * Mathf.Deg2Rad) / (540f / 960f)) * Mathf.Rad2Deg;
            var lighting = new GameObject("Lighting").AddComponent<WorldLightingController>();
            lighting.Initialize(sun, cam, null);
            WorldLightingProfile profile = WorldLightingProfile.CreatePreset(WorldId.FrozenKingdom);
            lighting.Apply(profile);
            LevelConfig config = LevelConfig.CreateDefault();
            config.Sanitize();
            var track = new GameObject("Track").AddComponent<FrozenKingdomTrack>();
            track.Build(config.trackLength, config.trackHalfWidth, 1, profile, null, cam);

            CharacterFactory.Library = AssetDatabase.LoadAssetAtPath<CharacterVisualLibrary>(CharacterPrefabBuilder.LibraryPath);
            Transform actors = new GameObject("Actors").transform;
            float z = 60f;
            GameObject commander = CharacterFactory.CreateCommander(actors);
            commander.transform.position = new Vector3(0f, 0f, z);
            var rng = new System.Random(7);
            for (int i = 0; i < 36; i++)
            {
                float a = i * 2.39996f;
                float r = 0.72f * Mathf.Sqrt(i + 1);
                var unit = (UnitClass)(i % 6);
                GameObject soldier = CharacterFactory.CreateSoldier(unit, actors, new Color(0.2f, 0.42f, 1f));
                soldier.transform.position = new Vector3(Mathf.Cos(a) * r, 0f, z - 1.2f + Mathf.Sin(a) * r * 0.8f);
                soldier.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 20f - 10f, 0f);
            }
            for (int i = 0; i < 18; i++)
            {
                GameObject enemy = CharacterFactory.CreateEnemy(actors, new Color(0.75f, 0.15f, 0.15f), (EnemyKind)(i % EnemyKinds.Count));
                enemy.transform.SetPositionAndRotation(new Vector3((i % 6 - 2.5f) * 1.1f, 0f, z + 9f + (i / 6) * 1.1f), Quaternion.Euler(0f, 180f, 0f));
            }
            foreach (Animator animator in actors.GetComponentsInChildren<Animator>())
            {
                AnimationClip run = animator.runtimeAnimatorController != null
                    ? animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name == "Run") : null;
                if (run != null) run.SampleAnimation(animator.gameObject, run.length * (float)rng.NextDouble());
            }
            foreach (EnvSkyBackdrop sky in UnityEngine.Object.FindObjectsByType<EnvSkyBackdrop>(FindObjectsSortMode.None)) sky.Follow();
            cam.transform.SetPositionAndRotation(new Vector3(0f, 12.2f, z - 9.9f), Quaternion.LookRotation(new Vector3(0f, 0f, z + 7f) - new Vector3(0f, 12.2f, z - 9.9f)));
            Save(Render(cam, 540, 960), Path.Combine(dir, "gameplay.png"));
            cam.transform.SetPositionAndRotation(new Vector3(2.5f, 3.2f, z - 6f), Quaternion.LookRotation(new Vector3(0f, 0.8f, z + 1f) - new Vector3(2.5f, 3.2f, z - 6f)));
            Save(Render(cam, 960, 540), Path.Combine(dir, "gameplay_close.png"));
        }

        // ---------- util ----------

        private static Bounds Bounds(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>().Where(r => !r.name.EndsWith("_LOD1") && !r.name.EndsWith("_LOD2")).ToArray();
            var b = new Bounds(go.transform.position + Vector3.up * 0.7f, Vector3.one * 0.2f);
            foreach (Renderer r in rs)
            {
                if (r is SkinnedMeshRenderer smr)
                {
                    var baked = new Mesh();
                    smr.BakeMesh(baked);
                    foreach (Vector3 v in baked.vertices) b.Encapsulate(smr.transform.TransformPoint(v));
                    UnityEngine.Object.DestroyImmediate(baked);
                }
                else b.Encapsulate(r.bounds);
            }
            return b;
        }

        private static Texture2D Render(Camera cam, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            cam.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var texture = new Texture2D(w, h, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            cam.targetTexture = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            return texture;
        }

        private static Texture2D Combine(List<Texture2D> parts)
        {
            int w = parts.Sum(p => p.width), h = parts.Max(p => p.height);
            var o = new Texture2D(w, h, TextureFormat.RGB24, false);
            int x = 0;
            foreach (Texture2D p in parts)
            {
                o.SetPixels(x, 0, p.width, p.height, p.GetPixels());
                x += p.width;
                UnityEngine.Object.DestroyImmediate(p);
            }
            o.Apply();
            return o;
        }

        private static Texture2D Stack(List<Texture2D> rows)
        {
            if (rows.Count == 0) return new Texture2D(4, 4);
            int w = rows.Max(p => p.width), h = rows.Sum(p => p.height);
            var o = new Texture2D(w, h, TextureFormat.RGB24, false);
            int y = h;
            foreach (Texture2D p in rows)
            {
                y -= p.height;
                o.SetPixels(0, y, p.width, p.height, p.GetPixels());
                UnityEngine.Object.DestroyImmediate(p);
            }
            o.Apply();
            return o;
        }

        private static void Save(Texture2D t, string path)
        {
            File.WriteAllBytes(path, t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
        }

        private static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }
    }
}
