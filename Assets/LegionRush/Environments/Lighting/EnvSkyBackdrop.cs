using UnityEngine;
using UnityEngine.Rendering;

namespace LegionRush.Environments
{
    // Céu em degradê com brilho do sol, nuvens fofas e anel de montanhas nevadas. Acompanha a câmera
    // (fica sempre "no infinito") e cabe dentro do far clip. Sem luz e sem neblina: custo de 2–3 draw calls.
    public class EnvSkyBackdrop : MonoBehaviour
    {
        public Color Zenith = new Color(0.16f, 0.42f, 0.92f);
        public Color Horizon = new Color(0.72f, 0.86f, 1f);
        public Color SunGlow = new Color(1f, 0.98f, 0.9f);
        public Color MountainSnow = new Color(0.93f, 0.96f, 1f);
        public Color MountainRock = new Color(0.5f, 0.6f, 0.82f);
        public Color MountainHaze = new Color(0.74f, 0.85f, 0.98f);

        private Camera cam;
        private Transform dome;
        private Transform ring;
        private float lastFar = -1f;

        public void Build(Camera targetCamera, Vector3 sunDirection, int seed)
        {
            cam = targetCamera;
            Material skyMaterial = EnvMaterials.Get(EnvMaterial.Sky);
            Material mountainMaterial = EnvMaterials.Get(EnvMaterial.Mountains);
            if (skyMaterial == null || mountainMaterial == null) return;

            dome = CreatePart("SkyDome", BuildDome(sunDirection.normalized), skyMaterial);
            ring = CreatePart("Mountains", BuildMountains(seed), mountainMaterial);
            Follow();
        }

        private Transform CreatePart(string label, Mesh mesh, Material material)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go.transform;
        }

        private void LateUpdate() => Follow();

