using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LegionRush.Environments
{
    // Portal de melhoria como na folha "Portais de melhoria (escolha)": moldura de pedra clara com friso
    // dourado, estandarte azul pendurado na frente de cada pilar, orbe luminoso na cor da categoria no topo
    // dos pilares, arco claro com friso colorido e pedra-chave dourada, interior translúcido luminoso em
    // degradê e faíscas subindo. Só a aparência: lógica, textos e valores continuam no GateRowController.
    public static class EnvGatePortal
    {
        public const float Height = 3.2f;
        public const float ArchTop = 3.95f;

        private static Material curtainMaterial;
        private static Material sparkMaterial;

        public static bool Available => EnvMaterials.Available;

        // Material translúcido compartilhado (cor vem do vértice); exposto para o gerador de prefabs salvar.
        public static Material CurtainMaterial
        {
            get
            {
                if (curtainMaterial == null)
                {
                    // Mistura alfa (não aditiva): cor saturada como nos PNGs, sem estourar para branco sobre neve.
                    curtainMaterial = MaterialLibrary.CreateGlow(null, Color.white, false, 1.25f);
                    if (curtainMaterial != null) curtainMaterial.name = "LR_Gate_Curtain";
                }
                return curtainMaterial;
            }
        }

        public static Material SparkMaterial
        {
            get
            {
                if (sparkMaterial == null)
                {
                    sparkMaterial = MaterialLibrary.CreateGlow(ProceduralTextures.SoftCircle, Color.white, true, 1.6f);
                    if (sparkMaterial != null) sparkMaterial.name = "LR_Gate_Spark";
                }
                return sparkMaterial;
            }
        }

        // Monta o portal como filho de "gate" (coordenadas locais; frente em -Z). As malhas pertencem ao
        // portal e são destruídas com ele (EnvOwnedMeshes). sparkles = intensidade das faíscas (0 desliga).
        // icon: categoria do portal (desenha o emblema da placa do topo, como nos PNGs 21–24).
        public static void Build(Transform gate, float width, Color color, int seed, float sparkles = 1f, GateCategory? icon = null)
        {
            var rng = new System.Random(seed);
            var chunk = new EnvChunk();
            EnvMeshBuilder m = chunk.Masonry;
            EnvMeshBuilder o = chunk.Opaque;
            EnvMeshBuilder g = chunk.Glow;
            o.NoSnow();
            float half = width * 0.5f - 0.2f;
            // Pedra clara levemente tingida pela cor do portal (moldura azulada, esverdeada, dourada, lilás).
            Color pale = Color.Lerp(new Color(0.88f, 0.9f, 0.95f), color, 0.22f);
            Color paleShade = Color.Lerp(new Color(0.76f, 0.8f, 0.9f), color, 0.25f);
            Color trim = Color.Lerp(color, Color.white, 0.1f);

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * half;
                // Base, fuste e capitel em pedra clara com frisos dourados.
                m.BeveledBox(new Vector3(x, 0.24f, 0f), new Vector3(0.66f, 0.48f, 0.66f), 0.07f, paleShade);
                o.BeveledBox(new Vector3(x, 0.52f, 0f), new Vector3(0.54f, 0.1f, 0.54f), 0.03f, EnvPalette.Gold);
                m.BeveledBox(new Vector3(x, 1.86f, 0f), new Vector3(0.44f, 2.62f, 0.44f), 0.08f, pale);
                o.BeveledBox(new Vector3(x, Height - 0.05f, 0f), new Vector3(0.58f, 0.12f, 0.58f), 0.04f, EnvPalette.Gold);
                m.BeveledBox(new Vector3(x, Height + 0.08f, 0f), new Vector3(0.62f, 0.18f, 0.62f), 0.05f, pale);
                // Estandarte azul com flor-de-lis pendurado na frente do pilar (barra dourada).
                o.BeveledBox(new Vector3(x, 2.72f, -0.26f), new Vector3(0.56f, 0.07f, 0.07f), 0.02f, EnvPalette.Gold);
                EnvCastle.HangingBanner(chunk.Banner, new Vector3(x, 2.68f, -0.3f), 0.46f, 1.45f);
                // Haste dourada com tocha acesa no topo do pilar (referências 21–24).
                o.Frustum(new Vector3(x, Height + 0.17f, 0f), 0.07f, 0.06f, ArchTop - Height + 0.15f, 8, EnvPalette.Gold * 0.9f, false, false, true);
                o.Frustum(new Vector3(x, ArchTop + 0.3f, 0f), 0.08f, 0.2f, 0.2f, 8, EnvPalette.Gold, true, true);
                EnvProps.Flame(g, new Vector3(x, ArchTop + 0.48f, 0f), 0.36f, rng);
                // Neve na base.
                o.BeveledBox(new Vector3(x + side * 0.1f, 0.06f, -0.15f), new Vector3(0.9f, 0.14f, 0.8f), 0.06f, EnvPalette.Snow, rng, 0.05f);
            }

            // Arco: aduelas claras em semicírculo achatado, friso na cor da categoria por dentro e neve por cima.
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
                o.Matrix = Matrix4x4.TRS(c, Quaternion.Euler(0f, 0f, tilt), Vector3.one);
                o.BeveledBox(Vector3.zero, new Vector3(len + 0.06f, 0.42f, 0.42f), 0.07f, i % 2 == 0 ? pale : paleShade);
                o.BeveledBox(new Vector3(0f, -0.19f, -0.16f), new Vector3(len + 0.04f, 0.1f, 0.12f), 0.02f, trim);
                o.BeveledBox(new Vector3(0f, 0.2f, -0.22f), new Vector3(len + 0.02f, 0.05f, 0.03f), 0.01f, EnvPalette.Gold);
                o.BeveledBox(new Vector3(0f, 0.25f, 0f), new Vector3(len + 0.12f, 0.12f, 0.5f), 0.05f, EnvPalette.Snow, rng, 0.02f);
                o.Matrix = saved;
            }
            // Pedra-chave dourada com gema na cor da categoria.
            o.BeveledBox(new Vector3(0f, ArchTop, -0.12f), new Vector3(0.46f, 0.6f, 0.28f), 0.08f, EnvPalette.Gold);
            g.BeveledBox(new Vector3(0f, ArchTop, -0.27f), new Vector3(0.24f, 0.32f, 0.05f), 0.04f, Color.Lerp(color, Color.white, 0.35f) * 0.8f);

            // Placa do topo com emblema da categoria (escudo, espadas, moeda, poção...).
            Emblem(chunk, new Vector3(0f, ArchTop + 0.5f, -0.1f), pale, color, icon ?? GateCategory.Defense);

            // Faixa luminosa no chão (marca a faixa da escolha).
            float innerHalf = half - 0.22f;
            g.BeveledBox(new Vector3(0f, 0.025f, -0.2f), new Vector3(innerHalf * 2f, 0.05f, 1.2f), 0.02f, color * 0.45f);

            var root = new GameObject("Portal").transform;
            root.SetParent(gate, false);
            chunk.Build(root, "Portal", EnvShadowMode.Large);

            BuildCurtain(root, innerHalf, color);
            if (sparkles > 0.01f) BuildSparkles(root, innerHalf, color, sparkles);

            var owner = root.gameObject.AddComponent<EnvOwnedMeshes>();
            owner.Collect();
        }

        private static void Emblem(EnvChunk chunk, Vector3 center, Color plaque, Color color, GateCategory category)
        {
            EnvMeshBuilder o = chunk.Opaque;
            EnvMeshBuilder g = chunk.Glow;
            Matrix4x4 saved = o.Matrix;
            o.BeveledBox(center + new Vector3(0f, 0f, 0.04f), new Vector3(1.05f, 0.95f, 0.16f), 0.1f, EnvPalette.Gold);
            o.BeveledBox(center, new Vector3(0.88f, 0.78f, 0.18f), 0.1f, plaque);
            o.Frustum(center + new Vector3(0f, 0.47f, 0.02f), 0.12f, 0f, 0.22f, 6, EnvPalette.Gold, true, false);
            Vector3 f = center + new Vector3(0f, 0f, -0.11f);
            Color ink = color * 0.85f;
            switch (category)
            {
                case GateCategory.Multiply: // espadas cruzadas
                    for (int k = -1; k <= 1; k += 2)
                    {
                        g.Matrix = saved * Matrix4x4.TRS(f, Quaternion.Euler(0f, 0f, k * 40f), Vector3.one);
                        g.BeveledBox(Vector3.zero, new Vector3(0.08f, 0.62f, 0.04f), 0.02f, ink);
                        g.BeveledBox(new Vector3(0f, -0.16f, 0f), new Vector3(0.22f, 0.06f, 0.05f), 0.02f, ink);
                    }
                    g.Matrix = saved;
                    break;
                case GateCategory.Resource:
                case GateCategory.Upgrade: // moeda
                    g.Matrix = saved * Matrix4x4.TRS(f, Quaternion.Euler(-90f, 0f, 0f), Vector3.one);
                    g.Frustum(Vector3.zero, 0.24f, 0.24f, 0.05f, 12, ink, true, true);
                    g.Matrix = saved;
                    o.Matrix = saved * Matrix4x4.TRS(f + new Vector3(0f, 0f, -0.03f), Quaternion.Euler(-90f, 0f, 0f), Vector3.one);
                    o.Frustum(Vector3.zero, 0.12f, 0.12f, 0.04f, 10, plaque, true, true);
                    o.Matrix = saved;
                    break;
                case GateCategory.Special:
                case GateCategory.Negative: // dois anéis (símbolo arcano)
                    for (int k = -1; k <= 1; k += 2)
                    {
                        g.Matrix = saved * Matrix4x4.TRS(f + new Vector3(k * 0.15f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f), Vector3.one);
                        g.Frustum(Vector3.zero, 0.15f, 0.15f, 0.05f, 10, ink, true, true);
                        g.Matrix = saved;
                        o.Matrix = saved * Matrix4x4.TRS(f + new Vector3(k * 0.15f, 0f, -0.03f), Quaternion.Euler(-90f, 0f, 0f), Vector3.one);
                        o.Frustum(Vector3.zero, 0.07f, 0.07f, 0.04f, 8, plaque, true, true);
                        o.Matrix = saved;
                    }
                    break;
                default: // escudo
                    g.BeveledBox(f + new Vector3(0f, 0.06f, 0f), new Vector3(0.42f, 0.36f, 0.05f), 0.04f, ink);
                    g.Matrix = saved * Matrix4x4.TRS(f + new Vector3(0f, -0.13f, 0f), Quaternion.Euler(0f, 0f, 45f), Vector3.one);
                    g.BeveledBox(Vector3.zero, new Vector3(0.3f, 0.3f, 0.05f), 0.03f, ink);
                    g.Matrix = saved;
                    o.BeveledBox(f + new Vector3(0f, 0f, -0.03f), new Vector3(0.06f, 0.3f, 0.03f), 0.01f, plaque);
                    break;
            }
        }

        // Interior translúcido: mais opaco e claro nas bordas e embaixo, quase transparente no meio (vê-se a pista).
        private static void BuildCurtain(Transform root, float innerHalf, Color color)
        {
            Material material = CurtainMaterial;
            if (material == null) return;
            var b = new EnvMeshBuilder();
            Color rim = Color.Lerp(color, Color.white, 0.55f);
            b.Sheet(new Vector3(-innerHalf, 0.04f, 0.02f), Vector3.right * innerHalf * 2f, Vector3.up * (Height - 0.06f), 10, 8,
                (u, v) =>
                {
                    float edge = Mathf.Max(Mathf.Pow(Mathf.Abs(u - 0.5f) * 2f, 3f), Mathf.Pow(1f - v, 2.5f) * 0.9f);
                    Color c = Color.Lerp(color, rim, edge);
                    // Na cor do vértice a Unity espera sRGB e o construtor converte; alfa vai como está.
                    c.a = Mathf.Lerp(0.72f, 0.95f, edge);
                    return c;
                });
            var go = new GameObject("Curtain");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("Portal_Curtain");
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        // Faíscas subindo pela abertura (partículas leves, compatíveis com Android/WebGL).
        private static void BuildSparkles(Transform root, float innerHalf, Color color, float amount)
        {
            Material material = SparkMaterial;
            if (material == null) return;
            var go = new GameObject("Sparkles");
            go.SetActive(false);
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0f, 0.2f, -0.05f);
            var ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = Color.Lerp(color, Color.white, 0.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 20;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 9f * Mathf.Clamp01(amount);
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(innerHalf * 2f, 0.1f, 0.2f);
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(gradient);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.SetActive(true);
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
