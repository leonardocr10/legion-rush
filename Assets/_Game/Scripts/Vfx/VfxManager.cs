using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LegionRush
{
    // Efeitos leves e pooled: explosões, anéis, indicadores de ataque (telegraph), feixes e bolhas de escudo.
    // Um material por forma; cor por instância via MaterialPropertyBlock. Sem alocações por quadro.
    public class VfxManager : MonoBehaviour
    {
        private const int MaxActive = 160;
        private const float GroundY = 0.06f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public static VfxManager Instance { get; private set; }

        private enum Shape { Burst, Ring, Disc, Rect, Beam, Bubble }

        private struct Fx
        {
            public Transform Body;
            public Renderer Renderer;
            public Shape Shape;
            public float Age;
            public float Duration;
            public float StartSize;
            public float EndSize;
            public Color Color;
            public Transform Follow;
            public Vector3 Position;
            public Vector3 Direction;
            public float Length;
            public float Width;
            public bool Grow;
            public bool IsSphere;
        }

        private readonly List<Fx> active = new List<Fx>(MaxActive);
        private readonly Stack<Transform> idleQuads = new Stack<Transform>(64);
        private readonly Stack<Transform> idleSpheres = new Stack<Transform>(8);
        private readonly List<Material> ownedMaterials = new List<Material>(6);
        private MaterialPropertyBlock block;
        private Material burstMaterial;
        private Material ringMaterial;
        private Material discMaterial;
        private Material rectMaterial;
        private Material beamMaterial;
        private Material bubbleMaterial;
        private Camera cam;
        private bool ready;

        public void Initialize()
        {
            Instance = this;
            block = new MaterialPropertyBlock();
            burstMaterial = Own(MaterialLibrary.CreateGlow(ProceduralTextures.SoftCircle, Color.white, true, 1.8f));
            ringMaterial = Own(MaterialLibrary.CreateGlow(ProceduralTextures.Ring, Color.white, false, 1f));
            discMaterial = Own(MaterialLibrary.CreateGlow(ProceduralTextures.SoftCircle, Color.white, false, 1f));
            rectMaterial = Own(MaterialLibrary.CreateGlow(ProceduralTextures.SoftSquare, Color.white, false, 1f));
            beamMaterial = Own(MaterialLibrary.CreateGlow(ProceduralTextures.SoftSquare, Color.white, true, 2.5f));
            bubbleMaterial = Own(MaterialLibrary.CreateGlow(null, Color.white, true, 1f));
            ready = burstMaterial != null;
            if (!ready) GameLog.Warn("VfxManager sem shader de brilho; efeitos visuais desativados.");
        }

        // Clarão esférico (impactos, magias, curas).
        public void Burst(Vector3 position, Color color, float radius, float duration = 0.45f)
        {
            Spawn(Shape.Burst, burstMaterial, false, position, Vector3.forward, color, radius * 0.35f, radius, duration, 0f, 0f, null, true);
        }

        // Anel no chão que expande (ondas, curas em área, explosões).
        public void GroundRing(Vector3 center, Color color, float radius, float duration = 0.6f)
        {
            Spawn(Shape.Ring, ringMaterial, false, Ground(center), Vector3.forward, color, radius * 0.2f, radius, duration, 0f, 0f, null, true);
        }

        // Aviso de ataque circular: anel fixo + disco que preenche até o impacto.
        public void Telegraph(Vector3 center, float radius, float duration, Color color)
        {
            Vector3 p = Ground(center);
            Spawn(Shape.Ring, ringMaterial, false, p, Vector3.forward, color, radius, radius, duration, 0f, 0f, null, false);
            Color fill = color;
            fill.a *= 0.55f;
            Spawn(Shape.Disc, discMaterial, false, p, Vector3.forward, fill, 0f, radius, duration, 0f, 0f, null, true);
        }

        // Aviso de ataque em linha (sopro, laser, fissura): retângulo que se estende a partir da origem.
        public void TelegraphLine(Vector3 origin, Vector3 direction, float length, float halfWidth, float duration, Color color)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-5f) direction = Vector3.forward;
            Spawn(Shape.Rect, rectMaterial, false, Ground(origin), direction.normalized, color, 0f, 1f, duration, length, halfWidth * 2f, null, true);
        }

        // Feixe brilhante momentâneo (execução de laser/sopro).
        public void Beam(Vector3 origin, Vector3 direction, float length, float halfWidth, Color color, float duration = 0.4f)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-5f) direction = Vector3.forward;
            Spawn(Shape.Beam, beamMaterial, false, Ground(origin) + Vector3.up * 0.4f, direction.normalized, color, 1f, 1f, duration, length, halfWidth * 2f, null, false);
        }

        // Bolha que acompanha um alvo (escudo coletivo).
        public void Bubble(Transform follow, float radius, Color color, float duration)
        {
            if (follow == null) return;
            Spawn(Shape.Bubble, bubbleMaterial, true, follow.position, Vector3.forward, color, radius, radius, duration, 0f, 0f, follow, false);
        }

        private void Spawn(Shape shape, Material material, bool sphere, Vector3 position, Vector3 direction, Color color,
            float startSize, float endSize, float duration, float length, float width, Transform follow, bool grow)
        {
            if (!ready || material == null || active.Count >= MaxActive || duration <= 0f) return;
            Transform body = sphere ? Take(idleSpheres, PrimitiveType.Sphere) : Take(idleQuads, PrimitiveType.Quad);
            Renderer renderer = body.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            body.gameObject.SetActive(true);

            var fx = new Fx
            {
                Body = body,
                Renderer = renderer,
                Shape = shape,
                Age = 0f,
                Duration = duration,
                StartSize = startSize,
                EndSize = endSize,
                Color = color,
                Follow = follow,
                Position = position,
                Direction = direction,
                Length = length,
                Width = width,
                Grow = grow,
                IsSphere = sphere
            };
            Apply(ref fx, 0f);
            active.Add(fx);
        }

        private void LateUpdate()
        {
            if (active.Count == 0) return;
            float dt = Time.deltaTime;
            if (cam == null) cam = Camera.main;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Fx fx = active[i];
                fx.Age += dt;
                float t = Mathf.Clamp01(fx.Age / fx.Duration);
                if (fx.Age >= fx.Duration || (fx.Follow == null && fx.Shape == Shape.Bubble))
                {
                    Release(fx);
                    active[i] = active[active.Count - 1];
                    active.RemoveAt(active.Count - 1);
                    continue;
                }
                Apply(ref fx, t);
                active[i] = fx;
            }
        }

        private void Apply(ref Fx fx, float t)
        {
            float ease = 1f - (1f - t) * (1f - t);
            Color color = fx.Color;
            Transform body = fx.Body;
            switch (fx.Shape)
            {
                case Shape.Burst:
                {
                    float size = Mathf.Lerp(fx.StartSize, fx.EndSize, ease) * 2f;
                    body.position = fx.Position;
                    body.localScale = new Vector3(size, size, size);
                    if (cam != null) body.rotation = cam.transform.rotation;
                    color.a *= Mathf.Pow(1f - t, 1.5f);
                    break;
                }
                case Shape.Ring:
                case Shape.Disc:
                {
                    float size = (fx.Grow ? Mathf.Lerp(fx.StartSize, fx.EndSize, fx.Shape == Shape.Disc ? t : ease) : fx.EndSize) * 2f;
                    body.position = fx.Position;
                    body.rotation = Quaternion.Euler(90f, 0f, 0f);
                    body.localScale = new Vector3(size, size, 1f);
                    if (fx.Shape == Shape.Ring && fx.Grow) color.a *= 1f - t;
                    else if (fx.Shape == Shape.Ring) color.a *= 0.75f + 0.25f * Mathf.Sin(fx.Age * 18f);
                    break;
                }
                case Shape.Rect:
                case Shape.Beam:
                {
                    float fill = fx.Grow ? Mathf.Max(0.02f, t) : 1f;
                    float length = fx.Length * fill;
                    body.position = fx.Position + fx.Direction * (length * 0.5f);
                    body.rotation = Quaternion.LookRotation(fx.Direction, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
                    body.localScale = new Vector3(fx.Width, length, 1f);
                    if (fx.Shape == Shape.Beam) color.a *= 1f - t * t;
                    break;
                }
                default:
                {
                    if (fx.Follow != null) fx.Position = fx.Follow.position + Vector3.up * 0.6f;
                    float pulse = 1f + 0.04f * Mathf.Sin(fx.Age * 6f);
                    float size = fx.EndSize * 2f * pulse;
                    body.position = fx.Position;
                    body.localScale = new Vector3(size, size * 0.6f, size);
                    float fade = Mathf.Min(1f, fx.Age / 0.25f) * Mathf.Min(1f, (fx.Duration - fx.Age) / 0.4f);
                    color.a *= Mathf.Clamp01(fade);
                    break;
                }
            }
            block.Clear();
            block.SetColor(BaseColorId, color);
            fx.Renderer.SetPropertyBlock(block);
        }

        private Transform Take(Stack<Transform> pool, PrimitiveType type)
        {
            if (pool.Count > 0) return pool.Pop();
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = "Fx_" + type;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            go.transform.SetParent(transform, false);
            Renderer renderer = go.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go.transform;
        }

        private void Release(Fx fx)
        {
            if (fx.Body == null) return;
            fx.Body.gameObject.SetActive(false);
            if (fx.IsSphere) idleSpheres.Push(fx.Body);
            else idleQuads.Push(fx.Body);
        }

        private static Vector3 Ground(Vector3 p) => new Vector3(p.x, GroundY, p.z);

        private Material Own(Material material)
        {
            if (material != null) ownedMaterials.Add(material);
            return material;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (Material material in ownedMaterials)
            {
                if (material != null) Destroy(material);
            }
        }
    }

    // Atalhos estáticos seguros (sem VfxManager, nada acontece).
    public static class Vfx
    {
        public static void Burst(Vector3 position, Color color, float radius, float duration = 0.45f)
        {
            if (VfxManager.Instance != null) VfxManager.Instance.Burst(position, color, radius, duration);
        }

        public static void Ring(Vector3 center, Color color, float radius, float duration = 0.6f)
        {
            if (VfxManager.Instance != null) VfxManager.Instance.GroundRing(center, color, radius, duration);
        }

        public static void Telegraph(Vector3 center, float radius, float duration, Color color)
        {
            if (VfxManager.Instance != null) VfxManager.Instance.Telegraph(center, radius, duration, color);
        }

        public static void TelegraphLine(Vector3 origin, Vector3 direction, float length, float halfWidth, float duration, Color color)
        {
            if (VfxManager.Instance != null) VfxManager.Instance.TelegraphLine(origin, direction, length, halfWidth, duration, color);
        }

        public static void Beam(Vector3 origin, Vector3 direction, float length, float halfWidth, Color color, float duration = 0.4f)
        {
            if (VfxManager.Instance != null) VfxManager.Instance.Beam(origin, direction, length, halfWidth, color, duration);
        }

        public static void Bubble(Transform follow, float radius, Color color, float duration)
        {
            if (VfxManager.Instance != null) VfxManager.Instance.Bubble(follow, radius, color, duration);
        }
    }
}
