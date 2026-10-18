using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Base das telas de fim de fase (vitória e derrota) no grid do mockup (941×1672): ilustração presa ao painel,
    // céu desfocado cobrindo a tela inteira (sobra céu em telas altas) e painel "Recompensas" com cartões.
    public abstract class OutcomeScreen : MenuScreen
    {
        // Cores do painel: casca, contorno da casca, miolo, contorno do miolo, cartão, contorno do cartão.
        protected struct Palette
        {
            public Color Shell, ShellEdge, Inner, InnerEdge, Card, CardEdge;
        }

        protected static readonly Color CaptionColor = new Color(0.84f, 0.88f, 0.97f);
        protected static readonly Color HighlightColor = new Color(1f, 0.8f, 0.25f);

        protected abstract Color Backdrop { get; }
        protected abstract string SkySprite { get; }
        protected abstract string HeroSprite { get; }
        // Caixa da ilustração no mockup (x0, y0, x1, y1), presa à base como o painel.
        protected abstract Vector4 HeroBox { get; }

        protected override bool ShowBack => false;
        protected override bool ShowWallet => false;
        protected override bool ShowHeader => false;
        protected override bool ShowSnow => false;
        public override bool AllowBack => false;

        protected override void BuildScenery(RectTransform root)
        {
            Image fill = root.gameObject.AddComponent<Image>();
            fill.color = Backdrop;

            RectTransform scenery = UI.Rect(root, "Scenery");
            scenery.gameObject.AddComponent<UIFullBleed>();
            Image sky = OutcomeArt.Image(scenery, SkySprite, "Sky");
            sky.preserveAspect = false;
            UI.Stretch(sky.rectTransform);
            Image hero = OutcomeArt.Image(scenery, HeroSprite, "Hero");
            hero.preserveAspect = false;
            Vector4 box = HeroBox;
            LegionUI.Place(hero.rectTransform, LegionUI.Edge.Bottom, box.x, box.y, box.z, box.w);
            hero.gameObject.AddComponent<UISlideIn>().offset = new Vector2(0f, 60f);
        }

        // Painel com casca, miolo e o título "Recompensas"; o conteúdo usa o grid do mockup sobre a tela inteira.
        protected static RectTransform RewardsPanel(RectTransform body, Vector4 box, Palette palette, float titleSize)
        {
            UI.Stretch(body);
            RectTransform panel = LegionUI.Place(UI.Rect(body, "Panel"), LegionUI.Edge.Bottom, box.x, box.y, box.z, box.w);
            Image shell = Box(panel, palette.Shell, palette.ShellEdge, 3f);
            Shadow shadow = shell.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(0f, -10f);
            Image inner = Box(UI.Rect(panel, "Inner"), palette.Inner, palette.InnerEdge, 2f);
            LegionUI.Inside(inner.rectTransform, box, box.x + 16, box.y + 16, box.z - 16, box.w - 16);
            LegionUI.Text(panel, box, box.x + 76, box.y + 28, box.z - 76, box.y + 92, "Recompensas", titleSize, LegionTextStyle.Menu, Color.white);
            return panel;
        }

        // Moldura arredondada com contorno fino colorido.
        protected static Image Box(RectTransform rect, Color color, Color edge, float thickness)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = UIStyle.Rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = edge;
            outline.effectDistance = new Vector2(thickness, -thickness);
            return image;
        }

        // Cartão de recompensa: ícone, valor grande e legenda, em frações da altura do cartão.
        protected static TMP_Text Card(RectTransform panel, Vector4 panelBox, Vector4 box, Sprite icon, string caption, Palette palette,
            float valueSize, float captionSize, out TMP_Text label)
        {
            RectTransform rect = LegionUI.Inside(UI.Rect(panel, "Card_" + caption), panelBox, box.x, box.y, box.z, box.w);
            Box(rect, palette.Card, palette.CardEdge, 2f);

            float h = box.w - box.y;
            float iconHalf = h * 0.21f;
            float cx = (box.x + box.z) * 0.5f;
            Image art = UI.Rect(rect, "Icon").gameObject.AddComponent<Image>();
            art.sprite = icon;
            art.preserveAspect = true;
            art.raycastTarget = false;
            LegionUI.Inside(art.rectTransform, box, cx - iconHalf, box.y + h * 0.075f, cx + iconHalf, box.y + h * 0.5f);
            art.gameObject.AddComponent<UIBreath>().amount = 0.03f;

            TMP_Text value = LegionUI.Text(rect, box, box.x + 10, box.y + h * 0.52f, box.z - 10, box.y + h * 0.76f, "+0", valueSize,
                LegionTextStyle.PrimaryButton, Color.white);
            label = LegionUI.Text(rect, box, box.x + 10, box.y + h * 0.77f, box.z - 10, box.y + h * 0.93f, caption, captionSize,
                LegionTextStyle.Menu, CaptionColor);
            return value;
        }

        // Quantia com separador de milhar brasileiro (1.250), sem depender dos dados de cultura da plataforma.
        protected static string Amount(int value, bool plus = true)
        {
            string text = value.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', '.');
            return plus ? "+" + text : text;
        }
    }

    // Sprites das telas de fim de fase (Assets/LegionRush/UI/Outcome/Resources/OutcomeArt/<nome>).
    public static class OutcomeArt
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out Sprite sprite)) return sprite;
            sprite = Resources.Load<Sprite>("OutcomeArt/" + name);
            cache[name] = sprite;
            return sprite;
        }

        public static Image Image(Transform parent, string sprite, string name = null)
        {
            RectTransform rect = UI.Rect(parent, name ?? sprite);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = Get(sprite);
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (image.sprite == null)
            {
                image.color = Color.clear;
                GameLog.Warn("Arte de fim de fase ausente: " + sprite);
            }
            return image;
        }
    }
}
