using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // HUD da partida (proposta 1b de entrega/telas/HUD Batalha - Propostas.html) com a arte Hud/ recortada da referência:
    // moedas e gemas abreviadas, nível com 3 nós, ajustes, barra do chefão, retratos laterais (o rei mostra o total do
    // exército) e 4 habilidades compactas. A postura saiu do HUD (fica na Preparação e na Pausa) e o cartão de fase também.
    // Menus, preparação, pausa e resultados são telas do UIRoot.
    public class GameHud : MonoBehaviour
    {
        private static readonly string[] ClassPortraits =
        {
            "BattleArt:Portrait_Archer", "BattleArt:Portrait_Warrior", "TroopCards/troop_lancer", "Hud/portrait_mage",
            "Hud/portrait_healer", "BattleArt:Portrait_Knight"
        };

        // Coluna da direita: rei + até 4 classes (cabem 5 retratos).
        private const int MaxPortraits = 5;

        private LevelManager level;
        private ArmyManager army;
        private GameBootstrap bootstrap;
        private SkillSystem skills;

        private CanvasGroup group;
        private RectTransform safeRoot;
        private Rect lastSafeArea;
        private TMP_Text goldText;
        private TMP_Text gemsText;
        private TMP_Text levelLabel;
        private Image progressFill;
        private readonly Image[] progressNodes = new Image[3];
        private GameObject bossPanel;
        private Image bossFill;
        private TMP_Text bossValue;
        private TMP_Text bossName;
        private TMP_Text kingCount;
        private readonly GameObject[] classSlots = new GameObject[ArmyConfig.ClassCount];
        private readonly TMP_Text[] classCounts = new TMP_Text[ArmyConfig.ClassCount];
        private readonly Button[] skillButtons = new Button[SkillSystem.SlotCount];
        private readonly Image[] skillCooldown = new Image[SkillSystem.SlotCount];
        private readonly TMP_Text[] skillTimer = new TMP_Text[SkillSystem.SlotCount];
        private readonly GameObject[] skillBadge = new GameObject[SkillSystem.SlotCount];
        private readonly bool[] skillReady = new bool[SkillSystem.SlotCount];
        private readonly GameObject[] skillReadyIcon = new GameObject[SkillSystem.SlotCount];
        private int shownGold = int.MinValue;
        private int shownGems = int.MinValue;
        private int shownSoldiers = -1;
        private float classRefresh;

        public void Initialize(LevelManager levelManager, ArmyManager armyManager, GameBootstrap owner, SkillSystem skillSystem)
        {
            level = levelManager;
            army = armyManager;
            bootstrap = owner;
            skills = skillSystem;
            level.BossAwakened += HandleBossAwakened;
            level.BossDefeated += HandleBossDefeated;
            level.GateApplied += HandleGateApplied;
            skills.SkillFailed += HandleSkillFailed;
            if (level.Boss != null) level.Boss.PhaseChanged += HandleBossPhase;
            Build();
        }

        private void OnDestroy()
        {
            if (level != null)
            {
                level.BossAwakened -= HandleBossAwakened;
                level.BossDefeated -= HandleBossDefeated;
                level.GateApplied -= HandleGateApplied;
                if (level.Boss != null) level.Boss.PhaseChanged -= HandleBossPhase;
            }
            if (skills != null) skills.SkillFailed -= HandleSkillFailed;
        }

        private static void Toast(string text)
        {
            if (UIRoot.Instance != null) UIRoot.Instance.Toast(text);
        }

        private void HandleBossAwakened(BossController boss) => Toast($"{boss.Definition.displayName.ToUpperInvariant()} DESPERTOU!");

        private void HandleBossDefeated(BossController boss)
        {
            Toast($"{boss.Definition.displayName.ToUpperInvariant()} DERROTADO!");
            Haptics.Impact();
        }

        private void HandleBossPhase(BossController boss, int phase) => Toast($"FASE {phase} DO CHEFÃO!");
        private void HandleSkillFailed(SkillDefinition skill) => Toast("Sem alvo no alcance");

        // O ganho ou a perda aparece como número flutuante sobre o exército (CommanderController); aqui só vibra.
        private void HandleGateApplied(GateOption option, int before, int after)
        {
            if (!option.IsPositive) Haptics.Impact();
        }

        // ---------- Construção ----------

        private void Build()
        {
            var canvasGo = new GameObject("LR_HudCanvas", typeof(RectTransform));
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = Screen.width / (float)Mathf.Max(1, Screen.height) > 0.7f ? 1f : 0f;
            canvasGo.AddComponent<GraphicRaycaster>();
            group = canvasGo.AddComponent<CanvasGroup>();
            safeRoot = UI.Rect(canvasGo.transform, "Safe");
            ApplySafeArea();

            BuildTop();
            BuildBoss();
            BuildPortraits();
            BuildSkills();
        }

        // Topo no grid do mockup (941×1672): moedas, nível com trilho e 3 nós, gemas e ajustes.
        private void BuildTop()
        {
            goldText = Wallet("Hud/hud_gold", LegionUI.Edge.TopLeft, 16, 16, 211, 86, 0.38f, 0.94f);
            gemsText = Wallet("Hud/hud_gems", LegionUI.Edge.TopRight, 676, 16, 852, 86, 0.4f, 0.94f);

            Image settings = LegionArt.Image(safeRoot, "Hud/hud_settings", false, "Settings");
            LegionUI.Place(settings.rectTransform, LegionUI.Edge.TopRight, 864, 16, 930, 86);
            MainMenuButtons.Make(settings.rectTransform, settings, Pause);

            // Nível: aba com o título e trilho preenchido pelo avanço na pista, coroa no fim.
            Image levelArt = BattleArt.Image(safeRoot, "Level_Bar", "Level");
            levelArt.preserveAspect = false;
            RectTransform levelBox = LegionUI.Place(levelArt.rectTransform, LegionUI.Edge.Top, 271, 10, 671, 97);
            levelLabel = UI.Label(levelBox, "NÍVEL 1", Mathf.RoundToInt(30 * LegionUI.Px), Color.white, TextAnchor.MiddleCenter, true, "Title",
                LegionTextStyle.Hud);
            UI.Place(levelLabel.rectTransform, 0.22f, 0.5f, 0.78f, 1f);
            levelLabel.textWrappingMode = TextWrappingModes.NoWrap;
            RectTransform track = UI.Rect(levelBox, "Track");
            UI.Place(track, 0.085f, 0.18f, 0.855f, 0.46f);
            progressFill = LegionArt.Image(track, "ProgressBars/bar_fill_orange", true, "Fill");
            progressFill.color = new Color(1f, 0.86f, 0.25f);
            UI.Stretch(progressFill.rectTransform, 2f, 2f, 2f, 2f);
            // 3 nós redondos nos quartos do trilho; ficam dourados quando o exército passa por eles.
            for (int i = 0; i < progressNodes.Length; i++)
            {
                RectTransform node = UI.Rect(track, "Node" + i);
                float x = (i + 1) / 4f;
                node.anchorMin = node.anchorMax = new Vector2(x, 0.5f);
                node.sizeDelta = new Vector2(30f, 30f);
                Image ring = node.gameObject.AddComponent<Image>();
                ring.sprite = UIStyle.Circle;
                ring.color = new Color(0.03f, 0.07f, 0.2f);
                ring.raycastTarget = false;
                Image dot = UI.Panel(node, Color.white, "Dot");
                dot.sprite = UIStyle.Circle;
                dot.type = Image.Type.Simple;
                UI.Stretch(dot.rectTransform, 5f, 5f, 5f, 5f);
                progressNodes[i] = dot;
            }
        }

        private TMP_Text Wallet(string sprite, LegionUI.Edge edge, float x0, float y0, float x1, float y1, float textMin, float textMax)
        {
            Image chip = LegionArt.Image(safeRoot, sprite, false, "Chip");
            chip.preserveAspect = false;
            LegionUI.Place(chip.rectTransform, edge, x0, y0, x1, y1);
            TMP_Text value = UI.Label(chip.transform, "0", Mathf.RoundToInt(34 * LegionUI.Px), Color.white, TextAnchor.MiddleCenter, true, "Value",
                LegionTextStyle.Hud);
            UI.Place(value.rectTransform, textMin, 0.1f, textMax, 0.9f);
            value.textWrappingMode = TextWrappingModes.NoWrap;
            return value;
        }

        // Barra do chefão no topo (285,110,670,176), caveira sobreposta à esquerda e valor dentro da barra. Aparece quando o
        // chefão entra na tela; nome e estado (22) só durante a luta.
        private void BuildBoss()
        {
            RectTransform panel = LegionUI.Place(UI.Rect(safeRoot, "Boss"), LegionUI.Edge.Top, 285, 110, 670, 176);
            bossPanel = panel.gameObject;
            RectTransform bar = UI.Rect(panel, "Bar");
            UI.Place(bar, 0.08f, 0.12f, 1f, 0.88f);
            Image track = LegionArt.Image(bar, "Hud/hud_boss_track", true, "Track");
            UI.Stretch(track.rectTransform);
            bossFill = LegionArt.Image(bar, "Hud/hud_boss_fill", true, "Fill");
            UI.Stretch(bossFill.rectTransform, 3f, 3f, 3f, 3f);
            bossValue = UI.Label(bar, "0", Mathf.RoundToInt(28 * LegionUI.Px), Color.white, TextAnchor.MiddleCenter, true, "Value", LegionTextStyle.Hud);
            UI.Stretch(bossValue.rectTransform, 40f, 10f, 0f, 0f);
            Image skull = LegionArt.Image(panel, "Hud/hud_skull", false, "Skull");
            skull.rectTransform.anchorMin = skull.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            skull.rectTransform.sizeDelta = new Vector2(96f, 100f);
            skull.rectTransform.anchoredPosition = new Vector2(12f, 0f);
            bossName = UI.Label(panel, string.Empty, Mathf.RoundToInt(22 * LegionUI.Px), Color.white, TextAnchor.MiddleCenter, true, "Name",
                LegionTextStyle.Menu);
            bossName.rectTransform.anchorMin = new Vector2(0f, 0f);
            bossName.rectTransform.anchorMax = new Vector2(1f, 0f);
            bossName.rectTransform.pivot = new Vector2(0.5f, 1f);
            bossName.rectTransform.sizeDelta = new Vector2(0f, 34f);
            bossName.rectTransform.anchoredPosition = new Vector2(0f, -4f);
            bossName.textWrappingMode = TextWrappingModes.NoWrap;
            bossPanel.SetActive(false);
        }

        // Retratos à direita: o rei com o total do exército e um retrato por classe presente.
        private void BuildPortraits()
        {
            kingCount = Portrait("BattleArt:Portrait_King", 0, out _);
            for (int i = 0; i < ArmyConfig.ClassCount; i++)
                classCounts[i] = Portrait(ClassPortraits[i], i + 1, out classSlots[i]);
        }

        // Coluna da direita no grid do mockup: um retrato a cada 94 px a partir de y = 560.
        private static void PlacePortrait(RectTransform rect, int slot)
        {
            float y0 = 560f + slot * 94f;
            LegionUI.Place(rect, LegionUI.Edge.TopRight, 850, y0, 930, y0 + 86);
        }

        private TMP_Text Portrait(string sprite, int index, out GameObject slot)
        {
            RectTransform rect = UI.Rect(safeRoot, "Portrait");
            PlacePortrait(rect, Mathf.Min(index, MaxPortraits - 1));
            Image art = sprite.StartsWith("BattleArt:")
                ? BattleArt.Image(rect, sprite.Substring(10), "Art")
                : LegionArt.Image(rect, sprite, false, "Art");
            UI.Stretch(art.rectTransform);
            // Selo redondo da contagem (cobre o número desenhado na arte).
            RectTransform badge = UI.Rect(rect, "Count");
            UI.Place(badge, 0.56f, -0.08f, 1.08f, 0.42f);
            Image box = badge.gameObject.AddComponent<Image>();
            box.sprite = UIStyle.Circle;
            box.color = new Color(0.05f, 0.11f, 0.24f, 1f);
            box.preserveAspect = true;
            box.raycastTarget = false;
            Outline ring = badge.gameObject.AddComponent<Outline>();
            ring.effectColor = new Color(0.35f, 0.7f, 1f);
            ring.effectDistance = new Vector2(2f, -2f);
            TMP_Text count = UI.Label(badge, "0", Mathf.RoundToInt(22 * LegionUI.Px), Color.white, TextAnchor.MiddleCenter, true, "Value",
                LegionTextStyle.Hud);
            UI.Stretch(count.rectTransform, 2f, 2f, 0f, 2f);
            count.textWrappingMode = TextWrappingModes.NoWrap;
            // O selo do rei mostra o total do exército (o contador sobre as tropas saiu); a coroa fica em cima do retrato.
            if (index == 0)
            {
                Image crown = LegionArt.Image(rect, "Hud/hud_crown", false, "Crown");
                UI.Place(crown.rectTransform, 0.22f, 0.86f, 0.78f, 1.22f);
                crown.preserveAspect = true;
            }
            slot = rect.gameObject;
            return count;
        }

        // 4 habilidades compactas de 121×163, x0 {200,340,480,620}, y 1455–1618. Só arte Hud/skill_* sem texto; Escudo e Cura
        // usam Hud/skill_shield e Hud/skill_heal quando existirem (Prompt 2) e, até lá, a moldura dourada com ícone.
        private void BuildSkills()
        {
            float[] x0 = { 200, 340, 480, 620 };
            for (int i = 0; i < SkillSystem.SlotCount; i++)
            {
                SkillDefinition skill = skills.Slot(i);
                if (skill == null) continue;
                int index = i;
                Button button = LegionUI.SpriteButton(safeRoot, SkillArt(skill.id, out string icon), () => UseSkill(index), "Skill" + i);
                ((Image)button.targetGraphic).preserveAspect = true;
                RectTransform rect = (RectTransform)button.transform;
                LegionUI.Place(rect, LegionUI.Edge.Bottom, x0[i % x0.Length], 1455, x0[i % x0.Length] + 121, 1618);
                if (icon != null)
                {
                    Image symbol = LegionArt.Image(rect, icon, false, "Icon");
                    UI.Place(symbol.rectTransform, 0.16f, 0.3f, 0.84f, 0.84f);
                }
                Image cooldown = UI.Panel(rect, new Color(0f, 0.02f, 0.08f, 0.62f), "Cooldown");
                cooldown.sprite = null;
                cooldown.type = Image.Type.Filled;
                cooldown.fillMethod = Image.FillMethod.Vertical;
                cooldown.fillOrigin = (int)Image.OriginVertical.Top;
                UI.Place(cooldown.rectTransform, 0.08f, 0.05f, 0.92f, 0.95f);
                // Círculo da própria arte (canto inferior direito): segundos de recarga ou "pronto".
                RectTransform badge = UI.Rect(rect, "Badge");
                UI.Place(badge, 0.6f, 0.02f, 0.98f, 0.27f);
                TMP_Text timer = UI.Label(badge, "0", Mathf.RoundToInt(28 * LegionUI.Px), Color.white, TextAnchor.MiddleCenter, true, "Value",
                    LegionTextStyle.Hud);
                UI.Stretch(timer.rectTransform);
                Image ready = LegionArt.Image(rect, "Badges/check", false, "Ready");
                UI.Place(ready.rectTransform, 0.64f, 0.04f, 0.94f, 0.25f);
                skillReadyIcon[i] = ready.gameObject;
                skillButtons[i] = button;
                skillCooldown[i] = cooldown;
                skillTimer[i] = timer;
                skillBadge[i] = badge.gameObject;
                skillReady[i] = true;
            }
        }

        private static string SkillArt(SkillId id, out string icon)
        {
            icon = null;
            switch (id)
            {
                case SkillId.ArrowRain: return "Hud/skill_arrows";
                case SkillId.CommanderStrike:
                case SkillId.CavalryCharge: return "Hud/skill_charge";
                case SkillId.ArcaneExplosion:
                case SkillId.Freeze: return "Hud/skill_storm";
                case SkillId.CollectiveShield:
                    if (LegionArt.Pick("Hud/skill_shield", "Hud/skill_frame") == "Hud/skill_shield") return "Hud/skill_shield";
                    icon = "Icons/shield";
                    break;
                case SkillId.AreaHeal:
                    if (LegionArt.Pick("Hud/skill_heal", "Hud/skill_frame") == "Hud/skill_heal") return "Hud/skill_heal";
                    icon = "Icons/potion_green";
                    break;
                default: icon = "Icons/star_badge"; break;
            }
            return "Hud/skill_frame";
        }

        // ---------- Ações ----------

        private void Pause()
        {
            if (level == null || level.Current != LevelManager.State.Running) return;
            level.SetPaused(true);
            if (UIRoot.Instance != null) UIRoot.Instance.Push<PauseScreen>();
        }

        private void UseSkill(int index)
        {
            if (!skills.IsReady(index)) return;
            if (skills.TryUse(index))
            {
                Haptics.Tap();
                LegionTextFx.Play(skillTimer[index], LegionTextAnimation.Pop);
            }
        }

        // ---------- Atualização ----------

        private void Update()
        {
            if (level == null || group == null) return;
            bool visible = level.Current == LevelManager.State.Running && !level.IsPaused &&
                           (UIRoot.Instance == null || !UIRoot.Instance.IsBlocking);
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            if (!visible) return;
            if (Screen.safeArea != lastSafeArea) ApplySafeArea();

            if (level.Gold != shownGold)
            {
                if (shownGold != int.MinValue && level.Gold > shownGold) LegionTextFx.Play(goldText, LegionTextAnimation.Pop);
                shownGold = level.Gold;
                goldText.text = LegionFormat.Short(shownGold);
            }
            if (Economy.Gems != shownGems)
            {
                shownGems = Economy.Gems;
                gemsText.text = LegionFormat.Short(shownGems);
            }
            levelLabel.SetText("NÍVEL {0}", level.Config.levelNumber);
            float progress = level.Progress;
            LegionUI.SetProgress(progressFill, progress);
            for (int i = 0; i < progressNodes.Length; i++)
                progressNodes[i].color = progress >= (i + 1) / 4f ? new Color(1f, 0.8f, 0.2f) : new Color(0.35f, 0.45f, 0.65f);

            UpdateArmy();
            UpdateBoss();
            UpdateSkills();
        }

        private void UpdateArmy()
        {
            classRefresh -= Time.unscaledDeltaTime;
            if (army.Count == shownSoldiers && classRefresh > 0f) return;
            if (shownSoldiers >= 0 && army.Count > shownSoldiers) LegionTextFx.Play(kingCount, LegionTextAnimation.Punch);
            shownSoldiers = army.Count;
            classRefresh = 0.5f;
            kingCount.SetText("{0}", shownSoldiers);
            // Empilha só os retratos visíveis, sem buracos; cabem 5 (rei + 4 classes).
            int slot = 1;
            for (int i = 0; i < ArmyConfig.ClassCount; i++)
            {
                int n = army.CountOf((UnitClass)i);
                bool show = n > 0 && slot < MaxPortraits;
                classSlots[i].SetActive(show);
                if (!show) continue;
                classCounts[i].SetText("{0}", n);
                PlacePortrait((RectTransform)classSlots[i].transform, slot);
                slot++;
            }
        }

        private void UpdateBoss()
        {
            BossController boss = level.Boss;
            bool engaged = boss != null && boss.IsEngaged;
            bool show = boss != null && boss.IsAlive && (engaged || OnScreen(boss.transform.position));
            if (bossPanel.activeSelf != show) bossPanel.SetActive(show);
            if (!show) return;
            bossName.gameObject.SetActive(engaged);
            LegionUI.SetProgress(bossFill, boss.HealthRatio);
            bossValue.text = Short(boss.Health);
            string status = boss.IsVulnerable ? "VULNERÁVEL!" : boss.IsAirborne ? "EM VOO" : $"FASE {boss.Phase}";
            bossName.text = $"{boss.Definition.displayName.ToUpperInvariant()}  ·  {status}";
            bossFill.color = boss.IsVulnerable ? new Color(1f, 0.9f, 0.4f) : Color.white;
        }

        private static bool OnScreen(Vector3 world)
        {
            Camera cam = Camera.main;
            if (cam == null) return false;
            Vector3 v = cam.WorldToViewportPoint(world);
            return v.z > 0f && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f;
        }

        private void UpdateSkills()
        {
            for (int i = 0; i < SkillSystem.SlotCount; i++)
            {
                if (skillButtons[i] == null) continue;
                bool ready = skills.IsReady(i);
                skillCooldown[i].fillAmount = ready ? 0f : skills.CooldownRatio(i);
                skillBadge[i].SetActive(!ready);
                skillReadyIcon[i].SetActive(ready);
                if (!ready) skillTimer[i].SetText("{0}", Mathf.CeilToInt(skills.CooldownRemaining(i)));
                skillReady[i] = ready;
            }
        }

        private void ApplySafeArea()
        {
            lastSafeArea = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0 || safeRoot == null) return;
            safeRoot.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }

        private static string Short(float value)
        {
            if (value >= 1000000f) return (value / 1000000f).ToString("0.#") + "M";
            if (value >= 10000f) return (value / 1000f).ToString("0.#") + "K";
            return Mathf.CeilToInt(value).ToString();
        }
    }
}
