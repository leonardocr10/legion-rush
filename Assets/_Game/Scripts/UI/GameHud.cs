using UnityEngine;

namespace LegionRush
{
    // HUD IMGUI temporário (a Fase E troca por UGUI). Preparação (composição e habilidades), habilidades com recarga,
    // postura, barra do chefão, pausa com ajustes e troca de mundo/chefão para testar no celular.
    public class GameHud : MonoBehaviour
    {
        private enum PanelAction { None, Primary, Secondary }

        private static readonly AudioBus[] Buses = { AudioBus.Master, AudioBus.Music, AudioBus.Sfx, AudioBus.Ui };
        private static readonly string[] BusLabels = { "GERAL", "MÚSICA", "EFEITOS", "INTERFACE" };
        private static readonly string[] TierLabels = { "BAIXO", "MÉDIO", "ALTO" };
        private static readonly FrameRateMode[] FpsOptions = { FrameRateMode.Auto, FrameRateMode.Fps30, FrameRateMode.Fps60 };
        private static readonly string[] FpsLabels = { "AUTO", "30", "60" };
        private static readonly string[] StanceLabels = { "EQUILIBRADA", "OFENSIVA", "DEFENSIVA" };
        private static readonly string[] ClassShort = { "ARQ", "GUE", "LAN", "MAG", "CUR", "CAV" };
        private static readonly Color SelectedTint = new Color(1f, 0.78f, 0.25f);
        private static readonly Color MutedTint = new Color(1f, 0.4f, 0.35f);
        private static readonly Color PanelColor = new Color(0.04f, 0.06f, 0.12f, 0.9f);

        private LevelManager level;
        private ArmyManager army;
        private GameBootstrap bootstrap;
        private VisualQualityManager quality;
        private SkillSystem skills;
        private GUIStyle leftStyle;
        private GUIStyle rightStyle;
        private GUIStyle centerStyle;
        private GUIStyle smallStyle;
        private GUIStyle hugeStyle;
        private GUIStyle toastStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallButtonStyle;
        private GUIStyle skillStyle;
        private GUIStyle sliderStyle;
        private GUIStyle thumbStyle;
        private Texture2D pixel;
        private float builtScale = -1f;
        private int shownGold = int.MinValue;
        private int shownSoldiers = int.MinValue;
        private float classTextTimer;
        private string goldText = string.Empty;
        private string soldierText = string.Empty;
        private string classText = string.Empty;
        private string levelText = string.Empty;
        private string worldButtonText = string.Empty;
        private string resultText = string.Empty;
        private string toastText = string.Empty;
        private float toastTimer;
        private bool resultSettingsOpen;

        public void Initialize(LevelManager levelManager, ArmyManager armyManager, GameBootstrap owner,
            VisualQualityManager qualityManager, SkillSystem skillSystem)
        {
            level = levelManager;
            army = armyManager;
            bootstrap = owner;
            quality = qualityManager;
            skills = skillSystem;
            string worldName = bootstrap.CurrentWorldName.ToUpperInvariant();
            levelText = $"FASE {level.Config.levelNumber}  ·  {worldName}";
            worldButtonText = $"MUNDO: {worldName}   >";
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            level.StateChanged += HandleStateChanged;
            level.BossAwakened += HandleBossAwakened;
            level.BossDefeated += HandleBossDefeated;
            skills.SkillFailed += HandleSkillFailed;
            level.GateApplied += HandleGateApplied;
            if (level.Boss != null) level.Boss.PhaseChanged += HandleBossPhase;
        }

        private void OnDestroy()
        {
            if (level != null)
            {
                level.StateChanged -= HandleStateChanged;
                level.BossAwakened -= HandleBossAwakened;
                level.BossDefeated -= HandleBossDefeated;
                level.GateApplied -= HandleGateApplied;
                if (level.Boss != null) level.Boss.PhaseChanged -= HandleBossPhase;
            }
            if (skills != null) skills.SkillFailed -= HandleSkillFailed;
            if (pixel != null) Destroy(pixel);
        }

