using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Faz um retângulo ocupar o canvas inteiro mesmo dentro da área segura (cenário atrás do entalhe da câmera).
    [RequireComponent(typeof(RectTransform))]
    public class UIFullBleed : MonoBehaviour
    {
        private Rect lastSafe;
        private Vector2Int lastSize;

        private void OnEnable() => Fit();

        private void LateUpdate()
        {
            if (Screen.safeArea != lastSafe || Screen.width != lastSize.x || Screen.height != lastSize.y) Fit();
        }

        private void Fit()
        {
            lastSafe = Screen.safeArea;
            lastSize = new Vector2Int(Screen.width, Screen.height);
            var rect = (RectTransform)transform;
            Canvas canvas = GetComponentInParent<Canvas>();
            var parent = rect.parent as RectTransform;
            if (canvas == null || parent == null) return;
            var root = (RectTransform)canvas.rootCanvas.transform;
            var corners = new Vector3[4];
            root.GetWorldCorners(corners);
            Vector2 min = parent.InverseTransformPoint(corners[0]);
            Vector2 max = parent.InverseTransformPoint(corners[2]);
            Rect area = parent.rect;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = min - area.min;
            rect.offsetMax = max - area.max;
        }
    }
}
