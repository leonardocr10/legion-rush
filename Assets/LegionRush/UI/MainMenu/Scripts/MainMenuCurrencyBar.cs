using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Barra de moeda/gema: trilho escuro, ícone sobreposto à esquerda, valor e botão verde "+".
    public class MainMenuCurrencyBar : MonoBehaviour
    {
        public Button AddButton { get; private set; }
        private TMP_Text value;
        private RectTransform iconRect;
        private int shown = -1;
        private int target;
        private float popTime = -1f;

        public static MainMenuCurrencyBar Create(Transform parent, string icon, Action onAdd, string name)
        {
            RectTransform root = UI.Rect(parent, name);
            MainMenuCurrencyBar bar = root.gameObject.AddComponent<MainMenuCurrencyBar>();
            Image track = MainMenuArt.Image(root, "Bar_Currency", "Track", true);
            UI.Place(track.rectTransform, 0.2f, 0.08f, 0.96f, 0.92f);
            Image art = MainMenuArt.Image(root, icon, "Icon");
            UI.Place(art.rectTransform, 0f, 0f, 0.3f, 1f);
            bar.iconRect = art.rectTransform;
            bar.value = MainMenuButtons.Label(root, "0", 29f, LegionTextStyle.Hud, Color.white);
            UI.Place(bar.value.rectTransform, 0.29f, 0.06f, 0.76f, 0.94f);

            Image add = MainMenuArt.Image(root, "Button_Add", "Add");
            UI.Place(add.rectTransform, 0.76f, 0.06f, 1f, 0.94f);
            bar.AddButton = MainMenuButtons.Make(add.rectTransform, add, onAdd);
            // O toque na barra inteira também abre a loja (alvo maior no celular).
            Image hit = track;
            MainMenuButtons.Make(track.rectTransform, hit, onAdd);
            return bar;
        }

        public void Set(int amount)
        {
            if (shown >= 0 && amount > target && Application.isPlaying) popTime = 0f;
            target = amount;
            if (shown < 0 || !Application.isPlaying || amount < shown) Show(amount);
        }

        private void Update()
        {
            if (shown != target)
            {
                int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(target - shown) * Time.unscaledDeltaTime * 6f));
                Show(shown < target ? Mathf.Min(target, shown + step) : target);
            }
            if (popTime >= 0f)
            {
                popTime += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(popTime / 0.45f);
                iconRect.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(t * Mathf.PI));
                if (t >= 1f) popTime = -1f;
            }
        }

        private void Show(int amount)
        {
            shown = amount;
            value.text = Short(amount);
        }

        // 125.4K / 1.250 (separador de milhar com ponto, como na arte).
        public static string Short(int amount)
        {
            if (amount >= 1000000) return (amount / 1000000f).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "M";
            if (amount >= 100000) return (amount / 1000f).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "K";
            return amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.');
        }
    }
}
