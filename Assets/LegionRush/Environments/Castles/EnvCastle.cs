using UnityEngine;

namespace LegionRush.Environments
{
    // Arquitetura do Reino Congelado: muralha com ameias, torres de telhado cônico azul, portão com
    // luz azul, escadaria e o castelo de fundo. Pedra em alvenaria texturizada; neve como camadas brancas.
    public static class EnvCastle
    {
        // Muralha reta de "length" metros ao longo do eixo X local, com ameias e neve no topo.
        public static void Wall(EnvChunk chunk, Matrix4x4 frame, float length, float height, float thickness, System.Random rng,
            bool merlons = true, bool endTowers = false)
        {
            if (endTowers) WallEndTowers(chunk, frame, length, height, thickness, rng);
            chunk.Matrix = frame;
            EnvMeshBuilder m = chunk.Masonry;
            EnvMeshBuilder o = chunk.Opaque;
            o.NoSnow();
            m.BeveledBox(new Vector3(0f, height * 0.5f, 0f), new Vector3(length, height, thickness), 0.12f, EnvPalette.Stone);
            // Cornija.
            m.BeveledBox(new Vector3(0f, height + 0.12f, 0f), new Vector3(length + 0.2f, 0.25f, thickness + 0.3f), 0.08f, EnvPalette.StoneLight);
            float top = height + 0.25f;
            if (merlons)
            {
                const float MerlonW = 0.9f, Gap = 0.7f;
                int count = Mathf.Max(1, Mathf.FloorToInt((length + Gap) / (MerlonW + Gap)));
                float used = count * MerlonW + (count - 1) * Gap;
                for (int i = 0; i < count; i++)
                {
                    float x = -used * 0.5f + MerlonW * 0.5f + i * (MerlonW + Gap);
                    m.BeveledBox(new Vector3(x, top + 0.4f, 0f), new Vector3(MerlonW, 0.8f, thickness + 0.1f), 0.1f, EnvPalette.Stone);
                    o.BeveledBox(new Vector3(x, top + 0.86f, 0f), new Vector3(MerlonW + 0.12f, 0.2f, thickness + 0.22f), 0.08f, EnvPalette.Snow, rng, 0.03f);
                }
            }
            o.BeveledBox(new Vector3(0f, top + 0.06f, 0f), new Vector3(length + 0.1f, 0.16f, thickness + 0.2f), 0.07f, EnvPalette.Snow, rng, 0.02f);
        }

        // Torres quadradas mais altas nas pontas da muralha, com ameias e neve (referência 04_muralha).
        private static void WallEndTowers(EnvChunk chunk, Matrix4x4 frame, float length, float height, float thickness, System.Random rng)
        {
            chunk.Matrix = frame;
            EnvMeshBuilder m = chunk.Masonry;
            EnvMeshBuilder o = chunk.Opaque;
            float w = thickness + 0.6f;
            float h = height + 2.3f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (length * 0.5f + w * 0.3f);
                m.BeveledBox(new Vector3(x, h * 0.5f, 0f), new Vector3(w, h, w), 0.12f, EnvPalette.Stone);
                m.BeveledBox(new Vector3(x, h + 0.12f, 0f), new Vector3(w + 0.2f, 0.25f, w + 0.2f), 0.06f, EnvPalette.StoneLight);
                for (int i = -1; i <= 1; i += 2)
                    for (int j = -1; j <= 1; j += 2)
                    {
                        m.BeveledBox(new Vector3(x + i * (w * 0.5f - 0.2f), h + 0.6f, j * (w * 0.5f - 0.2f)), new Vector3(0.45f, 0.7f, 0.45f), 0.06f, EnvPalette.Stone);
                        o.BeveledBox(new Vector3(x + i * (w * 0.5f - 0.2f), h + 1f, j * (w * 0.5f - 0.2f)), new Vector3(0.5f, 0.14f, 0.5f), 0.04f, EnvPalette.Snow, rng, 0.02f);
                    }
                o.BeveledBox(new Vector3(x, h + 0.28f, 0f), new Vector3(w, 0.12f, w), 0.04f, EnvPalette.Snow, rng, 0.03f);
            }
        }

