using UnityEngine;

namespace LegionRush
{
    // Unidade aliada. Comportamento por AttackKind: corpo a corpo (guerreiro, lanceiro com perfuração,
    // cavaleiro com investida), projétil (arqueiro), magia em área (mago) e cura (curandeira).
    public class Soldier : Combatant
    {
        private const float RetargetInterval = 0.25f;
        private const float MeleeReachTolerance = 0.6f;
        private const float ChargeRearmSeconds = 2f;
        private static readonly Color HealColor = new Color(0.45f, 1f, 0.55f, 0.8f);

        private ArmyManager army;
        private UnitStats stats;
        private Vector3 slotOffset;
        private Combatant target;
        private Soldier healTarget;
        private float levelMultiplier = 1f;
        private float cooldown;
        private float retargetTimer;
        private float bobPhase;
        private bool impactPending;
        private float impactTimer;
        private Combatant impactTarget;
        private DamageInfo impactDamage;
        private bool celebrating;
        private bool chargeReady;
        private float idleTime;

        public UnitClass UnitClass => stats.unitClass;
        public TroopRole Role => stats.role;
        public UnitStats Stats => stats;

        protected override float ArmorValue => Armor * army.ArmorMultiplier;

        public void Initialize(ArmyManager owner, UnitStats unitStats, float level)
        {
            army = owner;
            stats = unitStats;
            levelMultiplier = Mathf.Max(0.1f, level);
            SetupCombatant(stats.maxHealth * army.HealthMultiplier * levelMultiplier, stats.armor, stats.bodyRadius);
            cooldown = Random.Range(0f, stats.attackInterval);
            retargetTimer = Random.Range(0f, RetargetInterval);
            bobPhase = Random.Range(0f, 10f);
            target = null;
            healTarget = null;
            impactPending = false;
            impactTarget = null;
            celebrating = false;
            chargeReady = stats.chargeMultiplier > 1f;
            idleTime = 0f;
        }

        public void SetSlot(Vector3 offset) => slotOffset = offset;

        public void Celebrate()
        {
            celebrating = true;
            target = null;
            healTarget = null;
            impactPending = false;
            impactTarget = null;
            if (AnimDriver != null) AnimDriver.PlayVictory();
            else Punch(1f);
        }

