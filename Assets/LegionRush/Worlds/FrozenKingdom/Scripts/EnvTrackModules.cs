using UnityEngine;

namespace LegionRush.Environments
{
    // Módulos da pista do Reino Congelado para montar trechos à mão (prefabs FrozenTrack_*). Mesmas medidas
    // do jogo: largura = 2 × trackHalfWidth (padrão 10 m), comprimento do módulo = 10 m, chão em y = 0, pista
    // ao longo de +Z começando em z = 0. Só visual (sem colisores): a movimentação usa os limites do
    // CommanderController, então trocar o visual não muda a jogabilidade.
    public static class EnvTrackModules
    {
        public const float ModuleLength = 10f;

        public static void Straight(EnvChunk chunk, float halfWidth, float length, System.Random rng, bool borders = true)
        {
            EnvTerrain.Road(chunk, halfWidth, 0f, length);
            if (!borders) return;
            Border(chunk, -1, halfWidth, length, rng);
            Border(chunk, 1, halfWidth, length, rng);
        }

        // Mureta de um lado (side = -1 esquerda, +1 direita).
        public static void Border(EnvChunk chunk, int side, float halfWidth, float length, System.Random rng)
        {
            EnvProps.Parapet(chunk, side * (halfWidth + 0.3f), 0f, length, rng);
        }

        // Início: faixa azul e dourada de largada e dois estandartes.
        public static void Start(EnvChunk chunk, float halfWidth, System.Random rng)
        {
            Straight(chunk, halfWidth, ModuleLength, rng);
            chunk.Matrix = Matrix4x4.identity;
            chunk.Opaque.NoSnow();
            chunk.Opaque.BeveledBox(new Vector3(0f, 0.02f, 2f), new Vector3(halfWidth * 2f, 0.06f, 0.6f), 0.02f, EnvPalette.RoyalBlue);
            chunk.Opaque.BeveledBox(new Vector3(0f, 0.025f, 2f), new Vector3(halfWidth * 2f, 0.06f, 0.14f), 0.02f, EnvPalette.Gold);
            for (int side = -1; side <= 1; side += 2)
                EnvProps.BannerPole(chunk, new Vector3(side * (halfWidth + 1.8f), 0f, 2f), 3.6f, side * -18f, rng);
        }

        // Chegada: faixa quadriculada dourada e branca e arco de torres pequenas.
        public static void End(EnvChunk chunk, float halfWidth, System.Random rng)
        {
            Straight(chunk, halfWidth, ModuleLength, rng);
            chunk.Matrix = Matrix4x4.identity;
            chunk.Opaque.NoSnow();
            int cells = Mathf.RoundToInt(halfWidth * 2f / 0.5f);
            for (int i = 0; i < cells; i++)
                for (int row = 0; row < 2; row++)
                    chunk.Opaque.Box(new Vector3(-halfWidth + 0.25f + i * 0.5f, 0.02f, 5f + row * 0.5f), new Vector3(0.5f, 0.05f, 0.5f),
                        (i + row) % 2 == 0 ? EnvPalette.Gold : EnvPalette.Snow);
            for (int side = -1; side <= 1; side += 2)
                EnvCastle.Tower(chunk, new Vector3(side * (halfWidth + 1.7f), 0f, 5f), 1.25f, 5.2f, 2.8f, rng, false);
            chunk.Matrix = Matrix4x4.identity;
            chunk.Masonry.BeveledBox(new Vector3(0f, 5.6f, 5.15f), new Vector3(halfWidth * 2f + 2.2f, 0.9f, 0.7f), 0.12f, EnvPalette.Stone);
            chunk.Opaque.BeveledBox(new Vector3(0f, 6.12f, 5.15f), new Vector3(halfWidth * 2f + 2.3f, 0.2f, 0.85f), 0.08f, EnvPalette.Snow, rng, 0.03f);
        }

        // Trecho de portais: bases de pedra nas divisas das faixas (2 ou 3 portais) e neve varrida.
        public static void Gate(EnvChunk chunk, float halfWidth, int lanes, System.Random rng)
        {
            Straight(chunk, halfWidth, ModuleLength, rng);
            chunk.Matrix = Matrix4x4.identity;
            lanes = Mathf.Clamp(lanes, 2, 3);
            float lane = halfWidth * 2f / lanes;
            for (int i = 0; i <= lanes; i++)
            {
                float x = -halfWidth + lane * i;
                chunk.Masonry.BeveledBox(new Vector3(x, 0.05f, 5f), new Vector3(0.9f, 0.1f, 0.9f), 0.03f, EnvPalette.StoneDark);
            }
        }

