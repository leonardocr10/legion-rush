using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Pulso do botão: escala 1.00 → 1.025 → 1.00 em ~1,5 s com o brilho acompanhando.
    public class SplashButtonPulse : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private Graphic glow;
        [SerializeField] private float period = 1.5f;
        [SerializeField] private float scale = 0.025f;
        [SerializeField] private Vector2 glowAlpha = new Vector2(0.18f, 0.42f);

        public void Configure(RectTransform art, Graphic glowGraphic)
        {
            target = art;
            glow = glowGraphic;
        }

        private void Update()
        {
            float k = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime / period * Mathf.PI * 2f);
            if (target != null)
            {
                float s = 1f + scale * k;
                target.localScale = new Vector3(s, s, 1f);
            }
            if (glow != null) glow.canvasRenderer.SetAlpha(Mathf.Lerp(glowAlpha.x, glowAlpha.y, k));
        }
    }
}
