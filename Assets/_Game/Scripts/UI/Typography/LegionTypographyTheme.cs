using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LegionRush
{
    // Estilos tipográficos do jogo. Cada texto escolhe um estilo; o tema define fonte, cor e material.
    public enum LegionTextStyle
    {
        Title = 0,          // Lilita One dourada: títulos, logo, VITÓRIA/DERROTA, chefes
        PrimaryButton = 1,  // Lilita One branca: JOGAR, BATALHA, CONTINUAR
        Menu = 2,           // Fredoka Bold: menus, loja, inventário, configurações
        Body = 3,           // Fredoka Medium: descrições e mensagens
        Hud = 4,            // Baloo 2: contadores, moedas, portões, níveis
        Positive = 5,       // bônus, melhorias, recompensas
        Negative = 6,       // dano, penalidades, derrota
        Epic = 7            // gradiente dourado com brilho: recompensas raras, vitórias
    }

    public enum LegionFontRole { Title, Menu, Body, Numbers }

    [Serializable]
    public class LegionStyleSpec
    {
        public LegionTextStyle style;
        public LegionFontRole font;
        public Color color = Color.white;
        public bool useGradient;
        public Color gradientTop = Color.white;
        public Color gradientBottom = Color.white;
        [Range(0f, 0.5f)] public float outlineWidth = 0.15f;
        public Color outlineColor = new Color(0.03f, 0.05f, 0.14f, 1f);
        public bool underlay;
        public Color underlayColor = new Color(0f, 0f, 0f, 0.5f);
        public Vector2 underlayOffset = new Vector2(0f, -0.8f);
        [Range(0f, 1f)] public float underlaySoftness = 0.2f;
        [Range(-0.5f, 0.5f)] public float faceDilate;
        public float characterSpacing;
        public FontStyles fontStyle = FontStyles.Normal;
        [Tooltip("Material salvo pela ferramenta de geração. Vazio = criado em tempo de execução a partir dos valores acima.")]
        public Material materialPreset;
    }

    // Tema tipográfico (Resources/Typography/LegionTypographyTheme). Trocar o tema troca todos os textos estilizados.
    [CreateAssetMenu(menuName = "Legion Rush/Typography Theme", fileName = "LegionTypographyTheme")]
    public class LegionTypographyTheme : ScriptableObject
    {
        public const string ResourcePath = "Typography/LegionTypographyTheme";

        [Header("Font Assets (TextMeshPro SDF)")]
        public TMP_FontAsset titleFont;
        public TMP_FontAsset menuFont;
        public TMP_FontAsset bodyFont;
        public TMP_FontAsset numbersFont;

        [Header("Fontes-fonte (HUD IMGUI e geração dos SDF)")]
        public Font titleSource;
        public Font menuSource;
        public Font numbersSource;

        [Header("Estilos")]
        public List<LegionStyleSpec> styles = new List<LegionStyleSpec>();

        // Avisado quando o tema muda pelo Inspector, para reaplicar os estilos nos textos.
        public static event Action Changed;

        public TMP_FontAsset Font(LegionFontRole role)
        {
            TMP_FontAsset font = role switch
            {
                LegionFontRole.Title => titleFont,
                LegionFontRole.Body => bodyFont != null ? bodyFont : menuFont,
                LegionFontRole.Numbers => numbersFont,
                _ => menuFont
            };
            return font != null ? font : menuFont != null ? menuFont : titleFont;
        }

        public LegionStyleSpec Get(LegionTextStyle style)
        {
            foreach (LegionStyleSpec spec in styles)
            {
                if (spec.style == style) return spec;
            }
            return styles.Count > 0 ? styles[0] : null;
        }

        public static void NotifyChanged() => Changed?.Invoke();

        private void OnValidate() => NotifyChanged();

        // Valores padrão da identidade visual: fantasia cartoon, dourado, azul profundo.
        public static List<LegionStyleSpec> DefaultStyles()
        {
            var brown = new Color(0.24f, 0.11f, 0.02f, 1f);
            var navy = new Color(0.03f, 0.05f, 0.14f, 1f);
            return new List<LegionStyleSpec>
            {
                new LegionStyleSpec
                {
                    style = LegionTextStyle.Title, font = LegionFontRole.Title, color = new Color(1f, 0.8f, 0.22f),
                    useGradient = true, gradientTop = new Color(1f, 0.93f, 0.52f), gradientBottom = new Color(1f, 0.66f, 0.12f),
                    outlineWidth = 0.24f, outlineColor = brown, underlay = true, underlayColor = new Color(0f, 0f, 0f, 0.65f),
                    underlayOffset = new Vector2(0.7f, -0.9f), underlaySoftness = 0.1f, characterSpacing = 1f
                },
                new LegionStyleSpec
                {
                    style = LegionTextStyle.PrimaryButton, font = LegionFontRole.Title, color = Color.white,
                    outlineWidth = 0.2f, outlineColor = navy, underlay = true, underlayColor = new Color(0f, 0f, 0f, 0.45f),
                    underlayOffset = new Vector2(0f, -0.9f), underlaySoftness = 0.35f, characterSpacing = 1f
                },
                new LegionStyleSpec
                {
                    style = LegionTextStyle.Menu, font = LegionFontRole.Menu, color = new Color(1f, 0.98f, 0.92f),
                    outlineWidth = 0.14f, outlineColor = new Color(0.03f, 0.06f, 0.16f, 0.9f), characterSpacing = 0.5f
                },
                new LegionStyleSpec
                {
                    style = LegionTextStyle.Body, font = LegionFontRole.Body, color = new Color(0.86f, 0.9f, 1f),
                    outlineWidth = 0.06f, outlineColor = new Color(0.03f, 0.06f, 0.16f, 0.8f)
                },
                new LegionStyleSpec
                {
                    style = LegionTextStyle.Hud, font = LegionFontRole.Numbers, color = Color.white,
                    outlineWidth = 0.22f, outlineColor = navy, underlay = true, underlayColor = new Color(0f, 0f, 0f, 0.5f),
                    underlayOffset = new Vector2(0f, -0.8f), underlaySoftness = 0.25f
                },
                new LegionStyleSpec
                {
                    style = LegionTextStyle.Positive, font = LegionFontRole.Numbers, color = new Color(0.42f, 1f, 0.36f),
                    outlineWidth = 0.22f, outlineColor = new Color(0.02f, 0.2f, 0.04f, 1f), underlay = true,
                    underlayColor = new Color(0f, 0f, 0f, 0.45f), underlayOffset = new Vector2(0f, -0.8f), underlaySoftness = 0.25f
                },
                new LegionStyleSpec
                {
                    style = LegionTextStyle.Negative, font = LegionFontRole.Numbers, color = new Color(1f, 0.3f, 0.25f),
                    outlineWidth = 0.22f, outlineColor = new Color(0.25f, 0.02f, 0.02f, 1f), underlay = true,
                    underlayColor = new Color(0f, 0f, 0f, 0.45f), underlayOffset = new Vector2(0f, -0.8f), underlaySoftness = 0.25f
                },
                new LegionStyleSpec
                {
                    style = LegionTextStyle.Epic, font = LegionFontRole.Title, color = new Color(1f, 0.82f, 0.25f),
                    useGradient = true, gradientTop = new Color(1f, 0.98f, 0.66f), gradientBottom = new Color(1f, 0.58f, 0.06f),
                    outlineWidth = 0.22f, outlineColor = brown, underlay = true, underlayColor = new Color(1f, 0.72f, 0.15f, 0.55f),
                    underlayOffset = Vector2.zero, underlaySoftness = 0.65f, faceDilate = 0.05f, characterSpacing = 1f
                }
            };
        }
    }
}
