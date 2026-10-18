using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LegionRush.Environments
{
    public enum EnvUvMode { Explicit, WorldBox }

    // Acumula geometria estilizada (facetada, com cor por vértice) para um único material.
    // As peças do kit escrevem aqui com uma matriz de transformação; no fim vira uma malha só (poucas draw calls).
    // Cor do vértice: rgb = albedo; a = 1 (reservado).
    public class EnvMeshBuilder
    {
        private readonly List<Vector3> positions = new List<Vector3>(4096);
        private readonly List<Vector3> normals = new List<Vector3>(4096);
        private readonly List<Color32> colors = new List<Color32>(4096);
        private readonly List<Vector2> uvs = new List<Vector2>(4096);
        private readonly List<int> indices = new List<int>(8192);

        public Matrix4x4 Matrix = Matrix4x4.identity;
        public EnvUvMode UvMode = EnvUvMode.Explicit;
        public float WorldUvScale = 1f;

        // Neve automática: faces cujo normal (no mundo) aponta para cima recebem a cor da neve.
        public float SnowThreshold = 2f;
        public Color SnowColor = EnvPalette.Snow;

        public int VertexCount => positions.Count;
        public bool IsEmpty => indices.Count == 0;

        public void Clear()
        {
            positions.Clear();
            normals.Clear();
            colors.Clear();
            uvs.Clear();
            indices.Clear();
        }

        public void SetSnow(float threshold) => SnowThreshold = threshold;
        public void NoSnow() => SnowThreshold = 2f;

        // ---------- Primitivas básicas (coordenadas locais, transformadas por Matrix) ----------

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            Triangle(a, b, c, color, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f));
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color, Vector2 ua, Vector2 ub, Vector2 uc)
        {
            Vector3 wa = Matrix.MultiplyPoint3x4(a);
            Vector3 wb = Matrix.MultiplyPoint3x4(b);
            Vector3 wc = Matrix.MultiplyPoint3x4(c);
            Vector3 n = Vector3.Cross(wb - wa, wc - wa);
            if (n.sqrMagnitude < 1e-12f) return;
            n.Normalize();
            Color col = n.y > SnowThreshold ? SnowColor : color;
            int i = positions.Count;
            AddVertex(wa, n, col, ua);
            AddVertex(wb, n, col, ub);
            AddVertex(wc, n, col, uc);
            indices.Add(i);
            indices.Add(i + 1);
            indices.Add(i + 2);
        }

        // Polígono convexo (pontos em ordem); a face é orientada para fora em relação a "inside".
        public void ConvexFace(IList<Vector3> points, Vector3 inside, Color color)
        {
            int count = points.Count;
            if (count < 3) return;
            Vector3 centroid = Vector3.zero;
            for (int k = 0; k < count; k++) centroid += points[k];
            centroid /= count;
            Vector3 n = Vector3.Cross(points[1] - points[0], points[2] - points[0]);
            for (int k = 3; k < count && n.sqrMagnitude < 1e-10f; k++) n = Vector3.Cross(points[1] - points[0], points[k] - points[0]);
            bool flip = Vector3.Dot(n, centroid - inside) < 0f;
            for (int k = 1; k < count - 1; k++)
            {
                Vector3 a = points[0], b = points[k], c = points[k + 1];
                if (flip) Triangle(a, c, b, color, Uv(a), Uv(c), Uv(b));
                else Triangle(a, b, c, color, Uv(a), Uv(b), Uv(c));
            }
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        {
            Triangle(a, b, c, color, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));
            Triangle(a, c, d, color, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f));
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            Triangle(a, b, c, color, ua, ub, uc);
            Triangle(a, c, d, color, ua, uc, ud);
        }

        // Caixa chanfrada (bordas arredondadas facetadas): a base do visual "cartoon premium".
        // jitter desloca cada canto aleatoriamente (rochas irregulares).
        public void BeveledBox(Vector3 center, Vector3 size, float bevel, Color color, System.Random rng = null, float jitter = 0f,
            Color? topColor = null)
        {
            Vector3 h = size * 0.5f;
            float b = Mathf.Clamp(bevel, 0.001f, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.95f);
            var corners = new Vector3[8, 3];
            for (int ci = 0; ci < 8; ci++)
            {
                float sx = (ci & 1) == 0 ? -1f : 1f;
                float sy = (ci & 2) == 0 ? -1f : 1f;
                float sz = (ci & 4) == 0 ? -1f : 1f;
                Vector3 offset = Vector3.zero;
                if (rng != null && jitter > 0f)
                    offset = new Vector3(Rand(rng, -jitter, jitter), Rand(rng, -jitter, jitter) * (sy > 0 ? 1f : 0.3f), Rand(rng, -jitter, jitter));
                corners[ci, 0] = center + offset + new Vector3(sx * h.x, sy * (h.y - b), sz * (h.z - b));
                corners[ci, 1] = center + offset + new Vector3(sx * (h.x - b), sy * h.y, sz * (h.z - b));
                corners[ci, 2] = center + offset + new Vector3(sx * (h.x - b), sy * (h.y - b), sz * h.z);
            }

            Color top = topColor ?? color;
            var quad = new Vector3[4];
            // Faces principais.
            for (int axis = 0; axis < 3; axis++)
            {
                for (int sign = 0; sign < 2; sign++)
                {
                    int n = 0;
                    int[] order = { 0, 1, 3, 2 };
                    foreach (int o in order)
                    {
                        int ci = CornerIndex(axis, sign, o);
                        quad[n++] = corners[ci, axis];
                    }
                    ConvexFace(quad, center, axis == 1 && sign == 1 ? top : color);
                }
            }
            // Chanfros das 12 arestas.
            for (int a = 0; a < 3; a++)
            {
                int a1 = (a + 1) % 3, a2 = (a + 2) % 3;
                for (int s1 = 0; s1 < 2; s1++)
                {
                    for (int s2 = 0; s2 < 2; s2++)
                    {
                        // Aresta paralela ao eixo a, entre as faces a1 (sinal s1) e a2 (sinal s2).
                        int cLow = Corner(a, 0, a1, s1, a2, s2);
                        int cHigh = Corner(a, 1, a1, s1, a2, s2);
                        quad[0] = corners[cLow, a1];
                        quad[1] = corners[cHigh, a1];
                        quad[2] = corners[cHigh, a2];
                        quad[3] = corners[cLow, a2];
                        bool up = (a1 == 1 && s1 == 1) || (a2 == 1 && s2 == 1);
                        ConvexFace(quad, center, up ? Color.Lerp(color, top, 0.5f) : color);
                    }
                }
            }
            // Cantos.
            var tri = new Vector3[3];
            for (int ci = 0; ci < 8; ci++)
            {
                tri[0] = corners[ci, 0];
                tri[1] = corners[ci, 1];
                tri[2] = corners[ci, 2];
                ConvexFace(tri, center, color);
            }
        }

        public void Box(Vector3 center, Vector3 size, Color color) => BeveledBox(center, size, 0.001f, color);

        // Tronco de cone / cilindro / cone (r1 = 0). smooth = normais suaves na lateral.
        public void Frustum(Vector3 baseCenter, float r0, float r1, float height, int segments, Color color,
            bool capBottom = true, bool capTop = true, bool smooth = false, float phase = 0f, System.Random rng = null,
            float radialJitter = 0f, Color? tipColor = null)
        {
            segments = Mathf.Max(3, segments);
            var ring0 = new Vector3[segments];
            var ring1 = new Vector3[segments];
            for (int s = 0; s < segments; s++)
            {
                float angle = phase + s * Mathf.PI * 2f / segments;
                float jitter = rng != null && radialJitter > 0f ? 1f + Rand(rng, -radialJitter, radialJitter) : 1f;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                ring0[s] = baseCenter + dir * (r0 * jitter);
                ring1[s] = baseCenter + Vector3.up * height + dir * (r1 * jitter);
            }
            Color top = tipColor ?? color;
            Vector3 inside = baseCenter + Vector3.up * (height * 0.5f);
            if (smooth) SmoothSide(ring0, ring1, baseCenter, height, r0, r1, color, top);
            else
            {
                for (int s = 0; s < segments; s++)
                {
                    int t = (s + 1) % segments;
                    if (r1 <= 0.0001f) ConvexFace(new[] { ring0[s], ring0[t], ring1[s] }, inside, color);
                    else ConvexFace(new[] { ring0[s], ring0[t], ring1[t], ring1[s] }, inside, color);
                }
            }
            if (capBottom && r0 > 0.0001f) ConvexFace(ring0, inside, color);
            if (capTop && r1 > 0.0001f) ConvexFace(ring1, inside, top);
        }

        private void SmoothSide(Vector3[] ring0, Vector3[] ring1, Vector3 baseCenter, float height, float r0, float r1, Color color, Color top)
        {
            int segments = ring0.Length;
            float slope = (r0 - r1) / Mathf.Max(0.0001f, height);
            int start = positions.Count;
            for (int s = 0; s <= segments; s++)
            {
                int k = s % segments;
                Vector3 radial = ring0[k] - baseCenter;
                radial.y = 0f;
                radial.Normalize();
                Vector3 localNormal = (radial + Vector3.up * slope).normalized;
                Vector3 n = Matrix.MultiplyVector(localNormal).normalized;
                float u = s / (float)segments;
                Vector3 w0 = Matrix.MultiplyPoint3x4(ring0[k]);
                Vector3 w1 = Matrix.MultiplyPoint3x4(r1 <= 0.0001f ? baseCenter + Vector3.up * height : ring1[k]);
                AddVertex(w0, n, n.y > SnowThreshold ? SnowColor : color, UvFor(w0, n, new Vector2(u, 0f)));
                AddVertex(w1, n, n.y > SnowThreshold ? SnowColor : top, UvFor(w1, n, new Vector2(u, 1f)));
            }
            for (int s = 0; s < segments; s++)
            {
                int i = start + s * 2;
                indices.Add(i); indices.Add(i + 1); indices.Add(i + 3);
                indices.Add(i); indices.Add(i + 3); indices.Add(i + 2);
            }
            // Corrige a orientação se a matriz espelhar.
            if (Matrix.determinant < 0f) FlipLast(segments * 6);
        }

        // Grade de altura com normais suaves (terreno, neve). heightFn(x, z) em coordenadas locais.
        public void HeightGrid(float x0, float x1, float z0, float z1, int cellsX, int cellsZ,
            System.Func<float, float, float> heightFn, System.Func<float, float, Vector3, Color> colorFn)
        {
            int vx = cellsX + 1, vz = cellsZ + 1;
            var grid = new Vector3[vx * vz];
            for (int j = 0; j < vz; j++)
            {
                float z = Mathf.Lerp(z0, z1, j / (float)cellsZ);
                for (int i = 0; i < vx; i++)
                {
                    float x = Mathf.Lerp(x0, x1, i / (float)cellsX);
                    grid[j * vx + i] = new Vector3(x, heightFn(x, z), z);
                }
            }
            int start = positions.Count;
            for (int j = 0; j < vz; j++)
            {
                for (int i = 0; i < vx; i++)
                {
                    Vector3 p = grid[j * vx + i];
                    Vector3 l = grid[j * vx + Mathf.Max(0, i - 1)], r = grid[j * vx + Mathf.Min(vx - 1, i + 1)];
                    Vector3 d = grid[Mathf.Max(0, j - 1) * vx + i], u = grid[Mathf.Min(vz - 1, j + 1) * vx + i];
                    Vector3 localNormal = Vector3.Cross(u - d, r - l).normalized;
                    Vector3 w = Matrix.MultiplyPoint3x4(p);
                    Vector3 n = Matrix.MultiplyVector(localNormal).normalized;
                    AddVertex(w, n, colorFn(p.x, p.z, n), UvFor(w, n, new Vector2(p.x, p.z) * WorldUvScale));
                }
            }
            for (int j = 0; j < cellsZ; j++)
            {
                for (int i = 0; i < cellsX; i++)
                {
                    int a = start + j * vx + i;
                    int b = a + 1, c = a + vx, d = c + 1;
                    indices.Add(a); indices.Add(c); indices.Add(d);
                    indices.Add(a); indices.Add(d); indices.Add(b);
                }
            }
            if (Matrix.determinant < 0f) FlipLast(cellsX * cellsZ * 6);
        }

        // Tecido / placa subdividida: pontos = origin + u*uAxis + v*vAxis (+ deslocamento opcional).
        // colorFn(u, v) define cor e alfa (peso do vento); remap reposiciona (u, v) para recortar o contorno
        // (ponta em V, flâmula triangular). doubleSided cria as duas faces.
        public void Sheet(Vector3 origin, Vector3 uAxis, Vector3 vAxis, int cellsU, int cellsV,
            System.Func<float, float, Color> colorFn, System.Func<float, float, Vector2> remap = null, bool doubleSided = true)
        {
            for (int pass = 0; pass < (doubleSided ? 2 : 1); pass++)
            {
                Vector3 localNormal = Vector3.Cross(vAxis, uAxis).normalized * (pass == 0 ? 1f : -1f);
                Vector3 n = Matrix.MultiplyVector(localNormal).normalized;
                int start = positions.Count;
                for (int j = 0; j <= cellsV; j++)
                {
                    float v = j / (float)cellsV;
                    for (int i = 0; i <= cellsU; i++)
                    {
                        float u = i / (float)cellsU;
                        Vector2 uv = remap != null ? remap(u, v) : new Vector2(u, v);
                        Vector3 w = Matrix.MultiplyPoint3x4(origin + uAxis * uv.x + vAxis * uv.y);
                        positions.Add(w);
                        normals.Add(n);
                        colors.Add(ToVertexColor(colorFn(uv.x, uv.y)));
                        uvs.Add(uv);
                    }
                }
                int stride = cellsU + 1;
                bool flip = (pass == 1) ^ (Matrix.determinant < 0f);
                for (int j = 0; j < cellsV; j++)
                {
                    for (int i = 0; i < cellsU; i++)
                    {
                        int a = start + j * stride + i, b = a + 1, c = a + stride, d = c + 1;
                        if (!flip)
                        {
                            indices.Add(a); indices.Add(c); indices.Add(d);
                            indices.Add(a); indices.Add(d); indices.Add(b);
                        }
                        else
                        {
                            indices.Add(a); indices.Add(d); indices.Add(c);
                            indices.Add(a); indices.Add(b); indices.Add(d);
                        }
                    }
                }
            }
        }

        // Adiciona uma malha pronta (já em coordenadas locais) transformada por Matrix.
        public void Append(Mesh mesh, Color tint)
        {
            Vector3[] v = mesh.vertices;
            Vector3[] n = mesh.normals;
            Color32[] c = mesh.colors32;
            Vector2[] uv = mesh.uv;
            int[] t = mesh.triangles;
            int start = positions.Count;
            for (int k = 0; k < v.Length; k++)
            {
                Vector3 w = Matrix.MultiplyPoint3x4(v[k]);
                Vector3 nn = n.Length > k ? Matrix.MultiplyVector(n[k]).normalized : Vector3.up;
                Color col = c.Length > k ? ((Color)c[k]).gamma * tint : tint;
                AddVertex(w, nn, col, uv.Length > k ? uv[k] : Vector2.zero);
            }
            bool flip = Matrix.determinant < 0f;
            for (int k = 0; k < t.Length; k += 3)
            {
                indices.Add(start + t[k]);
                indices.Add(start + (flip ? t[k + 2] : t[k + 1]));
                indices.Add(start + (flip ? t[k + 1] : t[k + 2]));
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (positions.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(indices, 0, true);
            return mesh;
        }

        // ---------- Internos ----------

        private Vector2 Uv(Vector3 local)
        {
            return new Vector2(local.x, local.y);
        }

        private Vector2 UvFor(Vector3 world, Vector3 normal, Vector2 explicitUv)
        {
            if (UvMode != EnvUvMode.WorldBox) return explicitUv;
            float ax = Mathf.Abs(normal.x), ay = Mathf.Abs(normal.y), az = Mathf.Abs(normal.z);
            Vector2 uv = ay >= ax && ay >= az ? new Vector2(world.x, world.z)
                : ax >= az ? new Vector2(world.z, world.y) : new Vector2(world.x, world.y);
            return uv * WorldUvScale;
        }

        // As cores da paleta são sRGB (como no seletor de cor); o projeto renderiza em espaço linear e a cor
        // do vértice não é convertida pela Unity, então converte aqui (alfa = peso do vento, fica como está).
        private static Color32 ToVertexColor(Color c) => (Color32)c.linear;

        private void AddVertex(Vector3 world, Vector3 normal, Color color, Vector2 uv)
        {
            positions.Add(world);
            normals.Add(normal);
            colors.Add(ToVertexColor(color));
            uvs.Add(UvFor(world, normal, uv));
        }

        private void FlipLast(int count)
        {
            for (int k = indices.Count - count; k < indices.Count; k += 3)
            {
                int tmp = indices[k + 1];
                indices[k + 1] = indices[k + 2];
                indices[k + 2] = tmp;
            }
        }

        // Índice do canto para a face (axis, sign) na posição o (0..3) do contorno.
        private static int CornerIndex(int axis, int sign, int o)
        {
            int a1 = (axis + 1) % 3, a2 = (axis + 2) % 3;
            int s1 = o & 1, s2 = (o >> 1) & 1;
            int ci = 0;
            ci |= sign << axis;
            ci |= s1 << a1;
            ci |= s2 << a2;
            return ci;
        }

        private static int Corner(int a, int sa, int a1, int s1, int a2, int s2) => (sa << a) | (s1 << a1) | (s2 << a2);

        public static float Rand(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
    }
}
