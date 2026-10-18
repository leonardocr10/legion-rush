using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LegionRush
{
    // Gerenciador central: carrega o tema, aplica estilos em TMP_Text e mantém um material compartilhado por estilo.
    public static class LegionTypography
    {
        private static LegionTypographyTheme theme;
        private static bool warned;
        private static readonly Dictionary<(LegionTextStyle, TMP_FontAsset), Material> materials =
            new Dictionary<(LegionTextStyle, TMP_FontAsset), Material>();

        static LegionTypography()
        {
            LegionTypographyTheme.Changed += ClearCache;
        }

        public static LegionTypographyTheme Theme
        {
            get
            {
                if (theme == null)
                {
                    theme = Resources.Load<LegionTypographyTheme>(LegionTypographyTheme.ResourcePath);
                    if (theme == null && !warned)
                    {
                        warned = true;
                        GameLog.Warn("Tema tipográfico ausente. Rode Legion Rush > Typography > Generate Font Assets & Theme.");
                    }
                }
                return theme;
            }
        }

        // Troca o tema em tempo de execução; todos os LegionStyledText são reaplicados.
        public static void SetTheme(LegionTypographyTheme value)
        {
            theme = value;
            LegionTypographyTheme.NotifyChanged();
        }

        public static void Apply(TMP_Text text, LegionTextStyle style, bool applyColor = true)
        {
            LegionTypographyTheme current = Theme;
            if (text == null || current == null) return;
            LegionStyleSpec spec = current.Get(style);
            TMP_FontAsset font = spec != null ? current.Font(spec.font) : null;
            if (font == null) return;

            text.font = font;
            Material material = MaterialFor(style, spec, font);
            if (material != null) text.fontSharedMaterial = material;
            text.fontStyle = spec.fontStyle;
            text.characterSpacing = spec.characterSpacing;
            if (spec.useGradient)
            {
                text.enableVertexGradient = true;
                text.colorGradient = new VertexGradient(spec.gradientTop, spec.gradientTop, spec.gradientBottom, spec.gradientBottom);
                if (applyColor) text.color = Color.white;
            }
            else
            {
                text.enableVertexGradient = false;
                if (applyColor) text.color = spec.color;
            }
        }

        public static bool HasGradient(LegionTextStyle style)
        {
            LegionStyleSpec spec = Theme != null ? Theme.Get(style) : null;
            return spec != null && spec.useGradient;
        }

        // Fonte dinâmica (Font) para o HUD desenhado com IMGUI.
        public static Font ImguiFont(LegionFontRole role)
        {
            LegionTypographyTheme current = Theme;
            if (current == null) return null;
            return role switch
            {
                LegionFontRole.Title => current.titleSource,
                LegionFontRole.Numbers => current.numbersSource,
                _ => current.menuSource
            };
        }

        public static Material MaterialFor(LegionTextStyle style)
        {
            LegionTypographyTheme current = Theme;
            LegionStyleSpec spec = current != null ? current.Get(style) : null;
            return spec != null ? MaterialFor(style, spec, current.Font(spec.font)) : null;
        }

        private static Material MaterialFor(LegionTextStyle style, LegionStyleSpec spec, TMP_FontAsset font)
        {
            if (spec.materialPreset != null && spec.materialPreset.mainTexture == font.material.mainTexture) return spec.materialPreset;
            var key = (style, font);
            if (materials.TryGetValue(key, out Material cached) && cached != null) return cached;
            var material = new Material(font.material) { name = font.name + " - " + style };
            Configure(material, spec);
            materials[key] = material;
            return material;
        }

        // Escreve os parâmetros de contorno/sombra/brilho no material SDF (usado também pela ferramenta de editor).
        public static void Configure(Material material, LegionStyleSpec spec)
        {
            material.SetFloat(ShaderUtilities.ID_FaceDilate, spec.faceDilate);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, spec.outlineWidth);
            material.SetColor(ShaderUtilities.ID_OutlineColor, spec.outlineColor);
            if (spec.outlineWidth > 0f) material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            else material.DisableKeyword(ShaderUtilities.Keyword_Outline);
            if (spec.underlay)
            {
                material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                material.SetColor(ShaderUtilities.ID_UnderlayColor, spec.underlayColor);
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, spec.underlayOffset.x);
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, spec.underlayOffset.y);
                material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, spec.underlaySoftness);
            }
            else
            {
                material.DisableKeyword(ShaderUtilities.Keyword_Underlay);
            }
        }

        private static void ClearCache()
        {
            foreach (Material material in materials.Values)
            {
                if (material == null) continue;
                if (Application.isPlaying) Object.Destroy(material);
                else Object.DestroyImmediate(material);
            }
            materials.Clear();
        }

        public static TextAlignmentOptions Alignment(TextAnchor anchor) => anchor switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.Center
        };
    }
}
