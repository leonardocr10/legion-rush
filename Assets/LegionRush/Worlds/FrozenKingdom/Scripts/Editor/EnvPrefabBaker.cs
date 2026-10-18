using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LegionRush.Environments.EditorTools
{
    // Gera os prefabs do Reino Congelado em Assets/LegionRush/Worlds/FrozenKingdom/<categoria>/ (malhas em
    // Models, materiais em Materials, texturas em Textures). O jogo usa as mesmas funções do kit para montar o
    // cenário procedural; a cena FrozenKingdom_01 é montada com estes prefabs (FrozenKingdomSceneBuilder).
    // Pode rodar de novo à vontade: sobrescreve só os assets que ele mesmo gera.
    // Batch: Unity.exe -batchmode -projectPath . -executeMethod LegionRush.Environments.EditorTools.EnvPrefabBaker.BakeBatch
    public static class EnvPrefabBaker
    {
        public const string Root = "Assets/LegionRush/Worlds/FrozenKingdom";
        private const string ModelDir = Root + "/Models";
        private const string MaterialDir = Root + "/Materials";
        private const string TextureDir = Root + "/Textures";
        private const string ConfigDir = Root + "/Configurations/Resources";
        private const float HalfWidth = 5f; // LevelConfig.trackHalfWidth padrão
        private const float L = EnvTrackModules.ModuleLength;

        private static readonly Dictionary<EnvMaterial, Material> SavedMaterials = new Dictionary<EnvMaterial, Material>();
        private static readonly Dictionary<Material, Material> SavedOther = new Dictionary<Material, Material>();
        private static readonly Dictionary<string, string> PrefabPaths = new Dictionary<string, string>();

        private delegate Vector3[] Builder(EnvChunk chunk, System.Random rng, Transform root);

        private enum Extra { None, Lod, TrackSlab, TrackBordered, Ramp, Stairs, Platform, PlatformElevated, Crossroad, Curve, Boss, Border }

        private struct Piece
        {
            public string Folder, Name;
            public Builder Build;
            public Extra Extra;
            public Piece(string folder, string name, Extra extra, Builder build) { Folder = folder; Name = name; Extra = extra; Build = build; }
        }

        private static Builder Do(Action<EnvChunk, System.Random> a) => (c, r, t) => { a(c, r); return null; };

        private static List<Piece> Pieces() => new List<Piece>
        {
            // Pistas: módulos de 10 m, largura do jogo (2 × 5 m), pivô no início do módulo, eixo da pista em X = 0.
            new Piece("Tracks", "Track_Straight", Extra.TrackSlab, Do((c, r) => EnvTrackModules.Straight(c, HalfWidth, L, r, false))),
            new Piece("Tracks", "Track_Bordered", Extra.TrackBordered, Do((c, r) => EnvTrackModules.Straight(c, HalfWidth, L, r))),
            new Piece("Tracks", "Track_Start", Extra.TrackBordered, Do((c, r) => EnvTrackModules.Start(c, HalfWidth, r))),
            new Piece("Tracks", "Track_End", Extra.TrackBordered, Do((c, r) => EnvTrackModules.End(c, HalfWidth, r))),
            new Piece("Tracks", "Track_Gate2", Extra.TrackBordered, Do((c, r) => EnvTrackModules.Gate(c, HalfWidth, 2, r))),
            new Piece("Tracks", "Track_Gate3", Extra.TrackBordered, Do((c, r) => EnvTrackModules.Gate(c, HalfWidth, 3, r))),
            new Piece("Tracks", "Track_Enemy", Extra.TrackBordered, Do((c, r) => EnvTrackModules.Enemy(c, HalfWidth, r))),
            new Piece("Tracks", "Track_Boss", Extra.Boss, (c, r, t) => Flames(f => EnvTrackModules.Boss(c, HalfWidth, r, f))),
            new Piece("Tracks", "Track_Ramp", Extra.Ramp, Do((c, r) => EnvTrackModules.Ramp(c, HalfWidth, 1.5f, r))),
            new Piece("Tracks", "Track_Stairs", Extra.Stairs, Do((c, r) => EnvTrackModules.Stairs(c, HalfWidth, r))),
            new Piece("Tracks", "Track_Platform", Extra.Platform, Do((c, r) => EnvTrackModules.Platform(c, HalfWidth, 0.3f, r))),
            new Piece("Tracks", "Track_Elevated", Extra.PlatformElevated, Do((c, r) => EnvTrackModules.Platform(c, HalfWidth, 1.6f, r))),
            new Piece("Tracks", "Track_BorderLeft", Extra.Border, Do((c, r) => EnvTrackModules.Border(c, -1, HalfWidth, L, r))),
            new Piece("Tracks", "Track_BorderRight", Extra.Border, Do((c, r) => EnvTrackModules.Border(c, 1, HalfWidth, L, r))),
            new Piece("Tracks", "Track_Crossroad", Extra.Crossroad, Do((c, r) => EnvTrackModules.Crossroad(c, HalfWidth, r))),
            new Piece("Tracks", "Track_Curve", Extra.Curve, Do((c, r) => EnvTrackModules.Curve(c, HalfWidth, 14f, r))),

            // Castelo.
            new Piece("Castle", "Castle_Main", Extra.None, (c, r, t) => { EnvCastle.Castle(c, Vector3.zero, FrozenKingdomTrack.CastlePlateau, r); AddDoors(t, EnvCastle.GateOffset(FrozenKingdomTrack.CastlePlateau)); return null; }),
            new Piece("Castle", "Castle_Tower_Large", Extra.None, Do((c, r) => EnvCastle.Tower(c, Vector3.zero, 2.6f, 11f, 5.4f, r))),
            new Piece("Castle", "Castle_Tower_Small", Extra.None, Do((c, r) => EnvCastle.Tower(c, Vector3.zero, 1.25f, 5.2f, 2.8f, r, false))),
            new Piece("Castle", "Castle_Wall", Extra.None, Do((c, r) => EnvCastle.Wall(c, Matrix4x4.identity, 8f, 6.5f, 2.2f, r))),
            new Piece("Castle", "Castle_Wall_Short", Extra.None, Do((c, r) => EnvCastle.Wall(c, Matrix4x4.identity, 4f, 4f, 1.6f, r))),
            new Piece("Castle", "Castle_Gate", Extra.None, (c, r, t) => { EnvCastle.Gatehouse(c, Vector3.zero, EnvCastle.GateWidth, EnvCastle.GateHeight, r); AddDoors(t, Vector3.zero); return null; }),
            new Piece("Castle", "Castle_Arch", Extra.None, Do((c, r) => EnvMedieval.StoneArch(c, Vector3.zero, 0f, 3.5f, 5f, r))),
            new Piece("Castle", "Castle_Pillar", Extra.None, Do((c, r) => EnvMedieval.Pillar(c, Vector3.zero, 3.2f, r))),
            new Piece("Castle", "Castle_Stairs", Extra.None, Do((c, r) => EnvCastle.Stairs(c, Vector3.zero, 7f, 10, 0.32f, 0.55f, r))),
            new Piece("Castle", "Castle_Battlement", Extra.None, Do((c, r) => EnvCastle.Wall(c, Matrix4x4.identity, 4f, 0.6f, 1.2f, r))),
            new Piece("Castle", "Castle_Banner", Extra.None, Do((c, r) => CastleBanner(c))),
            new Piece("Castle", "Castle_Torch", Extra.None, (c, r, t) => new[] { EnvProps.Brazier(c, Vector3.zero, 1.6f, r) }),

            // Estruturas laterais.
            new Piece("Structures", "WatchTower", Extra.Lod, Do((c, r) => EnvMedieval.WatchTower(c, Vector3.zero, 0f, 5.6f, r))),
            new Piece("Structures", "StonePillar", Extra.Lod, Do((c, r) => EnvMedieval.Pillar(c, Vector3.zero, 2.4f, r))),
            new Piece("Structures", "StoneArch", Extra.Lod, Do((c, r) => EnvMedieval.StoneArch(c, Vector3.zero, 0f, 3.5f, 5f, r))),
            new Piece("Structures", "BannerPole", Extra.Lod, Do((c, r) => EnvProps.BannerPole(c, Vector3.zero, 3.6f, 0f, r))),
            new Piece("Structures", "Banner_Large", Extra.Lod, Do((c, r) => EnvProps.BannerPole(c, Vector3.zero, 3.6f, 0f, r, 1.5f))),
            new Piece("Structures", "Banner_Medium", Extra.Lod, Do((c, r) => EnvProps.BannerPole(c, Vector3.zero, 3.6f, 0f, r))),
            new Piece("Structures", "Banner_Small", Extra.Lod, Do((c, r) => EnvProps.BannerPole(c, Vector3.zero, 3.6f, 0f, r, 0.65f))),
            new Piece("Structures", "TorchStand", Extra.Lod, (c, r, t) => new[] { EnvProps.Brazier(c, Vector3.zero, 2.1f, r) }),
            new Piece("Structures", "Torch", Extra.Lod, (c, r, t) => new[] { EnvMedieval.Torch(c, Vector3.zero, 1.8f, r) }),
            new Piece("Structures", "Lantern", Extra.Lod, Do((c, r) => EnvMedieval.Lantern(c, Vector3.zero, 2.4f, r))),
            new Piece("Structures", "KnightStatue", Extra.Lod, Do((c, r) => EnvMedieval.KnightStatue(c, Vector3.zero, 0f, 1f, r))),

            // Natureza.
            new Piece("Nature", "PineTree_Snow", Extra.Lod, Do((c, r) => EnvVegetation.Pine(c, Vector3.zero, 5f, r))),
            new Piece("Nature", "PineTree_Snow_Small", Extra.Lod, Do((c, r) => EnvVegetation.Pine(c, Vector3.zero, 3.5f, r))),
            new Piece("Nature", "PineTree_Snow_Large", Extra.Lod, Do((c, r) => EnvVegetation.Pine(c, Vector3.zero, 7f, r))),
            new Piece("Nature", "PineTree_Snow_Heavy", Extra.Lod, Do((c, r) => EnvVegetation.Pine(c, Vector3.zero, 5.5f, r, 2f))),
            new Piece("Nature", "Tree_Frozen", Extra.Lod, Do((c, r) => EnvVegetation.FrozenTree(c, Vector3.zero, 4f, r))),
            new Piece("Nature", "Bush_Snow", Extra.Lod, Do((c, r) => EnvVegetation.SnowBush(c, Vector3.zero, 1f, r))),
            new Piece("Nature", "Plant_Snow", Extra.Lod, Do((c, r) => EnvVegetation.SmallPlant(c, Vector3.zero, 0.8f, r))),
            new Piece("Nature", "Rock_Snow", Extra.Lod, Do((c, r) => EnvRocks.Rock(c, Vector3.zero, new Vector3(1.6f, 1.1f, 1.3f), r))),
            new Piece("Nature", "Rock_Snow_Large", Extra.Lod, Do((c, r) => EnvRocks.Rock(c, Vector3.zero, new Vector3(2.6f, 2f, 2.2f), r, 0.55f))),
            new Piece("Nature", "Rock_Snow_Cluster", Extra.Lod, Do((c, r) => EnvRocks.RockCluster(c, Vector3.zero, 1f, r))),
            new Piece("Nature", "Ice_Crystal", Extra.Lod, Do((c, r) => EnvRocks.IceCrystals(c, Vector3.zero, 1.4f, r))),
            new Piece("Nature", "Ice_Block", Extra.Lod, Do((c, r) => EnvMedieval.IceBlock(c, Vector3.zero, 1.4f, r))),
            new Piece("Nature", "Snow_Pile", Extra.Lod, Do((c, r) => EnvVegetation.SnowDrift(c, Vector3.zero, new Vector3(2f, 0.5f, 1.5f), r))),

            // Terreno.
            new Piece("Terrain", "Cliff_Rock", Extra.None, Do((c, r) => EnvRocks.CliffSection(c, Vector3.zero, 12f, 7f, Vector3.back, r))),
            new Piece("Terrain", "FrozenWaterfall", Extra.None, Do((c, r) => EnvRocks.FrozenWaterfall(c, new Vector3(0f, 6f, 0f), 4f, 6f, Vector3.back, r))),
            new Piece("Terrain", "MountainPeak", Extra.None, Do((c, r) => EnvRocks.MountainPeak(c, Vector3.zero, 40f, 26f, r))),

            // Obstáculos e props.
            new Piece("Props", "Fence_Wood", Extra.Lod, Do((c, r) => EnvMedieval.Fence(c, Vector3.zero, 0f, 4f, r))),
            new Piece("Props", "Barrel_Wood", Extra.Lod, Do((c, r) => EnvProps.Barrel(c, Vector3.zero, 0.9f, r))),
            new Piece("Props", "Crate_Wood", Extra.Lod, Do((c, r) => EnvProps.Crate(c, Vector3.zero, 0.75f, r))),
            new Piece("Props", "Cart_Wood", Extra.Lod, Do((c, r) => EnvMedieval.Cart(c, Vector3.zero, 0f, r))),
            new Piece("Props", "Tent_Blue", Extra.Lod, Do((c, r) => EnvMedieval.Tent(c, Vector3.zero, 0f, 1f, r))),
            new Piece("Props", "Campfire", Extra.Lod, (c, r, t) => new[] { EnvMedieval.Campfire(c, Vector3.zero, r) }),
            new Piece("Props", "Snow_Decal", Extra.Lod, Do((c, r) => EnvRocks.SnowPatch(c, Vector3.zero, 1.2f, r))),
            new Piece("Props", "Stone_Decal", Extra.Lod, Do((c, r) => EnvRocks.PebbleScatter(c, Vector3.zero, 0.8f, r))),
            new Piece("Props", "Parapet", Extra.Lod, Do((c, r) => EnvProps.Parapet(c, 0f, -3f, 3f, r))),

            // Efeitos.
            new Piece("VFX", "Fx_Fire", Extra.None, (c, r, t) => new[] { Vector3.zero }),
        };

        [MenuItem("Legion Rush/Reino Congelado/1. Gerar prefabs")]
        public static void Bake()
        {
            foreach (string dir in new[] { ModelDir, MaterialDir, TextureDir, ConfigDir, Root + "/Lighting", Root + "/Prefabs", Root + "/Scenes" })
                EnsureFolder(dir);
            EnvTextures.KeepReadable = true;
            EnvTextures.ResetCache();
            SavedMaterials.Clear();
            SavedOther.Clear();
            PrefabPaths.Clear();

            int count = 0;
            foreach (Piece piece in Pieces())
            {
                BakePiece(piece);
                count++;
            }
            foreach (var gate in GateVariants())
            {
                BakeGate("Gate_" + gate.name, gate.category);
                count++;
            }
            count += BakeMarkers();
            BakeConfigAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EnvTextures.KeepReadable = false;
            Debug.Log($"[LegionRush] Reino Congelado: {count} prefabs em {Root}.");
        }

        public static void BakeBatch()
        {
            int code = 0;
            try { Bake(); }
            catch (Exception exception)
            {
                Debug.LogError("[LegionRush] Geração de prefabs falhou: " + exception);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        // Cores da folha "Portais de melhoria": azul, verde, amarelo e roxo = categorias do GateOption.
        public static (string name, GateCategory category)[] GateVariants() => new[]
        {
            ("Blue", GateCategory.Defense), ("Green", GateCategory.Multiply), ("Yellow", GateCategory.Resource), ("Purple", GateCategory.Special)
        };

        // Caminho do prefab gerado (procura na pasta do mundo se o gerador não rodou nesta sessão).
        public static GameObject LoadPrefab(string name)
        {
            if (PrefabPaths.TryGetValue(name, out string path)) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (string guid in AssetDatabase.FindAssets($"{name} t:Prefab", new[] { Root }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == name)
                {
                    PrefabPaths[name] = p;
                    return AssetDatabase.LoadAssetAtPath<GameObject>(p);
                }
            }
            return null;
        }

        private static Vector3[] Flames(Action<EnvFireFx> build)
        {
            var holder = new GameObject("FlameCollector").AddComponent<EnvFireFx>();
            holder.Initialize(EnvPalette.Fire);
            build(holder);
            var list = new List<Vector3>();
            foreach (Transform child in holder.transform) list.Add(child.position);
            UnityEngine.Object.DestroyImmediate(holder.gameObject);
            return list.Count > 0 ? list.ToArray() : null;
        }

        private static void CastleBanner(EnvChunk chunk)
        {
            chunk.Matrix = Matrix4x4.identity;
            chunk.Opaque.BeveledBox(new Vector3(0f, 4.3f, 0f), new Vector3(2.4f, 0.22f, 0.22f), 0.06f, EnvPalette.Gold);
            EnvCastle.HangingBanner(chunk.Banner, new Vector3(0f, 4.2f, -0.15f), 2.1f, 4.2f);
        }

        private static void AddDoors(Transform root, Vector3 gateOrigin)
        {
            var doors = new GameObject("CastleDoors").transform;
            doors.SetParent(root, false);
            var leaves = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? -1 : 1;
                var pivot = new GameObject(side < 0 ? "DoorLeft" : "DoorRight").transform;
                pivot.SetParent(doors, false);
                pivot.localPosition = gateOrigin + new Vector3(side * EnvCastle.GateOpening * 0.5f, 0f, 0.2f);
                var leaf = new EnvChunk();
                EnvCastle.DoorLeaf(leaf, EnvCastle.GateOpening * 0.5f, EnvCastle.GateArchTop, side);
                leaf.Build(pivot, "Door" + (side < 0 ? "L" : "R"), EnvShadowMode.Large);
                leaves[i] = pivot;
            }
            doors.gameObject.AddComponent<EnvGateDoors>().Initialize(leaves[0], leaves[1]);
        }

        private static void BakePiece(Piece piece)
        {
            var rng = new System.Random(StableHash(piece.Name));
            var chunk = new EnvChunk();
            var root = new GameObject(piece.Name);
            Vector3[] flames = piece.Build(chunk, rng, root.transform);
            var body = new GameObject("Mesh").transform;
            body.SetParent(root.transform, false);
            chunk.Build(body, piece.Name, EnvShadowMode.Large);
            AddExtras(root, piece.Extra);
            Finish(root, piece.Folder, piece.Name, flames, piece.Folder != "VFX");
        }

        // Colisores simplificados (só onde há apoio ou bloqueio: pista, bordas, plataformas) e LOD de corte.
        private static void AddExtras(GameObject root, Extra extra)
        {
            float w = HalfWidth * 2f + 0.6f;
            switch (extra)
            {
                case Extra.TrackSlab:
                    Box(root, new Vector3(0f, -0.25f, L * 0.5f), new Vector3(w, 0.5f, L));
                    break;
                case Extra.TrackBordered:
                    Box(root, new Vector3(0f, -0.25f, L * 0.5f), new Vector3(w, 0.5f, L));
                    Borders(root, HalfWidth, L);
                    break;
                case Extra.Border:
                    Box(root, new Vector3(-(HalfWidth + 0.75f), 0.4f, L * 0.5f), new Vector3(0.9f, 0.9f, L));
                    break;
                case Extra.Boss:
                    Box(root, new Vector3(0f, -0.25f, L), new Vector3(HalfWidth * 3.2f + 0.6f, 0.5f, L * 2f));
                    Borders(root, HalfWidth * 1.6f, L * 2f);
                    break;
                case Extra.Ramp:
                    Tilted(root, new Vector3(0f, 0.75f - 0.25f, L * 0.5f), new Vector3(w, 0.5f, Mathf.Sqrt(L * L + 1.5f * 1.5f)), -Mathf.Atan2(1.5f, L) * Mathf.Rad2Deg);
                    break;
                case Extra.Stairs:
                    Tilted(root, new Vector3(0f, 1f - 0.25f, L * 0.5f), new Vector3(HalfWidth * 2f, 0.5f, Mathf.Sqrt(L * L + 4f)), -Mathf.Atan2(2f, L) * Mathf.Rad2Deg);
                    break;
                case Extra.Platform:
                    Box(root, new Vector3(0f, 0f, L * 0.5f), new Vector3(HalfWidth * 2f + 1f, 0.6f, L));
                    break;
                case Extra.PlatformElevated:
                    Box(root, new Vector3(0f, 0.65f, L * 0.5f), new Vector3(HalfWidth * 2f + 1f, 1.9f, L));
                    break;
                case Extra.Crossroad:
                {
                    float arm = Mathf.Max(L, HalfWidth * 2f + 4.6f);
                    Box(root, new Vector3(0f, -0.25f, arm * 0.5f), new Vector3(w, 0.5f, arm));
                    Box(root, new Vector3(0f, -0.25f, arm * 0.5f), new Vector3(arm + HalfWidth * 2f, 0.5f, w));
                    break;
                }
                case Extra.Curve:
                {
                    const float Radius = 14f;
                    for (int i = 0; i < 8; i++)
                    {
                        float am = 90f * (i + 0.5f) / 8f;
                        Quaternion rot = Quaternion.Euler(0f, am, 0f);
                        Vector3 center = new Vector3(Radius, 0f, 0f) + rot * new Vector3(-Radius, 0f, 0f);
                        var seg = new GameObject("Collider" + i);
                        seg.transform.SetParent(root.transform, false);
                        seg.transform.SetLocalPositionAndRotation(center, rot);
                        var box = seg.AddComponent<BoxCollider>();
                        box.center = new Vector3(0f, -0.25f, 0f);
                        box.size = new Vector3(w, 0.5f, (Radius + HalfWidth + 1f) * 90f / 8f * Mathf.Deg2Rad);
                    }
                    break;
                }
                case Extra.Lod:
                {
                    // Uma malha só, cortada quando fica pequena na tela (a neblina já esconde a distância).
                    var group = root.AddComponent<LODGroup>();
                    group.SetLODs(new[] { new LOD(0.015f, root.GetComponentsInChildren<Renderer>()) });
                    group.RecalculateBounds();
                    break;
                }
            }
        }

        private static void Borders(GameObject root, float halfWidth, float length)
        {
            for (int side = -1; side <= 1; side += 2)
                Box(root, new Vector3(side * (halfWidth + 0.75f), 0.4f, length * 0.5f), new Vector3(0.9f, 0.9f, length));
        }

        private static void Box(GameObject root, Vector3 center, Vector3 size)
        {
            var box = root.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
        }

        private static void Tilted(GameObject root, Vector3 center, Vector3 size, float pitch)
        {
            var go = new GameObject("Collider");
            go.transform.SetParent(root.transform, false);
            go.transform.SetLocalPositionAndRotation(center, Quaternion.Euler(pitch, 0f, 0f));
            go.AddComponent<BoxCollider>().size = size;
        }

        private static void BakeGate(string name, GateCategory category)
        {
            var root = new GameObject(name);
            const float Width = 5f; // faixa de um portal numa fileira de 2 (pista de 10 m)
            EnvGatePortal.Build(root.transform, Width, GateOption.ColorOf(category), StableHash(name));
            foreach (EnvOwnedMeshes owner in root.GetComponentsInChildren<EnvOwnedMeshes>())
                UnityEngine.Object.DestroyImmediate(owner);
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, EnvGatePortal.Height * 0.5f, 0f);
            trigger.size = new Vector3(Width - 0.8f, EnvGatePortal.Height, 0.6f);
            root.AddComponent<EnvGatePortalInfo>().category = category;
            Finish(root, "Gates", name, null, false);
        }

        // Marcadores de spawn (Prefabs/): exército, onda inimiga, chefão e fileira de portais.
        private static int BakeMarkers()
        {
            foreach (var (name, kind, area, note) in new[]
            {
                ("PlayerSpawnPoint", EnvSpawnKind.Player, new Vector2(9f, 7f), "Formação inicial do exército azul (o comandante nasce em z = 0, centro da pista)."),
                ("EnemySpawnPoint", EnvSpawnKind.Enemy, new Vector2(9f, 5f), "Onda inimiga: o LevelManager enfileira os soldados a partir deste z, de frente para o exército."),
                ("BossSpawnPoint", EnvSpawnKind.Boss, new Vector2(7f, 7f), "Chefão do mundo, a 'distanceFromEnd' metros da linha de chegada."),
                ("GateRowPoint", EnvSpawnKind.GateRow, new Vector2(10f, 1.5f), "Fileira de portais: o GateRowController monta 2 ou 3 portais lado a lado neste z.")
            })
            {
                var root = new GameObject(name);
                var point = root.AddComponent<EnvSpawnPoint>();
                point.kind = kind;
                point.area = area;
                point.note = note;
                Finish(root, "Prefabs", name, null, false);
            }
            return 4;
        }

        private static void BakeConfigAssets()
        {
            string scatterPath = $"{ConfigDir}/{FrozenScatterProfile.DefaultResource}.asset";
            if (AssetDatabase.LoadAssetAtPath<FrozenScatterProfile>(scatterPath) == null)
                AssetDatabase.CreateAsset(FrozenScatterProfile.CreateDefault(), scatterPath); // não sobrescreve ajustes do designer
            string lightPath = Root + "/Lighting/WorldLighting_FrozenKingdom.asset";
            if (AssetDatabase.LoadAssetAtPath<WorldLightingProfile>(lightPath) == null)
                AssetDatabase.CreateAsset(WorldLightingProfile.CreatePreset(WorldId.FrozenKingdom), lightPath);
        }

        private static void Finish(GameObject root, string folder, string name, Vector3[] flames, bool markStatic)
        {
            string meshPath = $"{ModelDir}/{name}.asset";
            AssetDatabase.DeleteAsset(meshPath);
            bool first = true;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is MeshRenderer)
                {
                    Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    if (mesh != null && !AssetDatabase.Contains(mesh))
                    {
                        mesh.name = renderer.gameObject.name;
                        if (first) AssetDatabase.CreateAsset(mesh, meshPath);
                        else AssetDatabase.AddObjectToAsset(mesh, meshPath);
                        first = false;
                    }
                }
                renderer.sharedMaterial = SaveMaterial(renderer.sharedMaterial);
                if (markStatic && renderer is MeshRenderer && renderer.GetComponentInParent<EnvGateDoors>() == null)
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }
            if (flames != null)
            {
                var spawner = root.AddComponent<EnvFlameSpawner>();
                spawner.LocalFlames = flames;
            }
            string dir = $"{Root}/{folder}";
            EnsureFolder(dir);
            string path = $"{dir}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabPaths[name] = path;
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static Material SaveMaterial(Material runtime)
        {
            if (runtime == null || AssetDatabase.Contains(runtime)) return runtime;
            foreach (EnvMaterial id in Enum.GetValues(typeof(EnvMaterial)))
            {
                if (id == EnvMaterial.Count || EnvMaterials.Get(id) != runtime) continue;
                if (SavedMaterials.TryGetValue(id, out Material saved)) return saved;
                Material copy = SaveCopy(runtime);
                SavedMaterials[id] = copy;
                return copy;
            }
            // Outros materiais de runtime (cortina e faíscas dos portais).
            if (SavedOther.TryGetValue(runtime, out Material other)) return other;
            Material result = SaveCopy(runtime);
            SavedOther[runtime] = result;
            return result;
        }

        private static Material SaveCopy(Material runtime)
        {
            string path = $"{MaterialDir}/{runtime.name}.mat";
            var copy = new Material(runtime);
            foreach (string property in new[] { "_BaseMap", "_MainTex" })
            {
                if (!runtime.HasProperty(property)) continue;
                if (runtime.GetTexture(property) is Texture2D t2d && !AssetDatabase.Contains(t2d))
                    copy.SetTexture(property, SaveTexture(t2d));
            }
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(copy, path);
            return copy;
        }

        private static Texture2D SaveTexture(Texture2D texture)
        {
            string name = string.IsNullOrEmpty(texture.name) ? "LR_Texture" : texture.name;
            string path = $"{TextureDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null && SavedTextureThisRun.Contains(path)) return existing;
            AssetDatabase.DeleteAsset(path);
            Texture2D copy = texture.isReadable ? UnityEngine.Object.Instantiate(texture) : CopyViaRender(texture);
            copy.name = name;
            AssetDatabase.CreateAsset(copy, path);
            SavedTextureThisRun.Add(path);
            return copy;
        }

        private static readonly HashSet<string> SavedTextureThisRun = new HashSet<string>();

        // Textura não legível (ex.: ProceduralTextures.SoftCircle): copia pela GPU.
        private static Texture2D CopyViaRender(Texture2D texture)
        {
            var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(texture, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, true);
            copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            copy.Apply(true, false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            copy.wrapMode = texture.wrapMode;
            copy.filterMode = texture.filterMode;
            return copy;
        }

        private static int StableHash(string s)
        {
            unchecked
            {
                int h = 23;
                foreach (char ch in s) h = h * 31 + ch;
                return h;
            }
        }

        public static void EnsureFolder(string path)
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
