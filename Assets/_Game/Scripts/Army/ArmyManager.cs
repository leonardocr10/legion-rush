using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace LegionRush
{
    public class ArmyManager : MonoBehaviour
    {
        private const float GoldenAngle = 2.39996323f;

        public ArmyConfig Config { get; private set; }
        public int Count => soldiers.Count;
        public float FormationRadius { get; private set; }
        public float DamageMultiplier { get; set; } = 1f;
        public float AttackIntervalMultiplier { get; set; } = 1f;
        public float HealthMultiplier { get; set; } = 1f;
        public event System.Action<int, int> CountChanged;

        private readonly List<Soldier> soldiers = new List<Soldier>(256);
        private ObjectPool<Soldier> archerPool;
        private ObjectPool<Soldier> warriorPool;
        private ObjectPool<Projectile> projectilePool;
        private System.Action<Projectile> releaseProjectile;
        private CorpseQueue corpses;
        private Transform commander;
        private Transform poolRoot;
        private Material trailMaterial;
        private bool ownsTrailMaterial;
        private float halfWidth;
        private int archerCount;
        private bool formationDirty;

        public void Initialize(ArmyConfig config, Transform commanderTransform, float trackHalfWidth)
        {
            Config = config;
            commander = commanderTransform;
            halfWidth = trackHalfWidth;
            poolRoot = new GameObject("Pool").transform;
            poolRoot.SetParent(transform, false);
            trailMaterial = ResolveTrailMaterial();
            corpses = new CorpseQueue(ReleaseCorpse);

            archerPool = CreateSoldierPool(UnitClass.Archer);
            warriorPool = CreateSoldierPool(UnitClass.Warrior);
            projectilePool = new ObjectPool<Projectile>(CreateProjectile,
                p => p.gameObject.SetActive(true),
                p => p.gameObject.SetActive(false),
                p => { if (p != null) Destroy(p.gameObject); },
                false, config.projectilePoolSize, Mathf.Max(config.projectilePoolSize, 512));
            releaseProjectile = p => projectilePool.Release(p);

            SetCount(config.startingSoldiers);
            GameLog.Info($"Army ready: {Count} soldiers (max {config.maxSoldiers}).");
        }

        public void SetCount(int target)
        {
            target = Mathf.Clamp(target, 0, Config.maxSoldiers);
            int previous = soldiers.Count;
            while (soldiers.Count < target) SpawnSoldier();
            while (soldiers.Count > target) RemoveSoldierAt(soldiers.Count - 1, false);
            RebuildFormation();
            if (previous != soldiers.Count) CountChanged?.Invoke(previous, soldiers.Count);
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

        public void FireProjectile(Vector3 origin, Combatant target, float damage)
        {
            if (target == null || !target.IsAlive) return;
            projectilePool.Get().Launch(origin, target, damage, Config.projectileSpeed, releaseProjectile);
        }

        private void Update()
        {
            if (commander == null) return;
            float dt = Time.deltaTime;
            corpses.Tick(dt);
            if (formationDirty) RebuildFormation();

            Vector3 anchor = commander.position;
            anchor.y = 0f;
            for (int i = soldiers.Count - 1; i >= 0; i--)
            {
                if (i >= soldiers.Count) continue;
                soldiers[i].Tick(dt, anchor, halfWidth);
            }
        }

        private void RebuildFormation()
        {
            formationDirty = false;
            int n = soldiers.Count;
            float spacing = Config.formationSpacing;
            float rawRadius = spacing * Mathf.Sqrt(n + 1);
            float maxLateral = Mathf.Max(0.5f, halfWidth - 0.6f);
            float xScale = rawRadius > maxLateral ? maxLateral / rawRadius : 1f;
            float zScale = Mathf.Min(3f, 0.9f / xScale);

            for (int i = 0; i < n; i++)
            {
                float r = spacing * Mathf.Sqrt(i + 1);
                float a = i * GoldenAngle;
                soldiers[i].SetSlot(new Vector3(Mathf.Cos(a) * r * xScale, 0f, Mathf.Sin(a) * r * zScale - 0.8f));
            }
            FormationRadius = rawRadius * Mathf.Max(xScale, zScale);
        }

        private void SpawnSoldier()
        {
            int nextTotal = soldiers.Count + 1;
            bool archer = archerCount < Mathf.RoundToInt(nextTotal * Config.archerRatio);
            Soldier soldier = archer ? archerPool.Get() : warriorPool.Get();
            Vector3 origin = commander.position;
            soldier.transform.SetPositionAndRotation(
                new Vector3(origin.x + Random.Range(-0.6f, 0.6f), 0f, origin.z + Random.Range(-1f, 0f)),
                Quaternion.identity);
            soldier.Initialize(this, Config.Get(archer ? UnitClass.Archer : UnitClass.Warrior));
            soldiers.Add(soldier);
            if (archer) archerCount++;
        }

        private void RemoveSoldierAt(int index, bool asCorpse)
        {
            Soldier soldier = soldiers[index];
            soldiers.RemoveAt(index);
            if (soldier.UnitClass == UnitClass.Archer) archerCount--;
            if (asCorpse) corpses.Add(soldier);
            else ReleaseToPool(soldier);
        }

        private void ReleaseCorpse(Combatant body)
        {
            if (body is Soldier soldier) ReleaseToPool(soldier);
        }

        private void ReleaseToPool(Soldier soldier)
        {
            if (soldier.UnitClass == UnitClass.Archer) archerPool.Release(soldier);
            else warriorPool.Release(soldier);
        }

        private ObjectPool<Soldier> CreateSoldierPool(UnitClass unitClass)
        {
            return new ObjectPool<Soldier>(() => CreateSoldier(unitClass),
                s => s.gameObject.SetActive(true),
                s => s.gameObject.SetActive(false),
                s => { if (s != null) Destroy(s.gameObject); },
                false, 32, Mathf.Max(32, Config.maxSoldiers));
        }

        private Soldier CreateSoldier(UnitClass unitClass)
        {
            GameObject go = CharacterFactory.CreateSoldier(unitClass, poolRoot, Config.Get(unitClass).color);
            go.SetActive(false);
            return go.AddComponent<Soldier>();
        }

        private Projectile CreateProjectile()
        {
            var go = new GameObject("Arrow");
            go.transform.SetParent(poolRoot, false);
            PrimitiveFactory.Part(PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(0.07f, 0.07f, 0.55f),
                MaterialLibrary.Get(new Color(1f, 0.85f, 0.35f), 2.5f, 0f, false), default, false);
            go.SetActive(false);
            Projectile projectile = go.AddComponent<Projectile>();
            projectile.ConfigureTrail(trailMaterial);
            return projectile;
        }

        private Material ResolveTrailMaterial()
        {
            CharacterVisualLibrary library = CharacterFactory.Library;
            if (library != null && library.arrowTrailMaterial != null)
            {
                ownsTrailMaterial = false;
                return library.arrowTrailMaterial;
            }
            Material runtime = MaterialLibrary.CreateGlow(ProceduralTextures.SoftCircle, new Color(1f, 0.85f, 0.4f, 1f), true, 2f);
            ownsTrailMaterial = runtime != null;
            return runtime;
        }

        private void OnDestroy()
        {
            if (ownsTrailMaterial && trailMaterial != null) Destroy(trailMaterial);
        }
    }
}
