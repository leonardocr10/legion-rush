using System;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Base das telas de menu: fundo, cabeçalho com título, voltar e carteira (ouro/gemas), e corpo.
    public abstract class MenuScreen : UIScreen
    {
        protected RectTransform Body { get; private set; }
        protected abstract string Title { get; }
        protected virtual bool ShowBack => true;
        protected virtual bool ShowWallet => true;
        protected virtual Color BackgroundColor => UIStyle.Background;
        protected GameBootstrap Game => Ui.Game;

        private Text goldText;
        private Text gemsText;

        protected override void Build(RectTransform root)
        {
            Image background = root.gameObject.AddComponent<Image>();
            background.color = BackgroundColor;

            Image header = UI.Panel(root, UIStyle.Panel, "Header", true);
            UI.Place(header.rectTransform, 0.02f, 0.905f, 0.98f, 0.995f);
            Text title = UI.Label(header.transform, Title, 56, UIStyle.Gold);
            UI.Place(title.rectTransform, ShowBack ? 0.17f : 0.04f, 0.05f, ShowWallet ? 0.58f : 0.96f, 0.95f);
            title.alignment = ShowBack ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;

            if (ShowBack)
            {
                Button back = UI.Button(header.transform, "<", UIStyle.Blue, OnBack, 60, "Back");
                UI.Place((RectTransform)back.transform, 0.015f, 0.12f, 0.15f, 0.88f);
            }
            if (ShowWallet)
            {
                goldText = Chip(header.transform, 0.6f, 0.79f, UIStyle.Gold);
                gemsText = Chip(header.transform, 0.8f, 0.985f, UIStyle.Purple);
            }

            Body = UI.Rect(root, "Body");
            UI.Place(Body, 0.02f, 0.01f, 0.98f, 0.895f);
            BuildBody(Body);
        }

        private static Text Chip(Transform parent, float xMin, float xMax, Color color)
        {
            Image chip = UI.Panel(parent, new Color(0f, 0f, 0f, 0.35f), "Chip");
            UI.Place(chip.rectTransform, xMin, 0.18f, xMax, 0.82f);
            Image dot = UI.Panel(chip.transform, color, "Icon");
            dot.sprite = UIStyle.Circle;
            UI.Place(dot.rectTransform, 0.04f, 0.15f, 0.26f, 0.85f);
            Text value = UI.Label(chip.transform, "0", 36, Color.white, TextAnchor.MiddleRight);
            UI.Place(value.rectTransform, 0.28f, 0f, 0.95f, 1f);
            return value;
        }

        protected abstract void BuildBody(RectTransform body);
        protected virtual void RefreshBody() { }

        public override void Refresh()
        {
            if (goldText != null) goldText.text = Short(Economy.Gold);
            if (gemsText != null) gemsText.text = Short(Economy.Gems);
            RefreshBody();
        }

        protected static string Short(int value)
        {
            if (value >= 1000000) return (value / 1000000f).ToString("0.#") + "M";
            if (value >= 10000) return (value / 1000f).ToString("0.#") + "K";
            return value.ToString();
        }

        protected static string Price(int gold, int gems)
        {
            if (gold > 0 && gems > 0) return $"{gold} OURO + {gems} GEMAS";
            if (gems > 0) return $"{gems} GEMAS";
            return gold > 0 ? $"{gold} OURO" : "GRÁTIS";
        }

        // Linha padrão de lista: faixa colorida, título, descrição e botão de ação à direita.
        protected static Button ItemRow(RectTransform list, string title, string subtitle, string action, Color actionColor,
            Action onClick, Color accent, bool interactable = true, float height = 190f)
        {
            RectTransform row = UI.Row(list, height, UIStyle.Panel);
            Image stripe = UI.Panel(row, accent, "Accent");
            UI.Place(stripe.rectTransform, 0.01f, 0.1f, 0.025f, 0.9f);
            Text heading = UI.Label(row, title, 44, Color.white, TextAnchor.MiddleLeft);
            UI.Place(heading.rectTransform, 0.045f, 0.5f, 0.64f, 0.95f);
            Text detail = UI.Label(row, subtitle, 32, UIStyle.TextDim, TextAnchor.UpperLeft, false);
            UI.Place(detail.rectTransform, 0.045f, 0.05f, 0.64f, 0.52f);
            if (string.IsNullOrEmpty(action)) return null;
            Button button = UI.Button(row, action, actionColor, onClick, 34);
            UI.Place((RectTransform)button.transform, 0.66f, 0.18f, 0.985f, 0.82f);
            button.interactable = interactable;
            return button;
        }

        protected static void Section(RectTransform list, string text)
        {
            RectTransform row = UI.Row(list, 80f);
            Text label = UI.Label(row, text, 44, UIStyle.Gold, TextAnchor.MiddleLeft);
            UI.Stretch(label.rectTransform, 12f, 12f, 0f, 0f);
        }

        protected void Result(PurchaseResult result, string successMessage = null)
        {
            if (result == PurchaseResult.Success)
            {
                GameAudio.Play(SoundId.UiConfirm);
                Ui.Toast(successMessage ?? MetaService.Explain(result));
            }
            else
            {
                GameAudio.Play(SoundId.UiBack);
                Ui.Toast(MetaService.Explain(result));
            }
            Ui.RefreshTop();
        }

        protected void ConfirmPurchase(string item, int gold, int gems, Func<PurchaseResult> purchase, string successMessage = null)
        {
            if (!Economy.CanAfford(gold, gems))
            {
                Ui.Toast("Saldo insuficiente");
                GameAudio.Play(SoundId.UiBack);
                return;
            }
            Ui.Confirm("CONFIRMAR COMPRA", $"{item}\npor {Price(gold, gems)}?", "COMPRAR", () => Result(purchase(), successMessage));
        }
    }
}