        // Trecho de inimigos: pedras soltas, neve pisoteada e estandarte vermelho caído na borda.
        public static void Enemy(EnvChunk chunk, float halfWidth, System.Random rng)
        {
            Straight(chunk, halfWidth, ModuleLength, rng);
            chunk.Matrix = Matrix4x4.identity;
            for (int i = 0; i < 5; i++)
            {
                float x = EnvMeshBuilder.Rand(rng, -halfWidth + 0.5f, halfWidth - 0.5f);
                float z = EnvMeshBuilder.Rand(rng, 1f, ModuleLength - 1f);
                EnvVegetation.SnowDrift(chunk, new Vector3(x, -0.08f, z), new Vector3(EnvMeshBuilder.Rand(rng, 0.6f, 1.2f), 0.2f, EnvMeshBuilder.Rand(rng, 0.5f, 0.9f)), rng);
            }
            chunk.Matrix = Matrix4x4.identity;
            chunk.Opaque.NoSnow();
            Color red = new Color(0.75f, 0.12f, 0.12f);
            for (int side = -1; side <= 1; side += 2)
            {
                chunk.Matrix = Matrix4x4.TRS(new Vector3(side * (halfWidth + 1.6f), 0f, 6f), Quaternion.Euler(0f, side * 20f, 0f), Vector3.one);
                chunk.Opaque.Frustum(Vector3.zero, 0.08f, 0.06f, 3f, 6, EnvPalette.WoodDark, false, true);
                chunk.Opaque.BeveledBox(new Vector3(0f, 2.9f, 0f), new Vector3(1.2f, 0.1f, 0.1f), 0.03f, EnvPalette.Iron);
                chunk.Opaque.BeveledBox(new Vector3(0f, 2.1f, -0.06f), new Vector3(1f, 1.5f, 0.05f), 0.02f, red);
                chunk.Opaque.BeveledBox(new Vector3(0f, 2.2f, -0.1f), new Vector3(0.3f, 0.4f, 0.03f), 0.01f, EnvPalette.Iron);
            }
        }

