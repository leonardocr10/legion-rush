using System.Collections.Generic;
using UnityEngine;

namespace LegionRush.Environments
{
    // Texturas do cenário geradas em runtime (sem arquivos, funcionam offline e no WebGL):
    // calçamento de pedra, alvenaria de castelo, estandarte com flor-de-lis e veios de gelo.
    public static class EnvTextures
    {
        private static Texture2D cobble;
        private static Texture2D masonry;
        private static Texture2D banner;
        private static Texture2D iceStreaks;

        // Editor (gerador de prefabs): mantém os pixels na CPU para poder salvar as texturas como asset.
        public static bool KeepReadable;

        public static void ResetCache()
        {
            cobble = masonry = banner = iceStreaks = null;
        }

        public static Texture2D Cobble => cobble != null ? cobble : cobble = CreateCobble(512);
        public static Texture2D Masonry => masonry != null ? masonry : masonry = CreateMasonry(256);
        public static Texture2D Banner => banner != null ? banner : banner = CreateBanner(128, 256);
        public static Texture2D IceStreaks => iceStreaks != null ? iceStreaks : iceStreaks = CreateIceStreaks(64, 128);

        // Calçamento medieval (1 ladrilho = 3,2 m): fiadas de alturas diferentes, pedras de larguras variadas,
        // chanfro iluminado de cima, juntas azul-escuras fundas, rachaduras finas, desgaste no centro e neve em
        // algumas juntas. Cinza-azulado como na referência. Tileável nos dois eixos.
        private static Texture2D CreateCobble(int size)
        {
            var pixels = new Color32[size * size];
            var rng = new System.Random(4242);
            const int Rows = 7;
            // Alturas das fiadas variam ±20% (somam o tamanho exato para continuar tileável).
            var rowEdge = new float[Rows + 1];
            float total = 0f;
            var heights = new float[Rows];
            for (int r = 0; r < Rows; r++) { heights[r] = 0.8f + (float)rng.NextDouble() * 0.4f; total += heights[r]; }
            for (int r = 0; r < Rows; r++) rowEdge[r + 1] = rowEdge[r] + heights[r] / total * size;

            Color gap = new Color(0.16f, 0.19f, 0.27f);
            Color snow = new Color(0.9f, 0.94f, 1f);
            Color stoneDark = new Color(0.4f, 0.44f, 0.54f);
            Color stoneLight = new Color(0.62f, 0.66f, 0.74f);
            Color stoneWarm = new Color(0.56f, 0.56f, 0.6f);
            var rowStarts = new List<float>[Rows];
            var rowTints = new List<float>[Rows];
            var rowSnow = new List<bool>[Rows];
            var rowCrack = new List<Vector4>[Rows];
            for (int r = 0; r < Rows; r++)
            {
                rowStarts[r] = new List<float>();
                rowTints[r] = new List<float>();
                rowSnow[r] = new List<bool>();
                rowCrack[r] = new List<Vector4>();
                float offset = (float)rng.NextDouble() * size;
                float x = 0f;
                while (x < size - 30f)
                {
                    rowStarts[r].Add(Mathf.Repeat(x + offset, size));
                    rowTints[r].Add((float)rng.NextDouble());
                    rowSnow[r].Add(rng.NextDouble() < 0.14);
                    // Rachadura: segmento (u0, v0) -> (u1, v1) em coordenadas da pedra; x < 0 = sem rachadura.
                    rowCrack[r].Add(rng.NextDouble() < 0.3
                        ? new Vector4((float)rng.NextDouble() * 0.6f + 0.2f, 0f, (float)rng.NextDouble() * 0.6f + 0.2f, 1f)
                        : new Vector4(-1f, 0f, 0f, 0f));
                    x += size * (0.13f + (float)rng.NextDouble() * 0.13f);
                }
            }

            int rowIndex = 0;
            for (int y = 0; y < size; y++)
            {
                while (rowIndex < Rows - 1 && y >= rowEdge[rowIndex + 1]) rowIndex++;
                int r = rowIndex;
                float rowH = rowEdge[r + 1] - rowEdge[r];
                float vy = (y - rowEdge[r]) / rowH;
                List<float> starts = rowStarts[r];
                int count = starts.Count;
                for (int x = 0; x < size; x++)
                {
                    int idx = 0;
                    float best = float.MaxValue;
                    for (int k = 0; k < count; k++)
                    {
                        float d = Mathf.Repeat(x - starts[k], size);
                        if (d < best) { best = d; idx = k; }
                    }
                    float width = Mathf.Repeat(starts[(idx + 1) % count] - starts[idx], size);
                    if (width < 1f) width = size;
                    float vx = best / width;
                    // Cantos arredondados: distância à borda com raio nos cantos.
                    float ex = Mathf.Min(vx, 1f - vx) * width;
                    float ey = Mathf.Min(vy, 1f - vy) * rowH;
                    const float Corner = 7f;
                    float edge = ex < Corner && ey < Corner ? Corner - Mathf.Sqrt(Sq(Corner - ex) + Sq(Corner - ey)) : Mathf.Min(ex, ey);
                    // Borda irregular (pedra lascada).
                    edge += (EnvTerrain.Noise(x * 0.21f, y * 0.21f) - 0.5f) * 2.4f;
                    const float Gap = 3f, Bevel = 10f;
                    float tint = rowTints[r][idx];
                    Color stone = Color.Lerp(stoneDark, stoneLight, tint * 0.8f + 0.1f);
                    stone = Color.Lerp(stone, stoneWarm, Hash(idx * 5.3f, r * 2.1f) * 0.4f);
                    float n = (EnvTerrain.Noise(x * 0.06f, y * 0.06f) - 0.5f) * 0.14f
                        + (EnvTerrain.Noise(x * 0.25f + 40f, y * 0.25f) - 0.5f) * 0.08f
                        + Hash(x * 0.37f, y * 0.37f) * 0.05f - 0.025f;
                    Color c;
                    if (edge < Gap)
                    {
                        // Junta funda; neve acumulada em parte delas.
                        c = rowSnow[r][idx] && EnvTerrain.Noise(x * 0.1f, y * 0.1f) > 0.35f ? snow * 0.92f : gap;
                    }
                    else
                    {
                        float bevel = Mathf.Clamp01((edge - Gap) / Bevel);
                        // Luz de cima: borda de cima (v alto) clara, de baixo escura; laterais intermediárias.
                        float lit = vy > 0.5f ? 1.16f : 0.74f;
                        if (ex < ey) lit = vx < 0.5f ? 1.04f : 0.86f;
                        float shadeK = Mathf.Lerp(lit, 1f, Mathf.SmoothStep(0f, 1f, bevel));
                        // Desgaste: topo da pedra levemente mais claro e polido.
                        float wear = Mathf.SmoothStep(0.4f, 1f, bevel) * 0.05f;
                        c = stone * (shadeK + n + wear);
                        Vector4 crack = rowCrack[r][idx];
                        if (crack.x >= 0f)
                        {
                            float cu = Mathf.Lerp(crack.x, crack.z, vy) + (EnvTerrain.Noise(vy * 6f + idx, r) - 0.5f) * 0.12f;
                            float dist = Mathf.Abs(vx - cu) * width;
                            if (dist < 1.1f && vy > 0.12f && vy < 0.88f) c *= 0.62f;
                        }
                        if (rowSnow[r][idx] && vy < 0.16f) c = Color.Lerp(c, snow, 0.55f * (1f - vy / 0.16f));
                    }
                    c.a = 1f;
                    pixels[y * size + x] = c;
                }
            }
            return Finish("LR_Env_Cobble", size, size, pixels, TextureWrapMode.Repeat);
        }

