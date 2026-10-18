using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LegionRush.Environments.EditorTools
{
    // Gera os prefabs do Reino Congelado (malhas, materiais e texturas salvos como assets) em
    // Assets/LegionRush/Environments/FrozenKingdom/, para montar cenas à mão no editor. O jogo em si monta o
    // cenário por código (FrozenKingdomTrack) com as mesmas funções, então prefab e jogo ficam iguais.
    // Batch: Unity.exe -batchmode -projectPath . -executeMethod LegionRush.Environments.EditorTools.EnvPrefabBaker.BakeBatch
    public static class EnvPrefabBaker
    {
        private const string Root = "Assets/LegionRush/Environments/FrozenKingdom";
        private const string PrefabRoot = Root + "/Prefabs";
        private const string ModelDir = Root + "/Models";
        private const string MaterialDir = Root + "/Materials";
        private const string TextureDir = Root + "/Textures";
        private const string ProfileDir = Root + "/ScriptableObjects/Resources";
        private const float HalfWidth = 5f; // LevelConfig.trackHalfWidth padrão

        private static readonly Dictionary<EnvMaterial, Material> SavedMaterials = new Dictionary<EnvMaterial, Material>();

        private delegate Vector3[] Builder(EnvChunk chunk, System.Random rng, Transform root);

        [MenuItem("Legion Rush/Ambiente/Gerar prefabs do Reino Congelado")]
        public static void Bake()
        {
            foreach (string dir in new[] { ModelDir, MaterialDir, TextureDir, ProfileDir, Root + "/Scenes", Root + "/VFX", Root + "/Lighting" })
                EnsureFolder(dir);
            EnvTextures.KeepReadable = true;
            EnvTextures.ResetCache();
            SavedMaterials.Clear();

            var pieces = new List<(string folder, string name, Builder build)>
            {
                // Pista (módulos de 10 m, largura do jogo).
                ("Track", "FrozenTrack_Straight", (c, r, t) => { EnvTrackModules.Straight(c, HalfWidth, EnvTrackModules.ModuleLength, r); return null; }),
                ("Track", "FrozenTrack_Start", (c, r, t) => { EnvTrackModules.Start(c, HalfWidth, r); return null; }),
                ("Track", "FrozenTrack_End", (c, r, t) => { EnvTrackModules.End(c, HalfWidth, r); return null; }),
                ("Track", "FrozenTrack_Gate", (c, r, t) => { EnvTrackModules.Gate(c, HalfWidth, 2, r); return null; }),
                ("Track", "FrozenTrack_Enemy", (c, r, t) => { EnvTrackModules.Enemy(c, HalfWidth, r); return null; }),
                ("Track", "FrozenTrack_Boss", (c, r, t) => Flames(f => EnvTrackModules.Boss(c, HalfWidth, r, f))),
                ("Track", "FrozenTrack_Ramp", (c, r, t) => { EnvTrackModules.Ramp(c, HalfWidth, 1.5f, r); return null; }),
                ("Track", "FrozenTrack_Stairs", (c, r, t) => { EnvTrackModules.Stairs(c, HalfWidth, r); return null; }),
                ("Track", "FrozenTrack_Platform", (c, r, t) => { EnvTrackModules.Platform(c, HalfWidth, 0.3f, r); return null; }),
                ("Track", "FrozenTrack_PlatformElevated", (c, r, t) => { EnvTrackModules.Platform(c, HalfWidth, 1.6f, r); return null; }),
                ("Track", "FrozenTrack_BorderLeft", (c, r, t) => { EnvTrackModules.Border(c, -1, HalfWidth, EnvTrackModules.ModuleLength, r); return null; }),
                ("Track", "FrozenTrack_BorderRight", (c, r, t) => { EnvTrackModules.Border(c, 1, HalfWidth, EnvTrackModules.ModuleLength, r); return null; }),
                ("Track", "FrozenTrack_Crossroad", (c, r, t) => { EnvTrackModules.Crossroad(c, HalfWidth, r); return null; }),
                ("Track", "FrozenTrack_Curve", (c, r, t) => { EnvTrackModules.Curve(c, HalfWidth, 14f, r); return null; }),

                // Castelo.
                ("Castle", "FrozenCastle_Main", (c, r, t) => { EnvCastle.Castle(c, Vector3.zero, 3.2f, r); AddDoors(t, EnvCastle.GateOffset(3.2f)); return null; }),
                ("Castle", "FrozenCastle_TowerLarge", (c, r, t) => { EnvCastle.Tower(c, Vector3.zero, 2.6f, 11f, 5.4f, r); return null; }),
                ("Castle", "FrozenCastle_TowerSmall", (c, r, t) => { EnvCastle.Tower(c, Vector3.zero, 1.25f, 5.2f, 2.8f, r, false); return null; }),
                ("Castle", "FrozenCastle_Wall", (c, r, t) => { EnvCastle.Wall(c, Matrix4x4.identity, 8f, 6.5f, 2.2f, r); return null; }),
                ("Castle", "FrozenCastle_WallShort", (c, r, t) => { EnvCastle.Wall(c, Matrix4x4.identity, 4f, 4f, 1.6f, r); return null; }),
                ("Castle", "FrozenCastle_Gate", (c, r, t) => { EnvCastle.Gatehouse(c, Vector3.zero, EnvCastle.GateWidth, EnvCastle.GateHeight, r); AddDoors(t, Vector3.zero); return null; }),
                ("Castle", "FrozenCastle_Arch", (c, r, t) => { EnvMedieval.StoneArch(c, Vector3.zero, 0f, 3.5f, 5f, r); return null; }),
                ("Castle", "FrozenCastle_Stairs", (c, r, t) => { EnvCastle.Stairs(c, Vector3.zero, 7f, 10, 0.32f, 0.55f, r); return null; }),
                ("Castle", "FrozenCastle_Pillar", (c, r, t) => { EnvMedieval.Pillar(c, Vector3.zero, 3.2f, r); return null; }),
                ("Castle", "FrozenCastle_Battlement", (c, r, t) => { EnvCastle.Wall(c, Matrix4x4.identity, 4f, 0.6f, 1.2f, r); return null; }),
                ("Castle", "FrozenCastle_Banner", (c, r, t) => { CastleBanner(c); return null; }),

                // Terreno, rochas e gelo.
                ("Terrain", "FrozenTerrain_Cliff", (c, r, t) => { EnvRocks.CliffSection(c, Vector3.zero, 12f, 7f, Vector3.back, r); return null; }),
                ("Terrain", "FrozenTerrain_FrozenWaterfall", (c, r, t) => { EnvRocks.FrozenWaterfall(c, new Vector3(0f, 6f, 0f), 4f, 6f, Vector3.back, r); return null; }),
                ("Terrain", "FrozenTerrain_MountainPeak", (c, r, t) => { EnvRocks.MountainPeak(c, Vector3.zero, 40f, 26f, r); return null; }),
                ("Terrain", "FrozenTerrain_SnowPile", (c, r, t) => { EnvVegetation.SnowDrift(c, Vector3.zero, new Vector3(2f, 0.5f, 1.5f), r); return null; }),
                ("Terrain", "FrozenTerrain_IceBlock", (c, r, t) => { EnvMedieval.IceBlock(c, Vector3.zero, 1.4f, r); return null; }),
                ("Terrain", "FrozenTerrain_IceCrystal", (c, r, t) => { EnvRocks.IceCrystals(c, Vector3.zero, 1.4f, r); return null; }),
                ("Terrain", "FrozenTerrain_RockSmall", (c, r, t) => { EnvRocks.Rock(c, Vector3.zero, new Vector3(0.9f, 0.6f, 0.8f), r); return null; }),
                ("Terrain", "FrozenTerrain_RockLarge", (c, r, t) => { EnvRocks.Rock(c, Vector3.zero, new Vector3(2.6f, 2f, 2.2f), r, 0.55f); return null; }),
                ("Terrain", "FrozenTerrain_RockCluster", (c, r, t) => { EnvRocks.RockCluster(c, Vector3.zero, 1f, r); return null; }),

                // Vegetação.
                ("Vegetation", "FrozenPine_Small", (c, r, t) => { EnvVegetation.Pine(c, Vector3.zero, 3.5f, r); return null; }),
                ("Vegetation", "FrozenPine_Medium", (c, r, t) => { EnvVegetation.Pine(c, Vector3.zero, 5f, r); return null; }),
                ("Vegetation", "FrozenPine_Large", (c, r, t) => { EnvVegetation.Pine(c, Vector3.zero, 7f, r); return null; }),
                ("Vegetation", "FrozenPine_HeavySnow", (c, r, t) => { EnvVegetation.Pine(c, Vector3.zero, 5.5f, r, 2f); return null; }),
                ("Vegetation", "FrozenTree_PartiallyFrozen", (c, r, t) => { EnvVegetation.FrozenTree(c, Vector3.zero, 4f, r); return null; }),
                ("Vegetation", "FrozenBush_Snow", (c, r, t) => { EnvVegetation.SnowBush(c, Vector3.zero, 1f, r); return null; }),
                ("Vegetation", "FrozenPlant_Small", (c, r, t) => { EnvVegetation.SmallPlant(c, Vector3.zero, 0.8f, r); return null; }),

                // Decoração.
                ("Props", "FrozenProp_Torch", (c, r, t) => new[] { EnvMedieval.Torch(c, Vector3.zero, 1.8f, r) }),
                ("Props", "FrozenProp_Brazier", (c, r, t) => new[] { EnvProps.Brazier(c, Vector3.zero, 2.1f, r) }),
                ("Props", "FrozenProp_Campfire", (c, r, t) => new[] { EnvMedieval.Campfire(c, Vector3.zero, r) }),
                ("Props", "FrozenProp_BannerLarge", (c, r, t) => { EnvProps.BannerPole(c, Vector3.zero, 3.6f, 0f, r, 1.5f); return null; }),
                ("Props", "FrozenProp_BannerMedium", (c, r, t) => { EnvProps.BannerPole(c, Vector3.zero, 3.6f, 0f, r); return null; }),
                ("Props", "FrozenProp_BannerSmall", (c, r, t) => { EnvProps.BannerPole(c, Vector3.zero, 3.6f, 0f, r, 0.65f); return null; }),
                ("Props", "FrozenProp_Lantern", (c, r, t) => { EnvMedieval.Lantern(c, Vector3.zero, 2.4f, r); return null; }),
                ("Props", "FrozenProp_Fence", (c, r, t) => { EnvMedieval.Fence(c, Vector3.zero, 0f, 4f, r); return null; }),
                ("Props", "FrozenProp_Barrel", (c, r, t) => { EnvProps.Barrel(c, Vector3.zero, 0.9f, r); return null; }),
                ("Props", "FrozenProp_Crate", (c, r, t) => { EnvProps.Crate(c, Vector3.zero, 0.75f, r); return null; }),
                ("Props", "FrozenProp_Cart", (c, r, t) => { EnvMedieval.Cart(c, Vector3.zero, 0f, r); return null; }),
                ("Props", "FrozenProp_Tent", (c, r, t) => { EnvMedieval.Tent(c, Vector3.zero, 0f, 1f, r); return null; }),
                ("Props", "FrozenProp_WatchTower", (c, r, t) => { EnvMedieval.WatchTower(c, Vector3.zero, 0f, 5.6f, r); return null; }),
                ("Props", "FrozenProp_KnightStatue", (c, r, t) => { EnvMedieval.KnightStatue(c, Vector3.zero, 0f, 1f, r); return null; }),
                ("Props", "FrozenProp_Parapet", (c, r, t) => { EnvProps.Parapet(c, 0f, -3f, 3f, r); return null; }),
            };

            int count = 0;
            foreach (var piece in pieces)
            {
                BakePiece(piece.folder, piece.name, piece.build);
                count++;
            }

            // Portais (cores das categorias do GateOption; o texto e os valores vêm do jogo).
            foreach (var gate in new[] { ("Blue", GateOption.ColorOf(GateCategory.Defense)), ("Green", GateOption.ColorOf(GateCategory.Multiply)),
                         ("Yellow", GateOption.ColorOf(GateCategory.Resource)), ("Purple", GateOption.ColorOf(GateCategory.Special)) })
            {
                BakeGate("FrozenGate_" + gate.Item1, gate.Item2);
                count++;
            }

            BakeScatterProfile();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EnvTextures.KeepReadable = false;
            Debug.Log($"[LegionRush] Reino Congelado: {count} prefabs em {PrefabRoot}.");
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

        private static Vector3[] Flames(Action<EnvFireFx> build)
        {
            // Coleta as chamas sem criar partículas: um EnvFireFx temporário só guarda as posições.
            var holder = new GameObject("FlameCollector").AddComponent<EnvFireFx>();
            holder.Initialize(EnvPalette.Fire);
            build(holder);
            var list = new List<Vector3>();
            foreach (Transform child in holder.transform)
                list.Add(child.position);
            UnityEngine.Object.DestroyImmediate(holder.gameObject);
            return list.Count > 0 ? list.ToArray() : null;
        }

        // Estandarte grande de muralha: barra dourada e tecido azul com flor-de-lis.
        private static void CastleBanner(EnvChunk chunk)
        {
            chunk.Matrix = Matrix4x4.identity;
            chunk.Opaque.BeveledBox(new Vector3(0f, 4.3f, 0f), new Vector3(2.4f, 0.22f, 0.22f), 0.06f, EnvPalette.Gold);
            EnvCastle.HangingBanner(chunk.Banner, new Vector3(0f, 4.2f, -0.15f), 2.1f, 4.2f);
        }

        // Folhas da porta como filhos com pivô na dobradiça + EnvGateDoors (abre com Open()).
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

        private static void BakePiece(string folder, string name, Builder build)
        {
            var rng = new System.Random(StableHash(name));
            var chunk = new EnvChunk();
            var root = new GameObject(name);
            Vector3[] flames = build(chunk, rng, root.transform);
            var body = new GameObject("Mesh").transform;
            body.SetParent(root.transform, false);
            chunk.Build(body, name, EnvShadowMode.Large);
            Finish(root, folder, name, flames, true);
        }

        private static void BakeGate(string name, Color color)
        {
            var root = new GameObject(name);
            EnvGatePortal.Build(root.transform, 4.6f, color, StableHash(name));
            foreach (EnvOwnedMeshes owner in root.GetComponentsInChildren<EnvOwnedMeshes>())
                UnityEngine.Object.DestroyImmediate(owner);
            Finish(root, "Gates", name, null, false);
        }

        private static void Finish(GameObject root, string folder, string name, Vector3[] flames, bool markStatic)
        {
            string meshPath = $"{ModelDir}/{name}.asset";
            AssetDatabase.DeleteAsset(meshPath);
            bool first = true;
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = filter.sharedMesh;
                mesh.name = renderer.gameObject.name;
                if (first) AssetDatabase.CreateAsset(mesh, meshPath);
                else AssetDatabase.AddObjectToAsset(mesh, meshPath);
                first = false;
                renderer.sharedMaterial = SaveMaterial(renderer.sharedMaterial);
                // Peças paradas: Static Batching. Folhas da porta giram, então ficam dinâmicas.
                if (markStatic && renderer.GetComponentInParent<EnvGateDoors>() == null)
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }
            if (flames != null)
            {
                var spawner = root.AddComponent<EnvFlameSpawner>();
                spawner.LocalFlames = flames;
            }
            string dir = $"{PrefabRoot}/{folder}";
            EnsureFolder(dir);
            PrefabUtility.SaveAsPrefabAsset(root, $"{dir}/{name}.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void BakeScatterProfile()
        {
            string path = $"{ProfileDir}/{FrozenScatterProfile.DefaultResource}.asset";
            if (AssetDatabase.LoadAssetAtPath<FrozenScatterProfile>(path) != null) return; // não sobrescreve ajustes do designer
            AssetDatabase.CreateAsset(FrozenScatterProfile.CreateDefault(), path);
        }

        private static Material SaveMaterial(Material runtime)
        {
            foreach (EnvMaterial id in Enum.GetValues(typeof(EnvMaterial)))
            {
                if (id == EnvMaterial.Count || EnvMaterials.Get(id) != runtime) continue;
                if (SavedMaterials.TryGetValue(id, out Material saved)) return saved;
                string path = $"{MaterialDir}/{runtime.name}.mat";
                var copy = new Material(runtime);
                Texture texture = runtime.HasProperty("_BaseMap") ? runtime.GetTexture("_BaseMap") : null;
                if (texture is Texture2D t2d && !AssetDatabase.Contains(t2d))
                {
                    string texturePath = $"{TextureDir}/{t2d.name}.asset";
                    AssetDatabase.DeleteAsset(texturePath);
                    var textureCopy = UnityEngine.Object.Instantiate(t2d);
                    textureCopy.name = t2d.name;
                    AssetDatabase.CreateAsset(textureCopy, texturePath);
                    copy.SetTexture("_BaseMap", textureCopy);
                }
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(copy, path);
                SavedMaterials[id] = copy;
                return copy;
            }
            return runtime;
        }

        // string.GetHashCode muda entre execuções no .NET moderno: hash próprio para o mesmo prefab sair igual.
        private static int StableHash(string s)
        {
            unchecked
            {
                int h = 23;
                foreach (char ch in s) h = h * 31 + ch;
                return h;
            }
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
