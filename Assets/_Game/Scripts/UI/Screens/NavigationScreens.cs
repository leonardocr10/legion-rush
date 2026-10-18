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

    // 2. Menu inicial. Arte = mockup "Menu Heroico do Reino Congelado" (UIArt/menu_home, com os dados apagados);
    // por cima vão textos ao vivo e áreas de toque nas mesmas posições (coordenadas em pixels do mockup 941x1672).
    public class MainMenuScreen : UIScreen
    {
        private const float ArtWidth = 941f;
        private const float ArtHeight = 1672f;
        private const float Px = 1080f / ArtWidth; // unidades de canvas por pixel do mockup
        private static readonly Color BadgeRed = new Color(0.86f, 0.1f, 0.12f);
        private static readonly Color DarkGold = new Color(0.32f, 0.16f, 0.03f);

        public override bool AllowBack => false;
        private GameBootstrap Game => Ui.Game;

        private RectTransform art;
        private Text levelText;
        private Image xpFill;
        private Text xpText;
        private Text goldText;
        private Text gemsText;
        private Text freeChestText;
        private Text epicChestText;
        private Image passFill;
        private Text passText;
        private Text passLevelText;
        private Text playSubtitle;
        private Text missionsBadge;
        private GameObject dailyBadge;
        private GameObject eventsBadge;
        private readonly GameObject[] worldLocks = new GameObject[4];

        protected override void Build(RectTransform root)
        {
            Image fill = root.gameObject.AddComponent<Image>();
            fill.color = new Color(0.02f, 0.05f, 0.14f);
            // Fundo desfocado/escurecido para as faixas laterais em telas com proporção diferente de 9:16.
            Image backdrop = UIArt.Image(root, "menu_home", false);
            backdrop.color = new Color(0.35f, 0.4f, 0.55f);
            AspectRatioFitter outer = backdrop.gameObject.AddComponent<AspectRatioFitter>();
            outer.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            outer.aspectRatio = ArtWidth / ArtHeight;

            Image main = UIArt.Image(root, "menu_home", false);
            art = main.rectTransform;
            AspectRatioFitter fitter = main.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = ArtWidth / ArtHeight;

            BuildTopBar();
            BuildSides();
            BuildCenter();
            BuildWorlds();
            BuildNav();
            root.gameObject.AddComponent<UITicker>().Tick = UpdateTimers;
        }

        // ---------- Utilidades de posicionamento ----------

        private RectTransform At(string name, float x0, float y0, float x1, float y1)
        {
            RectTransform rect = UI.Rect(art, name);
            rect.anchorMin = new Vector2(x0 / ArtWidth, 1f - y1 / ArtHeight);
            rect.anchorMax = new Vector2(x1 / ArtWidth, 1f - y0 / ArtHeight);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private Text Live(string text, float size, Color color, float x0, float y0, float x1, float y1,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            RectTransform rect = At("Text", x0, y0, x1, y1);
            Text label = UI.Label(rect, text, Mathf.RoundToInt(size * Px), color, anchor);
            return label;
        }

        // Área de toque invisível sobre um botão desenhado na arte; clareia ao pressionar.
        private void Hotspot(string name, float x0, float y0, float x1, float y1, System.Action onClick)
        {
            RectTransform rect = At(name, x0, y0, x1, y1);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = UIStyle.Rounded;
            image.type = Image.Type.Sliced;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.07f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.25f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                GameAudio.Play(SoundId.UiClick);
                Haptics.Tap();
                onClick();
            });
        }

        private Text Badge(float cx, float cy, string text)
        {
            RectTransform rect = At("Badge", cx - 19f, cy - 19f, cx + 19f, cy + 19f);
            Image circle = rect.gameObject.AddComponent<Image>();
            circle.sprite = UIStyle.Circle;
            circle.color = BadgeRed;
            circle.raycastTarget = false;
            Outline ring = rect.gameObject.AddComponent<Outline>();
            ring.effectColor = Color.white;
            ring.effectDistance = new Vector2(2.5f, -2.5f);
            Text label = UI.Label(rect, text, Mathf.RoundToInt(28 * Px), Color.white);
            return label;
        }

        private Image Bar(float x0, float y0, float x1, float y1, Color color)
        {
            RectTransform rect = At("Bar", x0, y0, x1, y1);
            return UI.Bar(rect, 0f, color);
        }

        // ---------- Seções ----------

        private void BuildTopBar()
        {
            Hotspot("Profile", 15, 15, 407, 103, () => Ui.Push<HeroScreen>());
            Live("Comandante", 30, Color.white, 135, 28, 330, 64, TextAnchor.MiddleLeft);
            levelText = Live("Nível 1", 22, Color.white, 133, 68, 218, 96, TextAnchor.MiddleLeft);
            xpFill = Bar(225, 70, 398, 94, new Color(0.2f, 0.55f, 1f));
            xpText = Live("0/90", 20, Color.white, 225, 68, 398, 96);

            Hotspot("Gold", 437, 24, 640, 82, () => Ui.Push<ShopScreen>());
            goldText = Live("0", 28, Color.white, 497, 34, 592, 74);
            Hotspot("Gems", 657, 24, 848, 82, () => Ui.Push<ShopScreen>());
            gemsText = Live("0", 28, Color.white, 712, 34, 800, 74);
            Hotspot("Settings", 862, 22, 925, 82, () => Ui.Push<SettingsScreen>());
        }

        private void BuildSides()
        {
            Hotspot("Missions", 20, 145, 123, 262, () => Ui.Push<MissionsScreen>());
            missionsBadge = Badge(111, 159, "0");
            Hotspot("Daily", 20, 283, 123, 400, () => Ui.Push<DailyRewardScreen>());
            dailyBadge = Badge(111, 299, "!").transform.parent.gameObject;
            Hotspot("Events", 20, 422, 123, 540, () => Ui.Push<AchievementsScreen>());
            eventsBadge = Badge(111, 437, "!").transform.parent.gameObject;

            Hotspot("FreeChest", 668, 128, 925, 236, () => Ui.Push<DailyRewardScreen>());
            freeChestText = Live("00:00:00", 22, Color.white, 805, 182, 910, 216);
            Hotspot("EpicChest", 668, 246, 925, 348, () => Ui.Push<ShopScreen>());
            epicChestText = Live("0/10", 22, Color.white, 836, 298, 910, 330);
            Hotspot("BattlePass", 668, 363, 925, 496, () => Ui.Push<MissionsScreen>());
            passFill = Bar(686, 460, 864, 486, UIStyle.Gold);
            passText = Live("0/50", 20, Color.white, 686, 458, 864, 488);
            passLevelText = Live("1", 26, Color.white, 876, 446, 908, 482);
        }

        private void BuildCenter()
        {
            Hotspot("Worlds", 45, 1108, 210, 1252, () => Ui.Push<WorldSelectScreen>());
            Hotspot("Play", 232, 1072, 700, 1262, () => Game.PlayLevel(Game.DefaultLevelNumber()));
            playSubtitle = Live("FASE 1 - REINO CONGELADO", 22, DarkGold, 325, 1196, 615, 1234);
            playSubtitle.GetComponent<Outline>().effectColor = new Color(1f, 0.9f, 0.5f, 0.6f);
            Hotspot("Heroes", 725, 1108, 893, 1252, () => Ui.Push<HeroScreen>());
        }

        private void BuildWorlds()
        {
            float[] lefts = { 20f, 245f, 475f, 704f };
            float[] lockX = { 0f, 350f, 585f, 815f };
            for (int i = 0; i < lefts.Length; i++)
            {
                var world = (WorldId)i;
                Hotspot("World" + i, lefts[i], 1272, lefts[i] + 218f, 1476, () => OpenWorld(world));
                if (i == 0) continue;
                RectTransform rect = At("Lock", lockX[i] - 24f, 1336, lockX[i] + 24f, 1384);
                Image round = rect.gameObject.AddComponent<Image>();
                round.sprite = UIStyle.Circle;
                round.color = new Color(0f, 0f, 0f, 0.55f);
                round.raycastTarget = false;
                Image icon = UIArt.Image(rect, "icon_lock");
                UI.Stretch(icon.rectTransform, 7f, 7f, 5f, 9f);
                worldLocks[i] = rect.gameObject;
            }
        }

        private void OpenWorld(WorldId world)
        {
            (int first, _) = LevelCatalog.WorldRange(world);
            if (Game.IsLevelUnlocked(first)) Ui.Push<LevelSelectScreen>().Show(world);
            else Ui.Toast("Conclua o mundo anterior para desbloquear");
        }

        private void BuildNav()
        {
            float[] edges = { 0f, 145f, 272f, 402f, 530f, 660f, 790f, 941f };
            System.Action[] actions =
            {
                () => { }, () => Ui.Push<TroopScreen>(), () => Ui.Push<InventoryScreen>(), () => Ui.Push<ShopScreen>(),
                () => Ui.Push<RecordsScreen>(), () => Ui.Toast("Clãs chegam em breve!"), () => Ui.Push<SettingsScreen>()
            };
            for (int i = 0; i < actions.Length; i++) Hotspot("Tab" + i, edges[i], 1498, edges[i + 1], 1672, actions[i]);
        }

        // ---------- Dados ----------

        public override void Refresh()
        {
            SaveData save = SaveManager.Current;
            int level = Game.DefaultLevelNumber();
            int stars = save.progress.TotalStars();
            int maxStars = LevelCatalog.TotalLevels * 3;

            levelText.text = "Nível " + save.progress.highestUnlocked;
            SetBar(xpFill, stars / (float)maxStars);
            xpText.text = $"{stars}/{maxStars}";
            goldText.text = Short(Economy.Gold);
            gemsText.text = Short(Economy.Gems);

            epicChestText.text = $"{save.records.victories % 10}/10";
            int passStars = stars % 50;
            SetBar(passFill, passStars / 50f);
            passText.text = $"{passStars}/50";
            passLevelText.text = (stars / 50 + 1).ToString();

            playSubtitle.text = $"FASE {level} - {WorldLightingProfile.DefaultName(LevelCatalog.WorldOf(level)).ToUpperInvariant()}";

            int missions = MetaService.ClaimableMissions();
            missionsBadge.transform.parent.gameObject.SetActive(missions > 0);
            missionsBadge.text = missions.ToString();
            dailyBadge.SetActive(MetaService.CanClaimDaily(out _));
            eventsBadge.SetActive(MetaService.ClaimableAchievements() > 0);

            for (int i = 1; i < worldLocks.Length; i++)
            {
                (int first, _) = LevelCatalog.WorldRange((WorldId)i);
                worldLocks[i].SetActive(!Game.IsLevelUnlocked(first));
            }
            UpdateTimers();
            GameAudio.PlayMusic(MusicTrack.Menu);
        }

        // Baú grátis = recompensa diária: "ABRIR!" quando disponível, senão contagem até a meia-noite UTC.
        private void UpdateTimers()
        {
            if (!SaveManager.IsLoaded) return;
            if (MetaService.CanClaimDaily(out _))
            {
                freeChestText.text = "ABRIR!";
                freeChestText.color = new Color(0.45f, 1f, 0.45f);
                return;
            }
            System.TimeSpan left = System.DateTime.UtcNow.Date.AddDays(1) - System.DateTime.UtcNow;
            freeChestText.text = $"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
            freeChestText.color = Color.white;
        }

        private static void SetBar(Image fill, float ratio) => fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);

        private static string Short(int value)
        {
            if (value >= 1000000) return (value / 1000000f).ToString("0.#") + "M";
            if (value >= 10000) return (value / 1000f).ToString("0.#") + "K";
            return value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.');
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
