using UnityEngine;

namespace LegionRush.Environments
{
    // Paleta do Reino Congelado (tirada das folhas de referência): pedra azul-acinzentada, neve branco-azulada,
    // gelo ciano, pinheiro verde-petróleo, madeira marrom, azul real e dourado dos estandartes.
    public static class EnvPalette
    {
        public static readonly Color Snow = new Color(0.94f, 0.97f, 1f);
        public static readonly Color SnowShade = new Color(0.82f, 0.89f, 0.99f);
        public static readonly Color Stone = new Color(0.6f, 0.65f, 0.76f);
        public static readonly Color StoneLight = new Color(0.7f, 0.74f, 0.84f);
        public static readonly Color StoneDark = new Color(0.43f, 0.48f, 0.6f);
        public static readonly Color Cliff = new Color(0.5f, 0.56f, 0.7f);
        public static readonly Color CliffDark = new Color(0.38f, 0.43f, 0.57f);
        public static readonly Color Ice = new Color(0.45f, 0.82f, 1f);
        public static readonly Color IceDeep = new Color(0.2f, 0.55f, 0.95f);
        public static readonly Color Pine = new Color(0.1f, 0.38f, 0.33f);
        public static readonly Color PineLight = new Color(0.16f, 0.48f, 0.38f);
        public static readonly Color Wood = new Color(0.45f, 0.28f, 0.16f);
        public static readonly Color WoodDark = new Color(0.32f, 0.2f, 0.12f);
        public static readonly Color Gold = new Color(1f, 0.76f, 0.26f);
        public static readonly Color RoyalBlue = new Color(0.12f, 0.34f, 0.92f);
        public static readonly Color RoofBlue = new Color(0.16f, 0.36f, 0.86f);
        public static readonly Color Iron = new Color(0.26f, 0.27f, 0.32f);
        public static readonly Color Fire = new Color(1f, 0.45f, 0.08f);
        public static readonly Color FireCore = new Color(1f, 0.75f, 0.25f);
        public static readonly Color WindowGlow = new Color(1f, 0.7f, 0.3f);
        public static readonly Color GateGlow = new Color(0.2f, 0.5f, 1f);

        public static Color Vary(Color c, System.Random rng, float amount)
        {
            float k = 1f + EnvMeshBuilder.Rand(rng, -amount, amount);
            return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);
        }
    }
}
