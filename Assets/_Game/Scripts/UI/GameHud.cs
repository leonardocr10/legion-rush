using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // HUD da partida em UGUI com a arte da prancha de batalha (Resources/Hud): moedas, nível com progresso,
    // gemas, pausa, barra do chefão, retratos laterais com a contagem por classe e botões de habilidade.
    // Menus, preparação, pausa e resultados são telas do UIRoot.
    public class GameHud : MonoBehaviour
    {
        private static readonly string[] StanceLabels = { "EQUILIBRADA", "OFENSIVA", "DEFENSIVA" };
        private static readonly string[] ClassPortraits =
        {
            "Hud/portrait_archer", "Avatars/avatar_warrior", "TroopCards/troop_lancer", "Hud/portrait_mage",
            "Hud/portrait_healer", "TroopCards/troop_knight"
        };

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
        private RectTransform progressMarker;
        private GameObject bossPanel;
        private Image bossFill;
        private TMP_Text bossValue;
        private TMP_Text bossName;
        private TMP_Text stanceLabel;
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

        private void HandleGateApplied(GateOption option, int before, int after)
        {
            Toast($"{option.title} {option.subtitle}");
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

        private void BuildTop()
        {
            goldText = Chip("Hud/hud_gold", 0.015f, 0.29f, 0.41f, 0.83f);
            gemsText = Chip("Hud/hud_gems", 0.66f, 0.89f, 0.41f, 0.84f);

            Button pause = IconButton(safeRoot, "Hud/hud_settings", Pause);
            UI.Place((RectTransform)pause.transform, 0.9f, 0.945f, 0.99f, 0.993f);

            // Nível: título, trilho com marcadores e coroa no fim, preenchido pelo avanço na pista.
            RectTransform levelBox = UI.Rect(safeRoot, "Level");
            UI.Place(levelBox, 0.31f, 0.94f, 0.65f, 0.995f);
            levelLabel = UI.Label(levelBox, "NÍVEL 1", 34, Color.white, TextAnchor.MiddleCenter, true, "Title", LegionTextStyle.Hud);
            UI.Place(levelLabel.rectTransform, 0f, 0.55f, 1f, 1f);
            RectTransform track = UI.Rect(levelBox, "Track");
            UI.Place(track, 0.09f, 0.08f, 0.88f, 0.48f);
            Image back = LegionArt.Image(track, "ProgressBars/bar_track", true, "Back");
            UI.Stretch(back.rectTransform);
            progressFill = LegionArt.Image(track, "ProgressBars/bar_fill_orange", true, "Fill");
            UI.Stretch(progressFill.rectTransform, 4f, 4f, 4f, 4f);
            for (int i = 1; i <= 3; i++)
            {
                Image dot = UI.Panel(track, new Color(0.25f, 0.3f, 0.42f), "Checkpoint");
                dot.sprite = UIStyle.Circle;
                dot.type = Image.Type.Simple;
                dot.preserveAspect = true;
                float x = i / 4f;
                UI.Place(dot.rectTransform, x - 0.05f, 0.05f, x + 0.05f, 0.95f);
            }
            progressMarker = UI.Rect(track, "Marker");
            Image marker = progressMarker.gameObject.AddComponent<Image>();
            marker.sprite = UIStyle.Circle;
            marker.color = new Color(1f, 0.85f, 0.2f);
            marker.preserveAspect = true;
            marker.raycastTarget = false;
            Image start = LegionArt.Image(levelBox, "Hud/hud_level_start", false, "Start");
            UI.Place(start.rectTransform, 0f, 0f, 0.1f, 0.56f);
            Image crown = LegionArt.Image(levelBox, "Hud/hud_crown", false, "Crown");
            UI.Place(crown.rectTransform, 0.86f, 0f, 1f, 0.62f);

            // Postura do exército (funcionalidade existente), discreta abaixo das moedas.
            Button stance = UI.Button(safeRoot, string.Empty, UIStyle.Blue, CycleStance, 28, "Stance");
            UI.Place((RectTransform)stance.transform, 0.015f, 0.905f, 0.29f, 0.94f);
            stanceLabel = stance.GetComponentInChildren<TMP_Text>();
            UI.SetStyle(stanceLabel, LegionTextStyle.Menu);
        }

        private TMP_Text Chip(string sprite, float xMin, float xMax, float textMin, float textMax)
        {
            Image chip = LegionArt.Image(safeRoot, sprite, false, "Chip");
            UI.Place(chip.rectTransform, xMin, 0.945f, xMax, 0.993f);
            TMP_Text value = UI.Label(chip.transform, "0", 42, Color.white, TextAnchor.MiddleCenter, true, "Value", LegionTextStyle.Hud);
            UI.Place(value.rectTransform, textMin, 0.12f, textMax, 0.88f);
            value.textWrappingMode = TextWrappingModes.NoWrap;
            return value;
        }

        private void BuildBoss()
        {
            RectTransform panel = UI.Rect(safeRoot, "Boss");
            UI.Place(panel, 0.24f, 0.86f, 0.8f, 0.935f);
            bossPanel = panel.gameObject;
            RectTransform bar = UI.Rect(panel, "Bar");
            UI.Place(bar, 0.12f, 0.38f, 1f, 0.92f);
            Image track = LegionArt.Image(bar, "Hud/hud_boss_track", true, "Track");
            UI.Stretch(track.rectTransform);
            bossFill = LegionArt.Image(bar, "Hud/hud_boss_fill", true, "Fill");
            UI.Stretch(bossFill.rectTransform, 3f, 3f, 3f, 3f);
            bossValue = UI.Label(bar, "0", 36, Color.white, TextAnchor.MiddleCenter, true, "Value", LegionTextStyle.Hud);
            UI.Stretch(bossValue.rectTransform);
            Image skull = LegionArt.Image(panel, "Hud/hud_skull", false, "Skull");
            UI.Place(skull.rectTransform, 0f, 0.2f, 0.17f, 1.05f);
            bossName = UI.Label(panel, string.Empty, 26, Color.white, TextAnchor.MiddleCenter, true, "Name", LegionTextStyle.Menu);
            UI.Place(bossName.rectTransform, 0.12f, 0f, 1f, 0.36f);
            bossPanel.SetActive(false);
        }

        // Retratos à direita: o rei com o total do exército e um retrato por classe presente.
        private void BuildPortraits()
        {
            kingCount = Portrait("Hud/portrait_king", 0, out _);
            for (int i = 0; i < ArmyConfig.ClassCount; i++)
                classCounts[i] = Portrait(ClassPortraits[i], i + 1, out classSlots[i]);
        }

        private TMP_Text Portrait(string sprite, int index, out GameObject slot)
        {
            const float height = 0.07f;
            float top = 0.78f - index * (height + 0.008f);
            RectTransform rect = UI.Rect(safeRoot, "Portrait");
            UI.Place(rect, 0.875f, top - height, 0.99f, top);
            Image art = LegionArt.Image(rect, sprite, false, "Art");
            UI.Stretch(art.rectTransform);
            RectTransform badge = UI.Rect(rect, "Count");
            UI.Place(badge, 0.5f, -0.02f, 1.04f, 0.4f);
            Image box = badge.gameObject.AddComponent<Image>();
            box.sprite = UIStyle.Rounded;
            box.type = Image.Type.Sliced;
            box.color = new Color(0.04f, 0.1f, 0.26f, 0.97f);
            box.raycastTarget = false;
            Outline ring = badge.gameObject.AddComponent<Outline>();
            ring.effectColor = new Color(0.35f, 0.75f, 1f);
            ring.effectDistance = new Vector2(2f, -2f);
            TMP_Text count = UI.Label(badge, "0", 34, Color.white, TextAnchor.MiddleCenter, true, "Value", LegionTextStyle.Hud);
            UI.Stretch(count.rectTransform, 2f, 2f, 0f, 2f);
            slot = rect.gameObject;
            return count;
        }

        private void BuildSkills()
        {
            const float width = 0.2f;
            const float gap = 0.02f;
            float total = SkillSystem.SlotCount * width + (SkillSystem.SlotCount - 1) * gap;
            float x = 0.5f - total * 0.5f;
            for (int i = 0; i < SkillSystem.SlotCount; i++)
            {
                SkillDefinition skill = skills.Slot(i);
                if (skill == null) continue;
                int index = i;
                string sprite = SkillArt(skill.id, out string icon);
                Button button = LegionUI.SpriteButton(safeRoot, sprite, () => UseSkill(index), "Skill" + i);
                RectTransform rect = (RectTransform)button.transform;
                UI.Place(rect, x + i * (width + gap), 0.02f, x + i * (width + gap) + width, 0.15f);
                ((Image)button.targetGraphic).preserveAspect = true;
                if (icon != null)
                {
                    Image symbol = LegionArt.Image(rect, icon, false, "Icon");
                    UI.Place(symbol.rectTransform, 0.18f, 0.25f, 0.82f, 0.85f);
                    TMP_Text name = UI.Label(rect, skill.shortLabel, 26, Color.white, TextAnchor.MiddleCenter, true, "Name", LegionTextStyle.Menu);
                    UI.Place(name.rectTransform, 0.08f, 0.06f, 0.92f, 0.26f);
                }
                Image cooldown = UI.Panel(rect, new Color(0f, 0.02f, 0.08f, 0.62f), "Cooldown");
                cooldown.sprite = null;
                cooldown.type = Image.Type.Filled;
                cooldown.fillMethod = Image.FillMethod.Vertical;
                cooldown.fillOrigin = (int)Image.OriginVertical.Top;
                UI.Place(cooldown.rectTransform, 0.1f, 0.08f, 0.9f, 0.92f);
                // Selo da própria arte (canto inferior direito): segundos de recarga ou "pronto".
                RectTransform badge = UI.Rect(rect, "Badge");
                UI.Place(badge, 0.65f, 0.02f, 0.95f, 0.24f);
                TMP_Text timer = UI.Label(badge, "0", 40, Color.white, TextAnchor.MiddleCenter, true, "Value", LegionTextStyle.Hud);
                UI.Stretch(timer.rectTransform);
                Image ready = LegionArt.Image(rect, "Badges/check", false, "Ready");
                UI.Place(ready.rectTransform, 0.67f, 0.03f, 0.93f, 0.23f);
                skillReadyIcon[i] = ready.gameObject;
                skillButtons[i] = button;
                skillCooldown[i] = cooldown;
                skillTimer[i] = timer;
                skillBadge[i] = badge.gameObject;
                skillReady[i] = true;
            }
        }

        // Arte da habilidade: as três da prancha; as demais usam a moldura dourada com um ícone do pacote.
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
                case SkillId.CollectiveShield: icon = "Icons/shield"; break;
                case SkillId.AreaHeal: icon = "Icons/potion_green"; break;
                default: icon = "Icons/star_badge"; break;
            }
            return "Hud/skill_frame";
        }

        private static Button IconButton(Transform parent, string sprite, System.Action onClick)
        {
            Button button = LegionUI.SpriteButton(parent, sprite, onClick);
            ((Image)button.targetGraphic).preserveAspect = true;
            return button;
        }

        // ---------- Ações ----------

        private void Pause()
        {
            if (level == null || level.Current != LevelManager.State.Running) return;
            level.SetPaused(true);
            if (UIRoot.Instance != null) UIRoot.Instance.Push<PauseScreen>();
        }

        private void CycleStance() => army.SetStance((FormationStance)(((int)army.Stance + 1) % 3));

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
                goldText.SetText("{0}", shownGold);
            }
            gemsText.SetText("{0}", Economy.Gems);
            levelLabel.SetText("NÍVEL {0}", level.Config.levelNumber);
            float progress = level.Progress;
            LegionUI.SetProgress(progressFill, progress);
            progressMarker.anchorMin = new Vector2(progress - 0.05f, 0.05f);
            progressMarker.anchorMax = new Vector2(progress + 0.05f, 0.95f);
            progressMarker.offsetMin = progressMarker.offsetMax = Vector2.zero;
            stanceLabel.text = "POSTURA: " + StanceLabels[(int)army.Stance];

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
            for (int i = 0; i < ArmyConfig.ClassCount; i++)
            {
                int n = army.CountOf((UnitClass)i);
                classSlots[i].SetActive(n > 0);
                if (n > 0) classCounts[i].SetText("{0}", n);
            }
            // Empilha só os retratos visíveis, sem buracos.
            int slot = 1;
            const float height = 0.07f;
            for (int i = 0; i < ArmyConfig.ClassCount; i++)
            {
                if (!classSlots[i].activeSelf) continue;
                float top = 0.78f - slot * (height + 0.008f);
                UI.Place((RectTransform)classSlots[i].transform, 0.875f, top - height, 0.99f, top);
                slot++;
            }
        }

        private void UpdateBoss()
        {
            BossController boss = level.Boss;
            bool show = boss != null && boss.IsEngaged;
            if (bossPanel.activeSelf != show) bossPanel.SetActive(show);
            if (!show) return;
            LegionUI.SetProgress(bossFill, boss.HealthRatio);
            bossValue.text = Short(boss.Health);
            string status = boss.IsVulnerable ? "VULNERÁVEL!" : boss.IsAirborne ? "EM VOO" : $"FASE {boss.Phase}";
            bossName.text = $"{boss.Definition.displayName.ToUpperInvariant()}  ·  {status}";
            bossFill.color = boss.IsVulnerable ? new Color(1f, 0.9f, 0.4f) : Color.white;
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
