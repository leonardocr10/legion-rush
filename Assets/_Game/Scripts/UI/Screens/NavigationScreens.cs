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

    // 2. Menu inicial. Cada elemento é um sprite real recortado da arte de referência (Resources/UIArt/home_*),
    // com dados ao vivo por cima. Grupos presos às bordas: topo, colunas laterais e base, para qualquer proporção.
    public class MainMenuScreen : UIScreen
    {
        private static readonly Color DarkGold = new Color(0.32f, 0.16f, 0.03f);
        private static readonly Vector4 ProfileBox = new Vector4(15, 15, 407, 103);
        private static readonly Vector4 GoldBox = new Vector4(437, 22, 642, 84);
        private static readonly Vector4 GemsBox = new Vector4(655, 22, 850, 84);
        private static readonly Vector4 MissionsBox = new Vector4(19, 146, 124, 263);
        private static readonly Vector4 DailyBox = new Vector4(19, 285, 124, 401);
        private static readonly Vector4 EventsBox = new Vector4(19, 423, 124, 541);
        private static readonly Vector4 FreeChestBox = new Vector4(669, 127, 925, 236);
        private static readonly Vector4 EpicChestBox = new Vector4(669, 245, 925, 347);
        private static readonly Vector4 PassBox = new Vector4(669, 363, 925, 496);
        private static readonly Vector4 PlayBox = new Vector4(232, 1072, 700, 1262);
        private static readonly Vector4 HeroesBox = new Vector4(725, 1108, 893, 1252);
        private static readonly Vector4 NavBox = new Vector4(0, 1494, 941, 1672);
        private static readonly float[] WorldLefts = { 20f, 245f, 475f, 704f };
        private static readonly float[] WorldRights = { 237f, 465f, 695f, 922f };

        public override bool AllowBack => false;
        private GameBootstrap Game => Ui.Game;

        private TMP_Text levelText;
        private Image xpFill;
        private TMP_Text xpText;
        private TMP_Text goldText;
        private TMP_Text gemsText;
        private TMP_Text freeChestText;
        private TMP_Text epicChestText;
        private Image passFill;
        private TMP_Text passText;
        private TMP_Text passLevelText;
        private TMP_Text playSubtitle;
        private TMP_Text missionsBadge;
        private GameObject dailyBadge;
        private GameObject eventsBadge;
        private GameObject heroesBadge;
        private readonly GameObject[] worldLocks = new GameObject[4];
        private int lastGold = -1;
        private int lastGems = -1;

        protected override void Build(RectTransform root)
        {
            Image fill = root.gameObject.AddComponent<Image>();
            fill.color = new Color(0.02f, 0.05f, 0.14f);
            BuildBackground(root);
            BuildTopBar(root);
            BuildSides(root);
            BuildBottom(root);
            root.gameObject.AddComponent<UITicker>().Tick = UpdateTimers;
        }

        // ---------- Cenário: castelo e heróis, respiração lenta, neve e vinheta ----------

        private static void BuildBackground(RectTransform root)
        {
            Image art = UIArt.Image(root, "home_bg", false);
            AspectRatioFitter fitter = art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = art.sprite != null ? art.sprite.rect.width / art.sprite.rect.height : 0.55f;
            UIBreath breath = art.gameObject.AddComponent<UIBreath>();
            breath.amount = 0.015f;
            breath.speed = 0.35f;
            UISnow.Attach(root);
            Gradient(root, 0.86f, 1f, 0.55f, true);
            Gradient(root, 0f, 0.36f, 0.75f, false);
        }

        private static void Gradient(RectTransform root, float yMin, float yMax, float alpha, bool top)
        {
            const int steps = 10;
            for (int i = 0; i < steps; i++)
            {
                float a = alpha * (top ? (i + 1f) / steps : (steps - i) / (float)steps);
                RectTransform band = UI.Rect(root, "Shade");
                UI.Place(band, 0f, Mathf.Lerp(yMin, yMax, i / (float)steps), 1f, Mathf.Lerp(yMin, yMax, (i + 1f) / steps));
                Image image = band.gameObject.AddComponent<Image>();
                image.color = new Color(0.01f, 0.03f, 0.1f, a * a);
                image.raycastTarget = false;
            }
        }

        private Button Element(RectTransform root, string sprite, LegionUI.Edge edge, Vector4 box, System.Action onClick,
            Vector2 slideFrom, float delay)
        {
            Button button = LegionUI.SpriteButton(root, sprite, onClick);
            LegionUI.Place((RectTransform)button.transform, edge, box.x, box.y, box.z, box.w);
            UISlideIn slide = button.gameObject.AddComponent<UISlideIn>();
            slide.offset = slideFrom;
            slide.delay = delay;
            return button;
        }

        // ---------- Topo: perfil, moedas, ajustes ----------

        private void BuildTopBar(RectTransform root)
        {
            var down = new Vector2(0f, 90f);
            Transform profile = Element(root, "home_profile", LegionUI.Edge.TopLeft, ProfileBox, () => Ui.Push<HeroScreen>(), down, 0f).transform;
            LegionUI.Text(profile, ProfileBox, 135, 26, 330, 64, "Comandante", 30, LegionTextStyle.Menu, Color.white, TextAnchor.MiddleLeft);
            levelText = LegionUI.Text(profile, ProfileBox, 133, 66, 218, 97, "Nível 1", 22, LegionTextStyle.Hud, UIStyle.Gold, TextAnchor.MiddleLeft);
            xpFill = LegionUI.ProgressBar(profile, ProfileBox, 224, 70, 399, 94, new Color(0.2f, 0.58f, 1f));
            xpText = LegionUI.Text(profile, ProfileBox, 224, 68, 399, 96, "0/90", 20, LegionTextStyle.Hud, Color.white);

            Transform gold = Element(root, "home_gold", LegionUI.Edge.Top, GoldBox, () => Ui.Push<ShopScreen>(), down, 0.05f).transform;
            goldText = LegionUI.Text(gold, GoldBox, 497, 32, 592, 74, "0", 29, LegionTextStyle.Hud, Color.white);
            Transform gems = Element(root, "home_gems", LegionUI.Edge.Top, GemsBox, () => Ui.Push<ShopScreen>(), down, 0.1f).transform;
            gemsText = LegionUI.Text(gems, GemsBox, 712, 32, 800, 74, "0", 29, LegionTextStyle.Hud, Color.white);
            Element(root, "home_settings", LegionUI.Edge.TopRight, new Vector4(862, 22, 925, 82), () => Ui.Push<SettingsScreen>(), down, 0.15f);
        }

        // ---------- Colunas laterais ----------

        private void BuildSides(RectTransform root)
        {
            var fromLeft = new Vector2(-160f, 0f);
            var fromRight = new Vector2(220f, 0f);
            Transform missions = Element(root, "home_missions", LegionUI.Edge.TopLeft, MissionsBox, () => Ui.Push<MissionsScreen>(), fromLeft, 0.1f).transform;
            missionsBadge = LegionUI.Badge(missions, MissionsBox, 111, 159, "0");
            Transform daily = Element(root, "home_daily", LegionUI.Edge.TopLeft, DailyBox, () => Ui.Push<DailyRewardScreen>(), fromLeft, 0.16f).transform;
            dailyBadge = LegionUI.Badge(daily, DailyBox, 111, 299, "!").transform.parent.gameObject;
            Transform events = Element(root, "home_events", LegionUI.Edge.TopLeft, EventsBox, () => Ui.Push<AchievementsScreen>(), fromLeft, 0.22f).transform;
            eventsBadge = LegionUI.Badge(events, EventsBox, 111, 437, "!").transform.parent.gameObject;

            Transform free = Element(root, "home_free_chest", LegionUI.Edge.TopRight, FreeChestBox, () => Ui.Push<DailyRewardScreen>(), fromRight, 0.1f).transform;
            freeChestText = LegionUI.Text(free, FreeChestBox, 806, 182, 910, 216, "00:00:00", 23, LegionTextStyle.Hud, Color.white);
            Transform epic = Element(root, "home_epic_chest", LegionUI.Edge.TopRight, EpicChestBox, () => Ui.Push<ShopScreen>(), fromRight, 0.16f).transform;
            epicChestText = LegionUI.Text(epic, EpicChestBox, 836, 298, 910, 330, "0/10", 23, LegionTextStyle.Hud, Color.white);
            Transform pass = Element(root, "home_battle_pass", LegionUI.Edge.TopRight, PassBox, () => Ui.Push<MissionsScreen>(), fromRight, 0.22f).transform;
            passFill = LegionUI.ProgressBar(pass, PassBox, 684, 458, 866, 488, UIStyle.Gold);
            passText = LegionUI.Text(pass, PassBox, 684, 456, 866, 490, "0/50", 21, LegionTextStyle.Hud, Color.white);
            passLevelText = LegionUI.Text(pass, PassBox, 876, 446, 908, 482, "1", 26, LegionTextStyle.Hud, Color.white);
        }

        // ---------- Base: MUNDOS / JOGAR / HERÓIS, mundos e navegação ----------

        private void BuildBottom(RectTransform root)
        {
            var up = new Vector2(0f, -160f);
            Element(root, "home_worlds", LegionUI.Edge.Bottom, new Vector4(45, 1108, 210, 1252), () => Ui.Push<WorldSelectScreen>(), up, 0.12f);
            Transform heroes = Element(root, "home_heroes", LegionUI.Edge.Bottom, HeroesBox, () => Ui.Push<HeroScreen>(), up, 0.12f).transform;
            heroesBadge = LegionUI.Badge(heroes, HeroesBox, 880, 1124, "!").transform.parent.gameObject;

            // JOGAR: contêiner respirando + botão com toque + faixa de brilho.
            RectTransform playHolder = LegionUI.Place(UI.Rect(root, "PlayHolder"), LegionUI.Edge.Bottom, PlayBox.x, PlayBox.y, PlayBox.z, PlayBox.w);
            playHolder.gameObject.AddComponent<UIBreath>();
            UISlideIn playSlide = playHolder.gameObject.AddComponent<UISlideIn>();
            playSlide.offset = up;
            playSlide.delay = 0.05f;
            Button play = LegionUI.SpriteButton(playHolder, "home_play", () => Game.PlayLevel(Game.DefaultLevelNumber()));
            UI.Stretch((RectTransform)play.transform);
            UIShineSweep.Attach((RectTransform)play.transform);
            playSubtitle = LegionUI.Text(play.transform, PlayBox, 322, 1194, 618, 1236, "FASE 1 - REINO CONGELADO", 23, LegionTextStyle.Body, DarkGold);

            for (int i = 0; i < WorldLefts.Length; i++)
            {
                var world = (WorldId)i;
                var box = new Vector4(WorldLefts[i], 1272, WorldRights[i], 1476);
                Transform card = Element(root, "home_world_" + i, LegionUI.Edge.Bottom, box, () => OpenWorld(world), up, 0.18f + i * 0.04f).transform;
                if (i == 0) continue;
                float cx = new[] { 0f, 350f, 585f, 815f }[i];
                RectTransform lockRect = LegionUI.Inside(UI.Rect(card, "Lock"), box, cx - 25f, 1335f, cx + 25f, 1385f);
                Image round = lockRect.gameObject.AddComponent<Image>();
                round.sprite = UIStyle.Circle;
                round.color = new Color(0.02f, 0.04f, 0.1f, 0.75f);
                round.raycastTarget = false;
                Image icon = UIArt.Image(lockRect, "icon_lock");
                UI.Stretch(icon.rectTransform, 8f, 8f, 6f, 10f);
                worldLocks[i] = lockRect.gameObject;
            }

            Transform nav = LegionUI.Place(UI.Rect(root, "Nav"), LegionUI.Edge.Bottom, NavBox.x, NavBox.y, NavBox.z, NavBox.w);
            nav.gameObject.AddComponent<UISlideIn>().offset = new Vector2(0f, -200f);
            Image navArt = UIArt.Image(nav, "home_nav", false);
            UI.Stretch(navArt.rectTransform);
            float[] edges = { 0f, 145f, 272f, 402f, 530f, 660f, 790f, 941f };
            System.Action[] actions =
            {
                () => { }, () => Ui.Push<TroopScreen>(), () => Ui.Push<InventoryScreen>(), () => Ui.Push<ShopScreen>(),
                () => Ui.Push<RecordsScreen>(), () => Ui.Toast("Clãs chegam em breve!"), () => Ui.Push<SettingsScreen>()
            };
            for (int i = 0; i < actions.Length; i++)
            {
                RectTransform tab = LegionUI.Inside(UI.Rect(nav, "Tab" + i), NavBox, edges[i], 1498, edges[i + 1], 1672);
                Image hit = tab.gameObject.AddComponent<Image>();
                hit.color = new Color(1f, 1f, 1f, 0f);
                Button button = tab.gameObject.AddComponent<Button>();
                button.targetGraphic = hit;
                ColorBlock colors = button.colors;
                colors.normalColor = new Color(1f, 1f, 1f, 0f);
                colors.selectedColor = new Color(1f, 1f, 1f, 0f);
                colors.highlightedColor = new Color(1f, 1f, 1f, 0.06f);
                colors.pressedColor = new Color(1f, 1f, 1f, 0.2f);
                button.colors = colors;
                System.Action action = actions[i];
                button.onClick.AddListener(() =>
                {
                    GameAudio.Play(SoundId.UiClick);
                    Haptics.Tap();
                    action();
                });
            }
        }

        private void OpenWorld(WorldId world)
        {
            (int first, _) = LevelCatalog.WorldRange(world);
            if (Game.IsLevelUnlocked(first)) Ui.Push<LevelSelectScreen>().Show(world);
            else Ui.Toast("Conclua o mundo anterior para desbloquear");
        }

        // ---------- Dados ----------

        public override void Refresh()
        {
            SaveData save = SaveManager.Current;
            int level = Game.DefaultLevelNumber();
            int stars = save.progress.TotalStars();
            int maxStars = LevelCatalog.TotalLevels * 3;

            levelText.text = "Nível " + save.progress.highestUnlocked;
            LegionUI.SetProgress(xpFill, stars / (float)maxStars);
            xpText.text = $"{stars}/{maxStars}";
            if (lastGold >= 0 && Economy.Gold > lastGold) LegionTextFx.Play(goldText, LegionTextAnimation.Pop);
            if (lastGems >= 0 && Economy.Gems > lastGems) LegionTextFx.Play(gemsText, LegionTextAnimation.Pop);
            lastGold = Economy.Gold;
            lastGems = Economy.Gems;
            goldText.text = Short(Economy.Gold);
            gemsText.text = Short(Economy.Gems);

            epicChestText.text = $"{save.records.victories % 10}/10";
            int passStars = stars % 50;
            LegionUI.SetProgress(passFill, passStars / 50f);
            passText.text = $"{passStars}/50";
            passLevelText.text = (stars / 50 + 1).ToString();

            WorldId current = LevelCatalog.WorldOf(level);
            playSubtitle.text = $"FASE {level} - {WorldLightingProfile.DefaultName(current).ToUpperInvariant()}";

            int missions = MetaService.ClaimableMissions();
            missionsBadge.transform.parent.gameObject.SetActive(missions > 0);
            missionsBadge.text = missions.ToString();
            dailyBadge.SetActive(MetaService.CanClaimDaily(out _));
            eventsBadge.SetActive(MetaService.ClaimableAchievements() > 0);
            heroesBadge.SetActive(false);

            for (int i = 0; i < worldLocks.Length; i++)
            {
                (int first, _) = LevelCatalog.WorldRange((WorldId)i);
                if (worldLocks[i] != null) worldLocks[i].SetActive(!Game.IsLevelUnlocked(first));
            }
            UpdateTimers();
            GameAudio.PlayMusic(MusicTrack.Menu);
        }

        // Baú grátis = recompensa diária: "ABRIR!" quando disponível, senão contagem até a meia-noite UTC.
        private void UpdateTimers()
        {
            if (!SaveManager.IsLoaded || freeChestText == null) return;
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

        private static string Short(int value)
        {
            if (value >= 1000000) return (value / 1000000f).ToString("0.#") + "M";
            if (value >= 10000) return (value / 1000f).ToString("0.#") + "K";
            return value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.');
        }
    }

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