        private void OnDestroy()
        {
            foreach (MeshFilter f in GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null) continue;
                if (Application.isPlaying) Destroy(f.sharedMesh);
                else DestroyImmediate(f.sharedMesh);
            }
        }

        public void Follow()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || dome == null) return;
            Vector3 p = cam.transform.position;
            float far = cam.farClipPlane;
            if (!Mathf.Approximately(far, lastFar))
            {
                lastFar = far;
                float radius = far * 0.92f;
                dome.localScale = Vector3.one * radius;
                ring.localScale = new Vector3(radius * 0.86f, radius * 0.86f, radius * 0.86f);
            }
            dome.position = p;
            ring.position = new Vector3(p.x, -3f, p.z);
        }

        // Cúpula unitária (normais para dentro) com cor por vértice: zênite → horizonte, halo do sol, nuvens.
        private Mesh BuildDome(Vector3 sunDir)
        {
            const int Rings = 14, Segments = 32;
            var v = new Vector3[(Rings + 1) * (Segments + 1)];
            var c = new Color[v.Length];
            for (int r = 0; r <= Rings; r++)
            {
                // De -0.25 (abaixo do horizonte) até o topo.
                float t = r / (float)Rings;
                float elevation = Mathf.Lerp(-0.3f, Mathf.PI * 0.5f, t * t * 0.7f + t * 0.3f);
                float y = Mathf.Sin(elevation), rad = Mathf.Cos(elevation);
                for (int s = 0; s <= Segments; s++)
                {
                    float a = s / (float)Segments * Mathf.PI * 2f;
                    Vector3 d = new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad);
                    int i = r * (Segments + 1) + s;
                    v[i] = d;
                    float h = Mathf.Clamp01(y);
                    Color col = Color.Lerp(Horizon, Zenith, Mathf.Pow(h, 0.35f));
                    float sun = Mathf.Max(0f, Vector3.Dot(d, -sunDir));
                    col = Color.Lerp(col, SunGlow, Mathf.Pow(sun, 6f) * 0.55f);
                    float cloud = Mathf.Clamp01((EnvTerrain.Noise(a * 3f, h * 6f) - 0.55f) * 3f) * Mathf.Clamp01(1f - Mathf.Abs(h - 0.18f) * 4f);
                    col = Color.Lerp(col, Color.white, cloud * 0.6f);
                    if (y < 0f) col = Horizon;
                    c[i] = col.linear;
                }
            }
            var tris = new int[Rings * Segments * 6];
            int k = 0;
            for (int r = 0; r < Rings; r++)
            {
                for (int s = 0; s < Segments; s++)
                {
                    int a = r * (Segments + 1) + s, b = a + 1, cc = a + Segments + 1, d = cc + 1;
                    // Vistas de dentro.
                    tris[k++] = a; tris[k++] = b; tris[k++] = d;
                    tris[k++] = a; tris[k++] = d; tris[k++] = cc;
                }
            }
            var mesh = new Mesh { name = "LR_Env_SkyDome" };
            mesh.vertices = v;
            mesh.colors = c;
            mesh.triangles = tris;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            return mesh;
        }

        // Anel de picos nevados (raio 1) em 2 camadas: as distantes mais claras (névoa), as próximas mais nítidas.
        private Mesh BuildMountains(int seed)
        {
            var b = new EnvMeshBuilder();
            var rng = new System.Random(seed);
            for (int layer = 0; layer < 2; layer++)
            {
                float radius = layer == 0 ? 1f : 0.86f;
                float haze = layer == 0 ? 0.55f : 0.25f;
                int peaks = layer == 0 ? 34 : 26;
                float phase = (float)rng.NextDouble() * Mathf.PI * 2f;
                for (int i = 0; i < peaks; i++)
                {
                    float a = phase + (i + EnvMeshBuilder.Rand(rng, -0.25f, 0.25f)) * Mathf.PI * 2f / peaks;
                    float width = EnvMeshBuilder.Rand(rng, 0.16f, 0.28f);
                    float height = EnvMeshBuilder.Rand(rng, 0.09f, 0.17f) * (layer == 0 ? 1.1f : 0.8f);
                    Peak(b, a, radius, width, height, haze, rng);
                }
            }
            Mesh mesh = b.ToMesh("LR_Env_Mountains");
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2.5f);
            return mesh;
        }

        // Pico facetado: frente com 2 faces (luz e sombra), cume branco, base dissolvida na névoa do horizonte.
        private void Peak(EnvMeshBuilder b, float angle, float radius, float width, float height, float haze, System.Random rng)
        {
            Vector3 center = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            Vector3 inward = -center.normalized;
            float tipShift = EnvMeshBuilder.Rand(rng, -0.25f, 0.25f) * width;
            Vector3 left = center - tangent * width * 0.5f + Vector3.down * 0.05f;
            Vector3 right = center + tangent * width * 0.5f + Vector3.down * 0.05f;
            Vector3 tip = center + tangent * tipShift + Vector3.up * height + inward * 0.02f;
            Vector3 front = center + inward * width * 0.18f + tangent * (tipShift * 0.5f) + Vector3.up * height * 0.15f;
            Vector3 snowL = Vector3.Lerp(tip, left, 0.32f), snowR = Vector3.Lerp(tip, right, 0.32f), snowF = Vector3.Lerp(tip, front, 0.38f);

            Color rockLit = Color.Lerp(MountainRock * 1.15f, MountainHaze, haze);
            Color rockDark = Color.Lerp(MountainRock * 0.82f, MountainHaze, haze);
            Color snowLit = Color.Lerp(MountainSnow, MountainHaze, haze * 0.5f);
            Color snowDark = Color.Lerp(MountainSnow * 0.84f, MountainHaze, haze * 0.5f);
            Color baseHaze = MountainHaze;
            rockLit.a = rockDark.a = snowLit.a = snowDark.a = baseHaze.a = 1f;

            // Faces vistas do centro (de dentro do anel): ordem horária vista de dentro.
            AddTri(b, left, snowL, front, rockLit, rockLit, baseHaze);
            AddTri(b, snowL, snowF, front, rockLit, rockLit, rockLit);
            AddTri(b, front, snowF, right, rockDark, rockDark, baseHaze);
            AddTri(b, snowF, snowR, right, rockDark, rockDark, baseHaze);
            AddTri(b, snowL, tip, snowF, snowLit, snowLit, snowLit);
            AddTri(b, snowF, tip, snowR, snowDark, snowDark, snowDark);
            AddTri(b, left, front, right, baseHaze, baseHaze, baseHaze);
        }

        private static void AddTri(EnvMeshBuilder b, Vector3 p0, Vector3 p1, Vector3 p2, Color c0, Color c1, Color c2)
        {
            // Unlit: a cor do vértice é o que aparece; aproveita a cor média por face (estilo facetado).
            Color avg = (c0 + c1 + c2) / 3f;
            b.Triangle(p0, p1, p2, avg);
            b.Triangle(p0, p2, p1, avg);
        }
    }
}
