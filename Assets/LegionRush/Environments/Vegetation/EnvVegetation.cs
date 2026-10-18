using UnityEngine;

namespace LegionRush.Environments
{
    // Pinheiros em camadas com neve, arbustos nevados e montes de neve.
    public static class EnvVegetation
    {
        // Pinheiro estilizado: tronco curto + 3–5 camadas cônicas facetadas, cada uma com uma "saia" de neve
        // de borda recortada por cima. height em metros (3–7 típico).
        public static void Pine(EnvChunk chunk, Vector3 position, float height, System.Random rng, float snow = 1f)
        {
            float yaw = EnvMeshBuilder.Rand(rng, 0f, 360f);
            float lean = EnvMeshBuilder.Rand(rng, -3f, 3f);
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(lean, yaw, 0f), Vector3.one);
            EnvMeshBuilder b = chunk.Opaque;
            b.NoSnow();

            float trunkH = height * 0.16f;
            b.Frustum(Vector3.zero, height * 0.06f, height * 0.045f, trunkH + 0.2f, 6, EnvPalette.WoodDark, false, false);

            int tiers = height > 5f ? 5 : height > 3.5f ? 4 : 3;
            float baseR = height * 0.36f;
            float top = height;
            float y = trunkH;
            float span = (top - trunkH) / (tiers * 0.72f + 0.28f);
            Color green = EnvPalette.Vary(Color.Lerp(EnvPalette.Pine, EnvPalette.PineLight, (float)rng.NextDouble() * 0.5f), rng, 0.06f);
            for (int t = 0; t < tiers; t++)
            {
                float k = t / (float)tiers;
                float r = baseR * (1f - k * 0.78f);
                float h = span * (t == tiers - 1 ? 1.25f : 1f);
                float phase = EnvMeshBuilder.Rand(rng, 0f, 6.28f);
                int segments = 9;
                // Camada verde: cone com base irregular (galhos).
                b.Frustum(new Vector3(0f, y, 0f), r, 0f, h, segments, green, t == 0, false, false, phase, rng, 0.12f);
                if (snow > 0f)
                {
                    // Saia de neve: cone mais estreito apoiado na camada, borda em zigue-zague.
                    // snow > 1 = neve intensa (saia mais larga e mais alta).
                    float heavy = Mathf.Clamp01(snow - 1f);
                    float sy = y + h * Mathf.Lerp(0.3f, 0.22f, heavy);
                    float sr = r * Mathf.Lerp(0.78f, 0.95f, heavy);
                    b.Frustum(new Vector3(0f, sy, 0f), sr, 0f, h * Mathf.Lerp(0.74f, 0.84f, heavy), segments, EnvPalette.Snow, false, false, false, phase + 0.35f, rng, 0.18f);
                }
                y += span * 0.72f;
            }
        }

        // Arbusto nevado (montinho verde com neve por cima).
        public static void SnowBush(EnvChunk chunk, Vector3 position, float size, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, EnvMeshBuilder.Rand(rng, 0f, 360f), 0f), Vector3.one);
            EnvMeshBuilder b = chunk.Opaque;
            b.SetSnow(0.55f);
            b.BeveledBox(new Vector3(0f, size * 0.3f, 0f), new Vector3(size, size * 0.7f, size * 0.85f), size * 0.25f, EnvPalette.Pine, rng, size * 0.1f);
            b.NoSnow();
        }

        // Monte de neve achatado e irregular.
        public static void SnowDrift(EnvChunk chunk, Vector3 position, Vector3 size, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, EnvMeshBuilder.Rand(rng, 0f, 360f), 0f), Vector3.one);
            EnvMeshBuilder b = chunk.Opaque;
            b.NoSnow();
            b.BeveledBox(new Vector3(0f, size.y * 0.35f, 0f), size, Mathf.Min(size.y * 0.45f, size.x * 0.3f), EnvPalette.Snow, rng, size.y * 0.25f);
        }
            // Árvore parcialmente congelada: tronco torto sem folhas, galhos com neve e pontas de gelo azul.
        public static void FrozenTree(EnvChunk chunk, Vector3 position, float height, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, EnvMeshBuilder.Rand(rng, 0f, 360f), 0f), Vector3.one);
            EnvMeshBuilder b = chunk.Opaque;
            b.NoSnow();
            float r = height * 0.07f;
            b.Frustum(Vector3.zero, r, r * 0.6f, height * 0.55f, 7, EnvPalette.WoodDark, false, false);
            Matrix4x4 root = b.Matrix;
            int branches = 5;
            for (int i = 0; i < branches; i++)
            {
                float yaw = i * 360f / branches + EnvMeshBuilder.Rand(rng, -20f, 20f);
                float tilt = EnvMeshBuilder.Rand(rng, 30f, 55f);
                float y = height * EnvMeshBuilder.Rand(rng, 0.35f, 0.55f);
                float len = height * EnvMeshBuilder.Rand(rng, 0.35f, 0.5f);
                Matrix4x4 frame = root * Matrix4x4.TRS(new Vector3(0f, y, 0f), Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(tilt, 0f, 0f), Vector3.one);
                b.Matrix = frame;
                b.Frustum(Vector3.zero, r * 0.45f, r * 0.15f, len, 5, EnvPalette.WoodDark, false, false);
                // Neve por cima do galho e gelo na ponta.
                b.Frustum(new Vector3(0f, len * 0.15f, r * 0.25f), r * 0.32f, r * 0.12f, len * 0.7f, 5, EnvPalette.Snow, false, false);
                chunk.Ice.Matrix = frame;
                chunk.Ice.Frustum(new Vector3(0f, len * 0.85f, 0f), r * 0.5f, 0f, len * 0.4f, 5, EnvPalette.Ice, true, false);
            }
            b.Matrix = root;
            chunk.Ice.Matrix = root;
            chunk.Ice.Frustum(new Vector3(0f, height * 0.55f, 0f), r * 0.7f, 0f, height * 0.35f, 6, EnvPalette.Ice, true, false);
            b.BeveledBox(new Vector3(0f, 0.05f, 0f), new Vector3(height * 0.3f, 0.14f, height * 0.26f), 0.06f, EnvPalette.Snow, rng, 0.05f);
        }

        // Plantinha nevada: tufo de folhas pontudas verde-escuras com flocos de neve.
        public static void SmallPlant(EnvChunk chunk, Vector3 position, float size, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, EnvMeshBuilder.Rand(rng, 0f, 360f), 0f), Vector3.one);
            EnvMeshBuilder b = chunk.Opaque;
            b.NoSnow();
            Matrix4x4 root = b.Matrix;
            for (int i = 0; i < 6; i++)
            {
                b.Matrix = root * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(EnvMeshBuilder.Rand(rng, 10f, 35f), i * 60f + EnvMeshBuilder.Rand(rng, -15f, 15f), 0f), Vector3.one);
                b.Frustum(Vector3.zero, size * 0.08f, 0f, size * EnvMeshBuilder.Rand(rng, 0.45f, 0.7f), 4, EnvPalette.PineLight, false, false);
            }
            b.Matrix = root;
            b.BeveledBox(new Vector3(0f, size * 0.12f, 0f), new Vector3(size * 0.35f, size * 0.12f, size * 0.3f), size * 0.05f, EnvPalette.Snow, rng, size * 0.03f);
        }
    }
}