        // Torre redonda: corpo em alvenaria, anel de ameias, telhado cônico azul com ponta dourada e flâmula.
        public static void Tower(EnvChunk chunk, Vector3 basePosition, float radius, float height, float roofHeight, System.Random rng,
            bool windows = true, bool pennant = true)
        {
            chunk.Matrix = Matrix4x4.TRS(basePosition, Quaternion.Euler(0f, EnvMeshBuilder.Rand(rng, 0f, 30f), 0f), Vector3.one);
            EnvMeshBuilder m = chunk.Masonry;
            EnvMeshBuilder o = chunk.Opaque;
            EnvMeshBuilder g = chunk.Glow;
            o.NoSnow();
            const int Segments = 12;
            m.Frustum(Vector3.zero, radius * 1.08f, radius, height, Segments, EnvPalette.Stone, false, false);
            // Mísula + parapeito com ameias.
            m.Frustum(new Vector3(0f, height, 0f), radius, radius * 1.18f, 0.5f, Segments, EnvPalette.StoneLight, false, false);
            float ringR = radius * 1.18f;
            m.Frustum(new Vector3(0f, height + 0.5f, 0f), ringR, ringR, 0.6f, Segments, EnvPalette.Stone, false, true);
            for (int i = 0; i < Segments; i += 2)
            {
                float a = (i + 0.5f) * Mathf.PI * 2f / Segments;
                Vector3 p = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (ringR - 0.15f) + Vector3.up * (height + 1.4f);
                Quaternion face = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));
                Matrix4x4 local = Matrix4x4.TRS(p, face, Vector3.one);
                Matrix4x4 saved = m.Matrix;
                m.Matrix = saved * local;
                m.BeveledBox(new Vector3(0f, -0.15f, 0f), new Vector3(ringR * 0.42f, 0.7f, 0.35f), 0.07f, EnvPalette.Stone);
                m.Matrix = saved;
                o.Matrix = saved * local;
                o.BeveledBox(new Vector3(0f, 0.24f, 0f), new Vector3(ringR * 0.46f, 0.16f, 0.45f), 0.06f, EnvPalette.Snow);
                o.Matrix = saved;
            }
            // Telhado cônico com neve na beirada e ponta dourada.
            float roofBase = height + 1.1f;
            o.Frustum(new Vector3(0f, roofBase, 0f), ringR * 1.08f, 0f, roofHeight, 14, EnvPalette.RoofBlue, true, false, true);
            o.Frustum(new Vector3(0f, roofBase - 0.05f, 0f), ringR * 1.12f, ringR * 0.86f, roofHeight * 0.18f, 14, EnvPalette.Snow, false, false, true);
            o.Frustum(new Vector3(0f, roofBase + roofHeight - 0.15f, 0f), 0.12f, 0.02f, 1.1f, 6, EnvPalette.Gold, false, true);
            o.BeveledBox(new Vector3(0f, roofBase + roofHeight + 0.05f, 0f), Vector3.one * 0.32f, 0.12f, EnvPalette.Gold);
            if (pennant)
            {
                float poleTop = roofBase + roofHeight + 1.9f;
                o.Frustum(new Vector3(0f, roofBase + roofHeight + 0.1f, 0f), 0.05f, 0.04f, 1.9f, 5, EnvPalette.Iron, false, true);
                Pennant(chunk.Banner, new Vector3(0f, poleTop - 0.05f, 0f), 1.3f, 0.6f);
            }
            if (windows)
            {
                for (int w = 0; w < 3; w++)
                {
                    float a = (w * 120f + 30f) * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    float wy = height * (w == 1 ? 0.45f : 0.7f);
                    Matrix4x4 saved = g.Matrix;
                    g.Matrix = saved * Matrix4x4.TRS(dir * (radius * 1.02f) + Vector3.up * wy, Quaternion.LookRotation(dir), Vector3.one);
                    g.BeveledBox(Vector3.zero, new Vector3(0.38f, 0.75f, 0.16f), 0.06f, EnvPalette.WindowGlow * 0.55f);
                    g.Matrix = saved;
                }
            }
        }

        // Flâmula triangular presa a um mastro vertical (peso do vento cresce com a distância do mastro).
        public static void Pennant(EnvMeshBuilder banner, Vector3 attach, float length, float height)
        {
            banner.Sheet(attach + Vector3.down * height, Vector3.right * length, Vector3.up * height, 6, 2,
                (u, v) => new Color(1f, 1f, 1f, u),
                (u, v) => new Vector2(u, 0.5f + (v - 0.5f) * (1f - u * 0.92f)));
        }

        // Estandarte pendurado (tecido azul com flor-de-lis, ponta em V) sob uma barra horizontal.
        // origin = centro da barra; o tecido cai em -Y no plano XY local.
        public static void HangingBanner(EnvMeshBuilder banner, Vector3 topCenter, float width, float height)
        {
            banner.Sheet(topCenter + new Vector3(-width * 0.5f, -height, 0f), Vector3.right * width, Vector3.up * height, 4, 8,
                (u, v) => new Color(1f, 1f, 1f, Mathf.Pow(1f - v, 1.3f) * 0.7f),
                (u, v) => new Vector2(u, Mathf.Lerp(Mathf.Abs(u - 0.5f) * 2f * 0.2f, 1f, v)));
        }

        // Portão em arco com luz azul (passagem selada por magia), entre duas torres.
        public static void Gatehouse(EnvChunk chunk, Vector3 basePosition, float width, float height, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(basePosition, Quaternion.identity, Vector3.one);
            EnvMeshBuilder m = chunk.Masonry;
            EnvMeshBuilder o = chunk.Opaque;
            EnvMeshBuilder g = chunk.Glow;
            float pier = 1.6f;
            float opening = width - pier * 2f;
            float depth = 3f;
            // Pilares e lintel.
            m.BeveledBox(new Vector3(-(opening + pier) * 0.5f, height * 0.5f, 0f), new Vector3(pier, height, depth), 0.12f, EnvPalette.Stone);
            m.BeveledBox(new Vector3((opening + pier) * 0.5f, height * 0.5f, 0f), new Vector3(pier, height, depth), 0.12f, EnvPalette.Stone);
            float archTop = height * 0.72f;
            m.BeveledBox(new Vector3(0f, (archTop + height) * 0.5f, 0f), new Vector3(opening + 0.02f, height - archTop, depth), 0.12f, EnvPalette.Stone);
            // Arco: aduelas em degrau.
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.Lerp(10f, 80f, i / 5f) * Mathf.Deg2Rad;
                float x = Mathf.Cos(a) * opening * 0.5f;
                float y = archTop - opening * 0.5f * (1f - Mathf.Sin(a)) * 0.6f;
                float h = archTop - y;
                m.BeveledBox(new Vector3(x - 0.2f, y + h * 0.5f, 0f), new Vector3(0.5f, h + 0.05f, depth), 0.06f, EnvPalette.Stone);
                m.BeveledBox(new Vector3(-x + 0.2f, y + h * 0.5f, 0f), new Vector3(0.5f, h + 0.05f, depth), 0.06f, EnvPalette.Stone);
            }
            // Interior escuro com brilho azul (aparece quando as folhas da porta abrem; as folhas são montadas à
            // parte por DoorLeaf para poderem girar). Moldura de pedra clara recuada em volta do vão.
            o.BeveledBox(new Vector3(0f, archTop * 0.5f, 1.2f), new Vector3(opening, archTop, 0.2f), 0.05f, new Color(0.05f, 0.08f, 0.2f));
            g.BeveledBox(new Vector3(0f, archTop * 0.35f, 1.05f), new Vector3(opening * 0.7f, archTop * 0.6f, 0.1f), 0.05f, EnvPalette.GateGlow * 0.35f);
            for (int side = -1; side <= 1; side += 2)
                m.BeveledBox(new Vector3(side * (opening * 0.5f + 0.12f), archTop * 0.5f, -depth * 0.5f + 0.05f), new Vector3(0.34f, archTop, 0.3f), 0.08f, EnvPalette.StoneLight);
            // Ameias e neve.
            Matrix4x4 top = Matrix4x4.TRS(basePosition + Vector3.up * height, Quaternion.identity, Vector3.one);
            Wall(chunk, top, width + 0.4f, 0.01f, depth + 0.2f, rng);
            // Mastro com flâmula azul no alto do portão (referência 05_portao).
            chunk.Matrix = Matrix4x4.TRS(basePosition + Vector3.up * (height + 1.1f), Quaternion.identity, Vector3.one);
            chunk.Opaque.Frustum(Vector3.zero, 0.07f, 0.05f, 2.4f, 6, EnvPalette.Iron, true, true);
            chunk.Opaque.BeveledBox(new Vector3(0f, 2.45f, 0f), Vector3.one * 0.18f, 0.06f, EnvPalette.Gold);
            Pennant(chunk.Banner, new Vector3(0f, 2.35f, 0f), 1.5f, 0.75f);
            // Moldura dourada com brasão.
            chunk.Matrix = Matrix4x4.TRS(basePosition, Quaternion.identity, Vector3.one);
            o.BeveledBox(new Vector3(0f, archTop + 0.9f, -depth * 0.5f - 0.05f), new Vector3(1.4f, 1.6f, 0.2f), 0.15f, EnvPalette.Gold);
            HangingBanner(chunk.Banner, new Vector3(0f, archTop + 1.55f, -depth * 0.5f - 0.2f), 1.1f, 1.25f);
        }

        // Vão do portão do castelo, para montar as folhas da porta (DoorLeaf) no lugar certo.
        public const float GateWidth = 9f;
        public const float GateHeight = 8f;
        public const float GateOpening = GateWidth - 3.2f;
        public const float GateArchTop = GateHeight * 0.72f;
        public static Vector3 GateOffset(float plateauHeight) => new Vector3(0f, plateauHeight, 1.5f);

        // Folha da porta: madeira azul em tábuas, duas cintas douradas com cravos, meio brasão e argola.
        // Coordenadas locais com a dobradiça na origem; a folha se estende para o centro (-side em X), frente em -Z.
        // side = -1 folha esquerda, +1 direita.
        public static void DoorLeaf(EnvChunk chunk, float width, float height, int side)
        {
            chunk.Matrix = Matrix4x4.identity;
            EnvMeshBuilder o = chunk.Opaque;
            o.NoSnow();
            float dir = -side;
            Color wood = new Color(0.13f, 0.27f, 0.68f);
            Color woodDark = new Color(0.08f, 0.17f, 0.46f);
            o.BeveledBox(new Vector3(dir * width * 0.5f, height * 0.5f, 0f), new Vector3(width - 0.03f, height, 0.24f), 0.05f, wood);
            // Tábuas (frisos verticais mais escuros).
            for (int i = 1; i < 4; i++)
                o.Box(new Vector3(dir * width * i / 4f, height * 0.5f, -0.125f), new Vector3(0.05f, height * 0.96f, 0.03f), woodDark);
            // Cintas douradas com cravos.
            foreach (float band in new[] { 0.2f, 0.78f })
            {
                float y = height * band;
                o.BeveledBox(new Vector3(dir * width * 0.5f, y, -0.15f), new Vector3(width - 0.12f, 0.2f, 0.06f), 0.02f, EnvPalette.Gold);
                for (int k = 0; k < 4; k++)
                    o.BeveledBox(new Vector3(dir * (0.25f + k * (width - 0.5f) / 3f), y, -0.19f), new Vector3(0.11f, 0.11f, 0.06f), 0.03f, EnvPalette.Gold * 0.85f);
            }
            // Friso dourado no encontro das folhas e meio brasão (os dois formam um escudo inteiro fechados).
            o.BeveledBox(new Vector3(dir * (width - 0.06f), height * 0.5f, -0.15f), new Vector3(0.1f, height * 0.98f, 0.05f), 0.02f, EnvPalette.Gold);
            float cy = height * 0.5f;
            o.BeveledBox(new Vector3(dir * (width - 0.36f), cy + 0.1f, -0.17f), new Vector3(0.62f, 0.9f, 0.06f), 0.06f, EnvPalette.Gold);
            o.BeveledBox(new Vector3(dir * (width - 0.36f), cy + 0.12f, -0.2f), new Vector3(0.46f, 0.72f, 0.04f), 0.05f, EnvPalette.RoyalBlue);
            o.BeveledBox(new Vector3(dir * (width - 0.2f), cy - 0.45f, -0.17f), new Vector3(0.3f, 0.3f, 0.06f), 0.06f, EnvPalette.Gold);
            // Argola.
            o.Frustum(new Vector3(dir * (width - 0.75f), cy - 0.55f, -0.18f), 0.16f, 0.16f, 0.04f, 8, EnvPalette.Iron, false, true);
        }

        // Escadaria (degraus de pedra com neve nas bordas), subindo em +Z.
        public static void Stairs(EnvChunk chunk, Vector3 basePosition, float width, int steps, float rise, float run, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(basePosition, Quaternion.identity, Vector3.one);
            EnvMeshBuilder m = chunk.Masonry;
            EnvMeshBuilder o = chunk.Opaque;
            o.NoSnow();
            for (int i = 0; i < steps; i++)
            {
                float y = (i + 1) * rise;
                float z = i * run;
                m.BeveledBox(new Vector3(0f, y * 0.5f, z + run * 0.5f), new Vector3(width, y, run), 0.06f, i % 2 == 0 ? EnvPalette.StoneLight : EnvPalette.Stone);
                // Neve acumulada nas pontas dos degraus.
                for (int side = -1; side <= 1; side += 2)
                    o.BeveledBox(new Vector3(side * (width * 0.5f - 0.35f), y + 0.06f, z + run * 0.5f), new Vector3(0.8f, 0.16f, run * 0.9f), 0.06f, EnvPalette.Snow, rng, 0.04f);
            }
            // Balaustradas laterais.
            float totalRun = steps * run, totalRise = steps * rise;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < steps; i += 2)
                {
                    float y = (i + 1) * rise;
                    m.BeveledBox(new Vector3(side * (width * 0.5f + 0.35f), y * 0.5f + 0.45f, i * run + run), new Vector3(0.7f, y + 0.9f, run * 2f), 0.08f, EnvPalette.Stone);
                }
                o.BeveledBox(new Vector3(side * (width * 0.5f + 0.35f), totalRise + 0.95f, totalRun - run * 0.5f), new Vector3(0.85f, 0.2f, run * 1.2f), 0.06f, EnvPalette.Snow);
            }
        }

        // Castelo de fundo: torre de menagem central com vitral azul, torres de vários tamanhos, muralhas,
        // portão iluminado, escadaria e estandartes grandes na fachada. origin = centro da frente do portão (no chão).
        public static void Castle(EnvChunk chunk, Vector3 origin, float plateauHeight, System.Random rng)
        {
            float py = plateauHeight;
            Vector3 p0 = origin + Vector3.up * py;
            EnvMeshBuilder m = chunk.Masonry;
            EnvMeshBuilder o = chunk.Opaque;
            EnvMeshBuilder g = chunk.Glow;

            // Plataforma rochosa sob o castelo.
            chunk.Matrix = Matrix4x4.TRS(origin, Quaternion.identity, Vector3.one);
            o.SetSnow(0.55f);
            o.BeveledBox(new Vector3(0f, py * 0.5f - 0.2f, 22f), new Vector3(70f, py + 0.4f, 44f), 0.8f, EnvPalette.Cliff, rng, 0.4f);
            o.NoSnow();

            // Escadaria do chão até o portão.
            int steps = Mathf.Max(4, Mathf.RoundToInt(py / 0.32f));
            float rise = py / steps;
            Stairs(chunk, origin + new Vector3(0f, 0f, -steps * 0.55f), 7f, steps, rise, 0.55f, rng);

            // Portão principal.
            Gatehouse(chunk, origin + GateOffset(py), GateWidth, GateHeight, rng);

            // Muralhas frontais.
            for (int side = -1; side <= 1; side += 2)
            {
                Wall(chunk, Matrix4x4.TRS(p0 + new Vector3(side * 12f, 0f, 2f), Quaternion.identity, Vector3.one), 15f, 6.5f, 2.2f, rng);
                Tower(chunk, p0 + new Vector3(side * 6f, 0f, 1.5f), 2.1f, 10f, 4.6f, rng);
                Tower(chunk, p0 + new Vector3(side * 19.5f, 0f, 2.5f), 2.6f, 11f, 5.4f, rng);
                // Estandartes grandes pendurados na muralha.
                chunk.Matrix = Matrix4x4.TRS(p0 + new Vector3(side * 12f, 0f, 2f - 1.25f), Quaternion.identity, Vector3.one);
                o.BeveledBox(new Vector3(0f, 6.1f, 0f), new Vector3(2.4f, 0.22f, 0.22f), 0.06f, EnvPalette.Gold);
                HangingBanner(chunk.Banner, new Vector3(0f, 6f, -0.15f), 2.1f, 4.2f);
            }

            // Torre de menagem.
            Vector3 keep = p0 + new Vector3(0f, 0f, 14f);
            chunk.Matrix = Matrix4x4.TRS(keep, Quaternion.identity, Vector3.one);
            m.BeveledBox(new Vector3(0f, 9f, 0f), new Vector3(14f, 18f, 10f), 0.2f, EnvPalette.Stone);
            // Vitral em losango com flor-de-lis luminosa.
            // Moldura dourada atrás e vitral azul na frente (a moldura aparece só como aro).
            o.BeveledBox(new Vector3(0f, 11f, -5.05f), new Vector3(3.9f, 6.1f, 0.15f), 0.2f, EnvPalette.Gold);
            g.BeveledBox(new Vector3(0f, 11f, -5.2f), new Vector3(3.3f, 5.5f, 0.2f), 0.4f, EnvPalette.GateGlow * 0.6f);
            o.BeveledBox(new Vector3(0f, 11.2f, -5.33f), new Vector3(1.1f, 1.5f, 0.06f), 0.15f, EnvPalette.Gold);
            Wall(chunk, Matrix4x4.TRS(keep + new Vector3(0f, 18f, -4.6f), Quaternion.identity, Vector3.one), 14.4f, 0.01f, 1.2f, rng);
            for (int side = -1; side <= 1; side += 2)
            {
                Tower(chunk, keep + new Vector3(side * 7.2f, 0f, -4.8f), 2.2f, 21f, 6.5f, rng);
                Tower(chunk, keep + new Vector3(side * 4.2f, 18f, 2f), 1.4f, 4.5f, 3.6f, rng, false);
            }
            Tower(chunk, keep + new Vector3(0f, 18f, 1f), 2.4f, 7f, 7.5f, rng);

            // Cachoeiras congeladas descendo da plataforma (as muralhas ficam livres para os estandartes).
            for (int side = -1; side <= 1; side += 2)
            {
                EnvRocks.FrozenWaterfall(chunk, origin + new Vector3(side * 27f, py, 0.5f), 4.5f, py, Vector3.back, rng);
            }
        }
    }
}
