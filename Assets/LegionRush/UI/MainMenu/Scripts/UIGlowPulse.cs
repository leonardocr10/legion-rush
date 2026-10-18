using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Brilho que pisca suavemente (aura vermelha do Modo Boss).
    public class UIGlowPulse : MonoBehaviour
    {
        public float min = 0.25f;
        public float max = 0.7f;
        public float speed = 2.2f;
        private Image image;

        private void Awake() => image = GetComponent<Image>();

        private void Update()
        {
            if (image == null) return;
            Color c = image.color;
            c.a = Mathf.Lerp(min, max, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed));
            image.color = c;
        }
    }
}
