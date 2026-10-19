using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Pressionar: encolhe e escurece um pouco; soltar volta suave.
    public class UIPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float pressed = 0.92f;
        private float target = 1f;
        private Graphic graphic;

        private void Awake() => graphic = GetComponent<Graphic>();

        public void OnPointerDown(PointerEventData eventData) => SetTarget(pressed);
        public void OnPointerUp(PointerEventData eventData) => SetTarget(1f);
        public void OnPointerExit(PointerEventData eventData) => SetTarget(1f);

        // Escurece a cor atual ao pressionar e devolve a mesma cor ao soltar (botões tingidos mantêm o tom).
        private Color restore;
        private Color darkened;
        private bool darkenedActive;

        private void SetTarget(float value)
        {
            target = value;
            if (graphic == null) return;
            if (value < 1f && !darkenedActive)
            {
                restore = graphic.color;
                darkened = new Color(restore.r * 0.86f, restore.g * 0.86f, restore.b * 0.9f, restore.a);
                graphic.color = darkened;
                darkenedActive = true;
            }
            else if (value >= 1f && darkenedActive)
            {
                // Se a tela trocou a cor enquanto o dedo estava em cima, a cor nova vale.
                if (graphic.color == darkened) graphic.color = restore;
                darkenedActive = false;
            }
        }

        private void OnDisable()
        {
            target = 1f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            float current = transform.localScale.x;
            if (Mathf.Abs(current - target) < 0.001f) return;
            float next = Mathf.Lerp(current, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 22f));
            transform.localScale = new Vector3(next, next, 1f);
        }
    }
}
