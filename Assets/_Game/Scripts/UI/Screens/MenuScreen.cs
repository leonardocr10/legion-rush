using System;
using TMPro;
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

        private TMP_Text goldText;
        private TMP_Text gemsText;

        protected override void Build(RectTransform root)
        {
            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.02f, 0.05f, 0.14f);
            Image scenery = LegionArt.Image(root, "Backgrounds/bg_frozen_a", false, "Scenery");
            scenery.preserveAspect = false;
            AspectRatioFitter fitter = scenery.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = scenery.sprite != null ? scenery.sprite.rect.width / scenery.sprite.rect.height : 1.4f;
            Image shade = UI.Rect(root, "Shade").gameObject.AddComponent<Image>();
            shade.color = new Color(BackgroundColor.r, BackgroundColor.g, BackgroundColor.b, 0.72f);
            shade.raycastTarget = false;
            UISnow.Attach((RectTransform)root);

            // Cabeçalho ornamentado (coroa e faixas do pacote) com o título ao vivo.
            Image header = LegionArt.Image(root, "Popups/popup_header", false, "Header");
            UI.Place(header.rectTransform, 0.17f, 0.865f, 0.83f, 0.955f);
            TMP_Text title = UI.Label(header.transform, Title, 56, Color.white, TextAnchor.MiddleCenter, true, "Title", LegionTextStyle.Title);
            UI.Place(title.rectTransform, 0.27f, 0.12f, 0.74f, 0.58f);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            header.gameObject.AddComponent<UISlideIn>().offset = new Vector2(0f, 80f);

            if (ShowBack)
            {
                Button back = UI.Button(root, string.Empty, UIStyle.Blue, OnBack, 60, "Back");
                UI.Place((RectTransform)back.transform, 0.02f, 0.952f, 0.13f, 0.993f);
                Image arrow = LegionArt.Image(back.transform, "Navigation/arrow_yellow", false, "Arrow");
                UI.Place(arrow.rectTransform, 0.18f, 0.12f, 0.82f, 0.88f);
                arrow.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }
            if (ShowWallet)
            {
                goldText = Chip(root, 0.46f, 0.72f, "home_gold");
                gemsText = Chip(root, 0.73f, 0.985f, "home_gems");
            }

            Body = UI.Rect(root, "Body");
            UI.Place(Body, 0.02f, 0.01f, 0.98f, 0.86f);
            BuildBody(Body);
            Body.gameObject.AddComponent<UISlideIn>().offset = new Vector2(0f, -60f);
        }

        // LegionCurrencyBar: chip de moeda do menu principal (ícone + pílula + "+") com valor ao vivo.
        private TMP_Text Chip(RectTransform parent, float xMin, float xMax, string sprite)
        {
            Button chip = LegionUI.SpriteButton(parent, sprite, () => Ui.Push<ShopScreen>(), "Wallet");
            RectTransform rect = (RectTransform)chip.transform;
            UI.Place(rect, xMin, 0.955f, xMax, 0.99f);
            ((Image)chip.targetGraphic).preserveAspect = true;
            TMP_Text value = UI.Label(rect, "0", 34, Color.white, TextAnchor.MiddleCenter, true, "Value", LegionTextStyle.Hud);
            UI.Place(value.rectTransform, 0.29f, 0.12f, 0.76f, 0.88f);
            value.textWrappingMode = TextWrappingModes.NoWrap;
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
            TMP_Text heading = UI.Label(row, title, 44, Color.white, TextAnchor.MiddleLeft);
            UI.Place(heading.rectTransform, 0.045f, 0.5f, 0.64f, 0.95f);
            TMP_Text detail = UI.Label(row, subtitle, 32, UIStyle.TextDim, TextAnchor.UpperLeft, false);
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
            TMP_Text label = UI.Label(row, text, 44, UIStyle.Gold, TextAnchor.MiddleLeft);
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
