using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // 1. Splash
    public class SplashScreen : UIScreen
    {
        private Text prompt;
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

            Text legion = UI.Label(root, "LEGION", 170, UIStyle.Gold);
            UI.Place(legion.rectTransform, 0.05f, 0.6f, 0.95f, 0.72f);
            Text rush = UI.Label(root, "RUSH", 170, new Color(0.55f, 0.8f, 1f));
            UI.Place(rush.rectTransform, 0.05f, 0.49f, 0.95f, 0.61f);
            Text threeD = UI.Label(root, "3D", 110, UIStyle.Gold);
            UI.Place(threeD.rectTransform, 0.3f, 0.42f, 0.7f, 0.5f);
            Text subtitle = UI.Label(root, "RISE OF HEROES", 52, Color.white);
            UI.Place(subtitle.rectTransform, 0.1f, 0.36f, 0.9f, 0.41f);
            prompt = UI.Label(root, "TOQUE PARA COMEÇAR", 48, Color.white);
            UI.Place(prompt.rectTransform, 0.1f, 0.12f, 0.9f, 0.18f);
            Text version = UI.Label(root, "v" + Application.version, 30, UIStyle.TextDim, TextAnchor.MiddleCenter, false);
            UI.Place(version.rectTransform, 0.3f, 0.02f, 0.7f, 0.05f);
            root.gameObject.AddComponent<SplashPulse>().Target = prompt;
        }

        public override void Refresh() => GameAudio.PlayMusic(MusicTrack.Menu);
    }

    public class SplashPulse : MonoBehaviour
    {
        public Text Target;

        private void Update()
        {
            if (Target == null) return;
            Color c = Target.color;
            c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f));
            Target.color = c;
        }
    }

    // 2. Menu inicial
    public class MainMenuScreen : MenuScreen
    {
        private Button playButton;
        private Button missionsButton;
        private Button achievementsButton;
        private Button dailyButton;

        protected override string Title => "LEGION RUSH";
        protected override bool ShowBack => false;
        protected override Color BackgroundColor => new Color(0.03f, 0.06f, 0.15f, 0.55f);
        public override bool AllowBack => false;

        protected override void BuildBody(RectTransform body)
        {
            playButton = UI.Button(body, "JOGAR", UIStyle.Gold, () => Game.PlayLevel(Game.DefaultLevelNumber()), 72, "Play");
            UI.Place((RectTransform)playButton.transform, 0.12f, 0.6f, 0.88f, 0.72f);

            string[] labels = { "MUNDOS", "HERÓIS", "TROPAS", "MELHORIAS", "EQUIPAMENTOS", "INVENTÁRIO", "LOJA", "MISSÕES", "CONQUISTAS", "DIÁRIA", "RECORDES", "AJUSTES" };
            System.Action[] actions =
            {
                () => Ui.Push<WorldSelectScreen>(), () => Ui.Push<HeroScreen>(), () => Ui.Push<TroopScreen>(),
                () => Ui.Push<UpgradeScreen>(), () => Ui.Push<EquipmentScreen>(), () => Ui.Push<InventoryScreen>(),
                () => Ui.Push<ShopScreen>(), () => Ui.Push<MissionsScreen>(), () => Ui.Push<AchievementsScreen>(),
                () => Ui.Push<DailyRewardScreen>(), () => Ui.Push<RecordsScreen>(), () => Ui.Push<SettingsScreen>()
            };
            Color[] colors =
            {
                UIStyle.Blue, UIStyle.Purple, UIStyle.Blue, UIStyle.Green, UIStyle.Blue, UIStyle.Blue,
                UIStyle.Gold, UIStyle.Green, UIStyle.Purple, UIStyle.Gold, UIStyle.Blue, UIStyle.Gray
            };
            for (int i = 0; i < labels.Length; i++)
            {
                int column = i % 3;
                int row = i / 3;
                Button button = UI.Button(body, labels[i], colors[i], actions[i], 34, labels[i]);
                UI.Place((RectTransform)button.transform, 0.02f + column * 0.327f, 0.43f - row * 0.105f, 0.02f + column * 0.327f + 0.31f, 0.52f - row * 0.105f);
                if (i == 7) missionsButton = button;
                else if (i == 8) achievementsButton = button;
                else if (i == 9) dailyButton = button;
            }
        }

        protected override void RefreshBody()
        {
            int level = Game.DefaultLevelNumber();
            UI.SetLabel(playButton, $"JOGAR\nFASE {level} · {WorldLightingProfile.DefaultName(LevelCatalog.WorldOf(level)).ToUpperInvariant()}");
            int missions = MetaService.ClaimableMissions();
            int achievements = MetaService.ClaimableAchievements();
            UI.SetLabel(missionsButton, missions > 0 ? $"MISSÕES ({missions}!)" : "MISSÕES");
            UI.SetLabel(achievementsButton, achievements > 0 ? $"CONQUISTAS ({achievements}!)" : "CONQUISTAS");
            UI.SetLabel(dailyButton, MetaService.CanClaimDaily(out _) ? "DIÁRIA (!)" : "DIÁRIA");
            GameAudio.PlayMusic(MusicTrack.Menu);
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
        private Text heading;

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
                bool bossLevel = levelNumber == 8 || levelNumber == 15 || levelNumber == 22 || levelNumber == 26 || levelNumber == 30;
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
