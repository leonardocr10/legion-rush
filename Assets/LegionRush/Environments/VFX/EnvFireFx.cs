using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LegionRush.Environments
{
    // Fogo dos braseiros sem luzes dinâmicas: chama em partículas aditivas + faíscas, halo que pisca
    // virado para a câmera e poça de luz quente no chão. A quantidade escala com o perfil de qualidade.
    public class EnvFireFx : MonoBehaviour
    {
        private struct Flame
        {
            public Transform Glow;
            public Transform Pool;
            public ParticleSystem Fire;
            public ParticleSystem Sparks;
            public float Phase;
        }

        private readonly List<Flame> flames = new List<Flame>(32);
        private readonly List<Material> owned = new List<Material>(4);
        private Material fireMaterial;
        private Material glowMaterial;
        private Material poolMaterial;
        private Camera cam;
        private float time;
        private float particleScale = 1f;
        private const float GlowSize = 2.6f;
        private const float PoolSize = 5.5f;

        public int Count => flames.Count;

        public void Initialize(Color torchColor)
        {
            fireMaterial = Own(MaterialLibrary.CreateGlow(ProceduralTextures.SoftCircle, Color.white, true, 1.6f));
            glowMaterial = Own(MaterialLibrary.CreateGlow(ProceduralTextures.SoftCircle, WithAlpha(torchColor, 0.65f), true, 1.8f));
            poolMaterial = Own(MaterialLibrary.CreateGlow(ProceduralTextures.SoftCircle, WithAlpha(torchColor, 0.3f), true, 1f));
        }

        public void Add(Vector3 position, float groundY)
        {
            var root = new GameObject("Flame").transform;
            root.SetParent(transform, false);
            root.position = position;
            var flame = new Flame { Phase = flames.Count * 1.37f };
            if (glowMaterial != null)
                flame.Glow = Quad(root, Vector3.up * 0.1f, Vector3.one * GlowSize, glowMaterial, Quaternion.identity);
            if (poolMaterial != null)
                flame.Pool = Quad(root, new Vector3(0f, groundY - position.y + 0.05f, 0f), new Vector3(PoolSize, PoolSize, 1f), poolMaterial, Quaternion.Euler(90f, 0f, 0f));
            if (fireMaterial != null)
            {
                flame.Fire = CreateFire(root);
                flame.Sparks = CreateSparks(root);
            }
            flames.Add(flame);
        }

        // particles: 0 desliga (só chama de malha + halo), 1 = completo. pools liga a poça de luz no chão.
        public void ApplyQuality(float particles, bool pools)
        {
            particleScale = particles;
            for (int i = 0; i < flames.Count; i++)
            {
                Flame f = flames[i];
                if (f.Pool != null) f.Pool.gameObject.SetActive(pools);
                SetRate(f.Fire, 16f * particles);
                SetRate(f.Sparks, particles >= 0.99f ? 3f : 0f);
            }
        }

        private static void SetRate(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            bool on = rate > 0.01f;
            ps.gameObject.SetActive(on);
            if (!on) return;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;
            if (!ps.isPlaying) ps.Play();
        }

        private void LateUpdate()
        {
            time += Time.deltaTime;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            Quaternion face = cam.transform.rotation;
            for (int i = 0; i < flames.Count; i++)
            {
                Flame f = flames[i];
                float flicker = 0.86f + 0.09f * Mathf.Sin(time * 11f + f.Phase) + 0.05f * Mathf.Sin(time * 23.3f + f.Phase * 1.7f);
                if (f.Glow != null)
                {
                    f.Glow.rotation = face;
                    f.Glow.localScale = Vector3.one * (GlowSize * flicker);
                }
                if (f.Pool != null && f.Pool.gameObject.activeSelf)
                {
                    float s = PoolSize * (0.95f + 0.05f * flicker);
                    f.Pool.localScale = new Vector3(s, s, 1f);
                }
            }
        }

        private ParticleSystem CreateFire(Transform parent)
        {
            var go = new GameObject("Fire");
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.down * 0.35f;
            var ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 24;
            main.gravityModifier = -0.15f;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 16f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.28f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var color = new Gradient();
            color.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f), new GradientColorKey(new Color(1f, 0.55f, 0.12f), 0.45f), new GradientColorKey(new Color(0.9f, 0.2f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.12f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(color);
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.15f)));
            Setup(go, ps);
            return ps;
        }

        private ParticleSystem CreateSparks(Transform parent)
        {
            var go = new GameObject("Sparks");
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startColor = new Color(1f, 0.7f, 0.3f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 8;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 3f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 0.2f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 1.2f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            var color = new Gradient();
            color.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(color);
            Setup(go, ps);
            return ps;
        }

        private void Setup(GameObject go, ParticleSystem ps)
        {
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = fireMaterial;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingFudge = -1f;
            go.SetActive(particleScale > 0.01f);
        }

        private static Transform Quad(Transform parent, Vector3 localPosition, Vector3 scale, Material material, Quaternion rotation)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Collider collider = go.GetComponent<Collider>();
            if (Application.isPlaying) Object.Destroy(collider);
            else Object.DestroyImmediate(collider);
            go.name = "FxQuad";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go.transform;
        }

        private Material Own(Material material)
        {
            if (material != null) owned.Add(material);
            return material;
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        private void OnDestroy()
        {
            foreach (Material m in owned)
            {
                if (m == null) continue;
                if (Application.isPlaying) Destroy(m);
                else DestroyImmediate(m);
            }
            owned.Clear();
        }
    }
}
