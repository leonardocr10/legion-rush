using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Reflexo dourado que atravessa o logo de tempos em tempos (filho de uma máscara com o próprio logo).
    public class SplashShine : MonoBehaviour
    {
        [SerializeField] private float interval = 4.5f;
        [SerializeField] private float sweepSeconds = 0.9f;
        [SerializeField] private float firstDelay = 1.6f;
        [SerializeField] private float travel = 520f;

        private RectTransform rect;
        private Graphic graphic;

        private void Awake()
        {
            rect = (RectTransform)transform;
            graphic = GetComponent<Graphic>();
        }

        private void Update()
        {
            float t = Time.unscaledTime - firstDelay;
            float k = t < 0f ? -1f : (t % interval) / sweepSeconds;
            bool visible = k >= 0f && k <= 1f;
            if (graphic != null) graphic.canvasRenderer.SetAlpha(visible ? Mathf.Sin(k * Mathf.PI) * 0.75f : 0f);
            if (visible) rect.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, k), 0f);
        }
    }
}