        private void HandleStateChanged(LevelManager.State state)
        {
            resultSettingsOpen = false;
            if (state == LevelManager.State.Victory || state == LevelManager.State.Defeat)
            {
                LevelOutcome outcome = level.LastOutcome;
                string stars = state == LevelManager.State.Victory ? $"ESTRELAS {outcome.Stars}/3   ·   " : string.Empty;
                string best = outcome.NewBestScore ? "  (RECORDE!)" : string.Empty;
                resultText = $"{stars}Pontuação {outcome.Score}{best}\nSoldados {army.Count}   ·   Inimigos {level.Kills}\n" +
                             $"+{level.Gold} ouro   ·   +{level.Gems} gemas\nCarteira: {Economy.Gold} ouro   ·   {Economy.Gems} gemas";
            }
        }

        private void HandleBossAwakened(BossController boss) => Toast($"{boss.Definition.displayName.ToUpperInvariant()} DESPERTOU!");
        private void HandleBossDefeated(BossController boss) => Toast($"{boss.Definition.displayName.ToUpperInvariant()} DERROTADO!");
        private void HandleBossPhase(BossController boss, int phase) => Toast($"FASE {phase} DO CHEFÃO!");
        private void HandleSkillFailed(SkillDefinition skill) => Toast("Sem alvo no alcance");
        private void HandleGateApplied(GateOption option, int before, int after) => Toast($"{option.title} {option.subtitle}");

        private void Toast(string text)
        {
            toastText = text;
            toastTimer = 2.2f;
        }

        private void OnGUI()
        {
            if (level == null || army == null || pixel == null) return;
            float scale = Mathf.Max(0.35f, Mathf.Min(Screen.width / 1080f, Screen.height / 1920f));
            if (!Mathf.Approximately(scale, builtScale)) BuildStyles(scale);
            RefreshTexts();

            Rect sa = Screen.safeArea;
            Rect safe = new Rect(sa.x, Screen.height - sa.yMax, sa.width, sa.height);
            float pad = 24f * scale;
            Rect bar = DrawTopBar(safe, scale, pad);

            switch (level.Current)
            {
                case LevelManager.State.Ready:
                    DrawPreparation(safe, scale, pad);
                    break;
                case LevelManager.State.Running:
                {
                    if (!level.IsPaused)
                    {
                        DrawRunning(safe, bar, scale, pad);
                        break;
                    }
                    PanelAction action = DrawSettingsPanel("PAUSA", "CONTINUAR", "REINICIAR", safe, scale, pad);
                    if (action == PanelAction.Primary)
                    {
                        GameAudio.Play(SoundId.UiBack);
                        level.SetPaused(false);
                    }
                    else if (action == PanelAction.Secondary)
                    {
                        GameAudio.Play(SoundId.UiConfirm);
                        bootstrap.Restart();
                    }
                    break;
                }
                case LevelManager.State.Victory:
                case LevelManager.State.Defeat:
                {
                    bool victory = level.Current == LevelManager.State.Victory;
                    if (resultSettingsOpen)
                    {
                        if (DrawSettingsPanel("AJUSTES", "FECHAR", null, safe, scale, pad) == PanelAction.Primary)
                        {
                            GameAudio.Play(SoundId.UiBack);
                            resultSettingsOpen = false;
                        }
                        break;
                    }
                    PanelAction action = DrawResult(victory ? "VITÓRIA!" : "DERROTA",
                        victory ? new Color(1f, 0.82f, 0.3f) : new Color(1f, 0.35f, 0.3f),
                        victory ? "Jogar novamente" : "Tentar novamente", safe, scale, pad);
                    if (action == PanelAction.Primary)
                    {
                        GameAudio.Play(SoundId.UiConfirm);
                        bootstrap.Restart();
                    }
                    else if (action == PanelAction.Secondary)
                    {
                        GameAudio.Play(SoundId.UiClick);
                        resultSettingsOpen = true;
                    }
                    break;
                }
            }

            if (toastTimer > 0f)
            {
                if (Event.current.type == EventType.Repaint) toastTimer -= Time.unscaledDeltaTime;
                Shadowed(new Rect(safe.x + pad, safe.y + safe.height * 0.3f, safe.width - pad * 2f, 140f * scale), toastText, toastStyle, scale);
            }
        }

        // ---------- Preparação ----------

