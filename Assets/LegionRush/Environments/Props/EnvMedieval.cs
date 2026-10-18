using UnityEngine;

namespace LegionRush.Environments
{
    // Peças medievais do Reino Congelado que faltavam na folha de referência: cerca, carroça, tenda, fogueira,
    // lamparina, tocha, estátua de cavaleiro, arco, pilar, torre de observação e bloco de gelo.
    // Mesmo estilo do resto do kit (facetado, cor por vértice, neve nas faces de cima). As funções com fogo
    // devolvem a posição (mundo) da chama para o EnvFireFx.
    public static class EnvMedieval
    {
        // Cerca de madeira de "length" metros ao longo do X local, com neve nos postes.
        public static void Fence(EnvChunk chunk, Vector3 position, float yaw, float length, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, yaw, 0f), Vector3.one);
            EnvMeshBuilder o = chunk.Opaque;
            o.NoSnow();
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 1.4f) + 1);
            for (int i = 0; i < posts; i++)
            {
                float x = Mathf.Lerp(-length * 0.5f, length * 0.5f, i / (float)(posts - 1));
                float lean = EnvMeshBuilder.Rand(rng, -4f, 4f);
                Matrix4x4 saved = o.Matrix;
                o.Matrix = saved * Matrix4x4.TRS(new Vector3(x, 0f, 0f), Quaternion.Euler(0f, 0f, lean), Vector3.one);
                o.BeveledBox(new Vector3(0f, 0.55f, 0f), new Vector3(0.16f, 1.1f, 0.16f), 0.04f, EnvPalette.WoodDark);
                o.BeveledBox(new Vector3(0f, 1.14f, 0f), new Vector3(0.22f, 0.1f, 0.22f), 0.04f, EnvPalette.Snow, rng, 0.02f);
                o.Matrix = saved;
            }
            foreach (float y in new[] { 0.42f, 0.84f })
            {
                Matrix4x4 saved = o.Matrix;
                o.Matrix = saved * Matrix4x4.TRS(new Vector3(0f, y, 0.1f), Quaternion.Euler(0f, 0f, EnvMeshBuilder.Rand(rng, -1.5f, 1.5f)), Vector3.one);
                o.BeveledBox(Vector3.zero, new Vector3(length + 0.2f, 0.12f, 0.07f), 0.03f, EnvPalette.Wood);
                o.Matrix = saved;
            }
        }

        // Carroça de madeira com rodas, caixas e lona azul por cima da carga.
        public static void Cart(EnvChunk chunk, Vector3 position, float yaw, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, yaw, 0f), Vector3.one);
            EnvMeshBuilder o = chunk.Opaque;
            o.NoSnow();
            // Caçamba.
            o.BeveledBox(new Vector3(0f, 0.75f, 0f), new Vector3(1.4f, 0.12f, 2.2f), 0.03f, EnvPalette.Wood);
            for (int side = -1; side <= 1; side += 2)
            {
                o.BeveledBox(new Vector3(side * 0.67f, 1.0f, 0f), new Vector3(0.08f, 0.45f, 2.2f), 0.03f, EnvPalette.Wood);
                // Rodas (cilindros deitados): matriz girando o eixo Y para X.
                Matrix4x4 saved = o.Matrix;
                o.Matrix = saved * Matrix4x4.TRS(new Vector3(side * 0.8f, 0.48f, 0.35f), Quaternion.Euler(0f, 0f, 90f), Vector3.one);
                o.Frustum(new Vector3(0f, -0.06f, 0f), 0.48f, 0.48f, 0.12f, 10, EnvPalette.WoodDark, true, true);
                o.Frustum(new Vector3(0f, -0.09f, 0f), 0.12f, 0.12f, 0.18f, 6, EnvPalette.Iron, true, true);
                o.Matrix = saved;
            }
            o.BeveledBox(new Vector3(0f, 1.0f, 1.08f), new Vector3(1.4f, 0.45f, 0.08f), 0.03f, EnvPalette.Wood);
            o.BeveledBox(new Vector3(0f, 1.0f, -1.08f), new Vector3(1.4f, 0.45f, 0.08f), 0.03f, EnvPalette.Wood);
            // Varais.
            for (int side = -1; side <= 1; side += 2)
                o.BeveledBox(new Vector3(side * 0.4f, 0.62f, -1.75f), new Vector3(0.08f, 0.08f, 1.5f), 0.02f, EnvPalette.WoodDark);
            // Carga: lona azul abaulada + neve.
            o.BeveledBox(new Vector3(0f, 1.25f, 0.05f), new Vector3(1.25f, 0.55f, 1.9f), 0.22f, EnvPalette.RoyalBlue * 0.85f, rng, 0.05f);
            o.BeveledBox(new Vector3(0.05f, 1.55f, 0.1f), new Vector3(1f, 0.14f, 1.5f), 0.06f, EnvPalette.Snow, rng, 0.05f);
            EnvProps.Barrel(chunk, position + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.9f, 0f, -0.6f), 0.85f, rng);
        }

        // Tenda de lona azul e branca com mastro, estacas e neve no telhado.
        public static void Tent(EnvChunk chunk, Vector3 position, float yaw, float size, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, yaw, 0f), Vector3.one * size);
            EnvMeshBuilder o = chunk.Opaque;
            o.NoSnow();
            const float W = 2.4f, D = 2.8f, H = 2.1f;
            Color canvas = new Color(0.86f, 0.84f, 0.78f);
            Color stripe = EnvPalette.RoyalBlue;
            // Duas águas: cada lado é um quad inclinado (frente em -Z aberta, fundo fechado com triângulo).
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 a = new Vector3(side * W * 0.5f, 0f, -D * 0.5f), b = new Vector3(0f, H, -D * 0.5f);
                Vector3 c = new Vector3(0f, H, D * 0.5f), d = new Vector3(side * W * 0.5f, 0f, D * 0.5f);
                Vector3 inside = new Vector3(-side * 3f, -1f, 0f);
                o.ConvexFace(new[] { a, b, c, d }, inside, canvas);
                o.ConvexFace(new[] { a, d, c, b }, inside + new Vector3(side * 10f, 6f, 0f), canvas * 0.7f);
                // Faixa azul na barra e neve no alto.
                Vector3 a2 = Vector3.Lerp(a, b, 0.0f) + Vector3.up * 0.02f, b2 = Vector3.Lerp(a, b, 0.22f), c2 = Vector3.Lerp(d, c, 0.22f), d2 = Vector3.Lerp(d, c, 0f) + Vector3.up * 0.02f;
                Vector3 off = new Vector3(side * 0.03f, 0.03f, 0f);
                o.ConvexFace(new[] { a2 + off, b2 + off, c2 + off, d2 + off }, inside, stripe);
                Vector3 s0 = Vector3.Lerp(b, a, 0.35f) + off * 1.5f, s1 = b + off * 1.5f + Vector3.up * 0.05f, s2 = c + off * 1.5f + Vector3.up * 0.05f, s3 = Vector3.Lerp(c, d, 0.35f) + off * 1.5f;
                o.ConvexFace(new[] { s0, s1, s2, s3 }, inside, EnvPalette.Snow);
            }
            // Fundo fechado e entrada escura.
            o.ConvexFace(new[] { new Vector3(-W * 0.5f, 0f, D * 0.5f), new Vector3(0f, H, D * 0.5f), new Vector3(W * 0.5f, 0f, D * 0.5f) }, Vector3.zero, canvas * 0.85f);
            o.ConvexFace(new[] { new Vector3(-W * 0.32f, 0f, -D * 0.5f + 0.05f), new Vector3(0f, H * 0.7f, -D * 0.5f + 0.05f), new Vector3(W * 0.32f, 0f, -D * 0.5f + 0.05f) },
                new Vector3(0f, 0.5f, 1f), new Color(0.12f, 0.12f, 0.2f));
            // Mastro com flâmula.
            o.Frustum(new Vector3(0f, H - 0.1f, -D * 0.5f), 0.05f, 0.04f, 0.9f, 5, EnvPalette.WoodDark, false, true);
            chunk.Banner.Matrix = o.Matrix;
            EnvCastle.Pennant(chunk.Banner, new Vector3(0f, H + 0.78f, -D * 0.5f), 0.7f, 0.32f);
            // Estacas.
            for (int side = -1; side <= 1; side += 2)
                for (int k = -1; k <= 1; k += 2)
                    o.BeveledBox(new Vector3(side * (W * 0.5f + 0.25f), 0.12f, k * D * 0.4f), new Vector3(0.08f, 0.3f, 0.08f), 0.02f, EnvPalette.WoodDark);
        }

        // Fogueira: anel de pedras, toras cruzadas e chama. Retorna a posição da chama.
        public static Vector3 Campfire(EnvChunk chunk, Vector3 position, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, EnvMeshBuilder.Rand(rng, 0f, 360f), 0f), Vector3.one);
            EnvMeshBuilder o = chunk.Opaque;
            EnvMeshBuilder g = chunk.Glow;
            o.SetSnow(0.7f);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f;
                o.BeveledBox(new Vector3(Mathf.Cos(a) * 0.75f, 0.12f, Mathf.Sin(a) * 0.75f), new Vector3(0.36f, 0.26f, 0.3f), 0.08f, EnvPalette.StoneDark, rng, 0.05f);
            }
            o.NoSnow();
            for (int i = 0; i < 3; i++)
            {
                Matrix4x4 saved = o.Matrix;
                o.Matrix = saved * Matrix4x4.TRS(new Vector3(0f, 0.2f, 0f), Quaternion.Euler(0f, i * 60f, 18f), Vector3.one);
                o.BeveledBox(Vector3.zero, new Vector3(1.1f, 0.16f, 0.16f), 0.05f, EnvPalette.WoodDark);
                o.Matrix = saved;
            }
            g.Frustum(new Vector3(0f, 0.12f, 0f), 0.42f, 0.32f, 0.1f, 8, EnvPalette.Fire * 0.7f, false, true);
            Vector3 flame = new Vector3(0f, 0.2f, 0f);
            EnvProps.Flame(g, flame, 0.9f, rng);
            return o.Matrix.MultiplyPoint3x4(flame + Vector3.up * 0.4f);
        }

        // Poste de ferro com lamparina (vidro emissivo quente) e neve no chapéu.
        public static void Lantern(EnvChunk chunk, Vector3 position, float height, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, EnvMeshBuilder.Rand(rng, 0f, 360f), 0f), Vector3.one);
            EnvMeshBuilder o = chunk.Opaque;
            EnvMeshBuilder g = chunk.Glow;
            o.NoSnow();
            o.BeveledBox(new Vector3(0f, 0.15f, 0f), new Vector3(0.45f, 0.3f, 0.45f), 0.06f, EnvPalette.StoneDark);
            o.Frustum(new Vector3(0f, 0.3f, 0f), 0.07f, 0.05f, height - 0.3f, 6, EnvPalette.Iron, false, false);
            o.BeveledBox(new Vector3(0.35f, height - 0.05f, 0f), new Vector3(0.75f, 0.06f, 0.06f), 0.02f, EnvPalette.Iron);
            Vector3 lamp = new Vector3(0.65f, height - 0.45f, 0f);
            o.Frustum(lamp + Vector3.up * 0.36f, 0.24f, 0.04f, 0.2f, 6, EnvPalette.Iron, true, false);
            o.Frustum(lamp + Vector3.up * 0.5f, 0.2f, 0.02f, 0.1f, 6, EnvPalette.Snow, true, false);
            g.Frustum(lamp, 0.15f, 0.17f, 0.36f, 6, EnvPalette.WindowGlow * 0.9f, true, true);
            o.Frustum(lamp + Vector3.down * 0.04f, 0.19f, 0.17f, 0.05f, 6, EnvPalette.Iron, true, true);
        }

        // Tocha em haste de madeira com pano enrolado. Retorna a posição da chama.
        public static Vector3 Torch(EnvChunk chunk, Vector3 position, float height, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
            EnvMeshBuilder o = chunk.Opaque;
            EnvMeshBuilder g = chunk.Glow;
            o.NoSnow();
            o.Frustum(Vector3.zero, 0.08f, 0.06f, height, 6, EnvPalette.Wood, false, false);
            o.Frustum(new Vector3(0f, height - 0.1f, 0f), 0.1f, 0.13f, 0.28f, 6, EnvPalette.WoodDark, false, true);
            o.Frustum(new Vector3(0f, height + 0.1f, 0f), 0.14f, 0.14f, 0.05f, 6, EnvPalette.Gold, false, false);
            Vector3 flame = new Vector3(0f, height + 0.18f, 0f);
            EnvProps.Flame(g, flame, 0.42f, rng);
            return o.Matrix.MultiplyPoint3x4(flame + Vector3.up * 0.2f);
        }

        // Estátua de cavaleiro em pedra sobre pedestal: elmo, ombreiras, escudo e espada apoiada à frente.
        public static void KnightStatue(EnvChunk chunk, Vector3 position, float yaw, float scale, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, yaw, 0f), Vector3.one * scale);
            EnvMeshBuilder m = chunk.Masonry;
            EnvMeshBuilder o = chunk.Opaque;
            o.NoSnow();
            Color stone = EnvPalette.StoneLight;
            m.BeveledBox(new Vector3(0f, 0.4f, 0f), new Vector3(1.6f, 0.8f, 1.6f), 0.1f, EnvPalette.Stone);
            m.BeveledBox(new Vector3(0f, 0.88f, 0f), new Vector3(1.3f, 0.16f, 1.3f), 0.05f, EnvPalette.StoneLight);
            o.BeveledBox(new Vector3(0f, 0.5f, -0.81f), new Vector3(0.7f, 0.3f, 0.04f), 0.02f, EnvPalette.Gold);
            // Pernas, saia, tronco, ombreiras, cabeça com elmo e pluma.
            for (int side = -1; side <= 1; side += 2)
                o.BeveledBox(new Vector3(side * 0.18f, 1.35f, 0f), new Vector3(0.24f, 0.8f, 0.3f), 0.06f, stone);
            o.Frustum(new Vector3(0f, 1.55f, 0f), 0.42f, 0.34f, 0.45f, 8, stone * 0.95f, false, false);
            o.BeveledBox(new Vector3(0f, 2.25f, 0f), new Vector3(0.7f, 0.85f, 0.42f), 0.12f, stone);
            for (int side = -1; side <= 1; side += 2)
            {
                o.BeveledBox(new Vector3(side * 0.45f, 2.55f, 0f), new Vector3(0.32f, 0.22f, 0.45f), 0.08f, stone * 1.03f);
                o.BeveledBox(new Vector3(side * 0.46f, 2.15f, -0.08f), new Vector3(0.18f, 0.6f, 0.2f), 0.05f, stone);
            }
            o.Frustum(new Vector3(0f, 2.68f, 0f), 0.2f, 0.22f, 0.42f, 8, stone, false, true);
            o.BeveledBox(new Vector3(0f, 2.85f, -0.2f), new Vector3(0.3f, 0.05f, 0.04f), 0.01f, EnvPalette.StoneDark);
            o.BeveledBox(new Vector3(0f, 3.2f, 0.05f), new Vector3(0.08f, 0.32f, 0.38f), 0.03f, stone);
            // Escudo (com neve) à esquerda e espada com ponta no chão à frente.
            o.BeveledBox(new Vector3(-0.55f, 1.95f, -0.3f), new Vector3(0.12f, 0.85f, 0.6f), 0.06f, stone * 0.97f);
            o.BeveledBox(new Vector3(0f, 1.5f, -0.38f), new Vector3(0.1f, 1.3f, 0.04f), 0.02f, stone * 1.05f);
            o.BeveledBox(new Vector3(0f, 2.18f, -0.38f), new Vector3(0.42f, 0.08f, 0.08f), 0.02f, stone);
            // Neve nos ombros, cabeça e pedestal.
            o.BeveledBox(new Vector3(0f, 2.92f, 0f), new Vector3(0.48f, 0.08f, 0.44f), 0.03f, EnvPalette.Snow, rng, 0.02f);
            for (int side = -1; side <= 1; side += 2)
                o.BeveledBox(new Vector3(side * 0.45f, 2.68f, 0f), new Vector3(0.34f, 0.07f, 0.44f), 0.03f, EnvPalette.Snow, rng, 0.02f);
            o.BeveledBox(new Vector3(0.3f, 0.99f, 0.3f), new Vector3(0.7f, 0.08f, 0.6f), 0.03f, EnvPalette.Snow, rng, 0.03f);
        }

        // Arco medieval de pedra (dois pilares e aduelas) com neve no topo. Largura interna "span".
        public static void StoneArch(EnvChunk chunk, Vector3 position, float yaw, float span, float height, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, yaw, 0f), Vector3.one);
            EnvMeshBuilder m = chunk.Masonry;
            EnvMeshBuilder o = chunk.Opaque;
            o.NoSnow();
            float pier = 0.8f;
            float springY = height - span * 0.5f;
            for (int side = -1; side <= 1; side += 2)
                m.BeveledBox(new Vector3(side * (span * 0.5f + pier * 0.5f), springY * 0.5f, 0f), new Vector3(pier, springY, pier), 0.1f, EnvPalette.Stone);
            const int Stones = 9;
            float r = span * 0.5f + pier * 0.5f;
            for (int i = 0; i < Stones; i++)
            {
                float a = Mathf.PI * (i + 0.5f) / Stones;
                Vector3 c = new Vector3(-Mathf.Cos(a) * r, springY + Mathf.Sin(a) * r, 0f);
                Matrix4x4 saved = m.Matrix;
                m.Matrix = saved * Matrix4x4.TRS(c, Quaternion.Euler(0f, 0f, 90f - a * Mathf.Rad2Deg), Vector3.one);
                m.BeveledBox(Vector3.zero, new Vector3(r * Mathf.PI / Stones + 0.04f, pier, pier), 0.08f, i == Stones / 2 ? EnvPalette.StoneLight : EnvPalette.Stone);
                m.Matrix = saved;
                o.Matrix = saved * Matrix4x4.TRS(c, Quaternion.Euler(0f, 0f, 90f - a * Mathf.Rad2Deg), Vector3.one);
                if (Mathf.Abs(a - Mathf.PI * 0.5f) < 1.1f)
                    o.BeveledBox(new Vector3(0f, pier * 0.5f + 0.05f, 0f), new Vector3(r * Mathf.PI / Stones + 0.1f, 0.14f, pier + 0.1f), 0.05f, EnvPalette.Snow, rng, 0.02f);
                o.Matrix = saved;
            }
            o.BeveledBox(new Vector3(0f, springY + r + pier * 0.1f, -pier * 0.5f - 0.05f), new Vector3(0.6f, 0.7f, 0.12f), 0.08f, EnvPalette.Gold);
            o.BeveledBox(new Vector3(0f, springY + r + pier * 0.1f, -pier * 0.5f - 0.12f), new Vector3(0.42f, 0.52f, 0.06f), 0.05f, EnvPalette.RoyalBlue);
        }

        // Pilar de pedra com base, capitel e neve.
        public static void Pillar(EnvChunk chunk, Vector3 position, float height, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, EnvMeshBuilder.Rand(rng, 0f, 90f), 0f), Vector3.one);
            EnvMeshBuilder m = chunk.Masonry;
            EnvMeshBuilder o = chunk.Opaque;
            o.NoSnow();
            m.BeveledBox(new Vector3(0f, 0.25f, 0f), new Vector3(1.1f, 0.5f, 1.1f), 0.08f, EnvPalette.StoneDark);
            m.Frustum(new Vector3(0f, 0.5f, 0f), 0.42f, 0.38f, height - 0.9f, 8, EnvPalette.Stone, false, false);
            m.BeveledBox(new Vector3(0f, height - 0.2f, 0f), new Vector3(1f, 0.4f, 1f), 0.08f, EnvPalette.StoneLight);
            o.BeveledBox(new Vector3(0f, height + 0.05f, 0f), new Vector3(1.04f, 0.14f, 1.04f), 0.06f, EnvPalette.Snow, rng, 0.04f);
        }

        // Torre de observação: base de pedra, plataforma de madeira com parapeito, telhado azul e estandarte.
        public static void WatchTower(EnvChunk chunk, Vector3 position, float yaw, float height, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, yaw, 0f), Vector3.one);
            EnvMeshBuilder m = chunk.Masonry;
            EnvMeshBuilder o = chunk.Opaque;
            EnvMeshBuilder g = chunk.Glow;
            o.NoSnow();
            m.BeveledBox(new Vector3(0f, height * 0.5f, 0f), new Vector3(2.6f, height, 2.6f), 0.14f, EnvPalette.Stone);
            m.BeveledBox(new Vector3(0f, 0.3f, 0f), new Vector3(3f, 0.6f, 3f), 0.1f, EnvPalette.StoneDark);
            // Janela iluminada e estandarte na frente (-Z).
            g.BeveledBox(new Vector3(0f, height * 0.62f, -1.31f), new Vector3(0.45f, 0.8f, 0.06f), 0.04f, EnvPalette.WindowGlow * 0.55f);
            o.BeveledBox(new Vector3(0f, height * 0.32f + 1.2f, -1.32f), new Vector3(1.4f, 0.1f, 0.1f), 0.03f, EnvPalette.Gold);
            EnvCastle.HangingBanner(chunk.Banner, new Vector3(0f, height * 0.32f + 1.15f, -1.4f), 1.1f, 1.9f);
            // Topo de pedra com mísula e ameias (neve em cada merlão); postes de madeira sustentam o telhado.
            float py = height;
            m.BeveledBox(new Vector3(0f, py + 0.1f, 0f), new Vector3(3.4f, 0.3f, 3.4f), 0.08f, EnvPalette.StoneLight);
            for (int k = 0; k < 4; k++)
            {
                Quaternion r = Quaternion.Euler(0f, k * 90f, 0f);
                Matrix4x4 savedM = m.Matrix, saved = o.Matrix;
                m.Matrix = savedM * Matrix4x4.TRS(Vector3.zero, r, Vector3.one);
                o.Matrix = saved * Matrix4x4.TRS(Vector3.zero, r, Vector3.one);
                m.BeveledBox(new Vector3(0f, py + 0.45f, -1.55f), new Vector3(3.4f, 0.4f, 0.3f), 0.05f, EnvPalette.Stone);
                for (int i = -1; i <= 1; i++)
                {
                    m.BeveledBox(new Vector3(i * 1.15f, py + 0.9f, -1.55f), new Vector3(0.62f, 0.5f, 0.32f), 0.05f, EnvPalette.Stone);
                    o.BeveledBox(new Vector3(i * 1.15f, py + 1.18f, -1.55f), new Vector3(0.68f, 0.1f, 0.38f), 0.03f, EnvPalette.Snow, rng, 0.02f);
                }
                o.BeveledBox(new Vector3(-1.45f, py + 1.65f, -1.45f), new Vector3(0.16f, 1.3f, 0.16f), 0.03f, EnvPalette.WoodDark);
                m.Matrix = savedM;
                o.Matrix = saved;
            }
            // Telhado piramidal azul com neve e ponta dourada.
            float roofY = py + 2.25f;
            o.Frustum(new Vector3(0f, roofY, 0f), 2.9f, 0f, 2.2f, 4, EnvPalette.RoofBlue, true, false, false, Mathf.PI * 0.25f);
            o.Frustum(new Vector3(0f, roofY + 0.55f, 0f), 2.25f, 0f, 1.7f, 4, EnvPalette.Snow, false, false, false, Mathf.PI * 0.25f + 0.02f, rng, 0.08f);
            o.Frustum(new Vector3(0f, roofY + 2.1f, 0f), 0.1f, 0f, 0.7f, 5, EnvPalette.Gold, true, false);
            o.BeveledBox(new Vector3(0f, py + 1.3f, 0f), new Vector3(0.25f, 2.2f, 0.25f), 0.04f, EnvPalette.WoodDark);
        }

        // Bloco de gelo translúcido (facetado) com neve por cima.
        public static void IceBlock(EnvChunk chunk, Vector3 position, float size, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(EnvMeshBuilder.Rand(rng, -6f, 6f), EnvMeshBuilder.Rand(rng, 0f, 360f), EnvMeshBuilder.Rand(rng, -6f, 6f)), Vector3.one);
            chunk.Ice.BeveledBox(new Vector3(0f, size * 0.45f, 0f), new Vector3(size, size * 0.9f, size * 0.8f), size * 0.12f, EnvPalette.Ice, rng, size * 0.08f);
            chunk.Opaque.NoSnow();
            chunk.Opaque.BeveledBox(new Vector3(0f, size * 0.92f, 0f), new Vector3(size * 0.8f, size * 0.12f, size * 0.6f), size * 0.05f, EnvPalette.Snow, rng, size * 0.04f);
        }
    }
}
