using UnityEngine;

namespace LegionRush
{
    // Texturas geradas uma vez em runtime: sem arquivos externos, funcionam offline.
    public static class ProceduralTextures
    {
        private static Texture2D softCircle;
        private static Texture2D lightBeam;
        private static Texture2D lava;

        public static Texture2D SoftCircle
        {
            get
            {
                if (softCircle == null) softCircle = CreateSoftCircle(64);
                return softCircle;
            }
        }

        public static Texture2D LightBeam
        {
            get
            {
                if (lightBeam == null) lightBeam = CreateLightBeam(32, 128);
                return lightBeam;
            }
        }

        public static Texture2D Lava
        {
            get
            {
                if (lava == null) lava = CreateLava(128);
                return lava;
            }
        }

        private static Texture2D CreateSoftCircle(int size)
        {
            var texture = NewTexture("LR_SoftCircle", size, size, false, TextureWrapMode.Clamp);
            var pixels = new Color32[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    a = a * a * (3f - 2f * a);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            return Finish(texture, pixels, false);
        }

        private static Texture2D CreateLightBeam(int width, int height)
        {
            var texture = NewTexture("LR_LightBeam", width, height, false, TextureWrapMode.Clamp);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                float vertical = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.25f, v)) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, 0.55f, v));
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    float horizontal = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 2f);
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)(horizontal * vertical * 255f));
                }
            }
            return Finish(texture, pixels, false);
        }

        // Padrão periódico (tileável) de veias brilhantes sobre crosta escura.
        private static Texture2D CreateLava(int size)
        {
            var texture = NewTexture("LR_Lava", size, size, true, TextureWrapMode.Repeat);
            var pixels = new Color32[size * size];
            Color crust = new Color(0.12f, 0.03f, 0.03f);
            Color hot = new Color(1f, 0.5f, 0.08f);
            Color core = new Color(1f, 0.92f, 0.45f);
            const float Tau = Mathf.PI * 2f;
            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float a = Mathf.Abs(Mathf.Sin(Tau * (3f * u + 0.3f * Mathf.Sin(Tau * 2f * v))));
                    float b = Mathf.Abs(Mathf.Sin(Tau * (2f * v + 0.25f * Mathf.Sin(Tau * 3f * u))));
                    float c = Mathf.Abs(Mathf.Sin(Tau * (u + v) * 2f));
                    float crack = Mathf.Max(1f - Mathf.SmoothStep(0f, 0.28f, a), 1f - Mathf.SmoothStep(0f, 0.22f, b));
                    crack = Mathf.Max(crack, 0.35f * (1f - Mathf.SmoothStep(0f, 0.15f, c)));
                    Color color = Color.Lerp(crust, hot, crack);
                    color = Color.Lerp(color, core, Mathf.Pow(crack, 4f));
                    pixels[y * size + x] = color;
                }
            }
            return Finish(texture, pixels, true);
        }

        private static Texture2D NewTexture(string name, int width, int height, bool mips, TextureWrapMode wrap)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, mips)
            {
                name = name,
                wrapMode = wrap,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 1
            };
        }

        private static Texture2D Finish(Texture2D texture, Color32[] pixels, bool mips)
        {
            texture.SetPixels32(pixels);
            texture.Apply(mips, true);
            return texture;
        }
    }
}
