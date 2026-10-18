using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Pausa no layout do mockup (941×1672): a partida aparece escurecida ao fundo, faixa PAUSA com coroa,
    // painel com Continuar / Configurações / Reiniciar Batalha / Sair da Batalha e a missão em andamento na base.
    public class PauseScreen : MenuScreen
    {
        private static readonly Vector4 PanelBox = new Vector4(127, 560, 814, 1225);
        private static readonly Vector4 MissionBox = new Vector4(41, 1420, 900, 1623);

        private TMP_Text missionTitle;
        private TMP_Text missionCount;
        private Image missionFill;
        private RectTransform mission;

        protected override string Title => "PAUSA";
        protected override bool ShowHeader => false;
        protected override bool ShowSnow => false;
        protected override bool ShowBack => false;

        public override void OnBack() => Resume();

        protected override void BuildScenery(RectTransform root)
        {
            Image shade = root.gameObject.AddComponent<Image>();
            shade.color = new Color(0.01f, 0.03f, 0.08f, 0.55f);
        }

        protected override void BuildBody(RectTransform body)
        {
            UI.Stretch(body);

            Image title = PauseArt.Image(body, "Title_Pause", "Title");
            LegionUI.Place(title.rectTransform, LegionUI.Edge.Top, 78, 250, 864, 579);
            title.gameObject.AddComponent<UISlideIn>().offset = new Vector2(0f, 60f);

            RectTransform panel = LegionUI.Place(UI.Rect(body, "Panel"), LegionUI.Edge.Top, PanelBox.x, PanelBox.y, PanelBox.z, PanelBox.w);
            panel.SetSiblingIndex(title.transform.GetSiblingIndex());
            Box(panel, new Color32(8, 22, 48, 235), new Color32(60, 95, 150, 170), 3f);
            Shadow shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -10f);

            Row(panel, 587, 722, "Continuar", PauseArt.Get("Icon_Play"), true, Resume);
            Row(panel, 750, 881, "Configurações", PauseArt.Get("Icon_Gear"), false, () => Ui.Push<SettingsScreen>());
            Row(panel, 908, 1041, "Reiniciar Batalha", OutcomeArt.Get("Icon_Retry"), false,
                () => Ui.Confirm("REINICIAR", "Recomeçar a fase do início?", "REINICIAR", Game.RetryLevel));
            Row(panel, 1068, 1197, "Sair da Batalha", OutcomeArt.Get("Icon_Exit"), false,
                () => Ui.Confirm("SAIR DA FASE", "Voltar ao menu? O progresso desta partida será perdido.", "SAIR", Game.ReturnToMenu));

            BuildMission(body);
        }

        // Linha do painel: Continuar usa o botão azul do pacote; as demais, azul-marinho com contorno claro.
        private static void Row(RectTransform panel, float y0, float y1, string text, Sprite icon, bool primary, System.Action onClick)
        {
            Button button;
            if (primary)
            {
                button = UI.Button(panel, text, UIStyle.Blue, onClick, 46, "Row_" + text);
            }
            else
            {
                button = UI.Button(panel, text, new Color32(16, 40, 86, 255), onClick, 46, "Row_" + text);
                Outline outline = button.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color32(70, 120, 200, 170);
                outline.effectDistance = new Vector2(2f, -2f);
                Transform shine = button.transform.Find("Shine");
                if (shine != null) shine.GetComponent<Image>().color = new Color(0.45f, 0.65f, 1f, 0.07f);
            }
            LegionUI.Inside((RectTransform)button.transform, PanelBox, 158, y0, 783, y1);

            Image art = UI.Rect(button.transform, "Icon").gameObject.AddComponent<Image>();
            art.sprite = icon;
            art.preserveAspect = true;
            art.raycastTarget = false;
            UI.Place(art.rectTransform, 0.07f, 0.2f, 0.21f, 0.8f);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            UI.Place(label.rectTransform, 0.28f, 0.1f, 0.96f, 0.9f);
        }

        // Cartão da missão em andamento: pergaminho, título, barra de progresso e contagem.
        private void BuildMission(RectTransform body)
        {
            mission = LegionUI.Place(UI.Rect(body, "Mission"), LegionUI.Edge.Bottom, MissionBox.x, MissionBox.y, MissionBox.z, MissionBox.w);
            Box(mission, new Color32(8, 22, 48, 235), new Color32(60, 95, 150, 170), 3f);

            RectTransform slot = LegionUI.Inside(UI.Rect(mission, "IconSlot"), MissionBox, 68, 1445, 188, 1551);
            Box(slot, new Color32(14, 34, 70, 255), new Color32(50, 85, 140, 150), 2f);
            Image scroll = LegionArt.Image(slot, "Icons/scroll", false, "Scroll");
            scroll.preserveAspect = true;
            UI.Stretch(scroll.rectTransform, 14f, 14f, 8f, 8f);

            LegionUI.Text(mission, MissionBox, 211, 1446, 860, 1490, "Missões em andamento", 34f, LegionTextStyle.Menu, Color.white,
                TextAnchor.MiddleLeft);
            missionTitle = LegionUI.Text(mission, MissionBox, 211, 1494, 860, 1534, string.Empty, 30f, LegionTextStyle.Body,
                new Color(0.84f, 0.88f, 0.97f), TextAnchor.MiddleLeft);
            missionFill = LegionUI.ProgressBar(mission, MissionBox, 211, 1551, 845, 1588, new Color(0.15f, 0.55f, 1f));
            missionCount = LegionUI.Text(mission, MissionBox, 600, 1549, 830, 1590, string.Empty, 30f, LegionTextStyle.Hud, Color.white,
                TextAnchor.MiddleRight);
        }

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

        protected override void RefreshBody()
        {
            // Primeira missão ainda não concluída; sem nenhuma, o cartão some.
            foreach (MissionDef def in MetaCatalog.Missions)
            {
                MissionProgress progress = MetaService.Mission(def);
                if (progress.claimed || progress.progress >= def.Target) continue;
                mission.gameObject.SetActive(true);
                missionTitle.text = def.Title;
                missionCount.text = $"{progress.progress}/{def.Target}";
                LegionUI.SetProgress(missionFill, progress.progress / (float)def.Target);
                return;
            }
            mission.gameObject.SetActive(false);
        }

        private void Resume()
        {
            Ui.CloseAll();
            if (Game.CurrentLevel != null) Game.CurrentLevel.SetPaused(false);
        }
    }

    // Sprites da tela de pausa (Assets/LegionRush/UI/Pause/Resources/PauseArt/<nome>).
    public static class PauseArt
    {
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> cache = new System.Collections.Generic.Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out Sprite sprite)) return sprite;
            sprite = Resources.Load<Sprite>("PauseArt/" + name);
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
                GameLog.Warn("Arte da pausa ausente: " + sprite);
            }
            return image;
        }
    }
}
