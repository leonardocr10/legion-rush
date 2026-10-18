using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Selo vermelho "!" com pulso discreto. Some quando não há nada pendente.
    public class MainMenuBadge : MonoBehaviour
    {
        public static MainMenuBadge Create(Transform parent, Vector2 offset, float size = 36f)
        {
            Image image = MainMenuArt.Image(parent, "Badge_Alert", "Badge");
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * size * LegionUI.Px;
            rect.anchoredPosition = offset * LegionUI.Px;
            MainMenuBadge badge = image.gameObject.AddComponent<MainMenuBadge>();
            image.gameObject.AddComponent<UIPulse>();
            return badge;
        }

        public void Set(bool visible)
        {
            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
        }
    }
}
