using UnityEngine;

namespace LegionRush.Environments
{
    // Rochas chanfradas com neve no topo, penhascos, cristais de gelo emissivos e cachoeiras congeladas.
    public static class EnvRocks
    {
        // Rocha irregular: caixa chanfrada com cantos deslocados; faces voltadas para cima viram neve.
        public static void Rock(EnvChunk chunk, Vector3 position, Vector3 size, System.Random rng, float snowThreshold = 0.62f)
        {
            Quaternion rotation = Quaternion.Euler(EnvMeshBuilder.Rand(rng, -8f, 8f), EnvMeshBuilder.Rand(rng, 0f, 360f), EnvMeshBuilder.Rand(rng, -8f, 8f));
            chunk.Matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
            EnvMeshBuilder b = chunk.Opaque;
            b.SetSnow(snowThreshold);
            float m = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
            Color stone = EnvPalette.Vary(Color.Lerp(EnvPalette.Cliff, EnvPalette.StoneLight, (float)rng.NextDouble() * 0.4f), rng, 0.06f);
            b.BeveledBox(new Vector3(0f, size.y * 0.42f, 0f), size, m * 0.28f, stone, rng, m * 0.16f);
            b.NoSnow();
        }

        // Aglomerado de rochas (1 grande + 1–3 menores).
        public static void RockCluster(EnvChunk chunk, Vector3 position, float scale, System.Random rng)
        {
            Rock(chunk, position, new Vector3(1.6f, 1.1f, 1.3f) * scale, rng);
            int extra = 1 + rng.Next(3);
            for (int i = 0; i < extra; i++)
            {
                float a = EnvMeshBuilder.Rand(rng, 0f, Mathf.PI * 2f);
                Vector3 p = position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * scale * EnvMeshBuilder.Rand(rng, 0.9f, 1.4f);
                float s = scale * EnvMeshBuilder.Rand(rng, 0.35f, 0.6f);
                Rock(chunk, p, new Vector3(1.2f, 0.9f, 1.1f) * s, rng);
            }
        }

        // Paredão de penhasco: blocos altos empilhados em degraus, neve nas plataformas.
        // faceDir: direção para onde a face do penhasco olha (para a pista).
        public static void CliffSection(EnvChunk chunk, Vector3 basePosition, float length, float height, Vector3 faceDir, System.Random rng)
        {
            Quaternion rotation = Quaternion.LookRotation(faceDir, Vector3.up);
            EnvMeshBuilder b = chunk.Opaque;
            float x = -length * 0.5f;
            while (x < length * 0.5f)
            {
                float w = EnvMeshBuilder.Rand(rng, 2.2f, 4.2f);
                float h = height * EnvMeshBuilder.Rand(rng, 0.65f, 1.05f);
                float depth = EnvMeshBuilder.Rand(rng, 2.5f, 4f);
                float inset = EnvMeshBuilder.Rand(rng, -0.6f, 0.6f);
                Vector3 local = new Vector3(x + w * 0.5f, 0f, inset - depth * 0.5f);
                chunk.Matrix = Matrix4x4.TRS(basePosition + rotation * local, rotation * Quaternion.Euler(0f, EnvMeshBuilder.Rand(rng, -6f, 6f), 0f), Vector3.one);
                b.SetSnow(0.55f);
                Color stone = EnvPalette.Vary(Color.Lerp(EnvPalette.CliffDark, EnvPalette.Cliff, (float)rng.NextDouble()), rng, 0.05f);
                // Corpo principal + um degrau mais baixo à frente.
                b.BeveledBox(new Vector3(0f, h * 0.5f - 0.3f, 0f), new Vector3(w, h, depth), 0.45f, stone, rng, 0.35f);
                if (rng.NextDouble() < 0.7)
                {
                    float sh = h * EnvMeshBuilder.Rand(rng, 0.3f, 0.55f);
                    b.BeveledBox(new Vector3(EnvMeshBuilder.Rand(rng, -0.4f, 0.4f), sh * 0.5f - 0.3f, depth * 0.5f + 0.6f),
                        new Vector3(w * EnvMeshBuilder.Rand(rng, 0.6f, 0.9f), sh, 1.6f), 0.35f, stone * 1.05f, rng, 0.3f);
                }
                // Camada de neve espessa no topo (borda arredondada que transborda um pouco).
                b.NoSnow();
                b.BeveledBox(new Vector3(0f, h - 0.3f + 0.12f, 0.1f), new Vector3(w + 0.25f, 0.42f, depth + 0.3f), 0.18f, EnvPalette.Snow, rng, 0.08f);
                x += w * EnvMeshBuilder.Rand(rng, 0.8f, 0.95f);
            }
            b.NoSnow();
        }

