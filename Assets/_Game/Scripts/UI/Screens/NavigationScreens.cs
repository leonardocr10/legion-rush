using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // 1. Splash
    public class SplashScreen : UIScreen
    {
        private TMP_Text prompt;
        private float time;

        public override bool AllowBack => false;

        protected override void Build(RectTransform root)
        {
            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.03f, 0.05f, 0.14f, 0.82f);
            Button tap = root.gameObject.AddComponent<Button>();
            tap.targetGraphic = background;
            tap.onClick.AddListener(() =>
            {
                GameAudio.Play(SoundId.UiConfirm);
                Ui.Replace<MainMenuScreen>();
            });

            Image art = UIArt.Image(root, "splash_bg", false);
            AspectRatioFitter fitter = art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = art.sprite != null ? art.sprite.rect.width / art.sprite.rect.height : 0.55f;
            Image shade = UI.Rect(root, "Shade").gameObject.AddComponent<Image>();
            UI.Place(shade.rectTransform, 0f, 0f, 1f, 0.24f);
            shade.color = new Color(0.01f, 0.03f, 0.1f, 0.55f);
            shade.raycastTarget = false;

            Image logo = UIArt.Image(root, "logo_text");
            UI.Place(logo.rectTransform, 0.03f, 0.66f, 0.97f, 0.98f);
            logo.gameObject.AddComponent<UIFloat>();

            Image pill = UI.Panel(root, new Color(0.03f, 0.07f, 0.2f, 0.85f), "PromptPill", true);
            UI.Place(pill.rectTransform, 0.14f, 0.1f, 0.86f, 0.165f);
            prompt = UI.Label(pill.transform, "TOQUE PARA COMEÇAR", 52, Color.white, TextAnchor.MiddleCenter, true, "Prompt", LegionTextStyle.PrimaryButton);
            UI.Stretch(prompt.rectTransform, 12f, 12f, 4f, 4f);
            TMP_Text version = UI.Label(root, "v" + Application.version, 30, UIStyle.TextDim, TextAnchor.MiddleCenter, false);
            UI.Place(version.rectTransform, 0.3f, 0.02f, 0.7f, 0.05f);
            root.gameObject.AddComponent<SplashPulse>().Target = prompt;
        }

        public override void Refresh() => GameAudio.PlayMusic(MusicTrack.Menu);
    }

    public class SplashPulse : MonoBehaviour
    {
        public TMP_Text Target;

        private void Update()
        {
            if (Target == null) return;
            Color c = Target.color;
            c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f));
            Target.color = c;
        }
    }

    // 2. Menu inicial: Assets/LegionRush/UI/MainMenu/Scripts (MainMenuScreen, MainMenuView e componentes).

    // Flutuação vertical suave (logo).
    public class UIFloat : MonoBehaviour
    {
        private void Update()
        {
            ((RectTransform)transform).anchoredPosition = new Vector2(0f, 12f * Mathf.Sin(Time.unscaledTime * 1.4f));
        }
    }

    // Chama Tick uma vez por segundo enquanto a tela está ativa.
    public class UITicker : MonoBehaviour
    {
        public System.Action Tick;
        private float next;

        private void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 1f;
            Tick?.Invoke();
        }
    }

    // 3. Seleção de mundos
    public class WorldSelectScreen : MenuScreen
    {
        private RectTransform list;

        protected override string Title => "MUNDOS";

        protected override void BuildBody(RectTransform body) => list = UI.ScrollList(body);

        protected override void RefreshBody()
        {
            UI.Clear(list);
            SaveData save = SaveManager.Current;
            for (int w = 0; w < WorldLightingProfile.WorldCount; w++)
            {
                var world = (WorldId)w;
                (int first, int last) = LevelCatalog.WorldRange(world);
                int stars = 0;
                for (int n = first; n <= last; n++)
                {
                    LevelRecord record = save.progress.Get(n, false);
                    if (record != null) stars += record.stars;
                }
                bool unlocked = Game.IsLevelUnlocked(first);
                string boss = BossDefinition.NameOf(BossDefinition.DefaultFor(world));
                string subtitle = $"Fases {first}–{last}  ·  {stars}/{(last - first + 1) * 3} estrelas\nChefão: {boss}";
                WorldId captured = world;
                ItemRow(list, $"{w + 1}. {WorldLightingProfile.DefaultName(world).ToUpperInvariant()}", subtitle,
                    unlocked ? "ENTRAR" : "BLOQUEADO", unlocked ? UIStyle.Green : UIStyle.Gray,
                    () => Ui.Push<LevelSelectScreen>().Show(captured), WorldColor(world), unlocked, 230f);
            }
        }

        public static Color WorldColor(WorldId world) => world switch
        {
            WorldId.LostDesert => new Color(1f, 0.7f, 0.3f),
            WorldId.EnchantedForest => new Color(0.35f, 0.85f, 0.4f),
            WorldId.VolcanicRealm => new Color(1f, 0.35f, 0.2f),
            _ => new Color(0.5f, 0.8f, 1f)
        };
    }

    // 4. Seleção de fases
    public class LevelSelectScreen : MenuScreen
    {
        private WorldId world;
        private RectTransform grid;
        private TMP_Text heading;

        protected override string Title => "FASES";

        public LevelSelectScreen Show(WorldId target)
        {
            world = target;
            Refresh();
            return this;
        }

        protected override void BuildBody(RectTransform body)
        {
            heading = UI.Label(body, string.Empty, 50, Color.white);
            UI.Place(heading.rectTransform, 0f, 0.92f, 1f, 1f);
            grid = UI.Rect(body, "Grid");
            UI.Place(grid, 0f, 0f, 1f, 0.9f);
        }

        protected override void RefreshBody()
        {
            heading.text = WorldLightingProfile.DefaultName(world).ToUpperInvariant();
            UI.Clear(grid);
            (int first, int last) = LevelCatalog.WorldRange(world);
            int count = last - first + 1;
            for (int i = 0; i < count; i++)
            {
                int levelNumber = first + i;
                int column = i % 3;
                int row = i / 3;
                bool available = Game.IsLevelAvailable(levelNumber);
                bool unlocked = Game.IsLevelUnlocked(levelNumber);
                LevelRecord record = SaveManager.Current.progress.Get(levelNumber, false);
                int stars = record != null ? record.stars : 0;
                bool bossLevel = LevelCatalog.IsBossStage(levelNumber);
                string label = !available ? $"{levelNumber}\nEM BREVE"
                    : !unlocked ? $"{levelNumber}\nBLOQUEADA"
                    : $"{levelNumber}{(bossLevel ? " CHEFÃO" : string.Empty)}\n{new string('*', stars)}{new string('-', 3 - stars)}";
                Color color = !available || !unlocked ? UIStyle.Gray : bossLevel ? UIStyle.Red : WorldSelectScreen.WorldColor(world) * 0.8f;
                color.a = 1f;
                int captured = levelNumber;
                Button button = UI.Button(grid, label, color, () => Game.PlayLevel(captured), 44, "Level" + levelNumber);
                UI.Place((RectTransform)button.transform, 0.02f + column * 0.33f, 0.82f - row * 0.2f, 0.02f + column * 0.33f + 0.3f, 0.98f - row * 0.2f);
                button.interactable = available && unlocked;
            }
        }
    }
}
