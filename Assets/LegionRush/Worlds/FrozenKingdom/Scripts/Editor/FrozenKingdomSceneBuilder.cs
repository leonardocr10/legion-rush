using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LegionRush.Environments.EditorTools
{
    // Monta a cena FrozenKingdom_01 com os prefabs do Reino Congelado, na mesma disposição do cenário do jogo
    // (FrozenKingdomTrack registra cada peça: prefab, posição, giro, escala e seção da referência).
    // A cena nasce como cópia da Main (Bootstrap + câmera), então é jogável; o LevelManager usa o mapa da
    // cena quando a fase bate (Reino Congelado, 220 m, meia-largura 5 m). Rodar de novo apaga só o que ele
    // mesmo criou (raízes "FrozenKingdom_World" e "InspectionCameras") e remonta.
    // Batch: -executeMethod LegionRush.Environments.EditorTools.FrozenKingdomSceneBuilder.BuildAllBatch
    public static class FrozenKingdomSceneBuilder
    {
        private const string Root = EnvPrefabBaker.Root;
        public const string ScenePath = Root + "/Scenes/FrozenKingdom_01.unity";
        private const string MainScenePath = "Assets/_Game/Scenes/Main.unity";
        private const string WorldRootName = "FrozenKingdom_World";
        private const string CamerasRootName = "InspectionCameras";
        private const string SceneModelDir = Root + "/Models/Scene";
        private const string DocsDir = "docs/reino_congelado";

        private static readonly string[] Groups = { "Terrain", "Tracks", "Structures", "Nature", "Props", "Castle", "Mountains", "Gameplay_Preview" };

        // Vistas de inspeção (nomes = seções da folha de referência).
        private static readonly (string name, Vector3 position, Vector3 target, string section)[] Views =
        {
            ("Cam_VistaGeral", new Vector3(0f, 34f, 40f), new Vector3(0f, 0f, 190f), "Vista geral do cenário"),
            ("Cam_CasteloFinal", new Vector3(0f, 6f, 224f), new Vector3(0f, 9f, 272f), "Castelo final (entrada)"),
            ("Cam_VistaFrontal", new Vector3(0f, 11.8f, 18.3f), new Vector3(0f, 0f, 37f), "Vista frontal (câmera do jogo)"),
            ("Cam_VistaLateral", new Vector3(-30f, 20f, 70f), new Vector3(0f, 0f, 115f), "Vista lateral"),
            ("Cam_VistaAerea", new Vector3(0f, 75f, 95f), new Vector3(0f, 0f, 125f), "Vista aérea"),
            ("Cam_VistaTraseira", new Vector3(0f, 6f, -16f), new Vector3(0f, 2f, 22f), "Vista traseira (spawn)"),
        };

        [MenuItem("Legion Rush/Reino Congelado/2. Montar cena FrozenKingdom_01")]
        public static void BuildAll()
        {
            if (EnvPrefabBaker.LoadPrefab("FK_Gate_Blue") == null || EnvPrefabBaker.LoadPrefab("PineTree_Snow") == null) EnvPrefabBaker.Bake();
            BuildScene();
        }

        public static void BuildAllBatch() => RunBatch(() => { EnvPrefabBaker.Bake(); BuildScene(); });

        public static void CaptureBatch() => RunBatch(() => Capture(GetArgument("-captureDir") ?? Path.GetFullPath("Builds/EnvPreview/scene")));

        public static void BuildWebGLBatch() => RunBatch(BuildWebGL);

        private static void RunBatch(Action action)
        {
            int code = 0;
            try { action(); }
            catch (Exception exception)
            {
                Debug.LogError("[LegionRush] Reino Congelado: " + exception);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        public static void BuildScene()
        {
            EnvPrefabBaker.EnsureFolder(Root + "/Scenes");
            EnvPrefabBaker.EnsureFolder(SceneModelDir);
            ArchiveCurrentScene();
            Scene scene = OpenOrCreateScene();
            foreach (GameObject go in scene.GetRootGameObjects())
                if (go.name == WorldRootName || go.name == CamerasRootName) UnityEngine.Object.DestroyImmediate(go);

            MaterialLibrary.PreferredToonShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Game/Shaders/LegionToon.shader");
            MaterialLibrary.PreferredGlowShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Game/Shaders/LegionGlow.shader");
            LevelConfig config = LevelConfig.CreateDefault();
            config.Sanitize();
            WorldLightingProfile lighting = AssetDatabase.LoadAssetAtPath<WorldLightingProfile>(Root + "/Lighting/WorldLighting_FrozenKingdom.asset")
                                            ?? WorldLightingProfile.CreatePreset(WorldId.FrozenKingdom);

            // 1) Mesma lógica do jogo, registrando as peças.
            var placements = new List<EnvPlacement>(2048);
            var temp = new GameObject("TempTrack");
            var track = temp.AddComponent<FrozenKingdomTrack>();
            track.Recorder = placements;
            track.Build(config.trackLength, config.trackHalfWidth, config.levelNumber, lighting, null, null,
                AssetDatabase.LoadAssetAtPath<FrozenScatterProfile>(Root + "/Configurations/Resources/FrozenScatter_Default.asset"));
            EnvTerrain.Shape shape = track.Shape;
            float castleZ = track.CastleZ;
            UnityEngine.Object.DestroyImmediate(temp);

            var world = new GameObject(WorldRootName);
            var groups = new Dictionary<string, Transform>();
            foreach (string g in Groups)
            {
                var t = new GameObject(g).transform;
                t.SetParent(world.transform, false);
                groups[g] = t;
            }

            // 2) Terreno de neve (malhas salvas como asset, uma por trecho de 30 m).
            BuildTerrain(groups["Terrain"], shape, config.trackHalfWidth, castleZ);

            // 3) Pista modular contínua.
            AddTrackPlacements(placements, config, castleZ);

            // 4) Instancia as peças.
            int missing = 0;
            foreach (EnvPlacement p in placements)
            {
                GameObject prefab = EnvPrefabBaker.LoadPrefab(p.Prefab);
                if (prefab == null)
                {
                    missing++;
                    continue;
                }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, groups.TryGetValue(p.Group, out Transform parent) ? parent : world.transform);
                instance.transform.SetLocalPositionAndRotation(p.Position, Quaternion.Euler(0f, p.Yaw, 0f));
                instance.transform.localScale = p.Scale;
            }

            // 5) Marcadores de jogabilidade (só no editor: o jogo cria os reais).
            var preview = new List<EnvPlacement>();
            BuildGameplayPreview(groups["Gameplay_Preview"], config, preview);

            // 6) Raiz do mapa: o LevelManager encontra e usa este cenário.
            var layout = world.AddComponent<FrozenKingdomSceneLayout>();
            layout.trackLength = config.trackLength;
            layout.trackHalfWidth = config.trackHalfWidth;
            layout.castleZ = castleZ;
            layout.castlePlateau = FrozenKingdomTrack.CastlePlateau;
            layout.previewGameplay = groups["Gameplay_Preview"].gameObject;
            layout.detailGroups = new[] { groups["Nature"], groups["Props"] };
            layout.castleDoors = groups["Castle"].GetComponentInChildren<EnvGateDoors>(true);

            // 7) Câmeras de inspeção e luz da cena (o jogo reaplica a luz em runtime).
            BuildInspectionCameras();
            ApplySceneLighting(lighting);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            WriteCoordinateTable(placements, preview, config, castleZ);
            Debug.Log($"[LegionRush] Cena {ScenePath}: {placements.Count} peças ({missing} sem prefab), {preview.Count} marcadores de jogo.");
        }

        // Guarda a versão anterior da cena (Scenes/Versions/FrozenKingdom_01_vN.unity) antes de remontar.
        private static void ArchiveCurrentScene()
        {
            if (!File.Exists(ScenePath)) return;
            string dir = Root + "/Scenes/Versions";
            EnvPrefabBaker.EnsureFolder(dir);
            int n = 1;
            while (File.Exists($"{dir}/FrozenKingdom_01_v{n}.unity")) n++;
            AssetDatabase.CopyAsset(ScenePath, $"{dir}/FrozenKingdom_01_v{n}.unity");
        }

        private static Scene OpenOrCreateScene()
        {
            if (File.Exists(ScenePath)) return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            // Cópia da Main: mantém Bootstrap, Sol e câmera, então a cena roda o jogo inteiro.
            Scene main = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(main, ScenePath, true);
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void BuildTerrain(Transform parent, EnvTerrain.Shape shape, float halfWidth, float castleZ)
        {
            float endZ = castleZ + 8f;
            int index = 0;
            for (float z0 = FrozenKingdomTrack.StartZ; z0 < endZ; z0 += FrozenKingdomTrack.ChunkLength, index++)
            {
                float z1 = Mathf.Min(z0 + FrozenKingdomTrack.ChunkLength, endZ);
                var chunk = new EnvChunk();
                EnvTerrain.SnowBanks(chunk, shape, z0, z1, halfWidth + 38f, 2.5f);
                // Faixa de chão sob a pista entre os módulos e a escadaria do castelo.
                var go = new GameObject($"Terrain_{index:00}");
                go.transform.SetParent(parent, false);
                chunk.Build(go.transform, $"FK01_Terrain_{index:00}", EnvShadowMode.None);
                foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>())
                {
                    MeshFilter f = r.GetComponent<MeshFilter>();
                    string path = $"{SceneModelDir}/{f.sharedMesh.name}.asset";
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.CreateAsset(f.sharedMesh, path);
                    r.sharedMaterial = PersistentMaterial(r.sharedMaterial);
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                }
            }
        }

        private static Material PersistentMaterial(Material runtime)
        {
            if (runtime == null || AssetDatabase.Contains(runtime)) return runtime;
            Material saved = AssetDatabase.LoadAssetAtPath<Material>($"{Root}/Materials/{runtime.name}.mat");
            return saved != null ? saved : runtime;
        }

        // Módulos de 10 m alinhados para terminar exatamente na linha de chegada (Track_End em L − 5).
        private static void AddTrackPlacements(List<EnvPlacement> list, LevelConfig config, float castleZ)
        {
            float L = config.trackLength;
            const string Sec = "Terreno e pistas (modulares)";
            float end = L - 5f;
            float start = end - Mathf.Floor((end + 35f) / 10f) * 10f; // primeiro módulo antes do spawn (≈ −35 m)
            for (float z0 = start; z0 < end - 0.01f; z0 += 10f)
            {
                bool startModule = Mathf.Abs(z0 + 5f) < 5f; // o que contém o spawn (z ≈ 0)
                bool enemy = false;
                foreach (EnemyWave w in config.waves) if (w.z >= z0 && w.z < z0 + 10f) enemy = true;
                string prefab = startModule ? "Track_Start" : enemy ? "Track_Enemy" : "Track_Bordered";
                list.Add(new EnvPlacement(prefab, new Vector3(0f, 0f, z0), 0f, Vector3.one, "Tracks",
                    startModule ? Sec + ": início com faixa de largada (vista traseira / spawn)" : enemy ? Sec + ": trecho de inimigos" : Sec + ": piso com borda"));
            }
            list.Add(new EnvPlacement("Track_End", new Vector3(0f, 0f, end), 0f, Vector3.one, "Tracks", Sec + ": chegada com arco de torres"));
            float stairs = castleZ - 0.55f * Mathf.Max(4, Mathf.RoundToInt(FrozenKingdomTrack.CastlePlateau / 0.32f));
            for (float z0 = end + 10f; z0 < stairs - 0.5f; z0 += 10f)
            {
                float len = Mathf.Min(10f, stairs - z0);
                list.Add(new EnvPlacement("Track_Bordered", new Vector3(0f, 0f, z0), 0f, new Vector3(1f, 1f, len / 10f), "Tracks",
                    "Castelo final (entrada): pavimento até a escadaria"));
            }
        }

        private static void BuildGameplayPreview(Transform parent, LevelConfig config, List<EnvPlacement> preview)
        {
            float hw = config.trackHalfWidth;
            Marker(parent, "PlayerSpawnPoint", new Vector3(0f, 0f, 0f), 0f, Vector3.one, preview, 0, "Vista traseira (spawn): formação do exército azul");
            foreach (GateChoiceRow row in config.choiceRows)
            {
                int count = Mathf.Max(1, row.options.Count);
                Marker(parent, "GateRowPoint", new Vector3(0f, 0f, row.z), 0f, Vector3.one, preview, count, "Portais de melhoria (escolha): fileira");
                float lane = hw * 2f / count;
                for (int i = 0; i < count; i++)
                {
                    GateOption option = row.options[i];
                    string prefab = "FK_Gate_" + ColorName(option.category);
                    var pos = new Vector3(-hw + lane * (i + 0.5f), 0f, row.z);
                    var scale = new Vector3(lane / 5f, 1f, 1f);
                    Instance(parent, prefab, pos, 0f, scale);
                    preview.Add(new EnvPlacement(prefab, pos, 0f, scale, "Gameplay_Preview", $"Portais de melhoria: {option.title} {option.subtitle} ({option.category})"));
                }
            }
            foreach (EnemyWave wave in config.waves)
                Marker(parent, "EnemySpawnPoint", new Vector3(0f, 0f, wave.z), 180f, Vector3.one, preview, wave.count, "Unidades (inimigo): onda de soldados vermelhos");
            float bossZ = Mathf.Max(20f, config.trackLength - config.boss.distanceFromEnd);
            Marker(parent, "BossSpawnPoint", new Vector3(0f, 0f, bossZ), 180f, Vector3.one, preview, 1, "Castelo final: objetivo derrotar o chefão");
        }

        private static string ColorName(GateCategory category) => category switch
        {
            GateCategory.Multiply => "Green",
            GateCategory.Resource => "Yellow",
            GateCategory.Upgrade => "Yellow",
            GateCategory.Special => "Purple",
            GateCategory.Negative => "Purple",
            _ => "Blue"
        };

        private static void Marker(Transform parent, string prefab, Vector3 position, float yaw, Vector3 scale, List<EnvPlacement> preview, int count, string section)
        {
            GameObject go = Instance(parent, prefab, position, yaw, scale);
            if (go != null && go.TryGetComponent(out EnvSpawnPoint point)) point.count = count;
            if (go != null && count > 0) go.name = $"{prefab}_z{position.z:0}";
            preview.Add(new EnvPlacement(prefab, position, yaw, scale, "Gameplay_Preview", section + (count > 0 ? $" ({count})" : string.Empty)));
        }

        private static GameObject Instance(Transform parent, string prefabName, Vector3 position, float yaw, Vector3 scale)
        {
            GameObject prefab = EnvPrefabBaker.LoadPrefab(prefabName);
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = scale;
            return go;
        }

        private static void BuildInspectionCameras()
        {
            var root = new GameObject(CamerasRootName).transform;
            foreach (var view in Views)
            {
                var go = new GameObject(view.name);
                go.transform.SetParent(root, false);
                go.transform.SetPositionAndRotation(view.position, Quaternion.LookRotation(view.target - view.position));
                var cam = go.AddComponent<Camera>();
                cam.fieldOfView = 52f;
                cam.farClipPlane = 400f;
                cam.enabled = false;
                go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            }
        }

        private static void ApplySceneLighting(WorldLightingProfile profile)
        {
            Light sun = null;
            foreach (Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) sun = l;
            if (sun != null)
            {
                sun.color = profile.sunColor;
                sun.intensity = profile.sunIntensity;
                sun.transform.rotation = Quaternion.Euler(profile.sunPitch, profile.sunYaw, 0f);
                sun.shadowStrength = profile.shadowStrength;
                RenderSettings.sun = sun;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = profile.ambientSky;
            RenderSettings.ambientEquatorColor = profile.ambientEquator;
            RenderSettings.ambientGroundColor = profile.ambientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = profile.fogColor;
            RenderSettings.fogStartDistance = profile.fogStart;
            RenderSettings.fogEndDistance = profile.fogEnd;
        }

        // Tabela de coordenadas: CSV completo + resumo em Markdown (peças principais uma a uma, vegetação por tipo).
        private static void WriteCoordinateTable(List<EnvPlacement> placements, List<EnvPlacement> preview, LevelConfig config, float castleZ)
        {
            Directory.CreateDirectory(DocsDir);
            var ci = CultureInfo.InvariantCulture;
            var csv = new StringBuilder("objeto;tipo;x;y;z;giro_y;escala_x;escala_y;escala_z;prefab;secao_referencia\n");
            var all = new List<EnvPlacement>(placements);
            all.AddRange(preview);
            var counters = new Dictionary<string, int>();
            foreach (EnvPlacement p in all)
            {
                counters.TryGetValue(p.Prefab, out int n);
                counters[p.Prefab] = n + 1;
                csv.AppendLine(string.Join(";", $"{p.Prefab}_{n + 1:000}", p.Group, F(p.Position.x), F(p.Position.y), F(p.Position.z), F(p.Yaw),
                    F(p.Scale.x), F(p.Scale.y), F(p.Scale.z), p.Prefab, p.Section));
            }
            File.WriteAllText(Path.Combine(DocsDir, "coordenadas.csv"), csv.ToString(), new UTF8Encoding(true));

            var md = new StringBuilder();
            md.AppendLine("# Reino Congelado — mapa da cena FrozenKingdom_01");
            md.AppendLine();
            md.AppendLine("Gerado por *Legion Rush › Reino Congelado › 2. Montar cena* (`FrozenKingdomSceneBuilder`). Não editar à mão: rodar o construtor de novo.");
            md.AppendLine();
            md.AppendLine("## Sistema de coordenadas");
            md.AppendLine();
            md.AppendLine("- **X** = largura da pista (0 no eixo; pista de −" + F(config.trackHalfWidth) + " a +" + F(config.trackHalfWidth) + " m). **Y** = altura. **Z** = direção de avanço.");
            md.AppendLine("- **Origem** = ponto inicial do comandante (`PlayerSpawnPoint`). Linha de chegada em z = " + F(config.trackLength) + ", castelo em z = " + F(castleZ) + ".");
            md.AppendLine("- As medidas vêm do sistema do jogo (`LevelConfig` da fase 1: pista de " + F(config.trackLength) + " m × " + F(config.trackHalfWidth * 2f) + " m). " +
                          "A folha de referência não traz cotas: distâncias de decoração, alturas de torres, tamanho do castelo e das montanhas são **estimativas** feitas para bater com as vistas da folha, não medidas tiradas dela.");
            md.AppendLine();
            md.AppendLine("## Sequência do percurso (fase 1)");
            md.AppendLine();
            md.AppendLine("| z (m) | O quê | Seção da referência |");
            md.AppendLine("|---|---|---|");
            var seq = new List<(float z, string what, string sec)> { (0f, "Spawn do exército azul (formação)", "Vista traseira (spawn)") };
            foreach (GateChoiceRow row in config.choiceRows)
            {
                var names = new List<string>();
                foreach (GateOption o in row.options) names.Add($"{o.title} {o.subtitle}".Trim() + $" [{ColorName(o.category)}]");
                seq.Add((row.z, $"Fileira de {row.options.Count} portais: " + string.Join(" · ", names), "Portais de melhoria"));
            }
            foreach (EnemyWave w in config.waves) seq.Add((w.z, $"Onda inimiga: {w.count} soldados vermelhos", "Unidades (inimigo)"));
            seq.Add((config.trackLength - config.boss.distanceFromEnd, "Chefão", "Castelo final: derrotar o boss"));
            seq.Add((config.trackLength, "Linha de chegada (arco de torres)", "Vista geral"));
            seq.Add((castleZ, "Castelo: escadaria, portão azul, torres", "Castelo final (entrada)"));
            seq.Sort((a, b) => a.z.CompareTo(b.z));
            foreach (var s in seq) md.AppendLine($"| {F(s.z)} | {s.what} | {s.sec} |");
            md.AppendLine();
            md.AppendLine("## Peças principais (uma a uma)");
            md.AppendLine();
            md.AppendLine("| Objeto | Tipo | X | Y | Z | Giro | Escala | Prefab | Seção da referência |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|");
            var detail = new HashSet<string> { "Nature", "Mountains" };
            var mdCounters = new Dictionary<string, int>();
            foreach (EnvPlacement p in all)
            {
                mdCounters.TryGetValue(p.Prefab, out int n);
                mdCounters[p.Prefab] = n + 1;
                if (detail.Contains(p.Group)) continue;
                md.AppendLine($"| {p.Prefab}_{n + 1:000} | {p.Group} | {F(p.Position.x)} | {F(p.Position.y)} | {F(p.Position.z)} | {F(p.Yaw)} | {F(p.Scale.x)}×{F(p.Scale.y)}×{F(p.Scale.z)} | {p.Prefab} | {p.Section} |");
            }
            md.AppendLine();
            md.AppendLine("## Vegetação, rochas e montanhas (resumo; posições completas no CSV)");
            md.AppendLine();
            md.AppendLine("| Prefab | Quantidade | Faixa em X (m) | Faixa em Z (m) | Seção |");
            md.AppendLine("|---|---|---|---|---|");
            var summary = new Dictionary<string, (int n, float x0, float x1, float z0, float z1, string sec)>();
            foreach (EnvPlacement p in all)
            {
                if (!detail.Contains(p.Group)) continue;
                float ax = Mathf.Abs(p.Position.x);
                if (summary.TryGetValue(p.Prefab, out var s))
                    summary[p.Prefab] = (s.n + 1, Mathf.Min(s.x0, ax), Mathf.Max(s.x1, ax), Mathf.Min(s.z0, p.Position.z), Mathf.Max(s.z1, p.Position.z), s.sec);
                else summary[p.Prefab] = (1, ax, ax, p.Position.z, p.Position.z, p.Section);
            }
            foreach (var kv in summary)
                md.AppendLine($"| {kv.Key} | {kv.Value.n} | ±{F(kv.Value.x0)} a ±{F(kv.Value.x1)} | {F(kv.Value.z0)} a {F(kv.Value.z1)} | {kv.Value.sec} |");
            md.AppendLine();
            md.AppendLine("## Câmeras de inspeção");
            md.AppendLine();
            md.AppendLine("| Câmera | Posição | Olha para | Seção |");
            md.AppendLine("|---|---|---|---|");
            foreach (var v in Views) md.AppendLine($"| {v.name} | ({F(v.position.x)}, {F(v.position.y)}, {F(v.position.z)}) | ({F(v.target.x)}, {F(v.target.y)}, {F(v.target.z)}) | {v.section} |");
            File.WriteAllText(Path.Combine(DocsDir, "MAPA.md"), md.ToString(), new UTF8Encoding(false));
        }

        private static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        // ---------- Capturas das vistas de inspeção ----------

        public static void Capture(string dir)
        {
            Directory.CreateDirectory(dir);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MaterialLibrary.PreferredToonShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Game/Shaders/LegionToon.shader");
            MaterialLibrary.PreferredGlowShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Game/Shaders/LegionGlow.shader");
            WorldLightingProfile profile = AssetDatabase.LoadAssetAtPath<WorldLightingProfile>(Root + "/Lighting/WorldLighting_FrozenKingdom.asset")
                                           ?? WorldLightingProfile.CreatePreset(WorldId.FrozenKingdom);
            // O Bootstrap da cena não roda no editor: tira os objetos de jogo do caminho e liga só o cenário.
            foreach (GameObject go in scene.GetRootGameObjects())
                if (go.name != WorldRootName && go.name != CamerasRootName && go.GetComponent<Light>() == null) go.SetActive(false);

            var camGo = new GameObject("CaptureCamera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 140f;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            Light sun = null;
            foreach (Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) sun = l;
            var lighting = new GameObject("Lighting").AddComponent<WorldLightingController>();
            lighting.Initialize(sun, cam, null);
            lighting.Apply(profile);

            var layout = UnityEngine.Object.FindAnyObjectByType<FrozenKingdomSceneLayout>();
            layout.Activate(profile, null, cam);
            layout.previewGameplay.SetActive(true); // portais visíveis nas capturas
            MethodInfo start = typeof(EnvFlameSpawner).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (EnvFlameSpawner spawner in UnityEngine.Object.FindObjectsByType<EnvFlameSpawner>(FindObjectsSortMode.None)) start?.Invoke(spawner, null);

            // Exército e inimigos provisórios para a leitura de escala (os do jogo, criados por código).
            Transform actors = new GameObject("Actors").transform;
            SpawnArmy(actors, 4f);
            SpawnEnemies(actors, 55f, 12);
            foreach (ParticleSystem ps in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)) ps.Simulate(1.5f, true, true);

            const int W = 960, H = 540;
            foreach (var view in Views)
            {
                bool portrait = view.name == "Cam_VistaFrontal" || view.name == "Cam_VistaTraseira";
                int w = portrait ? 540 : W, h = portrait ? 960 : H;
                cam.fieldOfView = portrait ? 62f : 48f;
                cam.farClipPlane = view.name == "Cam_VistaFrontal" || view.name == "Cam_VistaTraseira" ? 140f : 400f;
                // Vistas panorâmicas mostram o mapa inteiro (como a folha); as de jogo usam a neblina real.
                RenderSettings.fogStartDistance = portrait ? profile.fogStart : 160f;
                RenderSettings.fogEndDistance = portrait ? profile.fogEnd : 420f;
                cam.transform.SetPositionAndRotation(view.position, Quaternion.LookRotation(view.target - view.position));
                foreach (EnvSkyBackdrop sky in UnityEngine.Object.FindObjectsByType<EnvSkyBackdrop>(FindObjectsSortMode.None)) sky.Follow();
                Render(cam, Path.Combine(dir, view.name + ".png"), w, h);
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); // descarta as mudanças da captura
            Debug.Log("[LegionRush] Capturas da cena em " + dir);
        }

        private static void Render(Camera cam, string path, int width, int height)
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
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }

        private static void SpawnArmy(Transform parent, float z)
        {
            GameObject commander = CharacterFactory.CreateCommander(parent);
            commander.transform.position = new Vector3(0f, 0f, z);
            Color tunic = new Color(0.2f, 0.42f, 1f);
            for (int i = 0; i < 30; i++)
            {
                float a = i * 2.39996f;
                float r = 0.75f * Mathf.Sqrt(i + 1);
                GameObject soldier = CharacterFactory.CreateSoldier((UnitClass)(i % 6), parent, tunic);
                soldier.transform.position = new Vector3(Mathf.Cos(a) * r, 0f, z - 1.2f + Mathf.Sin(a) * r * 0.8f);
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

        // ---------- Build WebGL só desta cena (validação no navegador) ----------

        public static void BuildWebGL()
        {
            LegionRush.EditorTools.ProjectSetup.ConfigureProject();
            string output = GetArgument("-customBuildPath") ?? "Builds/WebGL/FrozenKingdom01";
            Directory.CreateDirectory(output);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;
            BuildSummary summary = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None
            }).summary;
            PlayerSettings.WebGL.dataCaching = true;
            if (summary.result != BuildResult.Succeeded) throw new Exception($"Build WebGL falhou: {summary.result}, {summary.totalErrors} erros.");
            Debug.Log($"[LegionRush] WebGL da cena FrozenKingdom_01 em {output} ({summary.totalSize / (1024f * 1024f):0.0} MB).");
        }

        private static string GetArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }
    }
}
