using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Preparação da fase no layout do mockup (941×1672): cenário do castelo no topo e painel com fase, herói,
    // composição do exército, habilidades (toque para trocar) e JOGAR.
    public class PrepScreen : MenuScreen
    {
        private static readonly Vector4 PanelBox = new Vector4(20, 775, 921, 1618);
        private static readonly float[] Columns = { 50, 265, 480, 696 };
        private const float ButtonWidth = 202f;

        private TMP_Text info;
        private RectTransform panel;
        private readonly List<GameObject> dynamic = new List<GameObject>();
        private TMP_Text skillHint;

        protected override string Title => "PREPARAR TROPAS";
        protected override Vector4 HeaderBox => new Vector4(0.13f, 0.848f, 0.87f, 0.962f);

        public override void OnBack() => Game.ReturnToMenu();

        protected override void BuildScenery(RectTransform root)
        {
            Image fill = root.gameObject.AddComponent<Image>();
            fill.color = new Color(0.02f, 0.05f, 0.16f);

            RectTransform scenery = UI.Rect(root, "Scenery");
            scenery.gameObject.AddComponent<UIFullBleed>();
            Image sky = MainMenuArt.Image(scenery, "Background_Sky", "Sky");
            sky.preserveAspect = false;
            UI.Stretch(sky.rectTransform);

            Image mountains = MainMenuArt.Image(scenery, "Background_Mountains", "Mountains");
            mountains.preserveAspect = false;
            LegionUI.Place(mountains.rectTransform, LegionUI.Edge.Top, -40, 120, 981, 360);
            Image castle = MainMenuArt.Image(scenery, "Background_FrozenKingdom", "Castle");
            castle.preserveAspect = false;
            LegionUI.Place(castle.rectTransform, LegionUI.Edge.Top, 110, 215, 1190, 792);

            Image banner = PrepArt.Image(scenery, "Deco_Banner");
            LegionUI.Place(banner.rectTransform, LegionUI.Edge.TopLeft, -10, 280, 205, 700);
            Image torch = PrepArt.Image(scenery, "Deco_Torch");
            LegionUI.Place(torch.rectTransform, LegionUI.Edge.TopLeft, 340, 540, 440, 730);
            Image rocks = PrepArt.Image(scenery, "Deco_Rocks");
            LegionUI.Place(rocks.rectTransform, LegionUI.Edge.TopLeft, 120, 620, 520, 800);

            // Sombra suave sob o painel, fundindo o cenário com a base.
            Image shade = MainMenuArt.Image(scenery, "Shade_Vertical", "Shade");
            shade.preserveAspect = false;
            RectTransform s = shade.rectTransform;
            s.anchorMin = Vector2.zero;
            s.anchorMax = new Vector2(1f, 0f);
            s.pivot = new Vector2(0.5f, 0f);
            s.sizeDelta = new Vector2(0f, 1100f);
        }

        protected override void BuildBody(RectTransform body)
        {
            // O conteúdo usa o grid do mockup sobre a tela inteira (preso à base).
            UI.Stretch(body);
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;

            Image frame = PrepArt.Image(body, "Panel_Frame", "Panel");
            frame.preserveAspect = false;
            frame.type = Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = 0.6f;
            frame.color = new Color(1f, 1f, 1f, 0.97f);
            panel = LegionUI.Place(frame.rectTransform, LegionUI.Edge.Bottom, PanelBox.x, PanelBox.y, PanelBox.z, PanelBox.w);

            info = LegionUI.Text(panel, PanelBox, 58, 800, 905, 892, string.Empty, 29f, LegionTextStyle.Menu, Color.white, TextAnchor.MiddleLeft);
            info.textWrappingMode = TextWrappingModes.Normal;
            info.richText = true;

            Header("COMPOSIÇÃO", 925, 285, 670);
            Header("HABILIDADES <size=80%><color=#FFFFFF>(toque para trocar)</color></size>", 1146, 560, 820);

            skillHint = LegionUI.Text(panel, PanelBox, 58, 1346, 900, 1412, string.Empty, 21f, LegionTextStyle.Body,
                new Color(0.78f, 0.86f, 1f), TextAnchor.MiddleCenter);
            skillHint.textWrappingMode = TextWrappingModes.Normal;

            Image play = PrepArt.Image(panel, "Button_Play", "Play");
            play.preserveAspect = false;
            LegionUI.Inside(play.rectTransform, PanelBox, 168, 1424, 772, 1568);
            MainMenuButtons.Make(play.rectTransform, play, () =>
            {
                GameAudio.Play(SoundId.UiConfirm);
                Game.StartRun();
            });
            play.gameObject.AddComponent<UIBreath>().amount = 0.015f;
        }

        private void Header(string text, float y0, float textEnd, float lineEnd)
        {
            TMP_Text label = LegionUI.Text(panel, PanelBox, 58, y0, textEnd + 40, y0 + 52, text, 33f, LegionTextStyle.Title,
                new Color(1f, 0.8f, 0.25f), TextAnchor.MiddleLeft);
            label.richText = true;
            label.enableAutoSizing = false;
            label.fontSize = Mathf.RoundToInt(33f * LegionUI.Px);
            Image line = UI.Panel(panel, new Color(0.85f, 0.65f, 0.25f, 0.75f), "Line");
            line.raycastTarget = false;
            LegionUI.Inside(line.rectTransform, PanelBox, textEnd + 50, y0 + 25, lineEnd, y0 + 28);
            Image dot = UI.Panel(panel, new Color(0.95f, 0.78f, 0.35f), "Dot");
            dot.raycastTarget = false;
            LegionUI.Inside(dot.rectTransform, PanelBox, textEnd + 38, y0 + 21, textEnd + 46, y0 + 32);
            dot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        protected override void RefreshBody()
        {
            SaveData save = SaveManager.Current;
            HeroDef hero = MetaCatalog.Hero(save.heroes.selected);
            LevelManager level = Game.CurrentLevel;
            const string gold = "#FFC83A";
            string boss = level != null && level.Boss != null ? $"   ·   Chefão: {level.Boss.Definition.displayName}" : string.Empty;
            string potions = save.inventory.activePotions.Count > 0 ? $"   ·   Poções ativas: {save.inventory.activePotions.Count}" : string.Empty;
            string heroText = hero != null ? $"<color={gold}>{hero.Name.ToUpperInvariant()}</color> — {hero.Title}" : "—";
            info.text = $"<color={gold}>FASE {Game.CurrentLevelNumber}:</color> {Game.CurrentWorldName}{boss}\n" +
                        $"<color={gold}>Herói:</color> {heroText}{potions}";

            foreach (GameObject go in dynamic) Object.Destroy(go);
            dynamic.Clear();

            int count = Mathf.Min(Game.CompositionCount, Columns.Length);
            for (int i = 0; i < count; i++)
            {
                int index = i;
                string name = Game.CompositionName(i);
                bool selected = i == Game.CompositionIndex;
                bool available = Game.IsCompositionAvailable(i);
                Image art = Option("Comp_" + name, null, null, Columns[i], 978, 1114, () =>
                {
                    Game.SetComposition(index);
                    Ui.RefreshTop();
                });
                Highlight(art, selected, available);
                art.GetComponent<Button>().interactable = available;
            }

            SkillSystem skills = Game.CurrentSkills;
            if (skills == null) return;
            for (int i = 0; i < SkillSystem.SlotCount && i < Columns.Length; i++)
            {
                int slot = i;
                SkillDefinition skill = skills.Definition(Game.LoadoutSlot(i));
                Option("Skill_" + skill.shortLabel, skill, FallbackIcon(skill.shortLabel), Columns[i], 1200, 1338, () =>
                {
                    Game.CycleSkillSlot(slot);
                    SkillDefinition next = Game.CurrentSkills.Definition(Game.LoadoutSlot(slot));
                    Ui.RefreshTop();
                    skillHint.text = $"{next.displayName}: {next.description}";
                });
            }
        }

        // Botão de opção com a arte do pacote; sem arte própria, usa o botão azul vazio com ícone e rótulo.
        private Image Option(string sprite, SkillDefinition skill, string fallbackIcon, float x0, float y0, float y1, System.Action onClick)
        {
            bool hasArt = PrepArt.Get(sprite) != null;
            Image art = PrepArt.Image(panel, hasArt ? sprite : "Skill_Blank", sprite);
            art.preserveAspect = false;
            LegionUI.Inside(art.rectTransform, PanelBox, x0, y0, x0 + ButtonWidth, y1);
            if (!hasArt && skill != null)
            {
                art.color = Color.Lerp(Color.white, skill.color, 0.45f);
                Image icon = HeroesArt.Image(art.transform, fallbackIcon, "Icon");
                UI.Place(icon.rectTransform, 0.3f, 0.36f, 0.7f, 0.9f);
                TMP_Text label = MainMenuButtons.Label(art.transform, skill.shortLabel, 22f, LegionTextStyle.PrimaryButton, Color.white);
                UI.Place(label.rectTransform, 0.06f, 0.1f, 0.94f, 0.36f);
            }
            MainMenuButtons.Make(art.rectTransform, art, onClick);
            dynamic.Add(art.gameObject);
            return art;
        }

        private static string FallbackIcon(string label) => label switch
        {
            "CURA" => "Skill_Green",
            "GELO" => "Skill_Blue",
            "CAVALARIA" => "Skill_RoyalStrike",
            _ => "Skill_Gold"
        };

        // Selecionada: brilho dourado e um pouco maior. As outras ficam levemente apagadas.
        private void Highlight(Image art, bool selected, bool available)
        {
            if (selected)
            {
                Image glow = LegionArt.Image(art.transform, "Frames/frame_gold", false, "Selected");
                glow.type = Image.Type.Sliced;
                glow.fillCenter = false;
                glow.pixelsPerUnitMultiplier = 2.4f;
                glow.raycastTarget = false;
                UI.Stretch(glow.rectTransform, -7f, -7f, -7f, -7f);
                art.rectTransform.localScale = Vector3.one * 1.04f;
            }
            else
            {
                art.color = available ? new Color(0.82f, 0.84f, 0.9f) : new Color(0.45f, 0.47f, 0.55f);
            }
        }
    }

    // Sprites da tela Preparar Tropas (Assets/LegionRush/UI/Prep/Resources/PrepArt/<nome>).
    public static class PrepArt
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out Sprite sprite)) return sprite;
            sprite = Resources.Load<Sprite>("PrepArt/" + name);
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
                GameLog.Warn("Arte da preparação ausente: " + sprite);
            }
            return image;
        }
    }
}
