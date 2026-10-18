using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Flutuação vertical lenta de ícones (cada um com sua fase).
    public class UIIdleBob : MonoBehaviour
    {
        public float amplitude = 2.5f;
        public float speed = 1.6f;
        public float phase;
        private Vector2 home;
        private bool captured;

        private void OnEnable()
        {
            RectTransform rect = (RectTransform)transform;
            if (!captured)
            {
                home = rect.anchoredPosition;
                captured = true;
            }
        }

        private void Update()
        {
            ((RectTransform)transform).anchoredPosition = home + new Vector2(0f, amplitude * Mathf.Sin(Time.unscaledTime * speed + phase));
        }
    }
}
