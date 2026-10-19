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

        // Escurece ao pressionar pelo multiplicador do CanvasRenderer (o mesmo do ColorTint do Unity), sem tocar em
        // Graphic.color. Assim a cor/sprite de estado que a tela define (aba ligada, chave, botão tingido) nunca é
        // sobrescrita: antes, devolver a cor "guardada" ao soltar pintava de branco o estado ligado.
        private static readonly Color Pressed = new Color(0.86f, 0.86f, 0.9f, 1f);

        private void SetTarget(float value)
        {
            target = value;
            if (graphic != null) Tint(value < 1f ? Pressed : Color.white);
        }

        private void Tint(Color color)
        {
            color.a = graphic.canvasRenderer.GetAlpha();
            graphic.canvasRenderer.SetColor(color);
        }

        private void OnDisable()
        {
            target = 1f;
            transform.localScale = Vector3.one;
            if (graphic != null) Tint(Color.white);
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