        // Arena do chefão: plataforma mais larga, de pedra, com borda elevada e braseiros nos cantos.
        public static void Boss(EnvChunk chunk, float halfWidth, System.Random rng, EnvFireFx fire = null)
        {
            float w = halfWidth * 1.6f;
            float length = ModuleLength * 2f;
            EnvTerrain.Road(chunk, w, 0f, length);
            for (int side = -1; side <= 1; side += 2)
                EnvProps.Parapet(chunk, side * (w + 0.3f), 0f, length, rng);
            chunk.Matrix = Matrix4x4.identity;
            chunk.Opaque.NoSnow();
            // Círculo de pedras claras marcando a arena.
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI * 2f / 24f;
                chunk.Matrix = Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * w * 0.7f, 0.02f, length * 0.5f + Mathf.Sin(a) * w * 0.7f), Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f), Vector3.one);
                chunk.Masonry.BeveledBox(Vector3.zero, new Vector3(0.35f, 0.06f, 1f), 0.02f, EnvPalette.StoneLight);
            }
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = 0; sz <= 1; sz++)
                {
                    Vector3 flame = EnvProps.Brazier(chunk, new Vector3(sx * (w + 2f), 0f, 2f + sz * (length - 4f)), 2.1f, rng);
                    if (fire != null) fire.Add(flame, 0f);
                }
        }

        // Rampa: sobe "rise" metros ao longo do módulo (pedra contínua, bordas de mureta).
        public static void Ramp(EnvChunk chunk, float halfWidth, float rise, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.identity;
            EnvMeshBuilder b = chunk.Road;
            b.NoSnow();
            b.WorldUvScale = 1f / 3.2f;
            b.HeightGrid(-halfWidth - 0.3f, halfWidth + 0.3f, 0f, ModuleLength, 4, 5, (x, z) => z / ModuleLength * rise, (x, z, n) => Color.white);
            // Laterais de alvenaria sob a rampa.
            for (int side = -1; side <= 1; side += 2)
            {
                chunk.Matrix = Matrix4x4.identity;
                chunk.Masonry.ConvexFace(new[]
                {
                    new Vector3(side * (halfWidth + 0.3f), -0.3f, 0f), new Vector3(side * (halfWidth + 0.3f), -0.3f, ModuleLength),
                    new Vector3(side * (halfWidth + 0.3f), rise, ModuleLength), new Vector3(side * (halfWidth + 0.3f), 0f, 0f)
                }, Vector3.up * rise * 0.3f + Vector3.forward * ModuleLength * 0.5f, EnvPalette.Stone);
                for (float z = 0f; z < ModuleLength; z += 2.5f)
                {
                    float y = (z + 1.25f) / ModuleLength * rise;
                    chunk.Matrix = Matrix4x4.TRS(new Vector3(side * (halfWidth + 0.75f), y, z + 1.25f), Quaternion.Euler(-Mathf.Atan2(rise, ModuleLength) * Mathf.Rad2Deg, 0f, 0f), Vector3.one);
                    chunk.Opaque.BeveledBox(new Vector3(0f, 0.35f, 0f), new Vector3(0.85f, 0.75f, 2.4f), 0.1f, EnvPalette.Stone);
                    chunk.Opaque.BeveledBox(new Vector3(0f, 0.78f, 0f), new Vector3(0.95f, 0.14f, 2.2f), 0.05f, EnvPalette.Snow, rng, 0.03f);
                }
            }
            chunk.Matrix = Matrix4x4.identity;
        }

        public static void Stairs(EnvChunk chunk, float halfWidth, System.Random rng)
        {
            EnvCastle.Stairs(chunk, Vector3.zero, halfWidth * 2f, 8, 0.25f, ModuleLength / 8f, rng);
        }

        // Plataforma de pedra (elevada quando height > 0,3): borda chanfrada e neve nos cantos.
        public static void Platform(EnvChunk chunk, float halfWidth, float height, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.identity;
            chunk.Masonry.BeveledBox(new Vector3(0f, height * 0.5f - 0.15f, ModuleLength * 0.5f), new Vector3(halfWidth * 2f + 1f, height + 0.3f, ModuleLength), 0.15f, EnvPalette.Stone);
            chunk.Road.WorldUvScale = 1f / 3.2f;
            chunk.Road.HeightGrid(-halfWidth, halfWidth, 0.3f, ModuleLength - 0.3f, 4, 4, (x, z) => height + 0.01f, (x, z, n) => Color.white);
            chunk.Opaque.NoSnow();
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = 0; sz <= 1; sz++)
                    chunk.Opaque.BeveledBox(new Vector3(sx * (halfWidth + 0.1f), height + 0.05f, 0.5f + sz * (ModuleLength - 1f)), new Vector3(1.2f, 0.18f, 1.1f), 0.07f, EnvPalette.Snow, rng, 0.06f);
        }

        // Cruzamento: pista principal + pista transversal da mesma largura, muretas só nos cantos.
        public static void Crossroad(EnvChunk chunk, float halfWidth, System.Random rng)
        {
            float size = halfWidth * 2f + 0.6f;
            float arm = Mathf.Max(ModuleLength, size + 4f);
            float center = arm * 0.5f;
            EnvTerrain.Road(chunk, halfWidth, 0f, arm);
            chunk.Matrix = Matrix4x4.identity;
            EnvMeshBuilder b = chunk.Road;
            b.HeightGrid(-arm * 0.5f - halfWidth, -halfWidth - 0.3f, center - halfWidth - 0.3f, center + halfWidth + 0.3f, 2, 4, (x, z) => 0f, (x, z, n) => Color.white);
            b.HeightGrid(halfWidth + 0.3f, arm * 0.5f + halfWidth, center - halfWidth - 0.3f, center + halfWidth + 0.3f, 2, 4, (x, z) => 0f, (x, z, n) => Color.white);
            float gapStart = center - halfWidth - 0.3f, gapEnd = center + halfWidth + 0.3f;
            for (int side = -1; side <= 1; side += 2)
            {
                EnvProps.Parapet(chunk, side * (halfWidth + 0.3f), 0f, gapStart, rng);
                EnvProps.Parapet(chunk, side * (halfWidth + 0.3f), gapEnd, arm, rng);
            }
            for (int side = -1; side <= 1; side += 2)
                EnvMedieval.Pillar(chunk, new Vector3(side * (halfWidth + 1.2f), 0f, gapStart - 0.6f), 2.4f, rng);
        }

        // Curva de 90° para a direita (raio no eixo da pista = radius): segmentos de calçamento e muretas girados.
        public static void Curve(EnvChunk chunk, float halfWidth, float radius, System.Random rng)
        {
            const int Segments = 8;
            var pivot = new Vector3(radius, 0f, 0f);
            for (int i = 0; i < Segments; i++)
            {
                float a0 = 90f * i / Segments, a1 = 90f * (i + 1) / Segments;
                float am = (a0 + a1) * 0.5f;
                Quaternion rot = Quaternion.Euler(0f, am, 0f);
                Vector3 center = pivot + rot * new Vector3(-radius, 0f, 0f);
                Matrix4x4 frame = Matrix4x4.TRS(center, rot, Vector3.one);
                // Segmento levemente mais longo do lado de fora para não abrir fresta.
                float outer = (radius + halfWidth + 1f) * (a1 - a0) * Mathf.Deg2Rad;
                chunk.Road.Matrix = frame;
                chunk.Road.WorldUvScale = 1f / 3.2f;
                chunk.Road.HeightGrid(-halfWidth - 0.3f, halfWidth + 0.3f, -outer * 0.5f, outer * 0.5f, 4, 1, (x, z) => 0f, (x, z, n) => Color.white);
                chunk.Masonry.Matrix = frame;
                for (int side = -1; side <= 1; side += 2)
                {
                    float r = radius - side * (halfWidth + 0.75f);
                    float len = r * (a1 - a0) * Mathf.Deg2Rad;
                    chunk.Masonry.BeveledBox(new Vector3(side * (halfWidth + 0.75f), 0.35f, 0f), new Vector3(0.85f, 0.75f, len + 0.05f), 0.1f, EnvPalette.Stone);
                    chunk.Opaque.Matrix = frame;
                    chunk.Opaque.NoSnow();
                    chunk.Opaque.BeveledBox(new Vector3(side * (halfWidth + 0.75f), 0.78f, 0f), new Vector3(0.95f, 0.14f, len * 0.9f), 0.05f, EnvPalette.Snow, rng, 0.03f);
                }
            }
            chunk.Matrix = Matrix4x4.identity;
        }
    }
}
