using System;
using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    public enum BossId { DarkTitan = 0, MadPharaoh = 1, AncientDragon = 2, LordVulcan = 3, MechColossus = 4 }

    // Slam: círculo no alvo | Spin: círculo ao redor do chefão | Shockwave: anel que se expande
    // Rain: vários círculos sobre o exército | Line: faixa do chefão até o alvo (sopro, laser, fissura)
    // Storm: área global com lentidão e dano contínuo | Summon: invoca inimigos | Flight: voa (imune a corpo a corpo) e bombardeia
    public enum BossAttackType { Slam, Spin, Shockwave, Rain, Line, Storm, Summon, Flight }

    [Serializable]
    public class BossAttack
    {
        public string name = "Ataque";
        public BossAttackType type = BossAttackType.Slam;
        [Range(1, 3), Tooltip("Fase mínima em que o ataque aparece.")] public int minPhase = 1;
        [Min(0f)] public float weight = 1f;
        [Min(0.2f), Tooltip("Tempo de aviso antes do impacto.")] public float telegraph = 1f;
        [Min(0f)] public float damage = 40f;
        public DamageType damageType = DamageType.Physical;
        [Min(0.5f)] public float radius = 3f;
        [Min(1f), Tooltip("Comprimento (Line) ou alcance (Shockwave).")] public float length = 14f;
        [Min(1)] public int count = 1;
        [Min(0f)] public float duration = 4f;
        [Range(0.1f, 1f)] public float slowFactor = 1f;
        [Min(0f)] public float burnDps;
        [Min(0f)] public float burnSeconds;
        [Min(0f), Tooltip("Janela de vulnerabilidade após o ataque (dano extra recebido).")] public float vulnerableAfter;
        public SoundId sound = SoundId.BossStomp;
        public Color color = new Color(1f, 0.2f, 0.15f, 0.85f);
    }

    [CreateAssetMenu(fileName = "Boss_", menuName = "Legion Rush/Boss Definition")]
    public class BossDefinition : ScriptableObject
    {
        public BossId id;
        public string displayName = "Titã Sombrio";
        public CharacterId visual = CharacterId.BossDarkTitan;

        [Header("Atributos")]
        [Min(100f)] public float maxHealth = 14000f;
        [Min(0f)] public float armor = 40f;
        [Min(0.5f)] public float bodyRadius = 1.6f;
        [Min(0.5f)] public float scale = 4.2f;
        [Min(0.5f)] public float moveSpeed = 2.4f;
        [Min(4f)] public float aggroRange = 20f;
        [Min(1f), Tooltip("Distância que tenta manter do exército.")] public float preferredRange = 5f;
        [Min(0.2f), Tooltip("Pausa entre ataques na fase 1.")] public float attackSpacing = 1.6f;
        [Range(-1f, 1f)] public float magicResist;
        [Range(-1f, 1f)] public float fireResist;
        [Range(-1f, 1f)] public float iceResist;

        [Header("Fases")]
        [Range(0.1f, 0.95f)] public float phase2At = 0.66f;
        [Range(0.05f, 0.9f)] public float phase3At = 0.33f;
        public float[] phaseSpeed = { 1f, 1.25f, 1.5f };
        [Min(1f)] public float vulnerableMultiplier = 1.75f;
        [Min(0)] public int phaseSummonCount = 6;

        [Header("Visual")]
        public Color primaryColor = new Color(0.16f, 0.15f, 0.2f);
        public Color secondaryColor = new Color(0.35f, 0.38f, 0.48f);
        public Color glowColor = new Color(0.3f, 0.75f, 1f);
        public Color summonColor = new Color(0.25f, 0.28f, 0.4f);
        [Min(0.1f)] public float summonHealth = 1.2f;

        [Header("Recompensas")]
        [Min(0)] public int goldReward = 300;
        [Min(0)] public int gemReward = 10;

        public List<BossAttack> attacks = new List<BossAttack>();

        public float PhaseSpeed(int phase)
        {
            if (phaseSpeed == null || phaseSpeed.Length == 0) return 1f;
            return Mathf.Max(0.2f, phaseSpeed[Mathf.Clamp(phase - 1, 0, phaseSpeed.Length - 1)]);
        }

        public float Resistance(DamageType type) => type switch
        {
            DamageType.Magic => magicResist,
            DamageType.Fire => fireResist,
            DamageType.Ice => iceResist,
            _ => 0f
        };

        public static BossId DefaultFor(WorldId world) => world switch
        {
            WorldId.LostDesert => BossId.MadPharaoh,
            WorldId.EnchantedForest => BossId.AncientDragon,
            WorldId.VolcanicRealm => BossId.LordVulcan,
            _ => BossId.DarkTitan
        };

        public static string NameOf(BossId id) => id switch
        {
            BossId.MadPharaoh => "Faraó Insano",
            BossId.AncientDragon => "Dragão Ancião",
            BossId.LordVulcan => "Lord Vulcano",
            BossId.MechColossus => "Colosso Mecânico",
            _ => "Titã Sombrio"
        };

        public static BossDefinition CreatePreset(BossId id)
        {
            var d = CreateInstance<BossDefinition>();
            d.id = id;
            d.displayName = NameOf(id);
            d.name = "Boss_" + id;
            switch (id)
            {
                case BossId.MadPharaoh: d.SetupPharaoh(); break;
                case BossId.AncientDragon: d.SetupDragon(); break;
                case BossId.LordVulcan: d.SetupVulcan(); break;
                case BossId.MechColossus: d.SetupColossus(); break;
                default: d.SetupTitan(); break;
            }
            return d;
        }

        private static BossAttack A(string name, BossAttackType type, int minPhase, float weight, float telegraph, float damage,
            float radius, SoundId sound, Color color, float vulnerable = 0f)
        {
            return new BossAttack
            {
                name = name, type = type, minPhase = minPhase, weight = weight, telegraph = telegraph, damage = damage,
                radius = radius, sound = sound, color = color, vulnerableAfter = vulnerable
            };
        }

        private static readonly Color Red = new Color(1f, 0.18f, 0.12f, 0.85f);
        private static readonly Color Ice = new Color(0.45f, 0.85f, 1f, 0.85f);
        private static readonly Color Sand = new Color(1f, 0.78f, 0.35f, 0.85f);
        private static readonly Color Fire = new Color(1f, 0.45f, 0.08f, 0.9f);
        private static readonly Color Energy = new Color(0.35f, 0.9f, 1f, 0.9f);

        // Titã Sombrio (Reino Congelado): força bruta, ondas de choque e invocação. Ensina a desviar.
        private void SetupTitan()
        {
            visual = CharacterId.BossDarkTitan;
            maxHealth = 14000f; armor = 45f; bodyRadius = 1.6f; scale = 4.2f; moveSpeed = 2.4f; preferredRange = 4.5f; attackSpacing = 1.7f;
            iceResist = 0.4f; fireResist = -0.2f;
            primaryColor = new Color(0.15f, 0.14f, 0.2f); secondaryColor = new Color(0.36f, 0.4f, 0.52f); glowColor = new Color(0.3f, 0.75f, 1f);
            summonColor = new Color(0.22f, 0.25f, 0.38f); phaseSummonCount = 8; goldReward = 300; gemReward = 10;
            attacks = new List<BossAttack>
            {
                A("Golpe de Maça", BossAttackType.Slam, 1, 3f, 1.1f, 70f, 2.8f, SoundId.BossStomp, Red, 1.6f),
                new BossAttack { name = "Impacto no Chão", type = BossAttackType.Shockwave, minPhase = 1, weight = 2f, telegraph = 1.2f, damage = 35f, length = 13f, radius = 1f, sound = SoundId.BossStomp, color = Red, vulnerableAfter = 2f },
                A("Ataque Circular", BossAttackType.Spin, 2, 2f, 0.9f, 45f, 4.6f, SoundId.SwordSwing, Red),
                new BossAttack { name = "Invocar Soldados", type = BossAttackType.Summon, minPhase = 2, weight = 1f, telegraph = 1f, count = 8, sound = SoundId.BossRoar, color = Ice },
                new BossAttack { name = "Fúria Glacial", type = BossAttackType.Rain, minPhase = 3, weight = 2f, telegraph = 1.4f, damage = 55f, radius = 2.4f, count = 6, damageType = DamageType.Ice, slowFactor = 0.5f, duration = 2f, sound = SoundId.IceBurst, color = Ice }
            };
        }

        // Faraó Insano (Deserto Perdido): armadilhas, tempestade de areia e guardiões. Exige mobilidade e cura.
        private void SetupPharaoh()
        {
            visual = CharacterId.BossMadPharaoh;
            maxHealth = 16000f; armor = 30f; bodyRadius = 1.4f; scale = 3.8f; moveSpeed = 2.8f; preferredRange = 8f; attackSpacing = 1.5f;
            magicResist = 0.35f; fireResist = 0.2f;
            primaryColor = new Color(0.85f, 0.65f, 0.25f); secondaryColor = new Color(0.15f, 0.25f, 0.6f); glowColor = new Color(1f, 0.8f, 0.3f);
            summonColor = new Color(0.75f, 0.6f, 0.3f); phaseSummonCount = 6; goldReward = 400; gemReward = 14;
            attacks = new List<BossAttack>
            {
                new BossAttack { name = "Armadilhas de Areia", type = BossAttackType.Rain, minPhase = 1, weight = 3f, telegraph = 1.3f, damage = 45f, radius = 2.2f, count = 5, sound = SoundId.BossStomp, color = Sand },
                new BossAttack { name = "Tempestade de Areia", type = BossAttackType.Storm, minPhase = 1, weight = 1f, telegraph = 1f, damage = 5f, duration = 5f, slowFactor = 0.6f, sound = SoundId.MagicCast, color = Sand },
                new BossAttack { name = "Cetro Solar", type = BossAttackType.Line, minPhase = 2, weight = 2f, telegraph = 1f, damage = 55f, radius = 1.2f, length = 16f, damageType = DamageType.Magic, sound = SoundId.MagicCast, color = Sand, vulnerableAfter = 1.5f },
                new BossAttack { name = "Guardiões", type = BossAttackType.Summon, minPhase = 2, weight = 1f, telegraph = 1f, count = 6, sound = SoundId.BossRoar, color = Sand },
                new BossAttack { name = "Ira do Faraó", type = BossAttackType.Shockwave, minPhase = 3, weight = 2f, telegraph = 1.1f, damage = 40f, length = 14f, radius = 1f, damageType = DamageType.Magic, sound = SoundId.FireExplosion, color = Sand, vulnerableAfter = 1.8f }
            };
        }

        // Dragão Ancião (Floresta Encantada): voo (imune a corpo a corpo), sopro de fogo e chuva de fogo. Exige arqueiros/magos.
        private void SetupDragon()
        {
            visual = CharacterId.BossAncientDragon;
            maxHealth = 18000f; armor = 35f; bodyRadius = 1.9f; scale = 3.6f; moveSpeed = 3.2f; preferredRange = 7f; attackSpacing = 1.4f;
            fireResist = 0.6f; iceResist = -0.25f;
            primaryColor = new Color(0.2f, 0.5f, 0.22f); secondaryColor = new Color(0.85f, 0.7f, 0.3f); glowColor = new Color(1f, 0.55f, 0.15f);
            summonColor = new Color(0.25f, 0.55f, 0.25f); phaseSummonCount = 6; goldReward = 500; gemReward = 18;
            attacks = new List<BossAttack>
            {
                new BossAttack { name = "Sopro Elemental", type = BossAttackType.Line, minPhase = 1, weight = 3f, telegraph = 1.1f, damage = 50f, radius = 1.6f, length = 18f, damageType = DamageType.Fire, burnDps = 8f, burnSeconds = 3f, sound = SoundId.FireExplosion, color = Fire, vulnerableAfter = 1.2f },
                new BossAttack { name = "Voo e Ataque", type = BossAttackType.Flight, minPhase = 1, weight = 1.2f, telegraph = 0.6f, damage = 40f, radius = 2.3f, count = 5, duration = 4.5f, damageType = DamageType.Fire, sound = SoundId.BossRoar, color = Fire, vulnerableAfter = 2.5f },
                A("Golpe de Cauda", BossAttackType.Spin, 2, 2f, 0.9f, 45f, 4.2f, SoundId.SwordSwing, Red),
                new BossAttack { name = "Draconetes", type = BossAttackType.Summon, minPhase = 2, weight = 1f, telegraph = 1f, count = 6, sound = SoundId.BossRoar, color = Fire },
                new BossAttack { name = "Chuva de Fogo", type = BossAttackType.Rain, minPhase = 3, weight = 2f, telegraph = 1.3f, damage = 45f, radius = 2.4f, count = 7, damageType = DamageType.Fire, burnDps = 6f, burnSeconds = 3f, sound = SoundId.FireExplosion, color = Fire }
            };
        }

        // Lord Vulcano (Reino Vulcânico): explosões, fissuras de lava que queimam e meteoros. Pune exércitos agrupados.
        private void SetupVulcan()
        {
            visual = CharacterId.BossLordVulcan;
            maxHealth = 21000f; armor = 55f; bodyRadius = 1.7f; scale = 4.4f; moveSpeed = 2.2f; preferredRange = 5f; attackSpacing = 1.4f;
            fireResist = 0.7f; iceResist = -0.3f;
            primaryColor = new Color(0.22f, 0.1f, 0.08f); secondaryColor = new Color(0.45f, 0.12f, 0.08f); glowColor = new Color(1f, 0.4f, 0.08f);
            summonColor = new Color(0.6f, 0.18f, 0.08f); phaseSummonCount = 8; goldReward = 650; gemReward = 22;
            attacks = new List<BossAttack>
            {
                new BossAttack { name = "Golpe Explosivo", type = BossAttackType.Slam, minPhase = 1, weight = 3f, telegraph = 1f, damage = 65f, radius = 3.2f, damageType = DamageType.Fire, burnDps = 6f, burnSeconds = 2f, sound = SoundId.FireExplosion, color = Fire, vulnerableAfter = 1.4f },
                new BossAttack { name = "Fissura de Lava", type = BossAttackType.Line, minPhase = 1, weight = 2f, telegraph = 1.1f, damage = 30f, radius = 1f, length = 18f, damageType = DamageType.Fire, burnDps = 12f, burnSeconds = 4f, sound = SoundId.BossStomp, color = Fire },
                new BossAttack { name = "Chuva de Meteoros", type = BossAttackType.Rain, minPhase = 2, weight = 3f, telegraph = 1.3f, damage = 50f, radius = 2.2f, count = 8, damageType = DamageType.Fire, sound = SoundId.FireExplosion, color = Fire },
                new BossAttack { name = "Elementais", type = BossAttackType.Summon, minPhase = 2, weight = 1f, telegraph = 1f, count = 8, sound = SoundId.BossRoar, color = Fire },
                new BossAttack { name = "Erupção", type = BossAttackType.Shockwave, minPhase = 3, weight = 2f, telegraph = 1.2f, damage = 45f, length = 14f, radius = 1f, damageType = DamageType.Fire, sound = SoundId.FireExplosion, color = Fire, vulnerableAfter = 2f }
            };
        }

        // Colosso Mecânico (chefão final): giro, laser com superaquecimento, mísseis e autômatos.
        private void SetupColossus()
        {
            visual = CharacterId.BossMechColossus;
            maxHealth = 26000f; armor = 70f; bodyRadius = 1.8f; scale = 4.6f; moveSpeed = 2f; preferredRange = 6f; attackSpacing = 1.3f;
            magicResist = -0.2f; fireResist = 0.3f; iceResist = 0.3f;
            primaryColor = new Color(0.45f, 0.42f, 0.38f); secondaryColor = new Color(0.8f, 0.6f, 0.2f); glowColor = new Color(0.3f, 0.9f, 1f);
            summonColor = new Color(0.55f, 0.55f, 0.6f); phaseSummonCount = 10; goldReward = 900; gemReward = 35;
            phaseSpeed = new[] { 1f, 1.3f, 1.6f };
            attacks = new List<BossAttack>
            {
                A("Golpe Giratório", BossAttackType.Spin, 1, 3f, 0.9f, 50f, 5f, SoundId.SwordSwing, Red, 1.2f),
                new BossAttack { name = "Laser de Energia", type = BossAttackType.Line, minPhase = 1, weight = 2f, telegraph = 1.3f, damage = 60f, radius = 1f, length = 22f, damageType = DamageType.Magic, sound = SoundId.MagicCast, color = Energy, vulnerableAfter = 1.8f },
                new BossAttack { name = "Mísseis", type = BossAttackType.Rain, minPhase = 2, weight = 3f, telegraph = 1.2f, damage = 40f, radius = 2f, count = 10, sound = SoundId.FireExplosion, color = Red },
                new BossAttack { name = "Invocar Autômatos", type = BossAttackType.Summon, minPhase = 2, weight = 1f, telegraph = 1f, count = 10, sound = SoundId.BossRoar, color = Energy },
                new BossAttack { name = "Sobrecarga", type = BossAttackType.Shockwave, minPhase = 3, weight = 2f, telegraph = 1.2f, damage = 50f, length = 15f, radius = 1f, damageType = DamageType.Magic, sound = SoundId.BossStomp, color = Energy, vulnerableAfter = 2.2f }
            };
        }
    }
}
