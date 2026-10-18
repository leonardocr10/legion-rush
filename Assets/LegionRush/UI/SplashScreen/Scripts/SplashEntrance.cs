using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Entrada: fade + deslocamento (+ escala opcional com leve ultrapassagem). Parte do estado final do prefab.
    [DisallowMultipleComponent]
    public class SplashEntrance : MonoBehaviour
    {
        [SerializeField] private float delay;
        [SerializeField] private float duration = 0.6f;
        [SerializeField] private Vector2 offset = new Vector2(0f, -60f);
        [SerializeField, Tooltip("Escala inicial; 1 = sem escala.")] private float fromScale = 1f;
        [SerializeField, Tooltip("Ultrapassagem no fim (efeito 'pulo').")] private bool overshoot;

        public bool Done { get; private set; }

        private RectTransform rect;
        private CanvasGroup group;
        private Vector2 target;
        private Vector3 baseScale;
        private float time;

        public void Configure(float delaySeconds, float seconds, Vector2 fromOffset, float scale = 1f, bool bounce = false)
        {
            delay = delaySeconds;
            duration = seconds;
            offset = fromOffset;
            fromScale = scale;
            overshoot = bounce;
        }

        private void Awake()
        {
            rect = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            target = rect.anchoredPosition;
            baseScale = rect.localScale;
            Apply(0f);
        }

        private void Update()
        {
            if (Done) return;
            time += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01((time - delay) / Mathf.Max(0.01f, duration));
            Apply(k);
            if (k >= 1f)
            {
                Done = true;
                group.blocksRaycasts = true;
                group.interactable = true;
            }
        }

        private void Apply(float k)
        {
            float move = 1f - Mathf.Pow(1f - k, 3f); // ease-out cúbico
            group.alpha = Mathf.Clamp01(k * 1.6f);
            rect.anchoredPosition = target + offset * (1f - move);
            if (fromScale != 1f)
            {
                float s = overshoot ? BackOut(k) : move;
                rect.localScale = baseScale * Mathf.LerpUnclamped(fromScale, 1f, s);
            }
        }

        private static float BackOut(float k)
        {
            const float c = 1.7f;
            float t = k - 1f;
            return 1f + (c + 1f) * t * t * t + c * t * t;
        }

        // Interrompe a entrada no estado final (ex.: quando o jogador volta de outra tela).
        public void Finish()
        {
            time = delay + duration;
            Apply(1f);
            Done = true;
        }
    }
}
