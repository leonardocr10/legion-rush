using UnityEngine;

namespace LegionRush
{
    public class Enemy : Combatant
    {
        private const float RetargetInterval = 0.3f;
        private const float MeleeReachTolerance = 0.6f;

        private EnemyStats stats;
        private LevelManager level;
        private ArmyManager army;
        private Soldier target;
        private float cooldown;
        private float retargetTimer;
        private float bobPhase;
        private bool impactPending;
        private float impactTimer;
        private Soldier impactTarget;

        public bool IsAggro { get; private set; }

        public void Initialize(EnemyStats enemyStats, float healthMultiplier, LevelManager owner, ArmyManager targetArmy)
        {
            stats = enemyStats;
            level = owner;
            army = targetArmy;
            SetupCombatant(stats.maxHealth * Mathf.Max(0.1f, healthMultiplier));
            cooldown = Random.Range(0f, stats.attackInterval);
            retargetTimer = Random.Range(0f, RetargetInterval);
            bobPhase = Random.Range(0f, 10f);
            IsAggro = false;
            target = null;
            impactPending = false;
            impactTarget = null;
        }

        public void Tick(float deltaTime, Vector3 commanderPosition)
        {
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
            if (target == null || !target.IsAlive)
            {
                TickVisual(deltaTime, 0f, 0f);
                return;
            }

            Vector3 toTarget = target.transform.position - position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            float moveRatio = 0f;
            if (distance > stats.attackRange)
            {
                float stepLength = Mathf.Min(stats.moveSpeed * deltaTime, distance - stats.attackRange * 0.9f);
                transform.position = position + toTarget / distance * stepLength;
                moveRatio = 1f;
            }
            if (distance > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget),
                    1f - Mathf.Exp(-10f * deltaTime));
            }

            bobPhase += deltaTime * (6f + 8f * moveRatio);
            TickVisual(deltaTime, moveRatio, Mathf.Abs(Mathf.Sin(bobPhase)) * (0.02f + 0.07f * moveRatio));

            cooldown -= deltaTime;
            if (!impactPending && cooldown <= 0f && distance <= stats.attackRange + 0.15f)
            {
                PlayAttackAnimation(1f);
                cooldown = stats.attackInterval;
                impactTarget = target;
                impactTimer = ImpactDelay;
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

            Vector3 delta = victim.transform.position - transform.position;
            delta.y = 0f;
            float reach = stats.attackRange + MeleeReachTolerance;
            if (delta.sqrMagnitude > reach * reach) return;
            Vector3 hitPosition = victim.transform.position;
            victim.TakeDamage(stats.damage);
            GameAudio.PlayAt(SoundId.SwordHit, hitPosition);
        }

        protected override void OnDeath() => level.HandleEnemyDeath(this);
    }
}
