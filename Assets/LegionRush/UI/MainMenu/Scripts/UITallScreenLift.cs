using UnityEngine;

namespace LegionRush
{
    // Sobe o grupo (preso à base) uma fração da altura que passa da referência 1080×1920. Em celulares mais altos o
    // cenário de cima fica preso ao topo e o de baixo à base; sem isso abre um vão entre os dois.
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public class UITallScreenLift : MonoBehaviour
    {
        public float factor = 0.5f;
        public float referenceHeight = 1920f;

        private RectTransform rect;
        private float lastHeight = -1f;

        private void OnEnable()
        {
            rect = (RectTransform)transform;
            Apply();
        }

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (rect == null || !(rect.parent is RectTransform parent)) return;
            float height = parent.rect.height;
            if (Mathf.Approximately(height, lastHeight)) return;
            lastHeight = height;
            float lift = Mathf.Max(0f, height - referenceHeight) * factor;
            rect.offsetMin = new Vector2(0f, lift);
            rect.offsetMax = new Vector2(0f, lift);
        }
    }
}
