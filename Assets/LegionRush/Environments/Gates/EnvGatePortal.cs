using System.Collections.Generic;
using UnityEngine;

namespace LegionRush.Environments
{
    // Portal de melhoria no estilo da referência: pilares de pedra clara com base e capitel dourados, orbe
    // luminoso na cor da categoria, arco na cor da categoria com friso dourado e neve no topo, cortina de
    // energia com degradê (mais clara nas bordas) e faixa luminosa no chão. Só a aparência: a lógica, os
    // textos e os valores continuam no GateRowController (dados reais do jogo).
    public static class EnvGatePortal
    {
        public const float Height = 3.2f;
        public const float ArchTop = 3.95f;

        public static bool Available => EnvMaterials.Available;

        // Monta o portal como filho de "gate" (coordenadas locais; frente em -Z). As malhas pertencem ao
        // portal e são destruídas com ele (EnvOwnedMeshes).
        public static void Build(Transform gate, float width, Color color, int seed)
        {
            var rng = new System.Random(seed);
            var chunk = new EnvChunk();
            EnvMeshBuilder m = chunk.Masonry;
            EnvMeshBuilder o = chunk.Opaque;
            EnvMeshBuilder g = chunk.Glow;
            o.NoSnow();
            float half = width * 0.5f - 0.2f;
            Color frame = Color.Lerp(color, Color.white, 0.15f);
            Color frameDark = Color.Lerp(color, Color.black, 0.35f);

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * half;
                // Base, fuste e capitel.
                m.BeveledBox(new Vector3(x, 0.22f, 0f), new Vector3(0.62f, 0.44f, 0.62f), 0.07f, EnvPalette.StoneLight);
                o.BeveledBox(new Vector3(x, 0.5f, 0f), new Vector3(0.5f, 0.12f, 0.5f), 0.04f, EnvPalette.Gold);
                m.BeveledBox(new Vector3(x, 1.85f, 0f), new Vector3(0.4f, 2.6f, 0.4f), 0.08f, EnvPalette.StoneLight);
                // Faixa na cor da categoria ao longo do fuste (frente).
                o.BeveledBox(new Vector3(x, 1.85f, -0.19f), new Vector3(0.18f, 2.2f, 0.05f), 0.02f, frame);
                o.BeveledBox(new Vector3(x, Height + 0.02f, 0f), new Vector3(0.56f, 0.16f, 0.56f), 0.05f, EnvPalette.Gold);
                // Orbe no topo do pilar (acima do arco).
                o.Frustum(new Vector3(x, ArchTop + 0.05f, 0f), 0.2f, 0.12f, 0.16f, 8, EnvPalette.Gold, true, true);
                g.Frustum(new Vector3(x, ArchTop + 0.2f, 0f), 0.15f, 0.15f, 0.26f, 8, color, true, false, true);
                g.Frustum(new Vector3(x, ArchTop + 0.46f, 0f), 0.15f, 0f, 0.16f, 8, color, false, false, true);
                // Neve na base.
                o.BeveledBox(new Vector3(x + side * 0.1f, 0.06f, -0.15f), new Vector3(0.9f, 0.14f, 0.8f), 0.06f, EnvPalette.Snow, rng, 0.05f);
            }

            // Arco: aduelas em semicírculo achatado entre os capitéis.
            const int Stones = 9;
            float span = half;
            float rise = ArchTop - Height;
            for (int i = 0; i < Stones; i++)
            {
                float a0 = Mathf.PI * i / Stones, a1 = Mathf.PI * (i + 1) / Stones;
                float am = (a0 + a1) * 0.5f;
                Vector3 c = new Vector3(-Mathf.Cos(am) * span, Height + Mathf.Sin(am) * rise, 0f);
                float len = Vector2.Distance(new Vector2(Mathf.Cos(a0) * span, Mathf.Sin(a0) * rise), new Vector2(Mathf.Cos(a1) * span, Mathf.Sin(a1) * rise));
                float tilt = Mathf.Atan2(Mathf.Cos(am) * rise, Mathf.Sin(am) * span) * Mathf.Rad2Deg;
                Matrix4x4 saved = o.Matrix;
                Matrix4x4 local = Matrix4x4.TRS(c, Quaternion.Euler(0f, 0f, tilt), Vector3.one);
                o.Matrix = local;
                o.BeveledBox(Vector3.zero, new Vector3(len + 0.06f, 0.42f, 0.42f), 0.07f, i % 2 == 0 ? frame : frameDark);
                o.BeveledBox(new Vector3(0f, -0.2f, -0.2f), new Vector3(len + 0.04f, 0.08f, 0.06f), 0.02f, EnvPalette.Gold);
                o.BeveledBox(new Vector3(0f, 0.25f, 0f), new Vector3(len + 0.12f, 0.12f, 0.5f), 0.05f, EnvPalette.Snow, rng, 0.02f);
                o.Matrix = saved;
            }
            // Pedra-chave dourada.
            o.BeveledBox(new Vector3(0f, ArchTop, -0.12f), new Vector3(0.42f, 0.56f, 0.28f), 0.08f, EnvPalette.Gold);
            g.BeveledBox(new Vector3(0f, ArchTop, -0.27f), new Vector3(0.22f, 0.3f, 0.05f), 0.04f, Color.Lerp(color, Color.white, 0.4f));

            // Cortina de energia: degradê da cor para quase branco nas bordas e embaixo.
            float innerHalf = half - 0.2f;
            Color core = color * 0.3f;
            Color rim = Color.Lerp(color, Color.white, 0.4f) * 0.62f;
            g.Sheet(new Vector3(-innerHalf, 0.05f, 0.02f), Vector3.right * innerHalf * 2f, Vector3.up * (Height - 0.05f), 8, 6,
                (u, v) =>
                {
                    float edge = Mathf.Max(Mathf.Pow(Mathf.Abs(u - 0.5f) * 2f, 4f), Mathf.Pow(1f - v, 3f) * 0.8f);
                    Color c = Color.Lerp(core, rim, Mathf.Clamp01(edge));
                    c.a = 0f;
                    return c;
                });
            // Faixa luminosa no chão (marca a faixa da escolha).
            g.BeveledBox(new Vector3(0f, 0.025f, -0.2f), new Vector3(innerHalf * 2f, 0.05f, 1.2f), 0.02f, color * 0.6f);

            var root = new GameObject("Portal").transform;
            root.SetParent(gate, false);
            chunk.Build(root, "Portal", EnvShadowMode.Large);
            var owner = root.gameObject.AddComponent<EnvOwnedMeshes>();
            owner.Collect();
        }
    }

    // Destrói as malhas geradas em runtime pelos filhos quando o objeto some (evita vazamento entre fases).
    public class EnvOwnedMeshes : MonoBehaviour
    {
        private readonly List<Mesh> meshes = new List<Mesh>();

        public void Collect()
        {
            foreach (MeshFilter f in GetComponentsInChildren<MeshFilter>(true))
                if (f.sharedMesh != null) meshes.Add(f.sharedMesh);
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in meshes)
            {
                if (mesh == null) continue;
                if (Application.isPlaying) Destroy(mesh);
                else DestroyImmediate(mesh);
            }
        }
    }
}
