using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Entrada do logotipo: cresce de 70% com leve ultrapassagem, depois flutua; brilho passa de vez em quando.
    public class UILogoIntro : MonoBehaviour
    {
        public float delay = 0.1f;
        private float time;
        private CanvasGroup group;

        private void OnEnable()
        {
            time = -delay;
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            Apply(Application.isPlaying ? 0f : 1f);
        }

        private void Update()
        {
            time += Time.unscaledDeltaTime;
            Apply(Mathf.Clamp01(time / 0.55f));
            // Só o deslocamento da flutuação: a posição base é do layout (pode mudar ao redimensionar a tela).
            float bob = time > 0.55f ? 6f * Mathf.Sin((time - 0.55f) * 1.3f) : 0f;
            transform.localPosition += new Vector3(0f, bob - lastBob, 0f);
            lastBob = bob;
        }

        private float lastBob;

        private void OnDisable()
        {
            transform.localPosition -= new Vector3(0f, lastBob, 0f);
            lastBob = 0f;
        }

        private void Apply(float t)
        {
            float x = t - 1f;
            float eased = 1f + 2.70158f * x * x * x + 1.70158f * x * x;
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, eased);
            group.alpha = Mathf.Clamp01(t * 2f);
        }
    }
}
