using System;
using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    public enum GateEffectType
    {
        AddTroops, MultiplyTroops, RemoveTroops, RemovePercent, DamageBoost, AttackSpeedBoost, Shield, HealAll,
        LevelUp, GrantGold, GrantGems, SpawnEnemies, ReduceEnemies, SkipAhead
    }

    public enum GateCategory { Troops, Multiply, Upgrade, Defense, Special, Resource, Negative, Route }

    [Serializable]
    public class GateEffect
    {
        public GateEffectType type = GateEffectType.AddTroops;
        [Tooltip("Desmarcado = usa a classe abaixo. Marcado = respeita a composição do exército.")]
        public bool anyClass = true;
        public UnitClass unitClass = UnitClass.Warrior;
        [Tooltip("Quantidade, fator, fração (0–1) ou metros, conforme o tipo.")] public float value = 10f;
        [Min(0f), Tooltip("Segundos. 0 = até o fim da fase (bônus) ou instantâneo.")] public float duration;
    }

    [Serializable]
    public class GateOption
    {
        public string title = "+10";
        public string subtitle = "TROPAS";
        public GateCategory category = GateCategory.Troops;
        public List<GateEffect> effects = new List<GateEffect>();

        public bool IsPositive => category != GateCategory.Negative;

        public static Color ColorOf(GateCategory category) => category switch
        {
            GateCategory.Multiply => new Color(0.25f, 0.85f, 0.35f),
            GateCategory.Upgrade => new Color(1f, 0.55f, 0.15f),
            GateCategory.Defense => new Color(0.3f, 0.85f, 1f),
            GateCategory.Special => new Color(0.65f, 0.35f, 1f),
            GateCategory.Resource => new Color(1f, 0.78f, 0.2f),
            GateCategory.Negative => new Color(0.95f, 0.18f, 0.18f),
            GateCategory.Route => new Color(0.2f, 0.8f, 0.7f),
            _ => new Color(0.2f, 0.55f, 1f)
        };
    }

    [Serializable]
    public class GateChoiceRow
    {
        public float z = 30f;
        [Tooltip("2 ou 3 portais lado a lado.")] public List<GateOption> options = new List<GateOption>();
    }

    // Fábrica de portais usada por fases e ferramentas. Cada escolha muda o estado real do jogo.
    public static class GateLibrary
    {
        public static GateOption Troops(UnitClass unitClass, int amount)
        {
            string name = ArmyConfig.ClassName(unitClass).ToUpperInvariant();
            return Option($"+{amount}", amount == 1 ? name : PluralName(unitClass), unitClass == UnitClass.Knight ? GateCategory.Special : GateCategory.Troops,
                Effect(GateEffectType.AddTroops, amount, unitClass, true));
        }

        public static GateOption AnyTroops(int amount) =>
            Option($"+{amount}", "TROPAS", GateCategory.Troops, Effect(GateEffectType.AddTroops, amount));

        public static GateOption Multiply(int factor) =>
            Option($"x{factor}", "TROPAS", GateCategory.Multiply, Effect(GateEffectType.MultiplyTroops, factor));

        public static GateOption Lose(int amount) =>
            Option($"-{amount}", "TROPAS", GateCategory.Negative, Effect(GateEffectType.RemoveTroops, amount));

        public static GateOption Halve() =>
            Option("-50%", "TROPAS", GateCategory.Negative, Effect(GateEffectType.RemovePercent, 0.5f));

        public static GateOption Damage(float percent, float seconds = 0f) =>
            Option($"+{Mathf.RoundToInt(percent * 100f)}%", seconds > 0f ? $"DANO {seconds:0}s" : "DANO", GateCategory.Upgrade,
                Effect(GateEffectType.DamageBoost, percent, duration: seconds));

        public static GateOption AttackSpeed(float percent, float seconds = 0f) =>
            Option($"+{Mathf.RoundToInt(percent * 100f)}%", "VELOC. ATAQUE", GateCategory.Upgrade,
                Effect(GateEffectType.AttackSpeedBoost, percent, duration: seconds));

        public static GateOption Shield(float seconds) =>
            Option("ESCUDO", $"{seconds:0} SEGUNDOS", GateCategory.Defense, Effect(GateEffectType.Shield, 0.6f, duration: seconds));

        public static GateOption HealAndShield(float seconds) =>
            Option("CURA", "+ ESCUDO", GateCategory.Defense, Effect(GateEffectType.HealAll, 1f), Effect(GateEffectType.Shield, 0.4f, duration: seconds));

        public static GateOption LevelUp(int levels) =>
            Option($"+{levels} NÍVEL", "TODAS AS TROPAS", GateCategory.Upgrade, Effect(GateEffectType.LevelUp, levels));

        public static GateOption Gold(int amount) =>
            Option($"+{amount}", "OURO", GateCategory.Resource, Effect(GateEffectType.GrantGold, amount));

        public static GateOption ReduceEnemies(float fraction) =>
            Option($"-{Mathf.RoundToInt(fraction * 100f)}%", "INIMIGOS", GateCategory.Special, Effect(GateEffectType.ReduceEnemies, fraction));

        public static GateOption ShortRoute(float meters, int extraEnemies) =>
            Option("ATALHO", $"-{meters:0}m · +{extraEnemies} INIMIGOS", GateCategory.Route,
                Effect(GateEffectType.SkipAhead, meters), Effect(GateEffectType.SpawnEnemies, extraEnemies));

        public static GateOption LongRoute(int gold, UnitClass unitClass, int troops) =>
            Option("CAMINHO LONGO", $"+{gold} OURO · +{troops} {ShortName(unitClass)}", GateCategory.Route,
                Effect(GateEffectType.GrantGold, gold), Effect(GateEffectType.AddTroops, troops, unitClass, true));

        public static GateOption Sacrifice(int lose, int gems) =>
            Option($"-{lose} TROPAS", $"+{gems} GEMAS", GateCategory.Negative,
                Effect(GateEffectType.RemoveTroops, lose), Effect(GateEffectType.GrantGems, gems));

        public static GateOption FromLegacy(GateDef def) => def.op switch
        {
            GateOp.Add => AnyTroops(def.value),
            GateOp.Multiply => Multiply(Mathf.Max(1, def.value)),
            GateOp.Subtract => Lose(def.value),
            _ => Halve()
        };

        public static GateChoiceRow Row(float z, params GateOption[] options) =>
            new GateChoiceRow { z = z, options = new List<GateOption>(options) };

        private static GateOption Option(string title, string subtitle, GateCategory category, params GateEffect[] effects) =>
            new GateOption { title = title, subtitle = subtitle, category = category, effects = new List<GateEffect>(effects) };

        private static GateEffect Effect(GateEffectType type, float value, UnitClass unitClass = UnitClass.Warrior,
            bool specificClass = false, float duration = 0f)
        {
            return new GateEffect { type = type, value = value, unitClass = unitClass, anyClass = !specificClass, duration = duration };
        }

        private static string PluralName(UnitClass unitClass) => unitClass switch
        {
            UnitClass.Archer => "ARQUEIROS",
            UnitClass.Lancer => "LANCEIROS",
            UnitClass.Mage => "MAGOS",
            UnitClass.Healer => "CURANDEIRAS",
            UnitClass.Knight => "CAVALEIROS",
            _ => "GUERREIROS"
        };

        private static string ShortName(UnitClass unitClass) => unitClass switch
        {
            UnitClass.Archer => "ARQ",
            UnitClass.Lancer => "LAN",
            UnitClass.Mage => "MAG",
            UnitClass.Healer => "CUR",
            UnitClass.Knight => "CAV",
            _ => "GUE"
        };
    }
}
