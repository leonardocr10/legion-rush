using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Pergunta o nome do jogador no primeiro acesso (SaveData.playerName). "Comandante" é o padrão se ele pular.
    public static class PlayerNamePrompt
    {
        public const string DefaultName = "Comandante";
        public const int MaxLength = 14;

        public static string NameOf(SaveData save) =>
            save == null || string.IsNullOrWhiteSpace(save.playerName) ? DefaultName : save.playerName;

        public static bool NeedsAsking => SaveManager.IsLoaded && !SaveManager.Current.playerNameAsked;

        public static void Show(Transform parent, Action onDone)
        {
            LegionModal modal = LegionModal.Create(parent, "SEU NOME", 0.36f, 0.66f);
            RectTransform content = modal.Content;

            TMP_Text hint = UI.Label(content, "Como o seu exército vai chamar você?", Mathf.RoundToInt(24 * LegionUI.Px), Color.white,
                TextAnchor.MiddleCenter, false, "Hint", LegionTextStyle.Body);
            UI.Place(hint.rectTransform, 0f, 0.7f, 1f, 1f);

            TMP_InputField input = Field(content);
            UI.Place((RectTransform)input.transform, 0.04f, 0.38f, 0.96f, 0.66f);

            Button ok = UI.Button(content, "CONFIRMAR", UIStyle.Green, null, 40, "Confirm");
            UI.Place((RectTransform)ok.transform, 0.2f, 0f, 0.8f, 0.3f);
            ok.onClick.AddListener(() =>
            {
                GameAudio.Play(SoundId.UiClick);
                Save(input.text);
                modal.OnClosed = null;
                modal.Close();
                UnityEngine.Object.Destroy(modal.gameObject);
                onDone?.Invoke();
            });
            // Fechar sem confirmar = fica o padrão, e não pergunta de novo.
            modal.OnClosed = () =>
            {
                Save(string.Empty);
                UnityEngine.Object.Destroy(modal.gameObject);
                onDone?.Invoke();
            };
            modal.Open();
        }

        private static void Save(string text)
        {
            if (!SaveManager.IsLoaded) return;
            string name = (text ?? string.Empty).Trim();
            if (name.Length > MaxLength) name = name.Substring(0, MaxLength);
            SaveManager.Current.playerName = name.Length > 0 ? name : DefaultName;
            SaveManager.Current.playerNameAsked = true;
            SaveManager.MarkDirty();
        }

        private static TMP_InputField Field(Transform parent)
        {
            Image frame = UI.Panel(parent, new Color(0.03f, 0.08f, 0.22f, 0.98f), "NameField");
            frame.raycastTarget = true;
            Outline ring = frame.gameObject.AddComponent<Outline>();
            ring.effectColor = new Color(0.95f, 0.78f, 0.3f);
            ring.effectDistance = new Vector2(2f, -2f);

            RectTransform area = UI.Rect(frame.transform, "TextArea");
            UI.Stretch(area, 24f, 24f, 8f, 8f);
            area.gameObject.AddComponent<RectMask2D>();

            TMP_Text placeholder = UI.Label(area, DefaultName, Mathf.RoundToInt(30 * LegionUI.Px), new Color(1f, 1f, 1f, 0.4f),
                TextAnchor.MiddleLeft, false, "Placeholder", LegionTextStyle.Body);
            placeholder.enableAutoSizing = false;
            TMP_Text text = UI.Label(area, string.Empty, Mathf.RoundToInt(30 * LegionUI.Px), Color.white, TextAnchor.MiddleLeft, false, "Text",
                LegionTextStyle.Menu);
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            TMP_InputField input = frame.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = MaxLength;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.targetGraphic = frame;
            if (text.font != null) input.fontAsset = text.font;
            return input;
        }
    }
}
