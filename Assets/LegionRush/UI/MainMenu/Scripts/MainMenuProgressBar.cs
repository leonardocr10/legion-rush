using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Barra de progresso: trilho 9-slice + preenchimento com as pontas arredondadas, animando até o alvo.
    public class MainMenuProgressBar : MonoBehaviour
    {
        private RectTransform fill;
        private float shown = -1f;
        private float target;

        public static MainMenuProgressBar Create(Transform parent, string track, string fillSprite, string name = "Progress")
        {
            Image back = MainMenuArt.Image(parent, track, name, true);
            Image front = MainMenuArt.Image(back.transform, fillSprite, "Fill", true);
            UI.Stretch(front.rectTransform, 2f, 2f, 2f, 2f);
            MainMenuProgressBar bar = back.gameObject.AddComponent<MainMenuProgressBar>();
            bar.fill = front.rectTransform;
            return bar;
        }

        public void Set(float ratio, bool instant = false)
        {
            target = Mathf.Clamp01(ratio);
            if (instant || shown < 0f || !Application.isPlaying) Apply(target);
        }

        private void Update()
        {
            if (Mathf.Approximately(shown, target)) return;
            Apply(Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 0.8f));
        }

        private void Apply(float ratio)
        {
            shown = ratio;
            // Nunca mais estreito que a própria altura: as pontas arredondadas não se deformam.
            float width = ((RectTransform)transform).rect.width - 4f;
            float height = fill.rect.height;
            float min = width > 0f ? Mathf.Min(1f, height / width) : 0f;
            fill.anchorMax = new Vector2(Mathf.Max(ratio, min), 1f);
            fill.gameObject.SetActive(ratio > 0.001f);
        }
    }
}
