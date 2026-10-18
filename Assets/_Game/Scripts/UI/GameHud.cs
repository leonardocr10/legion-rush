using UnityEngine;

namespace LegionRush
{
    // HUD da partida (desenho leve por IMGUI): barra superior, habilidades com recarga, postura, barra do chefão e pausa.
    // Menus, preparação, pausa e resultados são telas UGUI (UIRoot).
    public class GameHud : MonoBehaviour
    {
        private static readonly string[] StanceLabels = { "EQUILIBRADA", "OFENSIVA", "DEFENSIVA" };
        private static readonly string[] ClassShort = { "ARQ", "GUE", "LAN", "MAG", "CUR", "CAV" };

        private LevelManager level;
        private ArmyManager army;
        private GameBootstrap bootstrap;
        private SkillSystem skills;
        private GUIStyle leftStyle;
        private GUIStyle rightStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallButtonStyle;
        private GUIStyle skillStyle;
        private Texture2D pixel;
        private float builtScale = -1f;
        private int shownGold = int.MinValue;
        private int shownSoldiers = int.MinValue;
        private float classTextTimer;
        private string goldText = string.Empty;
        private string soldierText = string.Empty;
        private string classText = string.Empty;
        private string levelText = string.Empty;

        public void Initialize(LevelManager levelManager, ArmyManager armyManager, GameBootstrap owner, SkillSystem skillSystem)
        {
            level = levelManager;
            army = armyManager;
            bootstrap = owner;
            skills = skillSystem;
            levelText = $"FASE {level.Config.levelNumber}  ·  {bootstrap.CurrentWorldName.ToUpperInvariant()}";
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            level.BossAwakened += HandleBossAwakened;
            level.BossDefeated += HandleBossDefeated;
            level.GateApplied += HandleGateApplied;
            skills.SkillFailed += HandleSkillFailed;
            if (level.Boss != null) level.Boss.PhaseChanged += HandleBossPhase;
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
            if (pixel != null) Destroy(pixel);
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

        private void OnGUI()
        {
            if (level == null || army == null || pixel == null) return;
            if (level.Current != LevelManager.State.Running || level.IsPaused) return;
            if (UIRoot.Instance != null && UIRoot.Instance.IsBlocking) return;

            float scale = Mathf.Max(0.35f, Mathf.Min(Screen.width / 1080f, Screen.height / 1920f));
            if (!Mathf.Approximately(scale, builtScale)) BuildStyles(scale);
            RefreshTexts();

            Rect sa = Screen.safeArea;
            Rect safe = new Rect(sa.x, Screen.height - sa.yMax, sa.width, sa.height);
            float pad = 24f * scale;
            Rect bar = DrawTopBar(safe, scale, pad);

            float size = 110f * scale;
            Rect pauseRect = new Rect(bar.xMax - size, bar.yMax + pad * 0.5f, size, size);
            if (GUI.Button(pauseRect, "II", buttonStyle))
            {
                GameAudio.Play(SoundId.UiClick);
                level.SetPaused(true);
                if (UIRoot.Instance != null) UIRoot.Instance.Push<PauseScreen>();
                return;
            }

            Rect stanceRect = new Rect(bar.x, bar.yMax + pad * 0.5f, bar.width * 0.55f, size * 0.7f);
            if (GUI.Button(stanceRect, "POSTURA: " + StanceLabels[(int)army.Stance], smallButtonStyle))
            {
                army.SetStance((FormationStance)(((int)army.Stance + 1) % 3));
                GameAudio.Play(SoundId.UiClick);
            }
            GUI.Label(new Rect(bar.x, stanceRect.yMax + 4f, bar.width, 50f * scale), classText, smallStyle);

            DrawBossBar(bar, scale, pad, stanceRect.yMax + 60f * scale);
            DrawSkills(safe, scale, pad);
        }

        private Rect DrawTopBar(Rect safe, float scale, float pad)
        {
            Rect bar = new Rect(safe.x + pad, safe.y + pad, safe.width - pad * 2f, 150f * scale);
            Fill(bar, new Color(0.05f, 0.08f, 0.15f, 0.6f));
            float lineHeight = 60f * scale;
            Rect line1 = new Rect(bar.x + pad, bar.y + 12f * scale, bar.width - pad * 2f, lineHeight);
            Shadowed(line1, levelText, leftStyle, scale);
            Shadowed(line1, goldText, rightStyle, scale);
            Rect line2 = new Rect(line1.x, line1.yMax, line1.width, lineHeight);
            Shadowed(line2, soldierText, leftStyle, scale);

            Rect progressBg = new Rect(line2.x + line2.width * 0.45f, line2.y + lineHeight * 0.3f, line2.width * 0.55f, lineHeight * 0.4f);
            Fill(progressBg, new Color(1f, 1f, 1f, 0.15f));
            Fill(new Rect(progressBg.x, progressBg.y, progressBg.width * level.Progress, progressBg.height), new Color(1f, 0.78f, 0.25f, 0.95f));
            return bar;
        }

        private void DrawBossBar(Rect bar, float scale, float pad, float top)
        {
            BossController boss = level.Boss;
            if (boss == null || !boss.IsEngaged) return;
            Rect panel = new Rect(bar.x, top, bar.width, 120f * scale);
            Fill(panel, new Color(0.12f, 0.02f, 0.04f, 0.75f));
            string title = $"{boss.Definition.displayName.ToUpperInvariant()}  ·  FASE {boss.Phase}";
            Shadowed(new Rect(panel.x + pad, panel.y + 4f, panel.width - pad * 2f, 50f * scale), title, leftStyle, scale);
            string status = boss.IsVulnerable ? "VULNERÁVEL!" : boss.IsAirborne ? "EM VOO" : boss.CurrentAttack;
            if (!string.IsNullOrEmpty(status)) Shadowed(new Rect(panel.x + pad, panel.y + 4f, panel.width - pad * 2f, 50f * scale), status, rightStyle, scale);

            Rect hpBg = new Rect(panel.x + pad, panel.y + 62f * scale, panel.width - pad * 2f, 38f * scale);
            Fill(hpBg, new Color(0f, 0f, 0f, 0.5f));
            Color hp = boss.IsVulnerable ? new Color(1f, 0.85f, 0.25f) : new Color(0.9f, 0.15f, 0.2f);
            Fill(new Rect(hpBg.x, hpBg.y, hpBg.width * boss.HealthRatio, hpBg.height), hp);
            Fill(new Rect(hpBg.x + hpBg.width * boss.Definition.phase2At - 2f, hpBg.y, 4f, hpBg.height), Color.white);
            Fill(new Rect(hpBg.x + hpBg.width * boss.Definition.phase3At - 2f, hpBg.y, 4f, hpBg.height), Color.white);
        }

        private void DrawSkills(Rect safe, float scale, float pad)
        {
            float size = Mathf.Min(190f * scale, (safe.width - pad * 2f) / SkillSystem.SlotCount - 12f);
            float total = size * SkillSystem.SlotCount + 16f * (SkillSystem.SlotCount - 1);
            float x = safe.x + (safe.width - total) * 0.5f;
            float y = safe.yMax - size - pad * 1.5f;
            Color previous = GUI.backgroundColor;
            for (int i = 0; i < SkillSystem.SlotCount; i++)
            {
                SkillDefinition skill = skills.Slot(i);
                if (skill == null) continue;
                Rect r = new Rect(x + i * (size + 16f), y, size, size);
                bool ready = skills.IsReady(i);
                GUI.backgroundColor = ready ? skill.color : new Color(0.35f, 0.35f, 0.4f);
                string label = ready ? skill.shortLabel : $"{skill.shortLabel}\n{Mathf.CeilToInt(skills.CooldownRemaining(i))}s";
                if (GUI.Button(r, label, skillStyle) && ready && skills.TryUse(i)) Haptics.Tap();
                if (!ready) Fill(new Rect(r.x, r.y, r.width, r.height * skills.CooldownRatio(i)), new Color(0f, 0f, 0f, 0.45f));
            }
            GUI.backgroundColor = previous;
        }

        private void RefreshTexts()
        {
            if (level.Gold != shownGold)
            {
                shownGold = level.Gold;
                goldText = $"OURO  {shownGold}";
            }
            classTextTimer -= Time.unscaledDeltaTime;
            if (army.Count == shownSoldiers && classTextTimer > 0f) return;
            shownSoldiers = army.Count;
            soldierText = $"SOLDADOS  {shownSoldiers}";
            var builder = new System.Text.StringBuilder(64);
            for (int i = 0; i < ArmyConfig.ClassCount; i++)
            {
                int n = army.CountOf((UnitClass)i);
                if (n == 0) continue;
                if (builder.Length > 0) builder.Append("  ·  ");
                builder.Append(ClassShort[i]).Append(' ').Append(n);
            }
            classText = builder.ToString();
            classTextTimer = 0.5f;
        }

        private void BuildStyles(float scale)
        {
            builtScale = scale;
            leftStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(34f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow
            };
            leftStyle.normal.textColor = Color.white;
            rightStyle = new GUIStyle(leftStyle) { alignment = TextAnchor.MiddleRight };
            smallStyle = new GUIStyle(leftStyle) { fontSize = Mathf.RoundToInt(28f * scale) };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(44f * scale), fontStyle = FontStyle.Bold };
            smallButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(28f * scale), fontStyle = FontStyle.Bold, wordWrap = true };
            skillStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(26f * scale), fontStyle = FontStyle.Bold, wordWrap = true };
        }

        private void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = previous;
        }

        private static void Shadowed(Rect rect, string text, GUIStyle style, float scale)
        {
            float offset = 3f * scale;
            Color color = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
            GUI.Label(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), text, style);
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }
    }
}
