using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Derrota no layout do mockup (941×1672): ilustração do rei derrotado com a faixa DERROTA no topo,
    // painel de recompensas (ouro e pontuação) e os botões Jogar Novamente / Sair na base.
    public class DefeatScreen : MenuScreen
    {
        private static readonly Color Backdrop = new Color32(14, 10, 22, 255);
        private static readonly Vector4 PanelBox = new Vector4(44, 925, 897, 1388);
        private static readonly Vector4 GoldCard = new Vector4(92, 1055, 466, 1344);
        private static readonly Vector4 ScoreCard = new Vector4(480, 1055, 854, 1344);

        private TMP_Text goldValue;
        private TMP_Text scoreValue;
        private TMP_Text scoreLabel;

        protected override string Title => "DERROTA";
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
            // Céu desfocado cobre a tela inteira; a ilustração fica presa ao painel e sobra céu em telas altas.
            Image sky = DefeatArt.Image(scenery, "Defeat_Sky", "Sky");
            sky.preserveAspect = false;
            UI.Stretch(sky.rectTransform);
            Image hero = DefeatArt.Image(scenery, "Defeat_Hero", "Hero");
            hero.preserveAspect = false;
            LegionUI.Place(hero.rectTransform, LegionUI.Edge.Bottom, 0, -30, 941, 1013);
            hero.gameObject.AddComponent<UISlideIn>().offset = new Vector2(0f, 60f);
        }

        protected override void BuildBody(RectTransform body)
        {
            // O conteúdo usa o grid do mockup sobre a tela inteira (preso à base).
            UI.Stretch(body);

            RectTransform panel = LegionUI.Place(UI.Rect(body, "Panel"), LegionUI.Edge.Bottom, PanelBox.x, PanelBox.y, PanelBox.z, PanelBox.w);
            Image shell = Box(panel, new Color32(60, 36, 66, 250), new Color32(150, 70, 140, 140), 3f);
            Shadow shadow = shell.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(0f, -10f);
            Image inner = Box(UI.Rect(panel, "Inner"), new Color32(29, 16, 34, 255), new Color32(120, 50, 110, 150), 2f);
            LegionUI.Inside(inner.rectTransform, PanelBox, PanelBox.x + 20, PanelBox.y + 22, PanelBox.z - 20, PanelBox.w - 22);

            LegionUI.Text(panel, PanelBox, 120, 960, 821, 1030, "Recompensas", 40f, LegionTextStyle.Menu, Color.white);

            goldValue = Card(panel, GoldCard, "Icon_Coin", "Ouro", out _);
            scoreValue = Card(panel, ScoreCard, "Icon_Crown", "Pontuação", out scoreLabel);

            Action(body, 70, 502, UIStyle.Blue, "Jogar Novamente", "Icon_Retry", () => Game.RetryLevel(), 0.06f, 0.2f);
            Action(body, 558, 877, UIStyle.Red, "Sair", "Icon_Exit", () => Game.ReturnToMenu(), 0.1f, 0.3f);
        }

        // Moldura arredondada escura com contorno fino colorido.
        private static Image Box(RectTransform rect, Color color, Color edge, float thickness)
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

        // Cartão de recompensa: ícone, valor grande e legenda.
        private static TMP_Text Card(RectTransform panel, Vector4 box, string icon, string caption, out TMP_Text label)
        {
            RectTransform rect = LegionUI.Inside(UI.Rect(panel, "Card_" + caption), PanelBox, box.x, box.y, box.z, box.w);
            Box(rect, new Color32(17, 7, 19, 255), new Color32(80, 45, 90, 120), 2f);

            float cx = (box.x + box.z) * 0.5f;
            Image art = HeroesArt.Image(rect, icon, "Icon");
            LegionUI.Inside(art.rectTransform, box, cx - 62, box.y + 22, cx + 62, box.y + 144);
            art.gameObject.AddComponent<UIBreath>().amount = 0.03f;

            TMP_Text value = LegionUI.Text(rect, box, box.x + 10, box.y + 150, box.z - 10, box.y + 220, "+0", 76f, LegionTextStyle.PrimaryButton, Color.white);
            label = LegionUI.Text(rect, box, box.x + 10, box.y + 222, box.z - 10, box.y + 268, caption, 34f, LegionTextStyle.Menu,
                new Color(0.84f, 0.88f, 0.97f));
            return value;
        }

        // Botão do pacote com ícone à esquerda e rótulo à direita.
        private static void Action(RectTransform body, float x0, float x1, Color color, string text, string icon, System.Action onClick,
            float iconMin, float iconMax)
        {
            Button button = UI.Button(body, text, color, onClick, 50, "Button_" + icon);
            LegionUI.Place((RectTransform)button.transform, LegionUI.Edge.Bottom, x0, 1460, x1, 1592);
            Image art = DefeatArt.Image(button.transform, icon, "Icon");
            UI.Place(art.rectTransform, iconMin, 0.18f, iconMax, 0.82f);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            UI.Place(label.rectTransform, iconMax, 0.1f, 0.95f, 0.9f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        protected override void RefreshBody()
        {
            LevelManager level = Game.CurrentLevel;
            if (level == null) return;
            LevelOutcome outcome = level.LastOutcome;
            goldValue.text = "+" + outcome.GoldEarned;
            scoreValue.text = outcome.Score.ToString();
            scoreLabel.text = outcome.NewBestScore ? "Novo recorde!" : "Pontuação";
            scoreLabel.color = outcome.NewBestScore ? new Color(1f, 0.8f, 0.25f) : new Color(0.84f, 0.88f, 0.97f);
            LegionTextFx.Play(goldValue, LegionTextAnimation.FadeScale);
            LegionTextFx.Play(scoreValue, LegionTextAnimation.FadeScale);
            Haptics.Impact();
        }
    }

    // Sprites da tela de derrota (Assets/LegionRush/UI/Defeat/Resources/DefeatArt/<nome>).
    public static class DefeatArt
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out Sprite sprite)) return sprite;
            sprite = Resources.Load<Sprite>("DefeatArt/" + name);
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
                GameLog.Warn("Arte da derrota ausente: " + sprite);
            }
            return image;
        }
    }
}