        private void DrawPreparation(Rect safe, float scale, float pad)
        {
            float width = Mathf.Min(safe.width - pad * 2f, 1000f * scale);
            float rowHeight = 110f * scale;
            float height = 120f * scale + rowHeight * 5f + 150f * scale;
            Rect panel = new Rect(safe.x + (safe.width - width) * 0.5f, safe.yMax - height - pad * 2f, width, height);
            Fill(panel, PanelColor);
            Shadowed(new Rect(panel.x, panel.y + 12f * scale, panel.width, 90f * scale), "PREPARAR TROPAS", hugeStyle, scale);

            float x = panel.x + pad;
            float inner = panel.width - pad * 2f;
            float y = panel.y + 120f * scale;

            GUI.Label(new Rect(x, y, inner, rowHeight * 0.45f), "COMPOSIÇÃO", leftStyle);
            int count = bootstrap.CompositionCount;
            float cell = inner / Mathf.Max(1, count);
            Color previous = GUI.backgroundColor;
            for (int i = 0; i < count; i++)
            {
                GUI.backgroundColor = i == bootstrap.CompositionIndex ? SelectedTint : previous;
                Rect r = new Rect(x + cell * i + 4f, y + rowHeight * 0.45f, cell - 8f, rowHeight * 0.6f);
                if (GUI.Button(r, bootstrap.CompositionName(i).ToUpperInvariant(), smallButtonStyle) && i != bootstrap.CompositionIndex)
                {
                    bootstrap.SetComposition(i);
                    GameAudio.Play(SoundId.UiClick);
                    classTextTimer = 0f;
                }
            }
            GUI.backgroundColor = previous;
            y += rowHeight * 1.15f;
            GUI.Label(new Rect(x, y, inner, rowHeight * 0.5f), classText, smallStyle);
            y += rowHeight * 0.6f;

            GUI.Label(new Rect(x, y, inner, rowHeight * 0.45f), "HABILIDADES (toque para trocar)", leftStyle);
            cell = inner / SkillSystem.SlotCount;
            for (int i = 0; i < SkillSystem.SlotCount; i++)
            {
                SkillDefinition skill = skills.Definition(bootstrap.LoadoutSlot(i));
                GUI.backgroundColor = skill.color;
                Rect r = new Rect(x + cell * i + 4f, y + rowHeight * 0.45f, cell - 8f, rowHeight * 0.75f);
                if (GUI.Button(r, skill.shortLabel, smallButtonStyle))
                {
                    bootstrap.CycleSkillSlot(i);
                    GameAudio.Play(SoundId.UiClick);
                    Toast(skills.Definition(bootstrap.LoadoutSlot(i)).displayName);
                }
            }
            GUI.backgroundColor = previous;
            y += rowHeight * 1.35f;

            GUI.backgroundColor = SelectedTint;
            Rect play = new Rect(panel.x + panel.width * 0.2f, y, panel.width * 0.6f, 130f * scale);
            if (GUI.Button(play, "JOGAR", buttonStyle)) level.StartRun();
            GUI.backgroundColor = previous;

            Shadowed(new Rect(safe.x, panel.y - 90f * scale, safe.width, 70f * scale), "Arraste para mover o exército", centerStyle, scale);
        }

        // ---------- Partida ----------

        private void DrawRunning(Rect safe, Rect bar, float scale, float pad)
        {
            float size = 110f * scale;
            Rect pauseRect = new Rect(bar.xMax - size, bar.yMax + pad * 0.5f, size, size);
            if (GUI.Button(pauseRect, "II", buttonStyle))
            {
                GameAudio.Play(SoundId.UiClick);
                level.SetPaused(true);
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
            float p2 = boss.Definition.phase2At;
            float p3 = boss.Definition.phase3At;
            Fill(new Rect(hpBg.x + hpBg.width * p2 - 2f, hpBg.y, 4f, hpBg.height), Color.white);
            Fill(new Rect(hpBg.x + hpBg.width * p3 - 2f, hpBg.y, 4f, hpBg.height), Color.white);
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
                if (GUI.Button(r, label, skillStyle) && ready) skills.TryUse(i);
                if (!ready)
                {
                    float ratio = skills.CooldownRatio(i);
                    Fill(new Rect(r.x, r.y, r.width, r.height * ratio), new Color(0f, 0f, 0f, 0.45f));
                }
            }
            GUI.backgroundColor = previous;
        }

        // ---------- Resultado e ajustes ----------

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

