using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LegionRush.Environments
{
    // Monta o cenário da pista do Reino Congelado com o kit de ambiente: calçamento, mureta de pedra,
    // braseiros e estandartes, barrancos de neve com pinheiros, rochas e cristais, cânion de penhascos com
    // cachoeiras congeladas, arco de chegada e castelo no fim. Só visual: não cria colisores nem mexe na
    // lógica de jogo (portões, inimigos, chefão continuam no LevelManager).
    public class FrozenKingdomTrack : MonoBehaviour
    {
        public const float ChunkLength = 30f;
        public const float StartZ = -30f;
        public const float CastlePlateau = 3.2f;
        private const float BraziersEvery = 12f;
        private const float PillarEvery = 6f;

        // Seções da folha de referência (para a tabela de coordenadas).
        private const string SecFront = "Vista frontal / traseira";
        private const string SecOverview = "Vista geral do cenário";
        private const string SecProps = "Obstáculos e elementos do cenário";
        private const string SecCastle = "Castelo final (entrada)";
        private const string SecStructures = "Castelo e estruturas";

        public static bool Supports(WorldId world) => world == WorldId.FrozenKingdom && EnvMaterials.Available;

        private readonly List<MeshRenderer> shadowCasters = new List<MeshRenderer>(64);
        private VisualQualityManager quality;
        private EnvFireFx fire;
        private float trackLength;
        private float halfWidth;
        private EnvTerrain.Shape shape;
        private EnvGateDoors gateDoors;
        private FrozenScatterProfile scatter;

        public float CastleZ => trackLength + 32f;
        public EnvTerrain.Shape Shape => shape;

        // Quando não nulo, cada peça colocada também é registrada aqui (prefab, posição, giro, escala, seção):
        // o construtor da cena FrozenKingdom_01 monta o mesmo mapa com prefabs a partir desta lista.
        public List<EnvPlacement> Recorder;

        private void Record(string prefab, Vector3 position, float yaw, float scale, string group, string section)
            => Record(prefab, position, yaw, Vector3.one * scale, group, section);

        private void Record(string prefab, Vector3 position, float yaw, Vector3 scale, string group, string section)
        {
            Recorder?.Add(new EnvPlacement(prefab, position, yaw, scale, group, section));
        }

        // Monta tudo. quality pode ser nulo (pré-visualização no editor = qualidade alta).
        public void Build(float length, float trackHalfWidth, int levelNumber, WorldLightingProfile world, VisualQualityManager qualityManager,
            Camera targetCamera, FrozenScatterProfile scatterProfile = null)
        {
            scatter = scatterProfile != null ? scatterProfile : FrozenScatterProfile.LoadOrDefault();
            trackLength = length;
            halfWidth = trackHalfWidth;
            quality = qualityManager;
            QualityTierSettings settings = Settings();
            float density = Mathf.Clamp(settings.EnvironmentDensity, 0.3f, 1f);
            var shadows = (EnvShadowMode)Mathf.Clamp(settings.EnvironmentShadows, 0, 2);
            int seed = levelNumber * 7919 + 17 + scatter.seed;
            var rng = new System.Random(seed);

            shape = new EnvTerrain.Shape
            {
                RoadHalfWidth = halfWidth + 0.3f,
                BankStart = halfWidth + 1.25f,
                // Campo de neve aberto com florestas (vista geral da referência), subindo devagar para colinas;
                // nada de cânion fechado ao lado da pista.
                CliffStart = halfWidth + 15f,
                CliffWidth = 9f,
                PlateauHeight = 3.2f,
                Seed = levelNumber % 97
            };

            fire = new GameObject("Fire").AddComponent<EnvFireFx>();
            fire.transform.SetParent(transform, false);
            fire.Initialize(world != null ? world.torchColor : EnvPalette.Fire);

            PlanLandmarks();
            float endZ = CastleZ + 8f;
            int chunkIndex = 0;
            int vertices = 0;
            for (float z0 = StartZ; z0 < endZ; z0 += ChunkLength, chunkIndex++)
            {
                float z1 = Mathf.Min(z0 + ChunkLength, endZ);
                var chunk = new EnvChunk();
                BuildGround(chunk, z0, z1);
                BuildEdges(chunk, z0, z1, rng);
                BuildLandmarks(chunk, z0, z1, rng);
                BuildBanks(chunk, z0, z1, density, rng);
                BuildCanyon(chunk, z0, z1, density, rng);
                vertices += chunk.VertexCount;
                Collect(chunk, $"Chunk{chunkIndex:00}", shadows);
            }

            var finale = new EnvChunk();
            BuildFinishArch(finale, rng);
            EnvCastle.Castle(finale, new Vector3(0f, 0f, CastleZ), CastlePlateau, rng);
            Record("FK_Castle_Main", new Vector3(0f, 0f, CastleZ), 0f, 1f, "Castle", SecCastle + ": fortaleza centralizada no fim do percurso");
            BuildCastleSurroundings(finale, density, rng);
            vertices += finale.VertexCount;
            Collect(finale, "Castle", shadows);
            BuildCastleDoors(shadows);

            var backdrop = new EnvChunk();
            BuildMountainRange(backdrop, density, rng);
            vertices += backdrop.VertexCount;
            Collect(backdrop, "CastleMountains", EnvShadowMode.None);

            var sky = new GameObject("Sky").AddComponent<EnvSkyBackdrop>();
            sky.transform.SetParent(transform, false);
            if (world != null)
            {
                // Horizonte e base das montanhas na cor da neblina: sem emenda entre cenário e céu.
                sky.Horizon = world.fogColor;
                sky.MountainHaze = world.fogColor;
            }
            Vector3 sunDirection = world != null ? Quaternion.Euler(world.sunPitch, world.sunYaw, 0f) * Vector3.forward : new Vector3(0.3f, -0.7f, 0.6f);
            sky.Build(targetCamera != null ? targetCamera : Camera.main, sunDirection, seed);

            ApplyQuality();
            if (quality != null) quality.TierChanged += HandleTierChanged;
            GameLog.Info($"Cenário Reino Congelado: {chunkIndex + 1} trechos, {vertices / 1000}k vértices, {fire.Count} braseiros, densidade {density:0.##}, sombras {shadows}.");
        }

        // Abre o portão do castelo (vitória). Seguro chamar mais de uma vez.
        public void OpenCastleGate()
        {
            if (gateDoors == null || gateDoors.IsOpen) return;
            gateDoors.Open();
            // Brilho de vitória no portão: explosão dourada + anel azul na escadaria.
            Vector3 gate = new Vector3(0f, 0f, CastleZ) + EnvCastle.GateOffset(CastlePlateau);
            Vfx.Burst(gate + Vector3.up * (EnvCastle.GateArchTop * 0.5f), EnvPalette.Gold, 4.5f, 1.1f);
            Vfx.Ring(gate + Vector3.up * 0.1f, EnvPalette.GateGlow, 7f, 1.2f);
        }

        private QualityTierSettings Settings()
        {
            if (quality != null) return quality.Settings;
            return new QualityTierSettings { EnvironmentDensity = 1f, EnvironmentShadows = 2, FireParticles = 1f, TorchPools = true };
        }

        private void HandleTierChanged(QualityTier tier) => ApplyQuality();

        private void ApplyQuality()
        {
            QualityTierSettings s = Settings();
            fire.ApplyQuality(s.FireParticles, s.TorchPools);
            bool cast = s.EnvironmentShadows > 0;
            foreach (MeshRenderer r in shadowCasters)
                if (r != null) r.shadowCastingMode = cast ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }

        private void Collect(EnvChunk chunk, string name, EnvShadowMode shadows)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            chunk.Build(root, name, shadows == EnvShadowMode.None ? EnvShadowMode.Large : shadows);
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>())
                if (r.shadowCastingMode == ShadowCastingMode.On) shadowCasters.Add(r);
        }

        // ---------- Partes ----------

        private void BuildGround(EnvChunk chunk, float z0, float z1)
        {
            float roadEnd = CastleZ - 9f;
            if (z0 < roadEnd) EnvTerrain.Road(chunk, halfWidth, z0, Mathf.Min(z1, roadEnd));
            EnvTerrain.SnowBanks(chunk, shape, z0, z1, halfWidth + 38f, 2.5f);
        }

        // Muro de pedra com capa de neve dos dois lados (como a folha do Reino Congelado): pilares a cada 6 m,
        // braseiros em pedestal sobre a linha do muro a cada 12 m, estandartes atrás do muro e uma faixa de
        // rochas nevadas encostada nele, antes do campo de pinheiros.
        private void BuildEdges(EnvChunk chunk, float z0, float z1, System.Random rng)
        {
            float edge = halfWidth + 0.3f;
            float stop = Mathf.Min(z1, trackLength - 2f);
            float resume = trackLength + 2f;
            System.Func<float, bool> brazierAt = z => IsBrazier(z);
            for (int side = -1; side <= 1; side += 2)
            {
                if (z0 < stop) EnvProps.Rampart(chunk, side * edge, z0, stop, PillarEvery, brazierAt, rng);
                if (z1 > resume) EnvProps.Rampart(chunk, side * edge, Mathf.Max(z0, resume), Mathf.Min(z1, CastleZ - 9f), PillarEvery, brazierAt, rng);
            }

            float first = Mathf.Ceil(z0 / BraziersEvery) * BraziersEvery;
            for (float z = first; z < z1; z += BraziersEvery)
            {
                if (!IsBrazier(z)) continue;
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 p = new Vector3(side * (edge + 0.48f), 0f, z);
                    Vector3 flame = EnvProps.Brazier(chunk, p, 2.1f, rng);
                    fire.Add(flame, 0f);
                    Record("TorchStand", p, 0f, 1f, "Structures", SecFront + ": braseiro em pedestal sobre o muro");
                }
                float bz = z + BraziersEvery * 0.5f;
                if (bz < z1 && IsBrazier(bz - BraziersEvery * 0.5f) && Mathf.Abs(bz - trackLength) > 6f && bz < CastleZ - 12f)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        EnvProps.BannerPole(chunk, new Vector3(side * (halfWidth + 2.3f), 0f, bz), 3.8f, side * -18f, rng);
                        Record("BannerPole", new Vector3(side * (halfWidth + 2.3f), 0f, bz), side * -18f, 1f, "Structures", SecFront + ": estandarte azul atrás do muro");
                    }
                }
            }

            // Rochas nevadas encostadas no muro (as "rochas grandes/médias" da folha), deixando os estandartes livres.
            for (float z = z0 + EnvMeshBuilder.Rand(rng, 0f, 2f); z < Mathf.Min(z1, CastleZ - 12f); z += EnvMeshBuilder.Rand(rng, 2.6f, 4.2f))
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    if (rng.NextDouble() < 0.25) continue;
                    float x = side * (halfWidth + EnvMeshBuilder.Rand(rng, 2.6f, 4.6f));
                    if (Mathf.Abs(Mathf.Repeat(z, BraziersEvery) - BraziersEvery * 0.5f) < 1.4f && Mathf.Abs(x) < halfWidth + 3.4f) continue;
                    float scale = EnvMeshBuilder.Rand(rng, 0.9f, 1.6f);
                    Vector3 p = Ground(x, z) + Vector3.down * 0.2f;
                    EnvRocks.RockCluster(chunk, p, scale, rng);
                    Record("Rock_Snow_Cluster", p, 0f, scale, "Nature", SecProps + ": rochas nevadas junto ao muro");
                }
            }
        }

        private bool IsBrazier(float z) => z > 4f && Mathf.Abs(z - trackLength) >= 6f && z <= CastleZ - 12f
                                           && Mathf.Abs(Mathf.Repeat(z + 0.01f, BraziersEvery)) < 0.1f;

        // Barranco de neve entre a mureta e o penhasco, conforme o perfil de distribuição (FrozenScatterProfile).
        // Nada fica a menos de 1,5 m da borda da pista: a área de movimento, portais e inimigos ficam livres.
        private void BuildBanks(EnvChunk chunk, float z0, float z1, float density, System.Random rng)
        {
            float span = z1 - z0;
            float maxDistance = shape.CliffStart + 1.5f - halfWidth;
            var placed = new List<Vector2>(64);
            foreach (EnvScatterRule rule in scatter.rules)
            {
                if (rule == null || rule.perMeter <= 0f) continue;
                placed.Clear();
                int count = Mathf.RoundToInt(span * rule.perMeter * density);
                float dMin = Mathf.Max(FrozenScatterProfile.MinRoadClearance, Mathf.Min(rule.distance.x, rule.distance.y));
                float dMax = Mathf.Min(maxDistance, Mathf.Max(rule.distance.x, rule.distance.y));
                if (dMax <= dMin) continue;
                for (int i = 0; i < count; i++)
                {
                    int side = rng.NextDouble() < 0.5 ? -1 : 1;
                    float x = side * (halfWidth + EnvMeshBuilder.Rand(rng, dMin, dMax));
                    float z = EnvMeshBuilder.Rand(rng, z0, z1);
                    float s = EnvMeshBuilder.Rand(rng, rule.scale.x, rule.scale.y);
                    float yaw = EnvMeshBuilder.Rand(rng, rule.yaw.x, rule.yaw.y);
                    if (rng.NextDouble() > rule.probability) continue;
                    if (rule.avoidBraziers && NearBrazier(z) && Mathf.Abs(x) < halfWidth + 4.5f) continue;
                    if (NearLandmark(x, z)) continue;
                    if (rule.minSpacing > 0f && TooClose(placed, x, z, rule.minSpacing)) continue;
                    placed.Add(new Vector2(x, z));
                    Place(chunk, rule.kind, Ground(x, z), s, yaw, rng);
                }
            }
        }

        private static bool TooClose(List<Vector2> placed, float x, float z, float spacing)
        {
            float sq = spacing * spacing;
            foreach (Vector2 p in placed)
                if ((p.x - x) * (p.x - x) + (p.y - z) * (p.y - z) < sq) return true;
            return false;
        }

        private void Place(EnvChunk chunk, EnvScatterKind kind, Vector3 p, float s, float yaw, System.Random rng)
        {
            switch (kind)
            {
                case EnvScatterKind.Pine:
                    float h = s * Mathf.Lerp(0.85f, 1.15f, Mathf.InverseLerp(halfWidth + 3f, shape.CliffStart, Mathf.Abs(p.x)));
                    EnvVegetation.Pine(chunk, p, h, rng);
                    Record("PineTree_Snow", p, yaw, h / 5f, "Nature", SecOverview + ": pinheiros nevados nas laterais");
                    break;
                case EnvScatterKind.PineHeavySnow:
                    EnvVegetation.Pine(chunk, p, s, rng, 2f);
                    Record("PineTree_Snow_Heavy", p, yaw, s / 5.5f, "Nature", SecOverview + ": pinheiros com neve intensa");
                    break;
                case EnvScatterKind.SnowBush:
                    EnvVegetation.SnowBush(chunk, p, s, rng);
                    Record("Bush_Snow", p, yaw, s, "Nature", SecOverview + ": arbustos nevados");
                    break;
                case EnvScatterKind.SnowDrift:
                    EnvVegetation.SnowDrift(chunk, p + Vector3.down * 0.1f, new Vector3(1.2f * s, EnvMeshBuilder.Rand(rng, 0.35f, 0.6f), s), rng);
                    Record("Snow_Pile", p + Vector3.down * 0.1f, yaw, new Vector3(0.6f * s, 1f, 0.67f * s), "Nature", SecProps + ": acúmulo de neve");
                    break;
                case EnvScatterKind.RockCluster:
                    EnvRocks.RockCluster(chunk, p + Vector3.down * 0.15f, s, rng);
                    Record("Rock_Snow_Cluster", p + Vector3.down * 0.15f, yaw, s, "Nature", SecProps + ": rochas nevadas");
                    break;
                case EnvScatterKind.RockLarge:
                    EnvRocks.Rock(chunk, p + Vector3.down * 0.3f, new Vector3(1.7f, 1.3f, 1.5f) * s, rng, 0.55f);
                    Record("Rock_Snow_Large", p + Vector3.down * 0.3f, yaw, s * 0.66f, "Nature", SecProps + ": rocha grande");
                    break;
                case EnvScatterKind.IceCrystals:
                    EnvRocks.IceCrystals(chunk, p, s, rng);
                    Record("Ice_Crystal", p, yaw, s / 1.4f, "Nature", SecOverview + ": cristais de gelo");
                    break;
                case EnvScatterKind.IceBlock:
                    EnvMedieval.IceBlock(chunk, p + Vector3.down * 0.1f, s, rng);
                    Record("Ice_Block", p + Vector3.down * 0.1f, yaw, s / 1.4f, "Nature", SecOverview + ": blocos de gelo");
                    break;
                case EnvScatterKind.Lantern:
                    EnvMedieval.Lantern(chunk, p, 2.4f * s, rng);
                    Record("Lantern", p, yaw, s, "Structures", "Elementos decorativos: lamparina");
                    break;
                case EnvScatterKind.FrozenTree:
                    EnvVegetation.FrozenTree(chunk, p, s, rng);
                    Record("Tree_Frozen", p, yaw, s / 4f, "Nature", SecOverview + ": árvore parcialmente congelada");
                    break;
                case EnvScatterKind.SmallPlant:
                    EnvVegetation.SmallPlant(chunk, p, s, rng);
                    Record("Plant_Snow", p, yaw, s / 0.8f, "Nature", SecOverview + ": plantinhas");
                    break;
            }
        }

        // Marcos da paisagem (torres de observação, acampamentos, estátuas). Posições fixas por fase, decididas
        // antes da distribuição para reservar a área (x, z, meia-largura, meia-profundidade) no barranco.
        private readonly List<Vector4> landmarkAreas = new List<Vector4>(16);
        private readonly List<Vector3> plannedTowers = new List<Vector3>(8);
        private readonly List<Vector3> plannedCamps = new List<Vector3>(4);

        private bool NearLandmark(float x, float z)
        {
            foreach (Vector4 a in landmarkAreas)
                if (Mathf.Abs(x - a.x) < a.z && Mathf.Abs(z - a.y) < a.w) return true;
            return false;
        }

        private void PlanLandmarks()
        {
            landmarkAreas.Clear();
            plannedTowers.Clear();
            plannedCamps.Clear();
            int side = 1;
            for (float z = 36f; z < trackLength - 20f; z += 64f)
            {
                var p = new Vector3(side * (halfWidth + 7.2f), 0f, z);
                plannedTowers.Add(p);
                landmarkAreas.Add(new Vector4(p.x, p.z, 2.6f, 2.6f));
                side = -side;
            }
            foreach (float k in new[] { 0.28f, 0.66f })
            {
                float z = Mathf.Round(trackLength * k / 12f) * 12f + 6f;
                int campSide = k < 0.5f ? -1 : 1;
                var p = new Vector3(campSide * (halfWidth + 6.3f), 0f, z);
                plannedCamps.Add(p);
                landmarkAreas.Add(new Vector4(p.x, p.z, 3.6f, 5f));
            }
            for (int s = -1; s <= 1; s += 2)
                landmarkAreas.Add(new Vector4(s * (halfWidth + 3f), trackLength - 10f, 1.6f, 1.6f));
        }

        private void BuildLandmarks(EnvChunk chunk, float z0, float z1, System.Random rng)
        {
            foreach (Vector3 t in plannedTowers)
            {
                if (t.z < z0 || t.z >= z1) continue;
                // Torre com a frente (estandarte) voltada para a pista.
                EnvMedieval.WatchTower(chunk, Ground(t.x, t.z) + Vector3.down * 0.2f, t.x > 0f ? -90f : 90f, 5.6f, rng);
                Record("WatchTower", Ground(t.x, t.z) + Vector3.down * 0.2f, t.x > 0f ? -90f : 90f, 1f, "Structures", SecOverview + ": torre lateral com estandarte");
            }
            foreach (Vector3 c in plannedCamps)
            {
                if (c.z < z0 || c.z >= z1) continue;
                float s = Mathf.Sign(c.x);
                Vector3 g = Ground(c.x, c.z);
                EnvMedieval.Tent(chunk, g + new Vector3(s * 0.6f, -0.1f, 1.6f), s > 0f ? -70f : 70f, 1f, rng);
                Record("Tent_Blue", g + new Vector3(s * 0.6f, -0.1f, 1.6f), s > 0f ? -70f : 70f, 1f, "Props", SecProps + ": tenda (acampamento)");
                Vector3 campfire = Ground(c.x - s * 1.6f, c.z - 1.6f) + Vector3.down * 0.05f;
                fire.Add(EnvMedieval.Campfire(chunk, campfire, rng), campfire.y);
                Record("Campfire", campfire, 0f, 1f, "Props", SecProps + ": fogueira (acampamento)");
                EnvMedieval.Cart(chunk, Ground(c.x + s * 0.4f, c.z - 4f), s > 0f ? 200f : 160f, rng);
                Record("Cart_Wood", Ground(c.x + s * 0.4f, c.z - 4f), s > 0f ? 200f : 160f, 1f, "Props", SecProps + ": carroça (acampamento)");
                EnvMedieval.Fence(chunk, Ground(c.x + s * 2.6f, c.z), 90f, 7f, rng);
                Record("Fence_Wood", Ground(c.x + s * 2.6f, c.z - 1.75f), 90f, new Vector3(0.875f, 1f, 1f), "Props", SecProps + ": cerca (acampamento)");
                Record("Fence_Wood", Ground(c.x + s * 2.6f, c.z + 1.75f), 90f, new Vector3(0.875f, 1f, 1f), "Props", SecProps + ": cerca (acampamento)");
                EnvProps.Crate(chunk, Ground(c.x - s * 0.2f, c.z + 4.2f), 0.75f, rng);
                Record("Crate_Wood", Ground(c.x - s * 0.2f, c.z + 4.2f), 20f, 1f, "Props", SecProps + ": caixa (acampamento)");
                EnvProps.Crate(chunk, Ground(c.x + s * 0.5f, c.z + 4.6f), 0.6f, rng);
                Record("Crate_Wood", Ground(c.x + s * 0.5f, c.z + 4.6f), 55f, 0.8f, "Props", SecProps + ": caixa (acampamento)");
                EnvMedieval.Lantern(chunk, Ground(c.x - s * 2.4f, c.z + 2.6f), 2.4f, rng);
                Record("Lantern", Ground(c.x - s * 2.4f, c.z + 2.6f), 0f, 1f, "Structures", "Elementos decorativos: lamparina (acampamento)");
            }
            // Estátuas de cavaleiro guardando a reta final, viradas para a pista.
            float zs = trackLength - 10f;
            if (zs >= z0 && zs < z1)
                for (int s = -1; s <= 1; s += 2)
                {
                    EnvMedieval.KnightStatue(chunk, Ground(s * (halfWidth + 3f), zs), s > 0 ? -90f : 90f, 0.95f, rng);
                    Record("KnightStatue", Ground(s * (halfWidth + 3f), zs), s > 0 ? -90f : 90f, 0.95f, "Structures", SecStructures + ": estátua de cavaleiro");
                }
        }

        // Paredões dos dois lados com cachoeiras congeladas e pinheiros na borda do planalto.
        // Colinas dos dois lados: afloramentos de rocha nevada em parte dos trechos (penhascos baixos como os da
        // vista geral) e floresta densa de pinheiros no alto, que fecha o horizonte entre a pista e as montanhas.
        private void BuildCanyon(EnvChunk chunk, float z0, float z1, float density, System.Random rng)
        {
            float zc = (z0 + z1) * 0.5f;
            int index = Mathf.RoundToInt((z0 - StartZ) / ChunkLength);
            for (int side = -1; side <= 1; side += 2)
            {
                // Afloramento rochoso alternando de lado a cada trecho.
                if ((index + (side > 0 ? 0 : 1)) % 2 == 0)
                {
                    float cliffX = side * (shape.CliffStart + shape.CliffWidth * 0.4f);
                    float top = Ground(side * (shape.CliffStart + shape.CliffWidth + 1f), zc).y;
                    float len = EnvMeshBuilder.Rand(rng, 9f, 16f);
                    float cz = EnvMeshBuilder.Rand(rng, z0 + len * 0.5f, Mathf.Max(z0 + len * 0.5f, z1 - len * 0.5f));
                    EnvRocks.CliffSection(chunk, new Vector3(cliffX, -0.3f, cz), len, top + 1.6f, new Vector3(-side, 0f, 0f), rng);
                    Record("Cliff_Rock", new Vector3(cliffX, -0.3f, cz), side > 0 ? 90f : -90f, new Vector3(len / 12f, (top + 1.6f) / 7f, 1f), "Terrain", SecOverview + ": afloramento rochoso nas colinas");
                }

                // Floresta: pinheiros nas colinas, mais altos e densos longe da pista.
                int forest = Mathf.RoundToInt((z1 - z0) * 0.55f * density);
                for (int i = 0; i < forest; i++)
                {
                    float x = side * EnvMeshBuilder.Rand(rng, shape.CliffStart + 1f, shape.CliffStart + shape.CliffWidth + 18f);
                    float z = EnvMeshBuilder.Rand(rng, z0, z1);
                    float ph = EnvMeshBuilder.Rand(rng, 4.5f, 8f);
                    bool heavy = rng.NextDouble() < 0.3;
                    EnvVegetation.Pine(chunk, Ground(x, z), ph, rng, heavy ? 2f : 1f);
                    Record(heavy ? "PineTree_Snow_Heavy" : "PineTree_Snow", Ground(x, z), (float)rng.NextDouble() * 360f, ph / (heavy ? 5.5f : 5f), "Nature", SecOverview + ": floresta nas colinas");
                }
            }

            // Cristais de gelo junto às rochas a cada 2 trechos.
            if (index % 2 == 1 && zc < trackLength + 10f)
            {
                int side = index % 4 == 1 ? 1 : -1;
                float x = side * (shape.CliffStart - 0.5f);
                EnvRocks.IceCrystals(chunk, Ground(x, zc + 2.5f), 1.6f, rng);
                EnvRocks.IceCrystals(chunk, Ground(x + side * 1.5f, zc - 2.8f), 1.2f, rng);
                Record("Ice_Crystal", Ground(x, zc + 2.5f), 0f, 1.6f / 1.4f, "Nature", SecOverview + ": cristais de gelo");
                Record("Ice_Crystal", Ground(x + side * 1.5f, zc - 2.8f), 40f, 1.2f / 1.4f, "Nature", SecOverview + ": cristais de gelo");
            }
        }

        // Arco da linha de chegada: duas torres pequenas e viga de pedra (o letreiro VITÓRIA é do LevelManager).
        private void BuildFinishArch(EnvChunk chunk, System.Random rng)
        {
            float z = trackLength;
            for (int side = -1; side <= 1; side += 2)
                EnvCastle.Tower(chunk, new Vector3(side * (halfWidth + 1.7f), 0f, z), 1.25f, 5.2f, 2.8f, rng, false);
            chunk.Matrix = Matrix4x4.identity;
            chunk.Masonry.BeveledBox(new Vector3(0f, 5.6f, z + 0.15f), new Vector3(halfWidth * 2f + 2.2f, 0.9f, 0.7f), 0.12f, EnvPalette.Stone);
            chunk.Opaque.NoSnow();
            chunk.Opaque.BeveledBox(new Vector3(0f, 6.12f, z + 0.15f), new Vector3(halfWidth * 2f + 2.3f, 0.2f, 0.85f), 0.08f, EnvPalette.Snow, rng, 0.03f);
            for (int side = -1; side <= 1; side += 2)
                chunk.Opaque.BeveledBox(new Vector3(side * (halfWidth * 0.55f), 5.6f, z - 0.25f), new Vector3(0.5f, 0.5f, 0.12f), 0.1f, EnvPalette.Gold);
        }

        private void BuildCastleSurroundings(EnvChunk chunk, float density, System.Random rng)
        {
            int pines = Mathf.RoundToInt(26 * density);
            for (int i = 0; i < pines; i++)
            {
                int side = rng.NextDouble() < 0.5 ? -1 : 1;
                float x = side * EnvMeshBuilder.Rand(rng, 24f, 40f);
                float z = CastleZ + EnvMeshBuilder.Rand(rng, -2f, 30f);
                float h = EnvMeshBuilder.Rand(rng, 4.5f, 8f);
                EnvVegetation.Pine(chunk, new Vector3(x, 3.2f, z), h, rng);
                Record("PineTree_Snow", new Vector3(x, 3.2f, z), (float)rng.NextDouble() * 360f, h / 5f, "Nature", SecCastle + ": pinheiros ao redor do castelo");
            }
            for (int side = -1; side <= 1; side += 2)
            {
                // Braseiros ao pé da escadaria (referência "Castelo final (entrada)"), estandartes e cristais por fora.
                Vector3 flame = EnvProps.Brazier(chunk, new Vector3(side * 4.7f, 0f, CastleZ - 7.2f), 1.6f, rng);
                fire.Add(flame, 0f);
                EnvProps.BannerPole(chunk, new Vector3(side * 6.8f, 0f, CastleZ - 9.5f), 4f, side * -12f, rng);
                EnvRocks.IceCrystals(chunk, new Vector3(side * 8.4f, 0f, CastleZ - 7.5f), 1.6f, rng);
                Record("FK_Castle_Torch", new Vector3(side * 4.7f, 0f, CastleZ - 7.2f), 0f, 1f, "Castle", SecCastle + ": braseiros ao pé da escadaria");
                Record("BannerPole", new Vector3(side * 6.8f, 0f, CastleZ - 9.5f), side * -12f, 4f / 3.6f, "Castle", SecCastle + ": estandartes da entrada");
                Record("Ice_Crystal", new Vector3(side * 8.4f, 0f, CastleZ - 7.5f), 0f, 1.6f / 1.4f, "Castle", SecCastle + ": cristais de gelo");
            }
        }

        // Folhas da porta em objetos próprios (pivô na dobradiça) para poderem abrir na vitória.
        private void BuildCastleDoors(EnvShadowMode shadows)
        {
            var root = new GameObject("CastleDoors").transform;
            root.SetParent(transform, false);
            Vector3 hingeLine = new Vector3(0f, 0f, CastleZ) + EnvCastle.GateOffset(CastlePlateau) + new Vector3(0f, 0f, 0.2f);
            var leaves = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? -1 : 1;
                var pivot = new GameObject(side < 0 ? "DoorLeft" : "DoorRight").transform;
                pivot.SetParent(root, false);
                pivot.localPosition = hingeLine + new Vector3(side * EnvCastle.GateOpening * 0.5f, 0f, 0f);
                var leaf = new EnvChunk();
                EnvCastle.DoorLeaf(leaf, EnvCastle.GateOpening * 0.5f, EnvCastle.GateArchTop, side);
                leaf.Build(pivot, "CastleDoor", shadows == EnvShadowMode.None ? EnvShadowMode.Large : shadows);
                foreach (MeshRenderer r in pivot.GetComponentsInChildren<MeshRenderer>())
                    if (r.shadowCastingMode == ShadowCastingMode.On) shadowCasters.Add(r);
                leaves[i] = pivot;
            }
            gateDoors = root.gameObject.AddComponent<EnvGateDoors>();
            gateDoors.Initialize(leaves[0], leaves[1]);
        }

        // Cordilheira 3D atrás e dos lados do castelo (a referência mostra picos nevados enormes emoldurando o
        // castelo). Picos facetados: base de rocha azulada e capa de neve com borda recortada. A neblina dá a
        // profundidade; o anel do EnvSkyBackdrop continua no horizonte mais distante.
        private void BuildMountainRange(EnvChunk chunk, float density, System.Random rng)
        {
            int peaks = Mathf.RoundToInt(Mathf.Lerp(9f, 16f, density));
            for (int i = 0; i < peaks; i++)
            {
                float t = peaks == 1 ? 0.5f : i / (float)(peaks - 1);
                // Arco em volta do castelo: laterais mais perto, fundo mais longe e mais alto.
                float angle = Mathf.Lerp(-80f, 80f, t) + EnvMeshBuilder.Rand(rng, -5f, 5f);
                float rad = angle * Mathf.Deg2Rad;
                // Dentro do far clip da câmera do jogo (140 m) quando o exército chega ao fim da pista.
                float distance = EnvMeshBuilder.Rand(rng, 62f, 80f);
                Vector3 p = new Vector3(Mathf.Sin(rad) * distance * 1.05f, -2f, CastleZ + 15f + Mathf.Cos(rad) * distance * 0.8f);
                if (Mathf.Abs(p.x) < 45f && p.z < CastleZ + 58f) p.z = CastleZ + 58f + EnvMeshBuilder.Rand(rng, 0f, 10f);
                float height = EnvMeshBuilder.Rand(rng, 34f, 58f) * Mathf.Lerp(0.8f, 1.15f, Mathf.Cos(rad));
                float radius = height * EnvMeshBuilder.Rand(rng, 0.55f, 0.75f);
                EnvRocks.MountainPeak(chunk, p, height, radius, rng);
                Record("MountainPeak", p, 0f, new Vector3(radius / 26f, height / 40f, radius / 26f), "Mountains", SecOverview + ": montanhas nevadas no horizonte");
                // Contraforte menor ao lado de cada pico (silhueta menos regular).
                Vector3 shoulder = p + new Vector3(EnvMeshBuilder.Rand(rng, -1f, 1f) * height * 0.5f, 0f, EnvMeshBuilder.Rand(rng, -10f, -4f));
                EnvRocks.MountainPeak(chunk, shoulder, height * 0.55f, height * 0.5f, rng);
                Record("MountainPeak", shoulder, 120f, new Vector3(height * 0.5f / 26f, height * 0.55f / 40f, height * 0.5f / 26f), "Mountains", SecOverview + ": contraforte das montanhas");
            }
        }

        private bool NearBrazier(float z)
        {
            float k = Mathf.Repeat(z, BraziersEvery);
            return k < 2.5f || k > BraziersEvery - 2.5f;
        }

        private Vector3 Ground(float x, float z) => new Vector3(x, EnvTerrain.Height(shape, x, z), z);

        private void OnDestroy()
        {
            if (quality != null) quality.TierChanged -= HandleTierChanged;
            // Malhas geradas pertencem a este cenário: libera ao trocar de fase.
            foreach (MeshFilter f in GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null || !f.sharedMesh.name.StartsWith("Chunk") && !f.sharedMesh.name.StartsWith("Castle")) continue;
                if (Application.isPlaying) Destroy(f.sharedMesh);
                else DestroyImmediate(f.sharedMesh);
            }
        }
    }
}
