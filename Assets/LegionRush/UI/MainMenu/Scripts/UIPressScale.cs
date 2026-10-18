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

        private void SetTarget(float value)
        {
            target = value;
            if (graphic != null) graphic.color = value < 1f ? new Color(0.86f, 0.86f, 0.9f, graphic.color.a) : new Color(1f, 1f, 1f, graphic.color.a);
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
