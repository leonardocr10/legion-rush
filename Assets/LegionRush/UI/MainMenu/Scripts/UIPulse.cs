using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Pulso discreto (selos de notificação).
    public class UIPulse : MonoBehaviour
    {
        public float amount = 0.1f;
        public float speed = 3.2f;

        private void OnDisable() => transform.localScale = Vector3.one;

        private void Update()
        {
            float s = 1f + amount * Mathf.Max(0f, Mathf.Sin(Time.unscaledTime * speed));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
