using System;
using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    public enum GateOp { Add, Multiply, Subtract, HalveDown }

    [Serializable]
    public struct GateDef
    {
        public GateOp op;
        [Min(0)] public int value;

        public GateDef(GateOp op, int value)
        {
            this.op = op;
            this.value = value;
        }

        public bool IsPositive => op == GateOp.Add || op == GateOp.Multiply;

        public string Label => op switch
        {
            GateOp.Add => "+" + value,
            GateOp.Multiply => "x" + value,
            GateOp.Subtract => "-" + value,
            GateOp.HalveDown => "-50%",
            _ => "?"
        };

        // Rules: add/subtract are exact, multiply uses factor >= 1,
        // halving rounds down, result is always clamped to [0, max].
        public int Apply(int current, int max)
        {
            long safeCurrent = Math.Max(0, current);
            long result = op switch
            {
                GateOp.Add => safeCurrent + Math.Max(0, value),
                GateOp.Multiply => safeCurrent * Math.Max(1, value),
                GateOp.Subtract => safeCurrent - Math.Max(0, value),
                GateOp.HalveDown => safeCurrent / 2,
                _ => safeCurrent
            };
            return (int)Math.Min(Math.Max(result, 0L), Math.Max(0L, (long)max));
        }
    }

    [Serializable]
    public class GateRow
    {
        public float z;
        public GateDef left;
        public GateDef right;
    }

    [Serializable]
    public class EnemyWave
    {
        public float z;
        [Min(1)] public int count = 10;
        [Min(0.1f)] public float healthMultiplier = 1f;
        [Min(0.6f)] public float spacing = 0.9f;
    }

    [Serializable]
    public class EnemyStats
    {
        [Min(1f)] public float maxHealth = 30f;
        [Min(0f)] public float damage = 6f;
        [Min(0.5f)] public float attackRange = 1.1f;
        [Min(0.1f)] public float attackInterval = 0.9f;
        [Min(0.5f)] public float moveSpeed = 3.2f;
        [Min(1f)] public float aggroRange = 16f;
        public Color color = new Color(0.55f, 0.1f, 0.12f);
    }

    [CreateAssetMenu(fileName = "Level_", menuName = "Legion Rush/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        public int levelNumber = 1;
        public string worldName = "Reino Congelado";
        [Min(50f)] public float trackLength = 220f;
        [Min(3f)] public float trackHalfWidth = 5f;
        [Min(1f)] public float forwardSpeed = 7f;
        [Min(0)] public int goldPerEnemy = 2;
        [Min(0)] public int victoryGold = 50;
        public EnemyStats enemy = new EnemyStats();
        public List<GateRow> gateRows = new List<GateRow>();
        public List<EnemyWave> waves = new List<EnemyWave>();

        public void Sanitize()
        {
            trackLength = Mathf.Max(50f, trackLength);
            trackHalfWidth = Mathf.Max(3f, trackHalfWidth);
            gateRows ??= new List<GateRow>();
            waves ??= new List<EnemyWave>();
            enemy ??= new EnemyStats();

            int removedRows = gateRows.RemoveAll(r => r == null);
            int removedWaves = waves.RemoveAll(w => w == null || w.count <= 0);
            if (removedRows + removedWaves > 0)
                GameLog.Warn($"Level {levelNumber}: removed {removedRows} invalid gate rows and {removedWaves} invalid waves.");

            foreach (GateRow row in gateRows) row.z = Mathf.Clamp(row.z, 5f, trackLength - 5f);
            foreach (EnemyWave wave in waves)
            {
                wave.z = Mathf.Clamp(wave.z, 10f, trackLength - 5f);
                wave.healthMultiplier = Mathf.Max(0.1f, wave.healthMultiplier);
                wave.spacing = Mathf.Max(0.6f, wave.spacing);
            }
            gateRows.Sort((a, b) => a.z.CompareTo(b.z));
            waves.Sort((a, b) => a.z.CompareTo(b.z));
        }

        public static LevelConfig CreateDefault()
        {
            var config = CreateInstance<LevelConfig>();
            config.name = "Level_01_Default";
            config.gateRows = new List<GateRow>
            {
                new GateRow { z = 28f, left = new GateDef(GateOp.Add, 10), right = new GateDef(GateOp.Multiply, 2) },
                new GateRow { z = 78f, left = new GateDef(GateOp.Multiply, 2), right = new GateDef(GateOp.Subtract, 10) },
                new GateRow { z = 128f, left = new GateDef(GateOp.HalveDown, 0), right = new GateDef(GateOp.Add, 25) },
                new GateRow { z = 172f, left = new GateDef(GateOp.Add, 50), right = new GateDef(GateOp.Multiply, 3) }
            };
            config.waves = new List<EnemyWave>
            {
                new EnemyWave { z = 55f, count = 12, healthMultiplier = 1f },
                new EnemyWave { z = 105f, count = 24, healthMultiplier = 1.1f },
                new EnemyWave { z = 150f, count = 36, healthMultiplier = 1.2f },
                new EnemyWave { z = 200f, count = 55, healthMultiplier = 1.4f }
            };
            return config;
        }
    }

    public static class GateMathSelfTest
    {
        public static bool Run()
        {
            int failures = 0;
            Expect(new GateDef(GateOp.Add, 10).Apply(5, 150), 15, "+10", ref failures);
            Expect(new GateDef(GateOp.Multiply, 3).Apply(60, 150), 150, "x3 respects cap", ref failures);
            Expect(new GateDef(GateOp.Multiply, 0).Apply(7, 150), 7, "x0 treated as x1", ref failures);
            Expect(new GateDef(GateOp.Subtract, 10).Apply(4, 150), 0, "-10 floors at 0", ref failures);
            Expect(new GateDef(GateOp.HalveDown, 0).Apply(7, 150), 3, "-50% rounds down", ref failures);
            Expect(new GateDef(GateOp.Add, 10).Apply(-5, 150), 10, "negative input sanitized", ref failures);
            Expect(new GateDef(GateOp.Multiply, 3).Apply(int.MaxValue, int.MaxValue), int.MaxValue, "no overflow", ref failures);
            if (failures == 0) GameLog.Info("Gate math self-test passed (7 cases).");
            return failures == 0;
        }

        private static void Expect(int actual, int expected, string label, ref int failures)
        {
            if (actual == expected) return;
            failures++;
            GameLog.Error($"Gate self-test '{label}' failed: expected {expected}, got {actual}.");
        }
    }
}
