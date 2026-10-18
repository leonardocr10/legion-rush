using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Partículas de UI baratas (neve, faíscas mágicas): um único Graphic desenha todos os quads num só draw call.
    // Fica num Canvas próprio (EffectsLayer) para que a malha refeita a cada quadro não suje o resto da tela.
    [RequireComponent(typeof(CanvasRenderer))]
    public class SplashParticleField : MaskableGraphic
    {
        [SerializeField] private Sprite sprite;
        [SerializeField, Range(1, 200)] private int count = 60;
        [SerializeField] private Vector2 sizeRange = new Vector2(6f, 16f);
        [SerializeField] private Vector2 speedRange = new Vector2(30f, 70f);
        [SerializeField, Tooltip("Direção base do movimento (normalizada).")] private Vector2 direction = new Vector2(0f, -1f);
        [SerializeField] private float sway = 18f;
        [SerializeField] private Vector2 lifeRange = new Vector2(6f, 12f);
        [SerializeField, Tooltip("Nasce dentro de uma elipse em volta do centro em vez do retângulo todo.")] private bool radial;
        [SerializeField] private Color tint = Color.white;

        private struct Particle
        {
            public Vector2 position;
            public Vector2 velocity;
            public float size;
            public float age;
            public float life;
            public float phase;
        }

        private Particle[] particles;
        private uint rng = 2463534242u;

        public override Texture mainTexture => sprite != null ? sprite.texture : s_WhiteTexture;

        public void Configure(Sprite dot, int amount, Vector2 size, Vector2 speed, Vector2 dir, float swayAmount, Vector2 life,
            bool radialSpawn, Color color)
        {
            sprite = dot;
            count = amount;
            sizeRange = size;
            speedRange = speed;
            direction = dir;
            sway = swayAmount;
            lifeRange = life;
            radial = radialSpawn;
            tint = color;
            raycastTarget = false;
            particles = null;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            Prewarm();
        }

        // Distribui as partículas no meio da vida (a tela já abre com neve caindo).
        public void Prewarm()
        {
            particles = new Particle[Mathf.Max(1, count)];
            for (int i = 0; i < particles.Length; i++)
            {
                Spawn(ref particles[i]);
                particles[i].age = Random01() * particles[i].life;
                particles[i].position += particles[i].velocity * particles[i].age;
            }
            SetVerticesDirty();
        }

        private void Update()
        {
            if (particles == null || !Application.isPlaying) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            Rect r = rectTransform.rect;
            for (int i = 0; i < particles.Length; i++)
            {
                ref Particle p = ref particles[i];
                p.age += dt;
                p.position += p.velocity * dt;
                bool outside = p.position.y < r.yMin - 40f || p.position.y > r.yMax + 40f;
                if (p.age >= p.life || outside) Spawn(ref p);
            }
            SetVerticesDirty();
        }

        private void Spawn(ref Particle p)
        {
            Rect r = rectTransform.rect;
            if (radial)
            {
                float a = Random01() * Mathf.PI * 2f;
                float d = Mathf.Sqrt(Random01());
                p.position = r.center + new Vector2(Mathf.Cos(a) * r.width, Mathf.Sin(a) * r.height) * 0.5f * d;
            }
            else
            {
                // neve: nasce em qualquer altura no primeiro preenchimento, depois pelo topo
                p.position = new Vector2(Mathf.Lerp(r.xMin, r.xMax, Random01()), Mathf.Lerp(r.yMin, r.yMax, Random01()));
            }
            float speed = Mathf.Lerp(speedRange.x, speedRange.y, Random01());
            Vector2 dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.down;
            p.velocity = dir * speed;
            p.size = Mathf.Lerp(sizeRange.x, sizeRange.y, Random01() * Random01()); // mais pequenas que grandes
            p.life = Mathf.Lerp(lifeRange.x, lifeRange.y, Random01());
            p.age = 0f;
            p.phase = Random01() * 10f;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (particles == null) return;
            Vector4 uv = sprite != null ? UnityEngine.Sprites.DataUtility.GetOuterUV(sprite) : new Vector4(0, 0, 1, 1);
            float time = Application.isPlaying ? Time.unscaledTime : 0f;
            for (int i = 0; i < particles.Length; i++)
            {
                Particle p = particles[i];
                float k = p.age / Mathf.Max(0.01f, p.life);
                float fade = Mathf.Clamp01(k * 5f) * Mathf.Clamp01((1f - k) * 4f);
                Color32 c = tint * color;
                c.a = (byte)(c.a * fade);
                Vector2 pos = p.position + new Vector2(Mathf.Sin(time * 0.9f + p.phase) * sway, 0f);
                float h = p.size * 0.5f;
                int v = vh.currentVertCount;
                vh.AddVert(new Vector3(pos.x - h, pos.y - h), c, new Vector2(uv.x, uv.y));
                vh.AddVert(new Vector3(pos.x - h, pos.y + h), c, new Vector2(uv.x, uv.w));
                vh.AddVert(new Vector3(pos.x + h, pos.y + h), c, new Vector2(uv.z, uv.w));
                vh.AddVert(new Vector3(pos.x + h, pos.y - h), c, new Vector2(uv.z, uv.y));
                vh.AddTriangle(v, v + 1, v + 2);
                vh.AddTriangle(v + 2, v + 3, v);
            }
        }

        // xorshift: determinístico e sem alocação
        private float Random01()
        {
            rng ^= rng << 13;
            rng ^= rng >> 17;
            rng ^= rng << 5;
            return (rng & 0xFFFFFF) / (float)0x1000000;
        }
    }
}
