using UnityEngine;

namespace LegionRush.Environments
{
    // Chão do mundo: calçamento da pista, encostas de neve que sobem até um planalto (cânion) e a
    // função de altura usada para assentar as peças decorativas.
    public static class EnvTerrain
    {
        public struct Shape
        {
            public float RoadHalfWidth;   // borda do calçamento
            public float BankStart;       // onde a neve começa (depois da mureta)
            public float CliffStart;      // pé do penhasco
            public float CliffWidth;      // largura da subida
            public float PlateauHeight;   // altura média do planalto
            public int Seed;
        }

        // Altura do terreno em (x, z): faixa plana, barranco de neve suave, subida íngreme, planalto ondulado.
        public static float Height(in Shape s, float x, float z)
        {
            float ax = Mathf.Abs(x);
            if (ax <= s.BankStart) return -0.02f;
            float side = x > 0f ? 1f : -1f;
            float n1 = Noise(x * 0.11f + s.Seed, z * 0.07f);
            float n2 = Noise(x * 0.31f - s.Seed, z * 0.23f + 11f);
            float cliffStart = s.CliffStart + (Noise(side * 7.3f + s.Seed, z * 0.045f) - 0.5f) * 3.5f;
            float bank = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(s.BankStart, s.BankStart + 3f, ax)) * (0.35f + n2 * 0.4f);
            if (ax < cliffStart) return bank + (n1 - 0.5f) * 0.3f * Mathf.InverseLerp(s.BankStart, cliffStart, ax);
            float plateau = s.PlateauHeight * (0.75f + 0.5f * Noise(side * 3.1f, z * 0.03f + s.Seed));
            float t = Mathf.InverseLerp(cliffStart, cliffStart + s.CliffWidth, ax);
            float rise = Mathf.SmoothStep(0f, 1f, t);
            return Mathf.Lerp(bank, plateau + (n1 - 0.5f) * 1.2f, rise);
        }

        // Faixas de neve à esquerda e à direita da pista, de z0 a z1.
        public static void SnowBanks(EnvChunk chunk, Shape s, float z0, float z1, float outerX, float cellSize)
        {
            chunk.Matrix = Matrix4x4.identity;
            EnvMeshBuilder b = chunk.Opaque;
            b.NoSnow();
            int cellsZ = Mathf.Max(1, Mathf.CeilToInt((z1 - z0) / cellSize));
            float inner = s.BankStart - 0.4f;
            int cellsX = Mathf.Max(2, Mathf.CeilToInt((outerX - inner) / cellSize));
            System.Func<float, float, Vector3, Color> color = (x, z, n) =>
            {
                // Inclinado vira rocha azulada; plano vira neve (com leve variação).
                float slope = 1f - n.y;
                float k = Noise(x * 0.5f, z * 0.5f);
                Color snow = Color.Lerp(EnvPalette.SnowShade, EnvPalette.Snow, 0.55f + k * 0.45f);
                Color rock = Color.Lerp(EnvPalette.CliffDark, EnvPalette.Cliff, k);
                return Color.Lerp(snow, rock, Mathf.SmoothStep(0.32f, 0.5f, slope));
            };
            Shape shape = s;
            b.HeightGrid(inner, outerX, z0, z1, cellsX, cellsZ, (x, z) => Height(shape, x, z), color);
            b.HeightGrid(-outerX, -inner, z0, z1, cellsX, cellsZ, (x, z) => Height(shape, x, z), color);
        }

        // Calçamento da pista (subdividido para a neblina por vértice ficar correta).
        public static void Road(EnvChunk chunk, float halfWidth, float z0, float z1)
        {
            chunk.Matrix = Matrix4x4.identity;
            EnvMeshBuilder b = chunk.Road;
            b.NoSnow();
            b.WorldUvScale = 1f / 4f;
            int cellsZ = Mathf.Max(1, Mathf.CeilToInt((z1 - z0) / 4f));
            b.HeightGrid(-halfWidth - 0.3f, halfWidth + 0.3f, z0, z1, 4, cellsZ, (x, z) => 0f, (x, z, n) => Color.white);
            // Faixa central levemente mais clara (caminho batido) feita pela textura; bordas com meio-fio.
        }

        // Ruído de valor suave (0..1) barato e determinístico.
        public static float Noise(float x, float y)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float xf = x - xi, yf = y - yi;
            float u = xf * xf * (3f - 2f * xf), v = yf * yf * (3f - 2f * yf);
            float a = Hash(xi, yi), b = Hash(xi + 1, yi), c = Hash(xi, yi + 1), d = Hash(xi + 1, yi + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        private static float Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0x7fffffff) / (float)int.MaxValue;
            }
        }
    }
}
