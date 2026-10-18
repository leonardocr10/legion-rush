using UnityEngine;

namespace LegionRush.Environments
{
    // Decoração da pista: braseiros em pilar de pedra, estandartes em mastro, mureta de blocos, caixas e barris.
    public static class EnvProps
    {
        // Pilar de pedra com braseiro de ferro/dourado e chama estilizada. Retorna a posição (mundo) da chama.
        public static Vector3 Brazier(EnvChunk chunk, Vector3 position, float height, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, EnvMeshBuilder.Rand(rng, -6f, 6f), 0f), Vector3.one);
            EnvMeshBuilder m = chunk.Masonry;
            EnvMeshBuilder o = chunk.Opaque;
            EnvMeshBuilder g = chunk.Glow;
            o.NoSnow();
            float w = 0.95f;
            // Base larga, fuste, capitel.
            m.BeveledBox(new Vector3(0f, 0.25f, 0f), new Vector3(w + 0.35f, 0.5f, w + 0.35f), 0.1f, EnvPalette.StoneDark);
            m.BeveledBox(new Vector3(0f, height * 0.5f, 0f), new Vector3(w, height - 0.2f, w), 0.1f, EnvPalette.Stone);
            m.BeveledBox(new Vector3(0f, height - 0.05f, 0f), new Vector3(w + 0.3f, 0.3f, w + 0.3f), 0.08f, EnvPalette.StoneLight);
            // Placa azul com brasão dourado em duas faces.
            for (int f = 0; f < 4; f++)
            {
                Quaternion r = Quaternion.Euler(0f, f * 90f, 0f);
                Vector3 n = r * Vector3.back;
                Matrix4x4 saved = o.Matrix;
                o.Matrix = saved * Matrix4x4.TRS(n * (w * 0.5f + 0.02f) + Vector3.up * (height * 0.55f), r, Vector3.one);
                o.BeveledBox(Vector3.zero, new Vector3(0.5f, 0.75f, 0.06f), 0.03f, EnvPalette.RoyalBlue);
                o.BeveledBox(new Vector3(0f, 0.05f, -0.04f), new Vector3(0.18f, 0.3f, 0.04f), 0.02f, EnvPalette.Gold);
                o.Matrix = saved;
            }
            // Neve nos cantos do capitel.
            o.BeveledBox(new Vector3(EnvMeshBuilder.Rand(rng, -0.2f, 0.2f), height + 0.12f, EnvMeshBuilder.Rand(rng, -0.2f, 0.2f)),
                new Vector3(w + 0.38f, 0.14f, w + 0.38f), 0.06f, EnvPalette.Snow, rng, 0.04f);
            // Braseiro: tigela de ferro com aro dourado.
            float bowlY = height + 0.15f;
            o.Frustum(new Vector3(0f, bowlY, 0f), 0.3f, 0.62f, 0.4f, 10, EnvPalette.Iron, true, false);
            o.Frustum(new Vector3(0f, bowlY + 0.38f, 0f), 0.66f, 0.66f, 0.1f, 10, EnvPalette.Gold, false, true);
            // Brasas + chama (emissivas).
            g.Frustum(new Vector3(0f, bowlY + 0.4f, 0f), 0.52f, 0.4f, 0.08f, 8, EnvPalette.Fire * 0.5f, false, true);
            Vector3 flameBase = new Vector3(0f, bowlY + 0.45f, 0f);
            Flame(g, flameBase, 1f, rng);
            return chunk.Opaque.Matrix.MultiplyPoint3x4(flameBase + Vector3.up * 0.45f);
        }

        // Chama estilizada: corpo laranja-avermelhado, línguas inclinadas em volta e miolo amarelo. Cores abaixo
        // de 1 porque o material Glow multiplica a emissão (acima disso o bloom deixa tudo amarelo-chapado).
        public static void Flame(EnvMeshBuilder g, Vector3 basePosition, float size, System.Random rng)
        {
            Matrix4x4 saved = g.Matrix;
            Color outer = EnvPalette.Fire * 0.5f;
            Color mid = Color.Lerp(EnvPalette.Fire, EnvPalette.FireCore, 0.35f) * 0.55f;
            Color core = EnvPalette.FireCore * 0.62f;
            g.Frustum(basePosition, 0.4f * size, 0f, 1.05f * size, 7, outer, true, false, false, (float)rng.NextDouble() * 6f, rng, 0.25f);
            int tongues = 4;
            for (int i = 0; i < tongues; i++)
            {
                float a = (i + EnvMeshBuilder.Rand(rng, -0.2f, 0.2f)) * 360f / tongues;
                Quaternion tilt = Quaternion.Euler(0f, a, 0f) * Quaternion.Euler(EnvMeshBuilder.Rand(rng, 14f, 26f), 0f, 0f);
                g.Matrix = saved * Matrix4x4.TRS(basePosition + Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, 0.16f * size), tilt, Vector3.one);
                g.Frustum(Vector3.zero, 0.15f * size, 0f, EnvMeshBuilder.Rand(rng, 0.45f, 0.75f) * size, 5, i % 2 == 0 ? outer : mid, true, false, false, 0f, rng, 0.2f);
            }
            g.Matrix = saved;
            g.Frustum(basePosition, 0.24f * size, 0f, 0.62f * size, 6, mid, false, false, false, 1f, rng, 0.15f);
            g.Frustum(basePosition, 0.14f * size, 0f, 0.38f * size, 5, core, false, false, false, 2f, rng, 0.1f);
        }

        // Mastro de madeira com barra, ponta dourada e estandarte azul pendurado.
        public static void BannerPole(EnvChunk chunk, Vector3 position, float height, float facingYaw, System.Random rng, float scale = 1f)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, facingYaw, 0f), Vector3.one * scale);
            EnvMeshBuilder o = chunk.Opaque;
            o.NoSnow();
            o.BeveledBox(new Vector3(0f, 0.2f, 0f), new Vector3(0.7f, 0.4f, 0.7f), 0.08f, EnvPalette.StoneDark);
            o.BeveledBox(new Vector3(0f, 0.44f, 0f), new Vector3(0.74f, 0.1f, 0.74f), 0.04f, EnvPalette.Snow, rng, 0.03f);
            o.Frustum(new Vector3(0f, 0.4f, 0f), 0.11f, 0.09f, height, 6, EnvPalette.Wood, false, false);
            // Barra horizontal e ponteira.
            float barY = height - 0.1f;
            o.BeveledBox(new Vector3(0f, barY, 0f), new Vector3(1.5f, 0.12f, 0.12f), 0.04f, EnvPalette.Wood);
            for (int side = -1; side <= 1; side += 2)
                o.BeveledBox(new Vector3(side * 0.78f, barY, 0f), Vector3.one * 0.17f, 0.06f, EnvPalette.Gold);
            o.Frustum(new Vector3(0f, height + 0.35f, 0f), 0.13f, 0f, 0.55f, 6, EnvPalette.Gold, true, false);
            o.BeveledBox(new Vector3(0f, height + 0.3f, 0f), new Vector3(0.24f, 0.12f, 0.24f), 0.04f, EnvPalette.Gold);
            EnvCastle.HangingBanner(chunk.Banner, new Vector3(0f, barY - 0.05f, -0.14f), 1.3f, 2.1f);
        }

        // Mureta da pista: blocos de pedra chanfrados, de alturas variadas, com ameias e neve.
        // Corre ao longo de Z, de z0 a z1, em x = edgeX. Retorna nada; usa Masonry + Opaque.
        public static void Parapet(EnvChunk chunk, float edgeX, float z0, float z1, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.identity;
            EnvMeshBuilder m = chunk.Opaque;
            EnvMeshBuilder o = chunk.Opaque;
            float z = z0;
            int index = 0;
            float side = Mathf.Sign(edgeX);
            while (z < z1)
            {
                float len = EnvMeshBuilder.Rand(rng, 1.1f, 1.7f);
                if (z + len > z1) len = Mathf.Max(0.4f, z1 - z);
                bool tall = index % 3 == 1;
                float h = tall ? EnvMeshBuilder.Rand(rng, 1.05f, 1.25f) : EnvMeshBuilder.Rand(rng, 0.6f, 0.75f);
                float depth = EnvMeshBuilder.Rand(rng, 0.8f, 0.95f);
                Color stone = EnvPalette.Vary(Color.Lerp(EnvPalette.StoneDark, EnvPalette.Stone, (float)rng.NextDouble()), rng, 0.05f);
                Vector3 c = new Vector3(edgeX + side * (depth * 0.5f), h * 0.5f - 0.05f, z + len * 0.5f);
                m.Matrix = Matrix4x4.TRS(c, Quaternion.Euler(0f, EnvMeshBuilder.Rand(rng, -2.5f, 2.5f), 0f), Vector3.one);
                m.BeveledBox(Vector3.zero, new Vector3(depth, h, len - 0.06f), 0.11f, stone);
                // Neve em cima (às vezes só parcial).
                if (rng.NextDouble() < 0.8)
                {
                    float sl = len * EnvMeshBuilder.Rand(rng, 0.55f, 0.95f);
                    o.BeveledBox(new Vector3(EnvMeshBuilder.Rand(rng, -0.05f, 0.05f), h * 0.5f + 0.05f, EnvMeshBuilder.Rand(rng, -0.1f, 0.1f)),
                        new Vector3(depth + 0.1f, 0.16f, sl), 0.07f, EnvPalette.Snow, rng, 0.04f);
                }
                // Neve acumulada no pé da mureta, do lado da pista.
                if (rng.NextDouble() < 0.55)
                {
                    o.Matrix = Matrix4x4.identity;
                    o.BeveledBox(new Vector3(edgeX - side * 0.12f, 0.06f, z + len * 0.5f), new Vector3(0.5f, 0.22f, len * EnvMeshBuilder.Rand(rng, 0.6f, 1f)), 0.1f,
                        EnvPalette.Snow, rng, 0.06f);
                }
                z += len;
                index++;
            }
            m.Matrix = Matrix4x4.identity;
        }

        // Caixa de madeira com reforço de ferro.
        public static void Crate(EnvChunk chunk, Vector3 position, float size, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, EnvMeshBuilder.Rand(rng, 0f, 90f), 0f), Vector3.one);
            EnvMeshBuilder o = chunk.Opaque;
            o.NoSnow();
            o.BeveledBox(new Vector3(0f, size * 0.5f, 0f), Vector3.one * size, size * 0.08f, EnvPalette.Wood);
            for (int i = -1; i <= 1; i += 2)
                o.BeveledBox(new Vector3(0f, size * 0.5f + i * size * 0.32f, 0f), new Vector3(size + 0.04f, size * 0.1f, size + 0.04f), 0.02f, EnvPalette.WoodDark);
            o.BeveledBox(new Vector3(0f, size + 0.04f, 0f), new Vector3(size * 0.9f, 0.1f, size * 0.85f), 0.04f, EnvPalette.Snow, rng, 0.03f);
        }

        // Barril com aros escuros e tampa nevada.
        public static void Barrel(EnvChunk chunk, Vector3 position, float height, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
            EnvMeshBuilder o = chunk.Opaque;
            o.NoSnow();
            float r = height * 0.36f;
            o.Frustum(Vector3.zero, r * 0.88f, r, height * 0.5f, 10, EnvPalette.Wood, true, false);
            o.Frustum(new Vector3(0f, height * 0.5f, 0f), r, r * 0.88f, height * 0.5f, 10, EnvPalette.Wood, false, false);
            o.Frustum(new Vector3(0f, height * 0.18f, 0f), r * 0.95f, r * 0.98f, height * 0.07f, 10, EnvPalette.Iron, false, false);
            o.Frustum(new Vector3(0f, height * 0.75f, 0f), r * 0.98f, r * 0.95f, height * 0.07f, 10, EnvPalette.Iron, false, false);
            o.Frustum(new Vector3(0f, height, 0f), r * 0.9f, r * 0.8f, 0.08f, 10, EnvPalette.Snow, false, true);
        }
    }
}