        // Blocos de alvenaria em fiadas (1 ladrilho = 2 m): blocos claros com chanfro e argamassa escura.
        private static Texture2D CreateMasonry(int size)
        {
            var pixels = new Color32[size * size];
            const int Rows = 4;
            float rowH = size / (float)Rows;
            for (int y = 0; y < size; y++)
            {
                int r = (int)(y / rowH);
                float vy = (y - r * rowH) / rowH;
                float brickW = size / 2f;
                float shift = (r % 2) * brickW * 0.5f;
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Repeat(x + shift, brickW);
                    float vx = px / brickW;
                    int brick = (int)(Mathf.Repeat(x + shift, size) / brickW) + r * 7;
                    float ex = Mathf.Min(vx, 1f - vx) * brickW;
                    float ey = Mathf.Min(vy, 1f - vy) * rowH;
                    float edge = Mathf.Min(ex, ey);
                    float tint = Hash(brick * 3.1f, r * 1.7f);
                    float n = Hash(x * 0.5f, y * 0.5f) * 0.05f;
                    float v;
                    if (edge < 2.5f) v = 0.55f;
                    else
                    {
                        float bevel = Mathf.Clamp01((edge - 2.5f) / 6f);
                        float lit = vy > 0.5f ? 1.05f : 0.88f;
                        v = Mathf.Lerp(lit * 0.86f, 1f, bevel) * (0.9f + tint * 0.1f) - n;
                    }
                    byte b = (byte)(Mathf.Clamp01(v) * 255f);
                    pixels[y * size + x] = new Color32(b, b, (byte)Mathf.Min(255, b + 6), 255);
                }
            }
            return Finish("LR_Env_Masonry", size, size, pixels, TextureWrapMode.Repeat);
        }

