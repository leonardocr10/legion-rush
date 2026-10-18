using UnityEngine;

namespace LegionRush
{
    public class Soldier : Combatant
    {
        private const float RetargetInterval = 0.25f;
        private const float MeleeReachTolerance = 0.6f;

        private ArmyManager army;
        private UnitStats stats;
        private Vector3 slotOffset;
        private Enemy target;
        private float cooldown;
        private float retargetTimer;
        private float bobPhase;
        private bool impactPending;
        private float impactTimer;
        private Enemy impactTarget;
        private float impactDamage;
        private bool celebrating;

        public UnitClass UnitClass => stats.unitClass;

        public void Initialize(ArmyManager owner, UnitStats unitStats)
        {
            army = owner;
            stats = unitStats;
            SetupCombatant(stats.maxHealth * army.HealthMultiplier);
            cooldown = Random.Range(0f, stats.attackInterval);
            retargetTimer = Random.Range(0f, RetargetInterval);
            bobPhase = Random.Range(0f, 10f);
            target = null;
            impactPending = false;
            impactTarget = null;
            celebrating = false;
        }

        public void SetSlot(Vector3 offset) => slotOffset = offset;

        public void Celebrate()
        {
            celebrating = true;
            target = null;
            impactPending = false;
            impactTarget = null;
            if (AnimDriver != null) AnimDriver.PlayVictory();
            else Punch(1f);
        }

        public void Tick(float deltaTime, Vector3 anchor, float halfWidth)
        {
            if (impactPending)
            {
                impactTimer -= deltaTime;
                if (impactTimer <= 0f) ResolveImpact();
            }

            Vector3 position = transform.position;
            if (!celebrating)
            {
                retargetTimer -= deltaTime;
                if (retargetTimer <= 0f || (target != null && !target.IsAlive))
                {
                    float searchRange = stats.unitClass == UnitClass.Warrior ? army.Config.warriorLeashRange : stats.attackRange;
                    target = CombatRegistry.FindNearestEnemy(position, searchRange);
                    retargetTimer = RetargetInterval;
                }
            }

            Vector3 desired = anchor + slotOffset;
            float targetDistance = float.MaxValue;
            if (target != null)
            {
                Vector3 toTarget = target.transform.position - position;
                toTarget.y = 0f;
                targetDistance = toTarget.magnitude;
                if (stats.unitClass == UnitClass.Warrior)
                {
                    desired = targetDistance > stats.attackRange
                        ? target.transform.position - toTarget / Mathf.Max(0.001f, targetDistance) * (stats.attackRange * 0.8f)
                        : position;
                }
            }
            desired.x = Mathf.Clamp(desired.x, -halfWidth + 0.3f, halfWidth - 0.3f);
            desired.y = 0f;

            float gap = Vector3.Distance(position, desired);
            Vector3 next = Vector3.MoveTowards(position, desired, (stats.moveSpeed + gap * 2.5f) * deltaTime);
            Vector3 step = next - position;
            transform.position = next;

            Vector3 facing;
            if (celebrating) facing = Vector3.back;
            else if (target != null) facing = target.transform.position - next;
            else facing = step.sqrMagnitude > 1e-6f ? step : Vector3.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude > 1e-4f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing),
                    1f - Mathf.Exp(-12f * deltaTime));
            }

            float speedRatio = deltaTime > 0f ? Mathf.Clamp01(step.magnitude / deltaTime / stats.moveSpeed) : 0f;
            bobPhase += deltaTime * (6f + 8f * speedRatio);
            TickVisual(deltaTime, speedRatio, Mathf.Abs(Mathf.Sin(bobPhase)) * (0.02f + 0.08f * speedRatio));
            if (celebrating) return;

            cooldown -= deltaTime;
            if (!impactPending && target != null && target.IsAlive && cooldown <= 0f && targetDistance <= stats.attackRange + 0.2f)
                BeginAttack();
        }

        private void BeginAttack()
        {
            float damage = stats.damage * army.DamageMultiplier;
            if (Random.value < stats.critChance) damage *= stats.critMultiplier;
            float interval = stats.attackInterval * army.AttackIntervalMultiplier;
            cooldown = interval;

            PlayAttackAnimation(stats.attackInterval / Mathf.Max(0.05f, interval));
            if (stats.unitClass == UnitClass.Warrior) GameAudio.PlayAt(SoundId.SwordSwing, transform.position);

            impactTarget = target;
            impactDamage = damage;
            impactTimer = ImpactDelay;
            impactPending = true;
            if (impactTimer <= 0f) ResolveImpact();
        }

        // Dano aplicado no frame de impacto da animação (antecipação → impacto → recuperação).
        private void ResolveImpact()
        {
            impactPending = false;
            Enemy victim = impactTarget;
            impactTarget = null;
            if (victim == null || !victim.IsAlive) return;

            if (stats.unitClass == UnitClass.Archer)
            {
                army.FireProjectile(transform.position + Vector3.up + transform.forward * 0.3f, victim, impactDamage);
                GameAudio.PlayAt(SoundId.ArrowShot, transform.position);
                return;
            }

            Vector3 delta = victim.transform.position - transform.position;
            delta.y = 0f;
            float reach = stats.attackRange + MeleeReachTolerance;
            if (delta.sqrMagnitude > reach * reach) return;
            victim.TakeDamage(impactDamage);
            GameAudio.PlayAt(SoundId.SwordHit, victim.transform.position);
        }

        protected override void OnDeath() => army.HandleSoldierDeath(this);
    }
}