        // Cristais de gelo: prismas hexagonais pontudos inclinados para fora, emissivos.
        public static void IceCrystals(EnvChunk chunk, Vector3 position, float scale, System.Random rng)
        {
            EnvMeshBuilder b = chunk.Glow;
            int count = 3 + rng.Next(3);
            for (int i = 0; i < count; i++)
            {
                float h = scale * EnvMeshBuilder.Rand(rng, 0.7f, 1.6f) * (i == 0 ? 1.4f : 1f);
                float r = h * EnvMeshBuilder.Rand(rng, 0.14f, 0.2f);
                float tiltX = i == 0 ? EnvMeshBuilder.Rand(rng, -6f, 6f) : EnvMeshBuilder.Rand(rng, -35f, 35f);
                float tiltZ = i == 0 ? EnvMeshBuilder.Rand(rng, -6f, 6f) : EnvMeshBuilder.Rand(rng, -35f, 35f);
                Vector3 offset = i == 0 ? Vector3.zero : new Vector3(EnvMeshBuilder.Rand(rng, -0.35f, 0.35f), 0f, EnvMeshBuilder.Rand(rng, -0.35f, 0.35f)) * scale;
                chunk.Matrix = Matrix4x4.TRS(position + offset + Vector3.down * 0.1f, Quaternion.Euler(tiltX, EnvMeshBuilder.Rand(rng, 0f, 60f), tiltZ), Vector3.one);
                Color c = Color.Lerp(EnvPalette.IceDeep, EnvPalette.Ice, (float)rng.NextDouble()) * 0.62f;
                c.a = 1f;
                b.Frustum(Vector3.zero, r, r * 0.85f, h * 0.72f, 6, c, false, false);
                b.Frustum(new Vector3(0f, h * 0.72f, 0f), r * 0.85f, 0f, h * 0.28f, 6, c * 1.25f, false, false);
            }
        }

        // Cachoeira congelada: colunas de gelo lado a lado escorrendo do topo do penhasco, com poça na base.
        // faceDir: para onde a cachoeira olha.
        public static void FrozenWaterfall(EnvChunk chunk, Vector3 topCenter, float width, float height, Vector3 faceDir, System.Random rng)
        {
            Quaternion rotation = Quaternion.LookRotation(faceDir, Vector3.up);
            EnvMeshBuilder b = chunk.Ice;
            int columns = Mathf.Max(3, Mathf.RoundToInt(width / 0.7f));
            for (int i = 0; i < columns; i++)
            {
                float u = columns == 1 ? 0.5f : i / (float)(columns - 1);
                float x = (u - 0.5f) * width;
                float edge = 1f - Mathf.Abs(u - 0.5f) * 2f;
                float h = height * (0.75f + 0.25f * edge) * EnvMeshBuilder.Rand(rng, 0.85f, 1f);
                float r = EnvMeshBuilder.Rand(rng, 0.35f, 0.55f);
                Vector3 local = new Vector3(x, -h, EnvMeshBuilder.Rand(rng, 0f, 0.35f));
                chunk.Matrix = Matrix4x4.TRS(topCenter + rotation * local, rotation, Vector3.one);
                Color c = Color.Lerp(EnvPalette.IceDeep, EnvPalette.Ice, EnvMeshBuilder.Rand(rng, 0.3f, 1f));
                // Coluna mais larga embaixo (gelo acumulado) e ponta de pingente no meio.
                b.Frustum(Vector3.zero, r * 1.25f, r, h, 7, c, false, true, true);
            }
            // Base: blocos de gelo acumulados.
            chunk.Matrix = Matrix4x4.TRS(topCenter + rotation * new Vector3(0f, -height, 0.6f), rotation, Vector3.one);
            b.BeveledBox(new Vector3(0f, 0.25f, 0f), new Vector3(width * 1.15f, 0.8f, 1.8f), 0.3f, EnvPalette.Ice, rng, 0.2f);
            // Topo: lábio de neve de onde o gelo nasce.
            chunk.Matrix = Matrix4x4.TRS(topCenter, rotation, Vector3.one);
            chunk.Opaque.NoSnow();
            chunk.Opaque.BeveledBox(new Vector3(0f, 0.05f, -0.2f), new Vector3(width + 0.6f, 0.5f, 1.4f), 0.2f, EnvPalette.Snow, rng, 0.1f);
        }
    
