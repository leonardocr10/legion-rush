using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Camada de cenário com deriva lenta (parallax por profundidade) e balanço opcional (bandeiras).
    public class UIParallaxLayer : MonoBehaviour
    {
        public float depth = 1f;    // 0 = fixo; maior = mais perto da câmera
        public float sway;          // graus de balanço (bandeiras)
        public float phase;
        private Vector2 home;
        private bool captured;

        private void OnEnable()
        {
            if (captured) return;
            home = ((RectTransform)transform).anchoredPosition;
            captured = true;
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            var offset = new Vector2(Mathf.Sin(t * 0.17f + phase) * 9f, Mathf.Sin(t * 0.23f + phase) * 4f) * depth;
            ((RectTransform)transform).anchoredPosition = home + offset;
            if (sway != 0f) transform.localRotation = Quaternion.Euler(0f, 0f, sway * Mathf.Sin(t * 1.4f + phase));
        }
    }
}
