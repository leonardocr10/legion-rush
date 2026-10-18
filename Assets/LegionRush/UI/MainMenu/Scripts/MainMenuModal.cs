using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Janela modal simples sobre a tela inicial (menu MAIS): escurece o fundo, moldura com título e conteúdo.
    public class MainMenuModal : MonoBehaviour
    {
        public RectTransform Content { get; private set; }

        public static MainMenuModal Create(Transform parent, string title)
        {
            RectTransform root = UI.Rect(parent, "Modal_" + title);
            MainMenuModal modal = root.gameObject.AddComponent<MainMenuModal>();
            Image dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(0.01f, 0.02f, 0.08f, 0.72f);
            Button close = dim.gameObject.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            close.onClick.AddListener(modal.Close);

            Image panel = UI.Panel(root, UIStyle.Panel, "Window");
            UI.Place(panel.rectTransform, 0.06f, 0.24f, 0.94f, 0.74f);
            panel.raycastTarget = true; // toques dentro da janela não fecham
            panel.gameObject.AddComponent<UIPopIn>();
            TMP_Text heading = UI.Label(panel.transform, title, 64, Color.white, TextAnchor.MiddleCenter, true, "Title", LegionTextStyle.Title);
            UI.Place(heading.rectTransform, 0.1f, 0.85f, 0.9f, 0.97f);
            modal.Content = UI.Rect(panel.transform, "Content");
            UI.Place(modal.Content, 0.05f, 0.05f, 0.95f, 0.84f);

            Button x = UI.Button(panel.transform, "X", UIStyle.Red, modal.Close, 44, "Close");
            RectTransform xr = (RectTransform)x.transform;
            xr.anchorMin = xr.anchorMax = new Vector2(1f, 1f);
            xr.sizeDelta = new Vector2(96f, 96f);
            xr.anchoredPosition = new Vector2(-20f, -20f);
            xr.pivot = new Vector2(1f, 1f);
            root.gameObject.SetActive(false);
            return modal;
        }

        public void Open()
        {
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            if (!gameObject.activeSelf) return;
            GameAudio.Play(SoundId.UiBack);
            gameObject.SetActive(false);
        }
    }

    // ---------------------------------------------------------------- animações leves
}
