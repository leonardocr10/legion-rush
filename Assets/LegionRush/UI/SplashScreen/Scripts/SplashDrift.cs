using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Deriva lenta (parallax discreto): cada camada balança com amplitude e período próprios.
    public class SplashDrift : MonoBehaviour
    {
        [SerializeField] private Vector2 amplitude = new Vector2(6f, 3f);
        [SerializeField] private float period = 9f;
        [SerializeField] private float phase;

        private RectTransform rect;
        private Vector2 origin;
        private bool ready;

        public void Configure(Vector2 amp, float seconds, float phaseOffset)
        {
            amplitude = amp;
            period = seconds;
            phase = phaseOffset;
        }

        private void OnEnable()
        {
            rect = (RectTransform)transform;
            if (!ready)
            {
                origin = rect.anchoredPosition;
                ready = true;
            }
        }

        private void LateUpdate()
        {
            float t = (Time.unscaledTime / Mathf.Max(0.5f, period) + phase) * Mathf.PI * 2f;
            rect.anchoredPosition = origin + new Vector2(Mathf.Sin(t) * amplitude.x, Mathf.Sin(t * 0.73f + 1.3f) * amplitude.y);
        }
    }
}
