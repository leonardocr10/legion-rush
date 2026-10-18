using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LegionRush
{
    // Efeitos de luz simulados sem luzes dinâmicas: tochas (chama emissiva + halo + poça de luz),
    // lava emissiva animada, feixes de sol aditivos e partículas ambientes. Tudo escala pelo tier.
    public class WorldAmbience : MonoBehaviour
    {
        private const float TorchStartZ = 10f;
        private const float GodRaySpacing = 30f;
        private const float GodRayTilt = 14f;
        private const float ParticleAhead = 12f;

        private struct TorchFx
        {
            public Transform Glow;
            public Transform Pool;
            public float GlowSize;
            public float PoolSize;
            public float Phase;
        }

        private readonly List<TorchFx> torches = new List<TorchFx>(32);
        private readonly List<Transform> godRays = new List<Transform>(16);
        private readonly List<Material> ownedMaterials = new List<Material>(8);

        private WorldLightingProfile profile;
        private VisualQualityManager quality;
        private Transform follow;
        private Camera cam;
        private ParticleSystem particles;
        private Material lavaMaterial;
        private Material godRayMaterial;
        private Color godRayBase;
        private float particleBaseRate;
        private float particleHeight;
        private float lavaScroll;
        private float time;

        // externalTorches: o cenário já tem braseiros próprios (kit de ambiente), então não cria as tochas simples.
        public void Build(WorldLightingProfile worldProfile, float trackLength, float halfWidth, Transform followTarget,
            VisualQualityManager qualityManager, bool externalTorches = false)
        {
            profile = worldProfile;
            quality = qualityManager;
            follow = followTarget;
            cam = Camera.main;

            if (profile.torchSpacing > 0.5f && !externalTorches) BuildTorches(trackLength, halfWidth);
            if (profile.lavaChannels) BuildLava(trackLength, halfWidth);
            if (profile.godRays) BuildGodRays(trackLength, halfWidth);
            if (profile.particles != AmbientParticleType.None && profile.particleRate > 0) BuildParticles(halfWidth);
            if (quality != null) quality.TierChanged += HandleTierChanged;
            ApplyQuality();
            GameLog.Info($"Ambiência: {torches.Count} tochas, lava {profile.lavaChannels}, {godRays.Count} feixes, partículas {profile.particles}.");
        }

        private void HandleTierChanged(QualityTier tier) => ApplyQuality();

        private void ApplyQuality()
        {
            float ambience = 1f;
            float rayDensity = 1f;
            bool pools = true;
            if (quality != null)
            {
                QualityTierSettings s = quality.Settings;
                ambience = s.AmbienceScale;
                rayDensity = s.GodRayDensity;
                pools = s.TorchPools;
            }

            if (particles != null)
            {
                ParticleSystem.EmissionModule emission = particles.emission;
                emission.enabled = ambience > 0.01f;
                emission.rateOverTime = particleBaseRate * ambience;
            }

            int stride = rayDensity <= 0.001f ? 0 : Mathf.Max(1, Mathf.RoundToInt(1f / rayDensity));
            for (int i = 0; i < godRays.Count; i++)
                godRays[i].gameObject.SetActive(stride > 0 && i % stride == 0);

            for (int i = 0; i < torches.Count; i++)
            {
                if (torches[i].Pool != null) torches[i].Pool.gameObject.SetActive(pools);
            }
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            time += dt;
            if (cam == null) cam = Camera.main;

            if (particles != null && follow != null) PositionEmitter();

            if (cam != null)
            {
                Quaternion faceCamera = cam.transform.rotation;
                for (int i = 0; i < torches.Count; i++)
                {
                    TorchFx torch = torches[i];
                    float flicker = 0.88f + 0.08f * Mathf.Sin(time * 11f + torch.Phase) + 0.05f * Mathf.Sin(time * 23.3f + torch.Phase * 1.7f);
                    if (torch.Glow != null)
                    {
                        torch.Glow.rotation = faceCamera;
                        torch.Glow.localScale = Vector3.one * (torch.GlowSize * flicker);
                    }
                    if (torch.Pool != null && torch.Pool.gameObject.activeSelf)
                    {
                        float pool = torch.PoolSize * (0.95f + 0.05f * flicker);
                        torch.Pool.localScale = new Vector3(pool, pool, 1f);
                    }
                }

                if (godRays.Count > 0)
                {
                    Vector3 forward = cam.transform.forward;
                    forward.y = 0f;
                    if (forward.sqrMagnitude > 1e-4f)
                    {
                        Quaternion rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 0f, GodRayTilt);
                        for (int i = 0; i < godRays.Count; i++)
                        {
                            if (godRays[i].gameObject.activeSelf) godRays[i].rotation = rotation;
                        }
                    }
                }
            }

            if (godRayMaterial != null)
            {
                Color color = godRayBase;
                color.a *= 0.75f + 0.25f * Mathf.Sin(time * 0.6f);
                godRayMaterial.color = color;
            }

            if (lavaMaterial != null)
            {
                lavaScroll = Mathf.Repeat(lavaScroll + dt * 0.03f, 1f);
                lavaMaterial.mainTextureOffset = new Vector2(0f, lavaScroll);
                float pulse = 1.6f + 0.5f * Mathf.Sin(time * 1.7f) + 0.2f * Mathf.Sin(time * 4.3f);
                MaterialLibrary.SetEmission(lavaMaterial, profile.lavaColor * pulse);
            }
        }

        private void BuildTorches(float trackLength, float halfWidth)
        {
            Transform root = CreateChild("Torches");
            Material stone = MaterialLibrary.Get(profile.borderColor * 0.85f, 0f, 0.1f);
            Material metal = MaterialLibrary.Get(new Color(0.22f, 0.2f, 0.2f), 0f, 0.7f);
            Material flame = MaterialLibrary.Get(profile.torchColor, 3.5f, 0f, false);
            Material core = MaterialLibrary.Get(new Color(1f, 0.9f, 0.55f), 4f, 0f, false);
            Material glow = Own(MaterialLibrary.CreateGlow(ProceduralTextures.SoftCircle, WithAlpha(profile.torchColor, 0.55f), true, 1.6f));
            Material pool = Own(MaterialLibrary.CreateGlow(ProceduralTextures.SoftCircle, WithAlpha(profile.torchColor, 0.22f), true, 1f));

            int index = 0;
            for (float z = TorchStartZ; z < trackLength; z += profile.torchSpacing)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Transform torch = new GameObject("Torch").transform;
                    torch.SetParent(root, false);
                    torch.localPosition = new Vector3(side * (halfWidth + 0.95f), 0f, z);
                    PrimitiveFactory.Part(PrimitiveType.Cube, torch, new Vector3(0f, 0.8f, 0f), new Vector3(0.55f, 1.6f, 0.55f), stone);
                    PrimitiveFactory.Part(PrimitiveType.Cylinder, torch, new Vector3(0f, 1.66f, 0f), new Vector3(0.7f, 0.08f, 0.7f), metal);
                    PrimitiveFactory.Part(PrimitiveType.Sphere, torch, new Vector3(0f, 1.95f, 0f), new Vector3(0.38f, 0.5f, 0.38f), flame, default, false);
                    PrimitiveFactory.Part(PrimitiveType.Sphere, torch, new Vector3(0f, 1.9f, 0f), new Vector3(0.2f, 0.3f, 0.2f), core, default, false);

                    var fx = new TorchFx { GlowSize = 2.4f, PoolSize = 5f, Phase = index * 1.37f };
                    if (glow != null)
                        fx.Glow = PrimitiveFactory.Part(PrimitiveType.Quad, torch, new Vector3(0f, 2f, 0f), Vector3.one * fx.GlowSize, glow, default, false);
                    if (pool != null)
                        fx.Pool = PrimitiveFactory.Part(PrimitiveType.Quad, torch, new Vector3(0f, 0.04f, 0f), new Vector3(fx.PoolSize, fx.PoolSize, 1f), pool, new Vector3(90f, 0f, 0f), false);
                    DisableShadowReceive(fx.Glow);
                    DisableShadowReceive(fx.Pool);
                    torches.Add(fx);
                    index++;
                }
            }
        }

        private void BuildLava(float trackLength, float halfWidth)
        {
            Transform root = CreateChild("Lava");
            Texture2D lavaTexture = ProceduralTextures.Lava;
            float length = trackLength + 60f;
            lavaMaterial = Own(new Material(MaterialLibrary.LitShader)
            {
                name = "LR_Lava",
                color = Color.Lerp(Color.black, profile.lavaColor, 0.4f),
                mainTexture = lavaTexture,
                enableInstancing = true
            });
            lavaMaterial.mainTextureScale = new Vector2(1f, length / 3.6f);
            if (lavaMaterial.HasProperty("_EmissionMap")) lavaMaterial.SetTexture("_EmissionMap", lavaTexture);
            if (lavaMaterial.HasProperty("_RimIntensity")) lavaMaterial.SetFloat("_RimIntensity", 0f);
            if (lavaMaterial.HasProperty("_OutlineWidth"))
            {
                lavaMaterial.SetFloat("_OutlineWidth", 0f);
                lavaMaterial.SetShaderPassEnabled("SRPDefaultUnlit", false);
            }
            MaterialLibrary.SetEmission(lavaMaterial, profile.lavaColor * 1.8f);

            float centerZ = trackLength * 0.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                PrimitiveFactory.Part(PrimitiveType.Quad, root, new Vector3(side * (halfWidth + 3.4f), -0.06f, centerZ),
                    new Vector3(3.6f, length, 1f), lavaMaterial, new Vector3(90f, 0f, 0f), false);
            }
        }

        private void BuildGodRays(float trackLength, float halfWidth)
        {
            godRayBase = profile.godRayColor;
            godRayMaterial = Own(MaterialLibrary.CreateGlow(ProceduralTextures.LightBeam, godRayBase, true, 1.3f));
            if (godRayMaterial == null) return;

            Transform root = CreateChild("GodRays");
            var rng = new System.Random(1337);
            int index = 0;
            for (float z = 18f; z < trackLength; z += GodRaySpacing)
            {
                float side = index % 2 == 0 ? -1f : 1f;
                float x = side * (halfWidth * 0.4f + (float)rng.NextDouble() * (halfWidth + 3f));
                float width = 3f + (float)rng.NextDouble() * 1.5f;
                Transform ray = PrimitiveFactory.Part(PrimitiveType.Quad, root, new Vector3(x, 7f, z + (float)rng.NextDouble() * 6f),
                    new Vector3(width, 16f, 1f), godRayMaterial, default, false);
                DisableShadowReceive(ray);
                godRays.Add(ray);
                index++;
            }
        }

        private void BuildParticles(float halfWidth)
        {
            bool additive = profile.particles == AmbientParticleType.Fireflies || profile.particles == AmbientParticleType.Embers;
            Material material = Own(MaterialLibrary.CreateGlow(ProceduralTextures.SoftCircle, Color.white, additive, additive ? 2.2f : 1f));
            if (material == null) return;

            var go = new GameObject("AmbientParticles");
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            particles = go.AddComponent<ParticleSystem>();

            float width = halfWidth * 2f + 14f;
            Vector3 box;
            Vector2 lifetime, size, velocityX, velocityY;
            float noise;
            switch (profile.particles)
            {
                case AmbientParticleType.Snow:
                    particleHeight = 9f; box = new Vector3(width, 0.5f, 44f);
                    lifetime = new Vector2(4.5f, 6.5f); size = new Vector2(0.06f, 0.14f);
                    velocityX = new Vector2(-0.6f, 0.3f); velocityY = new Vector2(-2.4f, -1.5f); noise = 0.25f;
                    break;
                case AmbientParticleType.Dust:
                    particleHeight = 1.8f; box = new Vector3(width, 3.5f, 44f);
                    lifetime = new Vector2(4f, 7f); size = new Vector2(0.35f, 1f);
                    velocityX = new Vector2(0.4f, 1.3f); velocityY = new Vector2(-0.05f, 0.25f); noise = 0.2f;
                    break;
                case AmbientParticleType.Fireflies:
                    particleHeight = 1.6f; box = new Vector3(width, 2.8f, 44f);
                    lifetime = new Vector2(3f, 5.5f); size = new Vector2(0.06f, 0.13f);
                    velocityX = new Vector2(-0.2f, 0.2f); velocityY = new Vector2(-0.1f, 0.25f); noise = 0.8f;
                    break;
                default:
                    particleHeight = 0.6f; box = new Vector3(width, 1f, 44f);
                    lifetime = new Vector2(2.2f, 3.8f); size = new Vector2(0.04f, 0.11f);
                    velocityX = new Vector2(-0.3f, 0.3f); velocityY = new Vector2(1.2f, 2.8f); noise = 0.6f;
                    break;
            }

            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = profile.particleColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.CeilToInt(profile.particleRate * lifetime.y * 1.3f) + 8;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = profile.particleRate;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box;

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(velocityX.x, velocityX.y);
            velocity.y = new ParticleSystem.MinMaxCurve(velocityY.x, velocityY.y);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystem.NoiseModule noiseModule = particles.noise;
            noiseModule.enabled = noise > 0f;
            noiseModule.strength = noise;
            noiseModule.frequency = 0.35f;
            noiseModule.scrollSpeed = 0.2f;
            noiseModule.quality = ParticleSystemNoiseQuality.Low;

            ParticleSystemRenderer particleRenderer = go.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;

            particleBaseRate = profile.particleRate;
            PositionEmitter();
            go.SetActive(true);
        }

        private void PositionEmitter()
        {
            Vector3 anchor = follow != null ? follow.position : Vector3.zero;
            particles.transform.position = new Vector3(0f, particleHeight, anchor.z + ParticleAhead);
        }

        private Transform CreateChild(string label)
        {
            var child = new GameObject(label).transform;
            child.SetParent(transform, false);
            return child;
        }

        private Material Own(Material material)
        {
            if (material != null) ownedMaterials.Add(material);
            return material;
        }

        private static void DisableShadowReceive(Transform target)
        {
            if (target == null) return;
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private void OnDestroy()
        {
            if (quality != null) quality.TierChanged -= HandleTierChanged;
            for (int i = 0; i < ownedMaterials.Count; i++)
            {
                if (ownedMaterials[i] != null) Destroy(ownedMaterials[i]);
            }
            ownedMaterials.Clear();
        }
    }
}
