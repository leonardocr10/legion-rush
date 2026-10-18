using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Ajusta o retângulo à área segura do aparelho (recorte da câmera, barra de gestos, dobráveis).
    [ExecuteAlways]
    public class SplashSafeArea : MonoBehaviour
    {
        private Rect last;
        private Vector2Int lastScreen;

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.safeArea != last || Screen.width != lastScreen.x || Screen.height != lastScreen.y) Apply();
        }

        private void Apply()
        {
            last = Screen.safeArea;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(last.xMin / Screen.width, last.yMin / Screen.height);
            rect.anchorMax = new Vector2(last.xMax / Screen.width, last.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
