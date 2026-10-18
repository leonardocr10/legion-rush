using System;
using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    // Ordem fixa: valores serializados em assets. Novos tipos sempre no final.
    public enum EnemyKind { Soldier = 0, Archer = 1, Pikeman = 2, Berserker = 3, HeavyWarrior = 4, DarkMage = 5 }

    // Variações do exército vermelho. Cada tipo parte do EnemyStats da fase (vida, dano, alcance...) e aplica
    // multiplicadores, para o balanceamento por fase continuar valendo para todos os tipos.
    public static class EnemyKinds
    {
        public const int Count = 6;

        public struct Profile
        {
            public float Health, Damage, Range, Interval, Speed, ArmorBonus;
            public bool Ranged;        // ataca com projétil (flecha ou orbe)
            public float AreaRadius;   // > 0: o projétil explode em área
            public float BlockChance;  // chance de bloquear com o escudo (reduz 70% do dano)
            public bool Rage;          // abaixo de 50% de vida: ataques mais rápidos e mais velocidade
            public float Weight;       // peso no sorteio da composição das ondas
            public int UnlockLevel;    // primeira fase em que aparece na composição automática
        }

        private static readonly Profile[] Profiles =
        {
            new Profile { Health = 1f, Damage = 1f, Range = 0f, Interval = 1f, Speed = 1f, Weight = 5f, UnlockLevel = 1 },
            new Profile { Health = 0.7f, Damage = 0.75f, Range = 7f, Interval = 1.35f, Speed = 0.95f, Ranged = true, Weight = 2f, UnlockLevel = 2 },
            new Profile { Health = 1.05f, Damage = 1.1f, Range = 0.9f, Interval = 1.1f, Speed = 0.95f, Weight = 2f, UnlockLevel = 3 },
            new Profile { Health = 1.25f, Damage = 1.45f, Range = 0f, Interval = 1.1f, Speed = 1.2f, Rage = true, Weight = 1.5f, UnlockLevel = 4 },
            new Profile { Health = 2.2f, Damage = 1.15f, Range = 0f, Interval = 1.25f, Speed = 0.72f, ArmorBonus = 25f, BlockChance = 0.3f, Weight = 1f, UnlockLevel = 5 },
            new Profile { Health = 0.8f, Damage = 1.3f, Range = 6.5f, Interval = 1.8f, Speed = 0.9f, Ranged = true, AreaRadius = 1.4f, Weight = 1f, UnlockLevel = 6 },
        };

        public static Profile Get(EnemyKind kind) => Profiles[Mathf.Clamp((int)kind, 0, Count - 1)];

        public static CharacterId Visual(EnemyKind kind) => kind switch
        {
            EnemyKind.Archer => CharacterId.EnemyArcher,
            EnemyKind.Pikeman => CharacterId.Pikeman,
            EnemyKind.Berserker => CharacterId.Berserker,
            EnemyKind.HeavyWarrior => CharacterId.HeavyWarrior,
            EnemyKind.DarkMage => CharacterId.DarkMage,
            _ => CharacterId.EnemySoldier
        };

        // Cópia do EnemyStats da fase com os multiplicadores do tipo (uma por tipo e fase, sem alocação por inimigo).
        public static EnemyStats Apply(EnemyStats source, EnemyKind kind)
        {
            Profile p = Get(kind);
            return new EnemyStats
            {
                maxHealth = source.maxHealth * p.Health,
                damage = source.damage * p.Damage,
                attackRange = p.Ranged ? Mathf.Max(source.attackRange, p.Range) : source.attackRange + p.Range,
                attackInterval = source.attackInterval * p.Interval,
                moveSpeed = source.moveSpeed * p.Speed,
                aggroRange = Mathf.Max(source.aggroRange, p.Ranged ? p.Range + 6f : source.aggroRange),
                armor = source.armor + p.ArmorBonus,
                color = source.color
            };
        }

        // Composição de uma onda: tipos liberados pela fase, sorteio determinístico, à distância nas fileiras de trás.
        public static void FillWave(List<EnemyKind> output, int count, int levelNumber, int seed, IReadOnlyList<EnemyKind> fixedMix)
        {
            output.Clear();
            var rng = new System.Random(seed);
            if (fixedMix != null && fixedMix.Count > 0)
            {
                for (int i = 0; i < count; i++) output.Add(fixedMix[i % fixedMix.Count]);
            }
            else
            {
                float total = 0f;
                for (int k = 0; k < Count; k++)
                    if (levelNumber >= Profiles[k].UnlockLevel) total += Profiles[k].Weight;
                for (int i = 0; i < count; i++)
                {
                    double roll = rng.NextDouble() * total;
                    EnemyKind pick = EnemyKind.Soldier;
                    for (int k = 0; k < Count; k++)
                    {
                        if (levelNumber < Profiles[k].UnlockLevel) continue;
                        roll -= Profiles[k].Weight;
                        if (roll <= 0) { pick = (EnemyKind)k; break; }
                    }
                    output.Add(pick);
                }
            }
            // corpo a corpo na frente (z menor), pesados primeiro, à distância atrás
            output.Sort((a, b) => Rank(a).CompareTo(Rank(b)));
        }

        private static int Rank(EnemyKind kind) => kind switch
        {
            EnemyKind.HeavyWarrior => 0,
            EnemyKind.Berserker => 1,
            EnemyKind.Soldier => 2,
            EnemyKind.Pikeman => 3,
            EnemyKind.DarkMage => 5,
            EnemyKind.Archer => 4,
            _ => 2
        };
    }
}