        private PanelAction DrawResult(string title, Color titleColor, string buttonLabel, Rect safe, float scale, float pad)
        {
            float width = Mathf.Min(safe.width - pad * 2f, 960f * scale);
            float height = 800f * scale;
            Rect panel = new Rect(safe.x + (safe.width - width) * 0.5f, safe.y + (safe.height - height) * 0.5f, width, height);
            Fill(panel, new Color(0.04f, 0.06f, 0.12f, 0.88f));

            Color previous = hugeStyle.normal.textColor;
            hugeStyle.normal.textColor = titleColor;
            Shadowed(new Rect(panel.x, panel.y + 40f * scale, panel.width, 140f * scale), title, hugeStyle, scale);
            hugeStyle.normal.textColor = previous;

            GUI.Label(new Rect(panel.x + pad, panel.y + 190f * scale, panel.width - pad * 2f, 260f * scale), resultText, centerStyle);
            Rect primary = new Rect(panel.x + panel.width * 0.15f, panel.yMax - 310f * scale, panel.width * 0.7f, 120f * scale);
            Rect secondary = new Rect(panel.x + panel.width * 0.25f, panel.yMax - 160f * scale, panel.width * 0.5f, 100f * scale);
            if (GUI.Button(primary, buttonLabel, buttonStyle)) return PanelAction.Primary;
            if (GUI.Button(secondary, "AJUSTES", smallButtonStyle)) return PanelAction.Secondary;
            return PanelAction.None;
        }

        private PanelAction DrawSettingsPanel(string title, string primaryLabel, string secondaryLabel, Rect safe, float scale, float pad)
        {
            float rowHeight = 104f * scale;
            float optionHeight = 96f * scale;
            float buttonHeight = 104f * scale;
            int buttons = secondaryLabel != null ? 2 : 1;
            float height = 120f * scale + rowHeight * Buses.Length + optionHeight * 5f + (buttonHeight + 16f * scale) * buttons + pad * 2f;
            float width = Mathf.Min(safe.width - pad * 2f, 1000f * scale);
            Rect panel = new Rect(safe.x + (safe.width - width) * 0.5f, safe.y + Mathf.Max(pad, (safe.height - height) * 0.5f), width, height);
            Fill(panel, new Color(0.04f, 0.06f, 0.12f, 0.92f));
            Shadowed(new Rect(panel.x, panel.y + 14f * scale, panel.width, 96f * scale), title, hugeStyle, scale);

            float inner = panel.width - pad * 2f;
            float x = panel.x + pad;
            float y = panel.y + 120f * scale;
            AudioManager audio = AudioManager.Instance;

            for (int i = 0; i < Buses.Length; i++)
            {
                AudioBus bus = Buses[i];
                float volume = audio != null ? audio.GetVolume(bus) : 0f;
                bool muted = audio != null && audio.IsMuted(bus);
                GUI.Label(new Rect(x, y, inner * 0.7f, rowHeight * 0.45f), BusLabels[i] + "  " + Mathf.RoundToInt(volume * 100f) + "%", leftStyle);
                Rect sliderRect = new Rect(x, y + rowHeight * 0.45f, inner * 0.7f, rowHeight * 0.45f);
                float next = GUI.HorizontalSlider(sliderRect, volume, 0f, 1f, sliderStyle, thumbStyle);
                if (audio != null && Mathf.Abs(next - volume) > 0.001f) audio.SetVolume(bus, next);

                Rect muteRect = new Rect(x + inner * 0.74f, y + rowHeight * 0.14f, inner * 0.26f, rowHeight * 0.72f);
                Color previousBackground = GUI.backgroundColor;
                if (muted) GUI.backgroundColor = MutedTint;
                if (GUI.Button(muteRect, muted ? "MUDO" : "SOM", smallButtonStyle) && audio != null)
                {
                    audio.SetMuted(bus, !muted);
                    GameAudio.Play(SoundId.UiClick);
                }
                GUI.backgroundColor = previousBackground;
                y += rowHeight;
            }

            int tierPick = DrawOptionRow("GRÁFICOS", TierLabels, quality != null ? (int)quality.Current : -1, x, y, inner, optionHeight);
            if (tierPick >= 0 && quality != null && tierPick != (int)quality.Current)
            {
                quality.SetTier((QualityTier)tierPick, true);
                GameAudio.Play(SoundId.UiClick);
            }
            y += optionHeight;

            int fpsSelected = -1;
            if (quality != null)
            {
                for (int i = 0; i < FpsOptions.Length; i++)
                {
                    if (FpsOptions[i] == quality.FrameRate) fpsSelected = i;
                }
            }
            int fpsPick = DrawOptionRow("FPS", FpsLabels, fpsSelected, x, y, inner, optionHeight);
            if (fpsPick >= 0 && quality != null && fpsPick != fpsSelected)
            {
                quality.SetFrameRate(FpsOptions[fpsPick]);
                GameAudio.Play(SoundId.UiClick);
            }
            y += optionHeight;

            if (GUI.Button(new Rect(x, y + optionHeight * 0.1f, inner, optionHeight * 0.8f), worldButtonText, smallButtonStyle))
            {
                GameAudio.Play(SoundId.UiConfirm);
                bootstrap.CycleWorld();
                return PanelAction.None;
            }
            y += optionHeight;

            if (GUI.Button(new Rect(x, y + optionHeight * 0.1f, inner, optionHeight * 0.8f), "CHEFÃO: " + bootstrap.BossOverrideName + "   >", smallButtonStyle))
            {
                GameAudio.Play(SoundId.UiConfirm);
                bootstrap.CycleBossOverride();
                return PanelAction.None;
            }
            y += optionHeight;

            GUI.Label(new Rect(x, y, inner * 0.3f, optionHeight), "PROGRESSO", leftStyle);
            float half = inner * 0.35f;
            if (GUI.Button(new Rect(x + inner * 0.3f + 4f, y + optionHeight * 0.1f, half - 8f, optionHeight * 0.8f), "EXPORTAR", smallButtonStyle))
            {
                GUIUtility.systemCopyBuffer = SaveManager.Export();
                GameAudio.Play(SoundId.UiConfirm);
                Toast("Código do progresso copiado");
            }
            if (GUI.Button(new Rect(x + inner * 0.65f + 4f, y + optionHeight * 0.1f, half - 8f, optionHeight * 0.8f), "IMPORTAR", smallButtonStyle))
            {
                bool imported = SaveManager.Import(GUIUtility.systemCopyBuffer, out string message);
                GameAudio.Play(imported ? SoundId.UiConfirm : SoundId.UiBack);
                Toast(message);
            }
            y += optionHeight + pad;

            Rect primaryRect = new Rect(panel.x + panel.width * 0.15f, y, panel.width * 0.7f, buttonHeight);
            if (GUI.Button(primaryRect, primaryLabel, buttonStyle)) return PanelAction.Primary;
            if (secondaryLabel != null)
            {
                Rect secondaryRect = new Rect(primaryRect.x, primaryRect.yMax + 16f * scale, primaryRect.width, buttonHeight);
                if (GUI.Button(secondaryRect, secondaryLabel, buttonStyle)) return PanelAction.Secondary;
            }
            return PanelAction.None;
        }

