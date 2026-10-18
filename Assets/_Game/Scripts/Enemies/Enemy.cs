using UnityEngine;

namespace LegionRush
{
    public class Enemy : Combatant
    {
        private const float RetargetInterval = 0.3f;
        private const float MeleeReachTolerance = 0.6f;
        private const float BlockReduction = 0.7f;
        private static readonly Color RageColor = new Color(1f, 0.25f, 0.1f, 0.8f);
        private static readonly Color BlockColor = new Color(1f, 0.9f, 0.6f, 0.9f);

        private EnemyStats stats;
        private EnemyKinds.Profile profile;
        private LevelManager level;
        private ArmyManager army;
        private Soldier target;
        private float cooldown;
        private float retargetTimer;
        private float bobPhase;
        private bool impactPending;
        private float impactTimer;
        private Soldier impactTarget;
        private bool enraged;
        private bool blockedThisHit;

        public bool IsAggro { get; private set; }
        public EnemyKind Kind { get; private set; }

        public void Initialize(EnemyStats enemyStats, float healthMultiplier, LevelManager owner, ArmyManager targetArmy)
            => Initialize(enemyStats, healthMultiplier, owner, targetArmy, EnemyKind.Soldier);

        // enemyStats já vem com os multiplicadores do tipo (EnemyKinds.Apply).
        public void Initialize(EnemyStats enemyStats, float healthMultiplier, LevelManager owner, ArmyManager targetArmy,
            EnemyKind kind)
        {
            stats = enemyStats;
            Kind = kind;
            profile = EnemyKinds.Get(kind);
            level = owner;
            army = targetArmy;
            SetupCombatant(stats.maxHealth * Mathf.Max(0.1f, healthMultiplier), stats.armor,
                kind == EnemyKind.HeavyWarrior ? 0.4f : 0.32f);
            cooldown = Random.Range(0f, stats.attackInterval);
            retargetTimer = Random.Range(0f, RetargetInterval);
            bobPhase = Random.Range(0f, 10f);
            IsAggro = false;
            target = null;
            impactPending = false;
            impactTarget = null;
            enraged = false;
        }

        public void ForceAggro() => IsAggro = true;

        // Guerreiro Pesado: chance de aparar o golpe com o escudo (avaliado uma vez por golpe recebido).
        protected override float DamageTakenMultiplier
        {
            get
            {
                blockedThisHit = profile.BlockChance > 0f && Random.value < profile.BlockChance;
                return blockedThisHit ? 1f - BlockReduction : 1f;
            }
        }

        protected override void OnDamaged(float amount, DamageInfo info)
        {
            if (blockedThisHit)
            {
                blockedThisHit = false;
                if (AnimDriver != null) AnimDriver.PlayBlock();
                Vfx.Burst(transform.position + Vector3.up * 0.8f + transform.forward * 0.35f, BlockColor, 0.35f, 0.18f);
            }
            if (profile.Rage && !enraged && IsAlive && HealthRatio < 0.5f) EnterRage();
        }

        // Berserker: abaixo de metade da vida entra em fúria (ataques 40% mais rápidos, 25% mais veloz).
        private void EnterRage()
        {
            enraged = true;
            cooldown = Mathf.Min(cooldown, 0.2f);
            if (AnimDriver != null) AnimDriver.PlaySkill();
            Vfx.Burst(transform.position + Vector3.up * 0.7f, RageColor, 1.1f, 0.45f);
            GameAudio.PlayAt(SoundId.BossRoar, transform.position);
        }

        private float AttackInterval => stats.attackInterval * (enraged ? 0.6f : 1f);
        private float SpeedFactor => enraged ? 1.25f : 1f;

        public void Tick(float deltaTime, Vector3 commanderPosition)
        {
            TickStatus(deltaTime);
            if (!IsAlive) return;

            if (impactPending)
            {
                impactTimer -= deltaTime;
                if (impactTimer <= 0f) ResolveImpact();
            }

            Vector3 position = transform.position;
            if (!IsAggro)
            {
                Vector3 toCommander = commanderPosition - position;
                toCommander.y = 0f;
                if (toCommander.sqrMagnitude > stats.aggroRange * stats.aggroRange)
                {
                    TickVisual(deltaTime, 0f, 0f);
                    return;
                }
                IsAggro = true;
            }

            retargetTimer -= deltaTime;
            if (retargetTimer <= 0f || (target != null && !target.IsAlive))
            {
                target = army.FindNearestSoldier(position, float.MaxValue);
                retargetTimer = RetargetInterval;
            }
            if (target == null || !target.IsAlive || IsStunned)
            {
                TickVisual(deltaTime, 0f, 0f);
                return;
            }

            Vector3 toTarget = target.transform.position - position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude - target.BodyRadius;
            float moveRatio = 0f;
            if (distance > stats.attackRange)
            {
                float stepLength = Mathf.Min(stats.moveSpeed * MoveMultiplier * SpeedFactor * deltaTime,
                    distance - stats.attackRange * 0.9f);
                transform.position = position + toTarget.normalized * stepLength;
                moveRatio = MoveMultiplier;
            }
            if (toTarget.sqrMagnitude > 1e-6f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget),
                    1f - Mathf.Exp(-10f * deltaTime));
            }

            bobPhase += deltaTime * (6f + 8f * moveRatio);
            TickVisual(deltaTime, moveRatio, Mathf.Abs(Mathf.Sin(bobPhase)) * (0.02f + 0.07f * moveRatio));

            cooldown -= deltaTime;
            if (!impactPending && cooldown <= 0f && distance <= stats.attackRange + 0.15f)
            {
                PlayAttackAnimation(enraged ? 1.4f : 1f);
                cooldown = AttackInterval;
                impactTarget = target;
                impactTimer = ImpactDelay / (enraged ? 1.4f : 1f);
                impactPending = true;
                if (impactTimer <= 0f) ResolveImpact();
            }
        }

        private void ResolveImpact()
        {
            impactPending = false;
            Soldier victim = impactTarget;
            impactTarget = null;
            if (victim == null || !victim.IsAlive) return;

            if (profile.Ranged)
            {
                Vector3 origin = ProjectileOrigin(transform.position + Vector3.up + transform.forward * 0.3f);
                var hit = new DamageInfo(stats.damage, profile.AreaRadius > 0f ? DamageType.Fire : DamageType.Physical);
                if (profile.AreaRadius > 0f)
                {
                    army.FireEnemyOrb(origin, victim.transform.position, hit, profile.AreaRadius);
                    GameAudio.PlayAt(SoundId.MagicCast, transform.position);
                }
                else
                {
                    army.FireEnemyArrow(origin, victim, hit);
                    GameAudio.PlayAt(SoundId.ArrowShot, transform.position);
                }
                return;
            }

            Vector3 delta = victim.transform.position - transform.position;
            delta.y = 0f;
            float reach = stats.attackRange + MeleeReachTolerance + victim.BodyRadius;
            if (delta.sqrMagnitude > reach * reach) return;
            Vector3 hitPosition = victim.transform.position;
            victim.TakeDamage(new DamageInfo(stats.damage));
            GameAudio.PlayAt(SoundId.SwordHit, hitPosition);
        }

        protected override void OnDeath() => level.HandleEnemyDeath(this);
    }
}
