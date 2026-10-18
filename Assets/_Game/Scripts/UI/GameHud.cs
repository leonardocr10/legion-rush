using UnityEngine;

namespace LegionRush
{
    // HUD IMGUI temporário (Etapa 9 troca por UI final). Inclui pausa com áudio, gráficos, FPS e troca de mundo
    // para testar tudo direto no celular.
    public class GameHud : MonoBehaviour
    {
        private enum PanelAction { None, Primary, Secondary }

        private static readonly AudioBus[] Buses = { AudioBus.Master, AudioBus.Music, AudioBus.Sfx, AudioBus.Ui };
        private static readonly string[] BusLabels = { "GERAL", "MÚSICA", "EFEITOS", "INTERFACE" };
        private static readonly string[] TierLabels = { "BAIXO", "MÉDIO", "ALTO" };
        private static readonly FrameRateMode[] FpsOptions = { FrameRateMode.Auto, FrameRateMode.Fps30, FrameRateMode.Fps60 };
        private static readonly string[] FpsLabels = { "AUTO", "30", "60" };
        private static readonly Color SelectedTint = new Color(1f, 0.78f, 0.25f);
        private static readonly Color MutedTint = new Color(1f, 0.4f, 0.35f);

        private LevelManager level;
        private ArmyManager army;
        private GameBootstrap bootstrap;
        private VisualQualityManager quality;
        private GUIStyle leftStyle;
        private GUIStyle rightStyle;
        private GUIStyle centerStyle;
        private GUIStyle hugeStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallButtonStyle;
        private GUIStyle sliderStyle;
        private GUIStyle thumbStyle;
        private Texture2D pixel;
        private float builtScale = -1f;
        private int shownGold = int.MinValue;
        private int shownSoldiers = int.MinValue;
        private string goldText = string.Empty;
        private string soldierText = string.Empty;
        private string levelText = string.Empty;
        private string worldButtonText = string.Empty;
        private string resultText = string.Empty;
        private bool resultSettingsOpen;

        public void Initialize(LevelManager levelManager, ArmyManager armyManager, GameBootstrap owner, VisualQualityManager qualityManager)
        {
            level = levelManager;
            army = armyManager;
            bootstrap = owner;
            quality = qualityManager;
            string worldName = bootstrap.CurrentWorldName.ToUpperInvariant();
            levelText = $"FASE {level.Config.levelNumber}  ·  {worldName}";
            worldButtonText = $"MUNDO: {worldName}   >";
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            level.StateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (level != null) level.StateChanged -= HandleStateChanged;
            if (pixel != null) Destroy(pixel);
        }

        private void HandleStateChanged(LevelManager.State state)
        {
            resultSettingsOpen = false;
            if (state == LevelManager.State.Victory || state == LevelManager.State.Defeat)
                resultText = $"Soldados: {army.Count}   ·   Inimigos derrotados: {level.Kills}   ·   Ouro: {level.Gold}";
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
                {
                    float y = safe.y + safe.height * 0.62f;
                    Shadowed(new Rect(safe.x, y, safe.width, 110f * scale), "ARRASTE PARA MOVER", hugeStyle, scale);
                    Shadowed(new Rect(safe.x, y + 110f * scale, safe.width, 70f * scale), "Toque para começar", centerStyle, scale);
                    break;
                }
                case LevelManager.State.Running:
                {
                    if (!level.IsPaused)
                    {
                        float size = 120f * scale;
                        Rect pauseRect = new Rect(bar.xMax - size, bar.yMax + pad, size, size);
                        if (GUI.Button(pauseRect, "II", buttonStyle))
                        {
                            GameAudio.Play(SoundId.UiClick);
                            level.SetPaused(true);
                        }
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

        private PanelAction DrawResult(string title, Color titleColor, string buttonLabel, Rect safe, float scale, float pad)
        {
            float width = Mathf.Min(safe.width - pad * 2f, 960f * scale);
            float height = 700f * scale;
            Rect panel = new Rect(safe.x + (safe.width - width) * 0.5f, safe.y + (safe.height - height) * 0.5f, width, height);
            Fill(panel, new Color(0.04f, 0.06f, 0.12f, 0.88f));

            Color previous = hugeStyle.normal.textColor;
            hugeStyle.normal.textColor = titleColor;
            Shadowed(new Rect(panel.x, panel.y + 40f * scale, panel.width, 140f * scale), title, hugeStyle, scale);
            hugeStyle.normal.textColor = previous;

            GUI.Label(new Rect(panel.x + pad, panel.y + 200f * scale, panel.width - pad * 2f, 140f * scale), resultText, centerStyle);
            Rect primary = new Rect(panel.x + panel.width * 0.15f, panel.yMax - 310f * scale, panel.width * 0.7f, 120f * scale);
            Rect secondary = new Rect(panel.x + panel.width * 0.25f, panel.yMax - 160f * scale, panel.width * 0.5f, 100f * scale);
            if (GUI.Button(primary, buttonLabel, buttonStyle)) return PanelAction.Primary;
            if (GUI.Button(secondary, "AJUSTES", smallButtonStyle)) return PanelAction.Secondary;
            return PanelAction.None;
        }

        private PanelAction DrawSettingsPanel(string title, string primaryLabel, string secondaryLabel, Rect safe, float scale, float pad)
        {
            float rowHeight = 112f * scale;
            float optionHeight = 104f * scale;
            float buttonHeight = 110f * scale;
            int buttons = secondaryLabel != null ? 2 : 1;
            float height = 130f * scale + rowHeight * Buses.Length + optionHeight * 3f + (buttonHeight + 18f * scale) * buttons + pad * 2f;
            float width = Mathf.Min(safe.width - pad * 2f, 1000f * scale);
            Rect panel = new Rect(safe.x + (safe.width - width) * 0.5f, safe.y + Mathf.Max(pad, (safe.height - height) * 0.5f), width, height);
            Fill(panel, new Color(0.04f, 0.06f, 0.12f, 0.92f));
            Shadowed(new Rect(panel.x, panel.y + 18f * scale, panel.width, 100f * scale), title, hugeStyle, scale);

            float inner = panel.width - pad * 2f;
            float x = panel.x + pad;
            float y = panel.y + 130f * scale;
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
            y += optionHeight + pad;

            Rect primaryRect = new Rect(panel.x + panel.width * 0.15f, y, panel.width * 0.7f, buttonHeight);
            if (GUI.Button(primaryRect, primaryLabel, buttonStyle)) return PanelAction.Primary;
            if (secondaryLabel != null)
            {
                Rect secondaryRect = new Rect(primaryRect.x, primaryRect.yMax + 18f * scale, primaryRect.width, buttonHeight);
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
            if (army.Count != shownSoldiers)
            {
                shownSoldiers = army.Count;
                soldierText = $"SOLDADOS  {shownSoldiers}";
            }
        }

        private void BuildStyles(float scale)
        {
            builtScale = scale;
            leftStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(36f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow
            };
            leftStyle.normal.textColor = Color.white;
            rightStyle = new GUIStyle(leftStyle) { alignment = TextAnchor.MiddleRight };
            centerStyle = new GUIStyle(leftStyle) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontSize = Mathf.RoundToInt(40f * scale) };
            hugeStyle = new GUIStyle(centerStyle) { fontSize = Mathf.RoundToInt(80f * scale), wordWrap = false };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(44f * scale), fontStyle = FontStyle.Bold };
            smallButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(32f * scale), fontStyle = FontStyle.Bold };
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
