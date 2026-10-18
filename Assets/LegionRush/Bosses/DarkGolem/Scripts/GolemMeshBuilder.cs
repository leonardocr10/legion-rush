using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LegionRush.Bosses.DarkGolem
{
    // Faixa da paleta (coluna da textura 4×1 do material): face da placa, chanfro claro, cavidade escura, filete colorido.
    public enum Swatch { Plate = 0, Edge = 1, Dark = 2, Trim = 3 }

    // Acumula geometria facetada (sombreamento plano) por osso e por material.
    // Cada vértice pertence a um único osso (pele rígida, como as placas de um golem) e escolhe a cor pela UV da paleta.
    public class GolemMeshBuilder
    {
        public const int PaletteColumns = 4;

        private readonly List<Vector3> positions = new List<Vector3>(8192);
        private readonly List<Vector3> normals = new List<Vector3>(8192);
        private readonly List<Vector2> uvs = new List<Vector2>(8192);
        private readonly List<int> bones = new List<int>(8192);
        private readonly List<int>[] triangles;

        public Matrix4x4 Matrix = Matrix4x4.identity;
        public int Bone;
        public int Slot;

        public GolemMeshBuilder(int slotCount)
        {
            triangles = new List<int>[slotCount];
            for (int i = 0; i < slotCount; i++) triangles[i] = new List<int>(4096);
        }

        public int VertexCount => positions.Count;

        public void Place(Vector3 position, Vector3 euler)
        {
            Matrix = Matrix4x4.TRS(position, Quaternion.Euler(euler), Vector3.one);
        }

        // ---------- Primitivas ----------

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Swatch swatch)
        {
            Vector3 wa = Matrix.MultiplyPoint3x4(a);
            Vector3 wb = Matrix.MultiplyPoint3x4(b);
            Vector3 wc = Matrix.MultiplyPoint3x4(c);
            Vector3 n = Vector3.Cross(wb - wa, wc - wa);
            if (n.sqrMagnitude < 1e-14f) return;
            n.Normalize();
            Vector2 uv = new Vector2(((int)swatch + 0.5f) / PaletteColumns, 0.5f);
            int i = positions.Count;
            Add(wa, n, uv);
            Add(wb, n, uv);
            Add(wc, n, uv);
            List<int> list = triangles[Slot];
            list.Add(i);
            list.Add(i + 1);
            list.Add(i + 2);
        }

        // Triângulo com orientação garantida para fora (forma convexa centrada na origem local).
        private void OutwardTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outward, Swatch swatch)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f) Triangle(a, c, b, swatch);
            else Triangle(a, b, c, swatch);
        }

        private void OutwardQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, Swatch swatch)
        {
            OutwardTriangle(a, b, c, outward, swatch);
            OutwardTriangle(a, c, d, outward, swatch);
        }

        // Caixa chanfrada: 6 faces, 12 chanfros e 8 cantos. taper* escala o topo (+Y) em relação à base;
        // lean empurra o topo para a frente (+Z). Faces usam 'face', chanfros usam 'edge'.
        public void ChamferBox(Vector3 size, float bevel, Swatch face = Swatch.Plate, Swatch edge = Swatch.Edge,
            float taperX = 1f, float taperZ = 1f, float lean = 0f)
        {
            Vector3 h = size * 0.5f;
            float b = Mathf.Clamp(bevel, 0.001f, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.95f);
            var shape = new BoxShape(h, b, taperX, taperZ, lean);

            for (int axis = 0; axis < 3; axis++)
            {
                int u = (axis + 1) % 3, v = (axis + 2) % 3;
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 outward = Vector3.zero;
                    outward[axis] = s;
                    Vector3 p0 = Vector3.zero, p1 = Vector3.zero, p2 = Vector3.zero, p3 = Vector3.zero;
                    p0[axis] = p1[axis] = p2[axis] = p3[axis] = s * h[axis];
                    p0[u] = -(h[u] - b); p0[v] = -(h[v] - b);
                    p1[u] = (h[u] - b); p1[v] = -(h[v] - b);
                    p2[u] = (h[u] - b); p2[v] = (h[v] - b);
                    p3[u] = -(h[u] - b); p3[v] = (h[v] - b);
                    shape.Quad(this, p0, p1, p2, p3, outward, face);
                }
            }

            for (int a1 = 0; a1 < 3; a1++)
            {
                int a2 = (a1 + 1) % 3, a3 = (a1 + 2) % 3;
                for (int s1 = -1; s1 <= 1; s1 += 2)
                for (int s2 = -1; s2 <= 1; s2 += 2)
                {
                    Vector3 outward = Vector3.zero;
                    outward[a1] = s1;
                    outward[a2] = s2;
                    Vector3 p0 = Vector3.zero, p1 = Vector3.zero, p2 = Vector3.zero, p3 = Vector3.zero;
                    p0[a1] = s1 * h[a1]; p0[a2] = s2 * (h[a2] - b); p0[a3] = -(h[a3] - b);
                    p1[a1] = s1 * (h[a1] - b); p1[a2] = s2 * h[a2]; p1[a3] = -(h[a3] - b);
                    p2[a1] = s1 * (h[a1] - b); p2[a2] = s2 * h[a2]; p2[a3] = (h[a3] - b);
                    p3[a1] = s1 * h[a1]; p3[a2] = s2 * (h[a2] - b); p3[a3] = (h[a3] - b);
                    shape.Quad(this, p0, p1, p2, p3, outward, edge);
                }
            }

            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                var outward = new Vector3(sx, sy, sz);
                var a = new Vector3(sx * h.x, sy * (h.y - b), sz * (h.z - b));
                var c = new Vector3(sx * (h.x - b), sy * h.y, sz * (h.z - b));
                var d = new Vector3(sx * (h.x - b), sy * (h.y - b), sz * h.z);
                shape.Tri(this, a, c, d, outward, edge);
            }
        }

        // Cristal pontiagudo ao longo de +Y local: base afundada, cintura mais larga e ponta. Facetas alternam tons.
        public void Crystal(float radius, float height, int sides = 5, float twist = 0f)
        {
            sides = Mathf.Clamp(sides, 3, 8);
            float baseY = -0.12f * height;
            float midY = 0.3f * height;
            var tip = new Vector3(0f, height, 0f);
            for (int i = 0; i < sides; i++)
            {
                float a0 = (i / (float)sides) * Mathf.PI * 2f + twist;
                float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f + twist;
                Vector3 b0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius * 0.85f + Vector3.up * baseY;
                Vector3 b1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius * 0.85f + Vector3.up * baseY;
                Vector3 m0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius + Vector3.up * midY;
                Vector3 m1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius + Vector3.up * midY;
                Vector3 outward = new Vector3(Mathf.Cos((a0 + a1) * 0.5f), 0f, Mathf.Sin((a0 + a1) * 0.5f));
                Swatch tone = (i & 1) == 0 ? Swatch.Plate : Swatch.Edge;
                OutwardQuad(b0, b1, m1, m0, outward, Swatch.Dark);
                OutwardTriangle(m0, m1, tip, outward + Vector3.up * 0.5f, tone);
            }
        }

        // Prisma (cabo da maça, pinos).
        public void Prism(float radius, float height, int sides, Swatch side, Swatch cap)
        {
            float half = height * 0.5f;
            for (int i = 0; i < sides; i++)
            {
                float a0 = (i / (float)sides) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius;
                Vector3 d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius;
                Vector3 outward = (d0 + d1).normalized;
                OutwardQuad(d0 - Vector3.up * half, d1 - Vector3.up * half, d1 + Vector3.up * half, d0 + Vector3.up * half, outward, side);
                OutwardTriangle(Vector3.up * half, d0 + Vector3.up * half, d1 + Vector3.up * half, Vector3.up, cap);
                OutwardTriangle(-Vector3.up * half, d0 - Vector3.up * half, d1 - Vector3.up * half, Vector3.down, cap);
            }
        }

        // Esfera facetada (icosaedro subdividido uma vez): bola da maça, rochedos.
        public void FacetedSphere(float radius, Swatch tone, Swatch alternate, float jitter = 0f, int seed = 0)
        {
            var rng = new System.Random(seed);
            List<Vector3> verts = Icosphere.Vertices;
            var displaced = new Vector3[verts.Count];
            for (int i = 0; i < verts.Count; i++)
                displaced[i] = verts[i] * radius * (1f + (jitter > 0f ? ((float)rng.NextDouble() * 2f - 1f) * jitter : 0f));
            List<int> tris = Icosphere.Triangles;
            for (int t = 0; t < tris.Count; t += 3)
            {
                Vector3 a = displaced[tris[t]], b = displaced[tris[t + 1]], c = displaced[tris[t + 2]];
                OutwardTriangle(a, b, c, (a + b + c), ((t / 3) % 3 == 0) ? alternate : tone);
            }
        }

        // ---------- Saída ----------

        public Mesh ToMesh(string name, bool skinned)
        {
            var mesh = new Mesh { name = name };
            if (positions.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            int used = 0;
            for (int i = 0; i < triangles.Length; i++)
                if (triangles[i].Count > 0) used = i + 1;
            mesh.subMeshCount = Mathf.Max(1, used);
            for (int i = 0; i < used; i++) mesh.SetTriangles(triangles[i], i, false);
            if (skinned)
            {
                var weights = new BoneWeight[bones.Count];
                for (int i = 0; i < weights.Length; i++) weights[i] = new BoneWeight { boneIndex0 = bones[i], weight0 = 1f };
                mesh.boneWeights = weights;
            }
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private void Add(Vector3 p, Vector3 n, Vector2 uv)
        {
            positions.Add(p);
            normals.Add(n);
            uvs.Add(uv);
            bones.Add(Bone);
        }

        // Deformação da caixa (afunilar/inclinar) aplicada depois de decidir a orientação de cada face.
        private readonly struct BoxShape
        {
            private readonly Vector3 half;
            private readonly float taperX, taperZ, lean;

            public BoxShape(Vector3 half, float bevel, float taperX, float taperZ, float lean)
            {
                this.half = half;
                this.taperX = taperX;
                this.taperZ = taperZ;
                this.lean = lean;
            }

            private Vector3 Warp(Vector3 p)
            {
                float t = half.y > 0f ? (p.y / half.y + 1f) * 0.5f : 0.5f;
                p.x *= Mathf.Lerp(1f, taperX, t);
                p.z *= Mathf.Lerp(1f, taperZ, t);
                p.z += lean * (t - 0.5f) * 2f * half.y;
                return p;
            }

            public void Quad(GolemMeshBuilder builder, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, Swatch swatch)
            {
                builder.OutwardQuadWarped(Warp(a), Warp(b), Warp(c), Warp(d), a, b, c, outward, swatch);
            }

            public void Tri(GolemMeshBuilder builder, Vector3 a, Vector3 b, Vector3 c, Vector3 outward, Swatch swatch)
            {
                builder.OutwardTriangleWarped(Warp(a), Warp(b), Warp(c), a, b, c, outward, swatch);
            }
        }

        // A orientação é decidida na forma original (convexa) e aplicada à forma deformada.
        private void OutwardQuadWarped(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 oa, Vector3 ob, Vector3 oc, Vector3 outward, Swatch swatch)
        {
            bool flip = Vector3.Dot(Vector3.Cross(ob - oa, oc - oa), outward) < 0f;
            if (flip)
            {
                Triangle(a, c, b, swatch);
                Triangle(a, d, c, swatch);
            }
            else
            {
                Triangle(a, b, c, swatch);
                Triangle(a, c, d, swatch);
            }
        }

        private void OutwardTriangleWarped(Vector3 a, Vector3 b, Vector3 c, Vector3 oa, Vector3 ob, Vector3 oc, Vector3 outward, Swatch swatch)
        {
            if (Vector3.Dot(Vector3.Cross(ob - oa, oc - oa), outward) < 0f) Triangle(a, c, b, swatch);
            else Triangle(a, b, c, swatch);
        }

        private static class Icosphere
        {
            public static readonly List<Vector3> Vertices = new List<Vector3>();
            public static readonly List<int> Triangles = new List<int>();

            static Icosphere()
            {
                float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
                Vector3[] v =
                {
                    new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                    new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                    new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
                };
                int[] f =
                {
                    0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                    3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
                };
                foreach (Vector3 p in v) Vertices.Add(p.normalized);
                var midCache = new Dictionary<long, int>();
                for (int i = 0; i < f.Length; i += 3)
                {
                    int a = f[i], b = f[i + 1], c = f[i + 2];
                    int ab = Mid(a, b, midCache), bc = Mid(b, c, midCache), ca = Mid(c, a, midCache);
                    Triangles.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
            }

            private static int Mid(int a, int b, Dictionary<long, int> cache)
            {
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (cache.TryGetValue(key, out int index)) return index;
                Vertices.Add(((Vertices[a] + Vertices[b]) * 0.5f).normalized);
                index = Vertices.Count - 1;
                cache[key] = index;
                return index;
            }
        }
    }
}
