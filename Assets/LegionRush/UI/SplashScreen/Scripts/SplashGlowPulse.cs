using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Brilho pulsante (aura do logo, orbe do mago) ou cintilação irregular (luz das tochas).
    public class SplashGlowPulse : MonoBehaviour
    {
        [SerializeField] private Vector2 alpha = new Vector2(0.35f, 0.7f);
        [SerializeField] private Vector2 scale = new Vector2(0.96f, 1.06f);
        [SerializeField] private float speed = 0.6f;
        [SerializeField, Tooltip("Ruído em vez de seno: chama de tocha.")] private bool flicker;
        [SerializeField] private float seed;

        private Graphic graphic;

        public void Configure(Vector2 alphaRange, Vector2 scaleRange, float cyclesPerSecond, bool noisy, float noiseSeed)
        {
            alpha = alphaRange;
            scale = scaleRange;
            speed = cyclesPerSecond;
            flicker = noisy;
            seed = noiseSeed;
        }

        private void Awake() => graphic = GetComponent<Graphic>();

        private void Update()
        {
            float t = Time.unscaledTime * speed;
            float k = flicker
                ? Mathf.PerlinNoise(t * 3f, seed) * 0.75f + Mathf.PerlinNoise(t * 9f, seed + 7f) * 0.25f
                : 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f);
            if (graphic != null) graphic.canvasRenderer.SetAlpha(Mathf.Lerp(alpha.x, alpha.y, k));
            float s = Mathf.Lerp(scale.x, scale.y, k);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