        // Estandarte: campo azul real com borda dourada e flor-de-lis branca. u = 0 no mastro, v = 0 embaixo.
        private static Texture2D CreateBanner(int width, int height)
        {
            var pixels = new Color32[width * height];
            Color blue = new Color(0.1f, 0.3f, 0.9f);
            Color blueDark = new Color(0.07f, 0.2f, 0.65f);
            Color gold = new Color(1f, 0.78f, 0.28f);
            Color white = new Color(0.97f, 0.98f, 1f);
            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    // Coordenadas da flor: centro do tecido, eixo vertical.
                    float fx = (u - 0.5f) * 2.6f;
                    float fy = (v - 0.6f) * 2.6f * height / width * 0.5f;
                    float border = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(1f - v, v * 1.2f));
                    Color c = Color.Lerp(blueDark, blue, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, border * 7f)));
                    if (border < 0.075f) c = gold;
                    float lis = FleurDeLis(fx, fy);
                    c = Color.Lerp(c, white, lis);
                    c.a = 1f;
                    pixels[y * width + x] = c;
                }
            }
            return Finish("LR_Env_Banner", width, height, pixels, TextureWrapMode.Clamp);
        }

        // Flor-de-lis por formas simples (cobertura 0..1 com borda suave). Espaço ~[-1,1], y para cima.
        private static float FleurDeLis(float x, float y)
        {
            float ax = Mathf.Abs(x);
            float center = 0f;
            if (y > -0.05f && y < 1f)
            {
                float t = (y + 0.05f) / 1.05f;
                float w = 0.26f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.75f)), 0.8f);
                center = Smooth(w - ax);
            }
            float side = Arc(ax, y, 0.43f, 0.3f, 0.26f, 200f, -55f, 0.17f, 0.05f);
            float band = Smooth(0.07f - Mathf.Abs(y + 0.08f)) * Smooth(0.4f - ax);
            float stem = y > -0.4f && y < -0.08f ? Smooth(0.09f - ax) : 0f;
            float lobe = Smooth(1f - Mathf.Sqrt(Sq((ax - 0.2f) / 0.11f) + Sq((y + 0.22f) / 0.065f)));
            float tip = y > -0.6f && y < -0.38f ? Smooth(0.09f * (y + 0.6f) / 0.22f - ax) : 0f;
            return Mathf.Clamp01(Mathf.Max(Mathf.Max(center, side), Mathf.Max(Mathf.Max(band, stem), Mathf.Max(lobe, tip))));
        }

        // Arco grosso do ângulo a0 ao a1 (graus, sentido horário), espessura t0 -> t1.
        private static float Arc(float x, float y, float cx, float cy, float r, float a0, float a1, float t0, float t1)
        {
            float dx = x - cx, dy = y - cy;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            while (a < a1) a += 360f;
            while (a >= a1 + 360f) a -= 360f;
            if (a > a0) return 0f;
            float t = (a0 - a) / (a0 - a1);
            return Smooth(Mathf.Lerp(t0, t1, t) * 0.5f - Mathf.Abs(d - r));
        }

        private static float Sq(float v) => v * v;

        private static float Smooth(float d) => Mathf.Clamp01(d * 45f + 0.5f);

        // Veios verticais irregulares para o gelo (tileável em V).
        private static Texture2D CreateIceStreaks(int width, int height)
        {
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = y / (float)height;
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)width;
                    const float Tau = Mathf.PI * 2f;
                    float a = Mathf.Sin(Tau * (u * 5f + 0.15f * Mathf.Sin(Tau * v * 2f)));
                    float b = Mathf.Sin(Tau * (u * 11f + 0.2f * Mathf.Sin(Tau * (v * 3f + u))));
                    float streak = Mathf.Pow(Mathf.Clamp01(a * 0.5f + 0.5f), 6f) * 0.8f + Mathf.Pow(Mathf.Clamp01(b * 0.5f + 0.5f), 10f) * 0.6f;
                    byte k = (byte)(Mathf.Clamp01(streak) * 255f);
                    pixels[y * width + x] = new Color32(k, k, k, 255);
                }
            }
            return Finish("LR_Env_IceStreaks", width, height, pixels, TextureWrapMode.Repeat);
        }

        private static float Hash(float x, float y)
        {
            float h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        private static Texture2D Finish(string name, int width, int height, Color32[] pixels, TextureWrapMode wrap)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = name,
                wrapMode = wrap,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4
            };
            texture.SetPixels32(pixels);
            texture.Apply(true, !KeepReadable);
            return texture;
        }
    }
}