        public void Tick(float deltaTime, Vector3 anchor, float halfWidth, float frontLeash)
        {
            TickStatus(deltaTime);
            if (!IsAlive) return;

            if (impactPending)
            {
                impactTimer -= deltaTime;
                if (impactTimer <= 0f) ResolveImpact();
            }

            Vector3 position = transform.position;
            bool melee = stats.attackKind == AttackKind.Melee;
            bool healer = stats.attackKind == AttackKind.Heal;

            if (!celebrating)
            {
                retargetTimer -= deltaTime;
                bool lost = healer ? healTarget != null && (!healTarget.IsAlive || healTarget.HealthRatio >= 0.99f)
                                   : target != null && !target.IsAlive;
                if (retargetTimer <= 0f || lost)
                {
                    if (healer) healTarget = army.FindMostInjured(position, stats.attackRange);
                    else target = CombatRegistry.FindNearestHostile(position, melee ? frontLeash : stats.attackRange, melee);
                    retargetTimer = RetargetInterval;
                }
            }

            Vector3 desired = anchor + slotOffset;
            float targetDistance = float.MaxValue;
            if (target != null)
            {
                Vector3 toTarget = target.transform.position - position;
                toTarget.y = 0f;
                float centerDistance = toTarget.magnitude;
                targetDistance = centerDistance - target.BodyRadius;
                if (melee)
                {
                    float stopDistance = stats.attackRange * 0.8f + target.BodyRadius;
                    desired = centerDistance > stopDistance
                        ? target.transform.position - toTarget / Mathf.Max(0.001f, centerDistance) * stopDistance
                        : position;
                }
                idleTime = 0f;
            }
            else
            {
                idleTime += deltaTime;
                if (idleTime >= ChargeRearmSeconds && stats.chargeMultiplier > 1f) chargeReady = true;
            }
            if (healTarget != null)
            {
                Vector3 toAlly = healTarget.transform.position - position;
                toAlly.y = 0f;
                targetDistance = toAlly.magnitude;
            }
            desired.x = Mathf.Clamp(desired.x, -halfWidth + 0.3f, halfWidth - 0.3f);
            desired.y = 0f;

            float gap = Vector3.Distance(position, desired);
            float speed = (stats.moveSpeed + gap * 2.5f) * MoveMultiplier;
            Vector3 next = Vector3.MoveTowards(position, desired, speed * deltaTime);
            Vector3 step = next - position;
            transform.position = next;

            Vector3 facing;
            if (celebrating) facing = Vector3.back;
            else if (target != null) facing = target.transform.position - next;
            else facing = step.sqrMagnitude > 1e-6f ? step : Vector3.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude > 1e-4f && !IsStunned)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing),
                    1f - Mathf.Exp(-12f * deltaTime));
            }

            float speedRatio = deltaTime > 0f ? Mathf.Clamp01(step.magnitude / deltaTime / stats.moveSpeed) : 0f;
            bobPhase += deltaTime * (6f + 8f * speedRatio);
            TickVisual(deltaTime, speedRatio, Mathf.Abs(Mathf.Sin(bobPhase)) * (0.02f + 0.08f * speedRatio));
            if (celebrating || IsStunned) return;

            cooldown -= deltaTime;
            if (impactPending || cooldown > 0f) return;
            if (healer)
            {
                if (healTarget != null && healTarget.IsAlive && targetDistance <= stats.attackRange) BeginHeal();
                return;
            }
            if (target != null && target.IsAlive && targetDistance <= stats.attackRange + 0.2f) BeginAttack();
        }

        private void BeginAttack()
        {
            float amount = stats.damage * army.DamageMultiplier * levelMultiplier;
            bool critical = Random.value < stats.critChance;
            if (critical) amount *= stats.critMultiplier;
            if (chargeReady && stats.chargeMultiplier > 1f)
            {
                amount *= stats.chargeMultiplier;
                chargeReady = false;
            }

            float interval = stats.attackInterval * army.AttackIntervalMultiplier;
            cooldown = interval;
            PlayAttackAnimation(stats.attackInterval / Mathf.Max(0.05f, interval));
            if (stats.attackKind == AttackKind.Melee) GameAudio.PlayAt(SoundId.SwordSwing, transform.position);
            else if (stats.attackKind == AttackKind.AreaMagic) GameAudio.PlayAt(SoundId.MagicCast, transform.position);

            impactTarget = target;
            impactDamage = new DamageInfo(amount, stats.damageType, critical);
            impactTimer = ImpactDelay;
            impactPending = true;
            if (impactTimer <= 0f) ResolveImpact();
        }

        private void BeginHeal()
        {
            cooldown = stats.attackInterval * army.AttackIntervalMultiplier;
            PlayAttackAnimation(1f);
            float amount = stats.damage * levelMultiplier * army.HealMultiplier;
            healTarget.Heal(amount);
            Vfx.Burst(healTarget.transform.position + Vector3.up * 0.7f, HealColor, 0.7f, 0.5f);
            GameAudio.PlayAt(SoundId.HealingSpell, healTarget.transform.position);
            if (healTarget.HealthRatio >= 0.99f) healTarget = null;
        }

        // Dano aplicado no frame de impacto da animação (antecipação → impacto → recuperação).
        private void ResolveImpact()
        {
            impactPending = false;
            Combatant victim = impactTarget;
            impactTarget = null;
            if (victim == null || !victim.IsAlive) return;
            Vector3 origin = transform.position + Vector3.up + transform.forward * 0.3f;

            switch (stats.attackKind)
            {
                case AttackKind.Projectile:
                    army.FireArrow(origin, victim, impactDamage);
                    GameAudio.PlayAt(SoundId.ArrowShot, transform.position);
                    return;
                case AttackKind.AreaMagic:
                    army.FireOrb(origin, victim, impactDamage, stats.areaRadius);
                    return;
            }

            Vector3 delta = victim.transform.position - transform.position;
            delta.y = 0f;
            float reach = stats.attackRange + MeleeReachTolerance + victim.BodyRadius;
            if (delta.sqrMagnitude > reach * reach) return;

            if (stats.pierceTargets > 1)
                CombatRegistry.DamageInLine(transform.position, delta, stats.attackRange + 0.8f, 0.55f, impactDamage, stats.pierceTargets);
            else
                victim.TakeDamage(impactDamage);
            GameAudio.PlayAt(SoundId.SwordHit, victim.transform.position);
        }

        protected override void OnDeath() => army.HandleSoldierDeath(this);
    }
}
