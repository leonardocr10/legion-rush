using System;
using System.Collections.Generic;
using UnityEngine;

namespace LegionRush.Environments
{
    public enum EnvScatterKind { Pine, PineHeavySnow, SnowBush, SnowDrift, RockCluster, RockLarge, IceCrystals, IceBlock, Lantern, FrozenTree, SmallPlant }

    // Uma regra de distribuição: que peça, a que distância da borda da pista, quantas por metro de pista,
    // espaçamento mínimo entre elas, giro, escala e chance de aparecer.
    [Serializable]
    public class EnvScatterRule
    {
        public string label = "Pinheiros";
        public EnvScatterKind kind = EnvScatterKind.Pine;
        [Tooltip("Distância (m) a partir da borda da pista: mínimo e máximo. Nunca menor que 1,5 m (área de movimento).")]
        public Vector2 distance = new Vector2(2f, 8f);
        [Tooltip("Peças por metro de pista (soma dos dois lados), antes da densidade do perfil gráfico.")]
        [Min(0f)] public float perMeter = 0.3f;
        [Tooltip("Distância mínima (m) entre peças desta regra. 0 = sem verificação.")]
        [Min(0f)] public float minSpacing;
        [Tooltip("Giro em Y (graus) sorteado entre mínimo e máximo. Peças com giro próprio ignoram.")]
        public Vector2 yaw = new Vector2(0f, 360f);
        [Tooltip("Escala sorteada. Para pinheiros é a altura em metros.")]
        public Vector2 scale = new Vector2(1f, 1f);
        [Range(0f, 1f)] public float probability = 1f;
        [Tooltip("Não coloca perto dos braseiros e estandartes da borda.")]
        public bool avoidBraziers = true;
    }

    // Perfil de distribuição da decoração do Reino Congelado (barrancos entre a mureta e o penhasco).
    // O padrão fica em Resources/FrozenScatter_Default (gerado pelo menu do kit); sem asset, usa CreateDefault.
    [CreateAssetMenu(fileName = "FrozenScatter_", menuName = "Legion Rush/Ambiente/Perfil de distribuição")]
    public class FrozenScatterProfile : ScriptableObject
    {
        public const string DefaultResource = "FrozenScatter_Default";
        public const float MinRoadClearance = 1.5f;

        [Tooltip("Somado à semente da fase: mesma fase = mesmo cenário.")]
        public int seed;
        public List<EnvScatterRule> rules = new List<EnvScatterRule>();

        public static FrozenScatterProfile LoadOrDefault()
        {
            FrozenScatterProfile profile = Resources.Load<FrozenScatterProfile>(DefaultResource);
            return profile != null ? profile : CreateDefault();
        }

        public static FrozenScatterProfile CreateDefault()
        {
            var p = CreateInstance<FrozenScatterProfile>();
            p.name = DefaultResource;
            p.rules = new List<EnvScatterRule>
            {
                new EnvScatterRule { label = "Pinheiros", kind = EnvScatterKind.Pine, distance = new Vector2(2.2f, 15f), perMeter = 0.45f, minSpacing = 1.6f, scale = new Vector2(3.2f, 6.2f) },
                new EnvScatterRule { label = "Pinheiros com muita neve", kind = EnvScatterKind.PineHeavySnow, distance = new Vector2(3.5f, 15f), perMeter = 0.12f, minSpacing = 2f, scale = new Vector2(3f, 5f) },
                new EnvScatterRule { label = "Rochas", kind = EnvScatterKind.RockCluster, distance = new Vector2(2f, 15f), perMeter = 0.14f, scale = new Vector2(0.6f, 1.2f) },
                new EnvScatterRule { label = "Rochas grandes", kind = EnvScatterKind.RockLarge, distance = new Vector2(5f, 15f), perMeter = 0.03f, minSpacing = 6f, scale = new Vector2(1.4f, 2.2f), probability = 0.8f },
                new EnvScatterRule { label = "Cristais de gelo", kind = EnvScatterKind.IceCrystals, distance = new Vector2(1.6f, 12f), perMeter = 0.07f, scale = new Vector2(0.9f, 1.7f) },
                new EnvScatterRule { label = "Blocos de gelo", kind = EnvScatterKind.IceBlock, distance = new Vector2(3f, 14f), perMeter = 0.025f, minSpacing = 5f, scale = new Vector2(0.9f, 1.6f), probability = 0.8f },
                new EnvScatterRule { label = "Arbustos nevados", kind = EnvScatterKind.SnowBush, distance = new Vector2(0.6f, 5f), perMeter = 0.06f, scale = new Vector2(0.7f, 1.2f) },
                new EnvScatterRule { label = "Árvores congeladas", kind = EnvScatterKind.FrozenTree, distance = new Vector2(3f, 14f), perMeter = 0.02f, minSpacing = 6f, scale = new Vector2(3f, 4.5f), probability = 0.7f },
                new EnvScatterRule { label = "Plantinhas", kind = EnvScatterKind.SmallPlant, distance = new Vector2(1.5f, 6f), perMeter = 0.08f, scale = new Vector2(0.6f, 1f) },
                new EnvScatterRule { label = "Montes de neve", kind = EnvScatterKind.SnowDrift, distance = new Vector2(0.4f, 5f), perMeter = 0.09f, scale = new Vector2(1f, 1.8f) },
            };
            return p;
        }
    }
}