        // Pico de montanha facetado (base de rocha azulada, capa de neve recortada). Material Distant: pouca neblina.
        public static void MountainPeak(EnvChunk chunk, Vector3 basePosition, float height, float radius, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(basePosition, Quaternion.Euler(EnvMeshBuilder.Rand(rng, -4f, 4f), EnvMeshBuilder.Rand(rng, 0f, 360f), 0f), Vector3.one);
            EnvMeshBuilder b = chunk.Distant;
            b.NoSnow();
            float phase = EnvMeshBuilder.Rand(rng, 0f, 6.28f);
            float rockH = height * EnvMeshBuilder.Rand(rng, 0.42f, 0.55f);
            Color rock = EnvPalette.Vary(Color.Lerp(EnvPalette.CliffDark, EnvPalette.Cliff, (float)rng.NextDouble()), rng, 0.05f);
            b.Frustum(Vector3.zero, radius, radius * 0.48f, rockH, 7, rock, false, false, false, phase, rng, 0.18f);
            // Neve: cone do cume + "saia" recortada que escorre pela rocha.
            b.Frustum(new Vector3(0f, rockH - height * 0.12f, 0f), radius * 0.6f, 0f, height - rockH + height * 0.12f, 7, EnvPalette.Snow, false, false, false,
                phase + 0.2f, rng, 0.22f);
            b.Frustum(new Vector3(0f, rockH * 0.55f, 0f), radius * 0.78f, radius * 0.5f, rockH * 0.5f, 7, EnvPalette.SnowShade, false, false, false,
                phase + 0.45f, rng, 0.25f);
        }

        // "Decal" de neve: placa fina de contorno irregular rente ao chão (cobre emendas e quebra o calçamento).
        public static void SnowPatch(EnvChunk chunk, Vector3 position, float radius, System.Random rng)
        {
            chunk.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, EnvMeshBuilder.Rand(rng, 0f, 360f), 0f), Vector3.one);
            EnvMeshBuilder o = chunk.Opaque;
            o.NoSnow();
            o.Frustum(Vector3.zero, radius, radius * 0.82f, 0.06f, 11, EnvPalette.Snow, false, true, false, 0f, rng, 0.28f);
            o.Frustum(new Vector3(radius * 0.25f, 0.05f, 0f), radius * 0.5f, radius * 0.3f, 0.06f, 8, EnvPalette.Snow, false, true, false, 1f, rng, 0.25f);
        }

        // "Decal" de pedras: punhado de pedrinhas achatadas espalhadas.
        public static void PebbleScatter(EnvChunk chunk, Vector3 position, float radius, System.Random rng)
        {
            EnvMeshBuilder o = chunk.Opaque;
            int count = 6 + rng.Next(5);
            for (int i = 0; i < count; i++)
            {
                float a = EnvMeshBuilder.Rand(rng, 0f, Mathf.PI * 2f);
                float d = Mathf.Sqrt((float)rng.NextDouble()) * radius;
                float s = EnvMeshBuilder.Rand(rng, 0.12f, 0.3f);
                chunk.Matrix = Matrix4x4.TRS(position + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d), Quaternion.Euler(0f, EnvMeshBuilder.Rand(rng, 0f, 360f), 0f), Vector3.one);
                o.SetSnow(0.8f);
                o.BeveledBox(new Vector3(0f, s * 0.2f, 0f), new Vector3(s * 1.3f, s * 0.5f, s), s * 0.2f, EnvPalette.Vary(EnvPalette.Stone, rng, 0.12f), rng, s * 0.15f);
            }
            o.NoSnow();
        }
    }
}