        private int DrawOptionRow(string label, string[] options, int selected, float x, float y, float width, float height)
        {
            GUI.Label(new Rect(x, y, width * 0.3f, height), label, leftStyle);
            float buttonsX = x + width * 0.3f;
            float buttonWidth = width * 0.7f / options.Length;
            int picked = -1;
            Color previous = GUI.backgroundColor;
            for (int i = 0; i < options.Length; i++)
            {
                GUI.backgroundColor = i == selected ? SelectedTint : previous;
                Rect rect = new Rect(buttonsX + buttonWidth * i + 4f, y + height * 0.1f, buttonWidth - 8f, height * 0.8f);
                if (GUI.Button(rect, options[i], smallButtonStyle)) picked = i;
            }
            GUI.backgroundColor = previous;
            return picked;
        }

        private void RefreshTexts()
        {
            if (level.Gold != shownGold)
            {
                shownGold = level.Gold;
                goldText = $"OURO  {shownGold}";
            }
            classTextTimer -= Time.unscaledDeltaTime;
            if (army.Count != shownSoldiers || classTextTimer <= 0f)
            {
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
            centerStyle = new GUIStyle(leftStyle) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontSize = Mathf.RoundToInt(38f * scale) };
            hugeStyle = new GUIStyle(centerStyle) { fontSize = Mathf.RoundToInt(72f * scale), wordWrap = false };
            toastStyle = new GUIStyle(centerStyle) { fontSize = Mathf.RoundToInt(54f * scale), wordWrap = true };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(44f * scale), fontStyle = FontStyle.Bold };
            smallButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(28f * scale), fontStyle = FontStyle.Bold, wordWrap = true };
            skillStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(26f * scale), fontStyle = FontStyle.Bold, wordWrap = true };
            sliderStyle = new GUIStyle(GUI.skin.horizontalSlider) { fixedHeight = 0f };
            thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb) { fixedWidth = 50f * scale, fixedHeight = 50f * scale };
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
