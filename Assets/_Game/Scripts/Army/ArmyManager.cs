using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace LegionRush
{
    // Exército com 6 classes, formação por papel (frente/retaguarda), composição configurável,
    // níveis por classe, posturas e utilitários usados por habilidades, portais e chefões.
    public class ArmyManager : MonoBehaviour
    {
        private static readonly UnitClass[] FrontOrder = { UnitClass.Knight, UnitClass.Warrior, UnitClass.Lancer };
        private static readonly UnitClass[] BackOrder = { UnitClass.Archer, UnitClass.Mage, UnitClass.Healer };
        private static readonly Color OrbColor = new Color(0.7f, 0.45f, 1f, 1f);

        public ArmyConfig Config { get; private set; }
        public int Count => soldiers.Count;
        public float FormationRadius { get; private set; }
        public FormationStance Stance { get; private set; } = FormationStance.Balanced;
        public float DamageMultiplier => permanentDamage * (1f + timedDamage);
        public float AttackIntervalMultiplier => 1f / ((1f + permanentAttackSpeed) * (1f + timedAttackSpeed));
        public float HealthMultiplier { get; set; } = 1f;
        public float HealMultiplier { get; set; } = 1f;
        public float ArmorMultiplier => metaArmor * (Stance == FormationStance.Defensive ? 1.35f : Stance == FormationStance.Aggressive ? 0.9f : 1f);
        public float FrontLeash => Config.frontLeashRange * (Stance == FormationStance.Aggressive ? 1.6f : Stance == FormationStance.Defensive ? 0.55f : 1f);
        public IReadOnlyList<Soldier> Soldiers => soldiers;
        public event System.Action<int, int> CountChanged;

        private readonly List<Soldier> soldiers = new List<Soldier>(256);
        private readonly List<Soldier>[] byClass = new List<Soldier>[ArmyConfig.ClassCount];
        private readonly ObjectPool<Soldier>[] pools = new ObjectPool<Soldier>[ArmyConfig.ClassCount];
        private readonly int[] classLevels = new int[ArmyConfig.ClassCount];
        private readonly int[] pendingLevels = new int[ArmyConfig.ClassCount];
        private readonly float[] metaClassDamage = { 1f, 1f, 1f, 1f, 1f, 1f };
        private readonly float[] metaClassHealth = { 1f, 1f, 1f, 1f, 1f, 1f };
        private readonly bool[] unlockedClasses = { true, true, true, true, true, true };
        private float metaArmor = 1f;
        private int metaStartTroops;
        private Color skinTint = Color.white;
        private readonly float[] weights = new float[ArmyConfig.ClassCount];
        private ObjectPool<Projectile> arrowPool;
        private ObjectPool<Projectile> orbPool;
        private System.Action<Projectile> releaseArrow;
        private System.Action<Projectile> releaseOrb;
        private CorpseQueue corpses;
        private Transform commander;
        private Transform poolRoot;
        private Material arrowTrailMaterial;
        private Material orbTrailMaterial;
        private bool ownsArrowTrail;
        private float halfWidth;
        private bool formationDirty;
        private float permanentDamage = 1f;
        private float permanentAttackSpeed;
        private float timedDamage;
        private float timedAttackSpeed;
        private readonly List<Buff> buffs = new List<Buff>(8);

        private struct Buff
        {
            public bool Damage;
            public float Amount;
            public float Remaining;
        }

        public void Initialize(ArmyConfig config, Transform commanderTransform, float trackHalfWidth, int compositionIndex)
        {
            Config = config;
            commander = commanderTransform;
            halfWidth = trackHalfWidth;
            poolRoot = new GameObject("Pool").transform;
            poolRoot.SetParent(transform, false);
            corpses = new CorpseQueue(ReleaseCorpse);
            for (int i = 0; i < ArmyConfig.ClassCount; i++)
            {
                byClass[i] = new List<Soldier>(64);
                classLevels[i] = Mathf.Clamp(pendingLevels[i] > 0 ? pendingLevels[i] : 1, 1, config.maxTroopLevel);
                pools[i] = CreateSoldierPool((UnitClass)i);
            }
            ApplyWeights(compositionIndex);

            ResolveTrailMaterials();
            int poolSize = config.projectilePoolSize;
            arrowPool = new ObjectPool<Projectile>(CreateArrow, p => p.gameObject.SetActive(true), p => p.gameObject.SetActive(false),
                p => { if (p != null) Destroy(p.gameObject); }, false, poolSize, Mathf.Max(poolSize, 512));
            orbPool = new ObjectPool<Projectile>(CreateOrb, p => p.gameObject.SetActive(true), p => p.gameObject.SetActive(false),
                p => { if (p != null) Destroy(p.gameObject); }, false, 16, 128);
            releaseArrow = p => arrowPool.Release(p);
            releaseOrb = p => orbPool.Release(p);

            SetCount(config.startingSoldiers + metaStartTroops);
            GameLog.Info($"Exército pronto: {Count} soldados ({Describe()}), composição '{config.compositions[CompositionIndex].displayName}'.");
        }

        public int CompositionIndex { get; private set; }

        public int CountOf(UnitClass unitClass) => byClass[(int)unitClass].Count;
        public int LevelOf(UnitClass unitClass) => classLevels[(int)unitClass];
        public float LevelMultiplier(UnitClass unitClass) => 1f + Config.levelScaling * (classLevels[(int)unitClass] - 1);

        // Bônus permanentes (melhorias, equipamentos, herói, poções), classes liberadas e visual. Chamar antes de Initialize.
        public void ApplyMeta(MetaBonuses bonuses, bool[] unlocked, Color tint)
        {
            for (int i = 0; i < ArmyConfig.ClassCount; i++)
            {
                metaClassDamage[i] = bonuses.ClassDamage[i] * bonuses.AllDamage;
                metaClassHealth[i] = bonuses.ClassHealth[i] * bonuses.AllHealth;
                unlockedClasses[i] = unlocked == null || i >= unlocked.Length || unlocked[i];
            }
            permanentAttackSpeed += bonuses.AttackSpeed;
            HealMultiplier = bonuses.Heal;
            metaArmor = bonuses.Armor;
            metaStartTroops = bonuses.StartTroops;
            skinTint = tint;
        }

        public float ClassDamageMultiplier(UnitClass unitClass) => metaClassDamage[(int)unitClass];
        public float ClassHealthMultiplier(UnitClass unitClass) => HealthMultiplier * metaClassHealth[(int)unitClass];
        public bool IsClassUnlocked(UnitClass unitClass) => unlockedClasses[(int)unitClass];

        // Níveis permanentes vindos do save, aplicados antes de criar as tropas iniciais.
        public void SetClassLevelBeforeSpawn(UnitClass unitClass, int level) => pendingLevels[(int)unitClass] = level;

        public void SetClassLevel(UnitClass unitClass, int level)
        {
            classLevels[(int)unitClass] = Mathf.Clamp(level, 1, Config.maxTroopLevel);
        }

        public void SetComposition(int compositionIndex)
        {
            ApplyWeights(compositionIndex);
            int total = soldiers.Count;
            int previous = total;
            for (int i = soldiers.Count - 1; i >= 0; i--) RemoveSoldierAt(i, false);
            for (int i = 0; i < total; i++) SpawnSoldier(PickClassToAdd());
            RebuildFormation();
            CountChanged?.Invoke(previous, soldiers.Count);
        }

        public void SetStance(FormationStance stance)
        {
            if (Stance == stance) return;
            Stance = stance;
            formationDirty = true;
        }

        // Ajuste genérico (portais numéricos): mantém a proporção da composição.
        public void SetCount(int target)
        {
            target = Mathf.Clamp(target, 0, Config.maxSoldiers);
            int previous = soldiers.Count;
            while (soldiers.Count < target) SpawnSoldier(PickClassToAdd());
            while (soldiers.Count > target) RemoveSoldierOf(PickClassToRemove());
            RebuildFormation();
            if (previous != soldiers.Count) CountChanged?.Invoke(previous, soldiers.Count);
        }

        public int ClassCap(UnitClass unitClass) => Config.classCaps[(int)unitClass];

        // Multiplicador com teto de ganho por portal e respeitando o limite total.
        public int MultiplyTroops(float factor, int maxGain)
        {
            int current = soldiers.Count;
            int target = Mathf.Min(Mathf.FloorToInt(current * Mathf.Max(1f, factor)), current + Mathf.Max(0, maxGain));
            SetCount(target);
            return soldiers.Count - current;
        }

        public void AddDamageBonus(float bonus, float duration)
        {
            if (duration <= 0f) permanentDamage *= 1f + bonus;
            else buffs.Add(new Buff { Damage = true, Amount = bonus, Remaining = duration });
            RecomputeBuffs();
        }

        public void AddAttackSpeedBonus(float bonus, float duration)
        {
            if (duration <= 0f) permanentAttackSpeed += bonus;
            else buffs.Add(new Buff { Damage = false, Amount = bonus, Remaining = duration });
            RecomputeBuffs();
        }

        // Sobe o nível de uma classe (ou de todas). Tropas existentes ganham vida proporcional; dano usa o nível atual.
        public void LevelUp(bool allClasses, UnitClass unitClass, int levels)
        {
            for (int c = 0; c < ArmyConfig.ClassCount; c++)
            {
                if (!allClasses && c != (int)unitClass) continue;
                float before = LevelMultiplier((UnitClass)c);
                SetClassLevel((UnitClass)c, classLevels[c] + levels);
                float ratio = LevelMultiplier((UnitClass)c) / before;
                foreach (Soldier soldier in byClass[c]) soldier.ScaleMaxHealth(ratio);
            }
        }

        private void RecomputeBuffs()
        {
            timedDamage = 0f;
            timedAttackSpeed = 0f;
            foreach (Buff buff in buffs)
            {
                if (buff.Damage) timedDamage += buff.Amount;
                else timedAttackSpeed += buff.Amount;
            }
        }

        private void TickBuffs(float dt)
        {
            if (buffs.Count == 0) return;
            bool changed = false;
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                Buff buff = buffs[i];
                buff.Remaining -= dt;
                if (buff.Remaining <= 0f)
                {
                    buffs.RemoveAt(i);
                    changed = true;
                    continue;
                }
                buffs[i] = buff;
            }
            if (changed) RecomputeBuffs();
        }

        // Adiciona tropas de uma classe específica (portais e invocações). Respeita limite total e por classe.
        public int AddTroops(UnitClass unitClass, int amount)
        {
            int previous = soldiers.Count;
            int room = Mathf.Max(0, Mathf.Min(Config.maxSoldiers - soldiers.Count, ClassCap(unitClass) - CountOf(unitClass)));
            int added = Mathf.Min(room, Mathf.Max(0, amount));
            for (int i = 0; i < added; i++) SpawnSoldier(unitClass);
            if (added > 0)
            {
                RebuildFormation();
                CountChanged?.Invoke(previous, soldiers.Count);
            }
            return added;
        }

        public void HandleSoldierDeath(Soldier soldier)
        {
            int index = soldiers.IndexOf(soldier);
            if (index < 0) return;
            int previous = soldiers.Count;
            RemoveSoldierAt(index, true);
            formationDirty = true;
            CountChanged?.Invoke(previous, soldiers.Count);
        }

        public void PlayVictory()
        {
            for (int i = 0; i < soldiers.Count; i++) soldiers[i].Celebrate();
        }

        public Vector3 Centroid()
        {
            if (soldiers.Count == 0) return commander != null ? commander.position : Vector3.zero;
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < soldiers.Count; i++) sum += soldiers[i].transform.position;
            Vector3 c = sum / soldiers.Count;
            c.y = 0f;
            return c;
        }

        public Soldier FindNearestSoldier(Vector3 from, float maxRange)
        {
            float best = maxRange >= float.MaxValue ? float.PositiveInfinity : maxRange * maxRange;
            Soldier result = null;
            for (int i = 0; i < soldiers.Count; i++)
            {
                Soldier soldier = soldiers[i];
                Vector3 delta = soldier.transform.position - from;
                delta.y = 0f;
                float sq = delta.sqrMagnitude;
                if (sq < best)
                {
                    best = sq;
                    result = soldier;
                }
            }
            return result;
        }

        public Soldier FindMostInjured(Vector3 from, float range)
        {
            float rangeSq = range * range;
            float lowest = 0.98f;
            Soldier result = null;
            for (int i = 0; i < soldiers.Count; i++)
            {
                Soldier soldier = soldiers[i];
                float ratio = soldier.HealthRatio;
                if (ratio >= lowest) continue;
                Vector3 delta = soldier.transform.position - from;
                delta.y = 0f;
                if (delta.sqrMagnitude > rangeSq) continue;
                lowest = ratio;
                result = soldier;
            }
            return result;
        }

        // ---------- Efeitos em grupo (habilidades e chefões) ----------

        public int DamageInRadius(Vector3 center, float radius, DamageInfo info, float stun = 0f,
            float burnDps = 0f, float burnSeconds = 0f, float slowFactor = 1f, float slowSeconds = 0f)
        {
            float radiusSq = radius * radius;
            int hits = 0;
            for (int i = soldiers.Count - 1; i >= 0; i--)
            {
                if (i >= soldiers.Count) continue;
                Soldier soldier = soldiers[i];
                Vector3 delta = soldier.transform.position - center;
                delta.y = 0f;
                if (delta.sqrMagnitude > radiusSq) continue;
                if (stun > 0f) soldier.ApplyStun(stun);
                if (burnSeconds > 0f) soldier.ApplyBurn(burnDps, burnSeconds);
                if (slowSeconds > 0f) soldier.ApplySlow(slowFactor, slowSeconds);
                soldier.TakeDamage(info);
                hits++;
            }
            return hits;
        }

        public int DamageInRing(Vector3 center, float innerRadius, float outerRadius, DamageInfo info, HashSet<Soldier> alreadyHit)
        {
            float innerSq = innerRadius * innerRadius;
            float outerSq = outerRadius * outerRadius;
            int hits = 0;
            for (int i = soldiers.Count - 1; i >= 0; i--)
            {
                if (i >= soldiers.Count) continue;
                Soldier soldier = soldiers[i];
                Vector3 delta = soldier.transform.position - center;
                delta.y = 0f;
                float sq = delta.sqrMagnitude;
                if (sq < innerSq || sq > outerSq) continue;
                if (alreadyHit != null && !alreadyHit.Add(soldier)) continue;
                soldier.TakeDamage(info);
                hits++;
            }
            return hits;
        }

        public int DamageInLine(Vector3 origin, Vector3 direction, float length, float halfWidth, DamageInfo info, float burnDps = 0f, float burnSeconds = 0f)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-5f) return 0;
            direction.Normalize();
            int hits = 0;
            for (int i = soldiers.Count - 1; i >= 0; i--)
            {
                if (i >= soldiers.Count) continue;
                Soldier soldier = soldiers[i];
                Vector3 delta = soldier.transform.position - origin;
                delta.y = 0f;
                float along = Vector3.Dot(delta, direction);
                if (along < 0f || along > length) continue;
                if ((delta - direction * along).sqrMagnitude > halfWidth * halfWidth) continue;
                if (burnSeconds > 0f) soldier.ApplyBurn(burnDps, burnSeconds);
                soldier.TakeDamage(info);
                hits++;
            }
            return hits;
        }

        public void ApplyToAll(System.Action<Soldier> action)
        {
            for (int i = soldiers.Count - 1; i >= 0; i--)
            {
                if (i < soldiers.Count) action(soldiers[i]);
            }
        }

        public void ShieldAll(float reduction, float duration)
        {
            for (int i = 0; i < soldiers.Count; i++) soldiers[i].ApplyShield(reduction, duration);
        }

        public void HealAllPercent(float fraction)
        {
            for (int i = 0; i < soldiers.Count; i++) soldiers[i].Heal(soldiers[i].MaxHealth * fraction);
        }

        public void SlowAll(float factor, float duration)
        {
            for (int i = 0; i < soldiers.Count; i++) soldiers[i].ApplySlow(factor, duration);
        }

        public string Describe()
        {
            var parts = new System.Text.StringBuilder(64);
            for (int i = 0; i < ArmyConfig.ClassCount; i++)
            {
                if (byClass[i].Count == 0) continue;
                if (parts.Length > 0) parts.Append(", ");
                parts.Append(byClass[i].Count).Append(' ').Append(ArmyConfig.ClassName((UnitClass)i));
            }
            return parts.ToString();
        }

        // ---------- Projéteis ----------

        public void FireArrow(Vector3 origin, Combatant target, DamageInfo damage)
        {
            if (target == null || !target.IsAlive) return;
            arrowPool.Get().Launch(origin, target, damage, Config.projectileSpeed, 0f, releaseArrow);
        }

        public void FireArrowAtPoint(Vector3 origin, Vector3 point, DamageInfo damage, float radius)
        {
            arrowPool.Get().LaunchAtPoint(origin, point, damage, Config.projectileSpeed * 1.2f, radius, releaseArrow);
        }

        public void FireOrb(Vector3 origin, Combatant target, DamageInfo damage, float radius)
        {
            if (target == null || !target.IsAlive) return;
            orbPool.Get().Launch(origin, target, damage, Config.orbSpeed, Mathf.Max(0.5f, radius), releaseOrb);
        }

        // ---------- Ciclo ----------

        private void Update()
        {
            if (commander == null) return;
            float dt = Time.deltaTime;
            corpses.Tick(dt);
            TickBuffs(dt);
            if (formationDirty) RebuildFormation();

            Vector3 anchor = commander.position;
            anchor.y = 0f;
            float leash = FrontLeash;
            for (int i = soldiers.Count - 1; i >= 0; i--)
            {
                if (i >= soldiers.Count) continue;
                soldiers[i].Tick(dt, anchor, halfWidth, leash);
            }
        }

        // Linha de frente à frente do comandante (cavaleiros mais à frente), retaguarda atrás (curandeiras no fundo).
        private void RebuildFormation()
        {
            formationDirty = false;
            float spacing = Config.formationSpacing * (Stance == FormationStance.Defensive ? 0.85f : 1f);
            float rowSpacing = spacing * 0.9f;
            float maxLateral = Mathf.Max(0.5f, halfWidth - 0.6f);
            int columns = Mathf.Max(1, Mathf.FloorToInt(maxLateral * 2f / spacing) + 1);

            int frontCount = 0;
            foreach (UnitClass unitClass in FrontOrder) frontCount += byClass[(int)unitClass].Count;
            int frontRows = Mathf.CeilToInt(frontCount / (float)columns);
            float frontStart = Stance == FormationStance.Aggressive ? 1.4f : 0.9f;

            int index = 0;
            float maxDepth = 0f;
            foreach (UnitClass unitClass in FrontOrder)
            {
                List<Soldier> list = byClass[(int)unitClass];
                for (int i = 0; i < list.Count; i++, index++)
                {
                    int row = index / columns;
                    float z = frontStart + (frontRows - 1 - row) * rowSpacing;
                    list[i].SetSlot(new Vector3(RowX(index, row, frontCount, columns, spacing, maxLateral), 0f, z));
                    maxDepth = Mathf.Max(maxDepth, z);
                }
            }

            int backCount = soldiers.Count - frontCount;
            index = 0;
            float minDepth = 0f;
            foreach (UnitClass unitClass in BackOrder)
            {
                List<Soldier> list = byClass[(int)unitClass];
                for (int i = 0; i < list.Count; i++, index++)
                {
                    int row = index / columns;
                    float z = -1.1f - row * rowSpacing;
                    list[i].SetSlot(new Vector3(RowX(index, row, backCount, columns, spacing, maxLateral), 0f, z));
                    minDepth = Mathf.Min(minDepth, z);
                }
            }

            float lateral = Mathf.Min(maxLateral, (Mathf.Min(columns, Mathf.Max(frontCount, backCount)) - 1) * spacing * 0.5f);
            FormationRadius = Mathf.Max(lateral, (maxDepth - minDepth) * 0.5f);
        }

        private static float RowX(int index, int row, int total, int columns, float spacing, float maxLateral)
        {
            int inRow = Mathf.Min(columns, total - row * columns);
            int column = index % columns;
            float x = (column - (inRow - 1) * 0.5f) * spacing + (row % 2 == 1 ? spacing * 0.25f : 0f);
            return Mathf.Clamp(x, -maxLateral, maxLateral);
        }

        private void ApplyWeights(int compositionIndex)
        {
            CompositionIndex = Mathf.Clamp(compositionIndex, 0, Config.compositions.Count - 1);
            float[] source = Config.compositions[CompositionIndex].weights;
            float total = 0f;
            for (int i = 0; i < ArmyConfig.ClassCount; i++) total += unlockedClasses[i] ? Mathf.Max(0f, source[i]) : 0f;
            for (int i = 0; i < ArmyConfig.ClassCount; i++)
                weights[i] = total > 0f && unlockedClasses[i] ? Mathf.Max(0f, source[i]) / total : 0f;
            if (total <= 0f)
            {
                weights[(int)UnitClass.Warrior] = 0.5f;
                weights[(int)UnitClass.Archer] = 0.5f;
            }
        }

        private UnitClass PickClassToAdd()
        {
            int next = soldiers.Count + 1;
            int best = (int)UnitClass.Warrior;
            float bestDeficit = float.MinValue;
            for (int i = 0; i < ArmyConfig.ClassCount; i++)
            {
                if (weights[i] <= 0f || byClass[i].Count >= Config.classCaps[i]) continue;
                float deficit = weights[i] * next - byClass[i].Count;
                if (deficit > bestDeficit)
                {
                    bestDeficit = deficit;
                    best = i;
                }
            }
            return (UnitClass)best;
        }

        private UnitClass PickClassToRemove()
        {
            int total = soldiers.Count;
            int best = -1;
            float bestSurplus = float.MinValue;
            for (int i = 0; i < ArmyConfig.ClassCount; i++)
            {
                if (byClass[i].Count == 0) continue;
                float surplus = byClass[i].Count - weights[i] * total;
                if (surplus > bestSurplus)
                {
                    bestSurplus = surplus;
                    best = i;
                }
            }
            return best < 0 ? UnitClass.Warrior : (UnitClass)best;
        }

        private void SpawnSoldier(UnitClass unitClass)
        {
            Soldier soldier = pools[(int)unitClass].Get();
            Vector3 origin = commander.position;
            soldier.transform.SetPositionAndRotation(
                new Vector3(origin.x + Random.Range(-0.6f, 0.6f), 0f, origin.z + Random.Range(-1f, 0f)),
                Quaternion.identity);
            soldier.Initialize(this, Config.Get(unitClass), LevelMultiplier(unitClass));
            soldiers.Add(soldier);
            byClass[(int)unitClass].Add(soldier);
        }

        private void RemoveSoldierOf(UnitClass unitClass)
        {
            List<Soldier> list = byClass[(int)unitClass];
            if (list.Count == 0) return;
            int index = soldiers.IndexOf(list[list.Count - 1]);
            if (index >= 0) RemoveSoldierAt(index, false);
        }

        private void RemoveSoldierAt(int index, bool asCorpse)
        {
            Soldier soldier = soldiers[index];
            soldiers.RemoveAt(index);
            byClass[(int)soldier.UnitClass].Remove(soldier);
            if (asCorpse) corpses.Add(soldier);
            else pools[(int)soldier.UnitClass].Release(soldier);
        }

        private void ReleaseCorpse(Combatant body)
        {
            if (body is Soldier soldier) pools[(int)soldier.UnitClass].Release(soldier);
        }

        private ObjectPool<Soldier> CreateSoldierPool(UnitClass unitClass)
        {
            return new ObjectPool<Soldier>(() => CreateSoldier(unitClass),
                s => s.gameObject.SetActive(true),
                s => s.gameObject.SetActive(false),
                s => { if (s != null) Destroy(s.gameObject); },
                false, 16, Mathf.Max(32, Config.maxSoldiers));
        }

        private Soldier CreateSoldier(UnitClass unitClass)
        {
            Color baseColor = Config.Get(unitClass).color;
            Color tunic = skinTint == Color.white ? baseColor : Color.Lerp(baseColor, skinTint, 0.6f);
            GameObject go = CharacterFactory.CreateSoldier(unitClass, poolRoot, tunic);
            go.SetActive(false);
            return go.AddComponent<Soldier>();
        }

        private Projectile CreateArrow()
        {
            var go = new GameObject("Arrow");
            go.transform.SetParent(poolRoot, false);
            PrimitiveFactory.Part(PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(0.07f, 0.07f, 0.55f),
                MaterialLibrary.Get(new Color(1f, 0.85f, 0.35f), 2.5f, 0f, false), default, false);
            go.SetActive(false);
            Projectile projectile = go.AddComponent<Projectile>();
            projectile.ConfigureTrail(arrowTrailMaterial, new Color(1f, 0.92f, 0.55f), new Color(1f, 0.55f, 0.15f), 0.14f);
            projectile.ConfigureImpact(true, SoundId.ArrowImpact, new Color(1f, 0.8f, 0.4f, 0.8f));
            return projectile;
        }

        private Projectile CreateOrb()
        {
            var go = new GameObject("ArcaneOrb");
            go.transform.SetParent(poolRoot, false);
            PrimitiveFactory.Part(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 0.32f,
                MaterialLibrary.Get(OrbColor, 3f, 0f, false), default, false);
            go.SetActive(false);
            Projectile projectile = go.AddComponent<Projectile>();
            projectile.ConfigureTrail(orbTrailMaterial, new Color(0.8f, 0.6f, 1f), new Color(0.3f, 0.2f, 1f), 0.3f);
            projectile.ConfigureImpact(false, SoundId.MagicCast, new Color(0.7f, 0.45f, 1f, 0.85f));
            return projectile;
        }

        private void ResolveTrailMaterials()
        {
            CharacterVisualLibrary library = CharacterFactory.Library;
            if (library != null && library.arrowTrailMaterial != null)
            {
                arrowTrailMaterial = library.arrowTrailMaterial;
                ownsArrowTrail = false;
            }
            else
            {
                arrowTrailMaterial = MaterialLibrary.CreateGlow(ProceduralTextures.SoftCircle, new Color(1f, 0.85f, 0.4f, 1f), true, 2f);
                ownsArrowTrail = arrowTrailMaterial != null;
            }
            orbTrailMaterial = MaterialLibrary.CreateGlow(ProceduralTextures.SoftCircle, Color.white, true, 2.4f);
        }

        private void OnDestroy()
        {
            if (ownsArrowTrail && arrowTrailMaterial != null) Destroy(arrowTrailMaterial);
            if (orbTrailMaterial != null) Destroy(orbTrailMaterial);
        }
    }
}
