using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    // Máquina de estados do chefão: Dormente → Intro → (Movendo ⇄ Aviso → Execução → Vulnerável) → Troca de fase → Morte.
    // Todo ataque tem aviso visual antecipado; vários deixam o chefão vulnerável (dano extra) depois de executar.
    public class BossController : Combatant
    {
        public enum BossState { Dormant, Intro, Moving, Telegraph, Recover, PhaseShift, Flying, Dead }

        private const float IntroSeconds = 2.2f;
        private const float PhaseShiftSeconds = 1.6f;
        private const float FlightHeight = 5f;
        private const float DeathSeconds = 3.5f;

        private struct Hazard
        {
            public BossAttackType Type;
            public Vector3 Center;
            public float Age;
            public float Duration;
            public float Reach;
            public float TickTimer;
            public BossAttack Attack;
            public HashSet<Soldier> Hit;
        }

        public BossDefinition Definition { get; private set; }
        public BossState State { get; private set; } = BossState.Dormant;
        public int Phase { get; private set; } = 1;
        public bool IsVulnerable => State == BossState.Recover;
        public bool IsEngaged => State != BossState.Dormant && State != BossState.Dead;
        public string CurrentAttack => currentAttack != null && State == BossState.Telegraph ? currentAttack.name : string.Empty;
        public override bool IsAirborne => State == BossState.Flying;
        public event System.Action<BossController> Awakened;
        public event System.Action<BossController, int> PhaseChanged;

        protected override float DamageTakenMultiplier
        {
            get
            {
                switch (State)
                {
                    case BossState.Dormant:
                    case BossState.Intro:
                    case BossState.PhaseShift:
                    case BossState.Dead:
                        return 0f;
                    case BossState.Recover:
                        return Definition.vulnerableMultiplier;
                    default:
                        return 1f;
                }
            }
        }

        protected override float StunResistance => 1f;

        private readonly List<Hazard> hazards = new List<Hazard>(4);
        private readonly Vector3[] rainPoints = new Vector3[16];
        private LevelManager level;
        private ArmyManager army;
        private ArmyCameraController cameraRig;
        private BossAttack currentAttack;
        private int lastAttackIndex = -1;
        private int pendingPhase = 1;
        private int rainCount;
        private float stateTimer;
        private float stateDuration;
        private float nextAttackTimer;
        private float bobPhase;
        private float flightBaseX;
        private float flightTargetX;
        private bool flightBarrageDone;
        private Vector3 targetPoint;
        private Vector3 lineDirection;
        private float halfWidth;

        public void Initialize(BossDefinition definition, float healthMultiplier, LevelManager owner, ArmyManager targetArmy,
            ArmyCameraController camera, float trackHalfWidth)
        {
            Definition = definition;
            level = owner;
            army = targetArmy;
            cameraRig = camera;
            halfWidth = trackHalfWidth;
            SetupCombatant(definition.maxHealth * Mathf.Max(0.1f, healthMultiplier), definition.armor, definition.bodyRadius);
            State = BossState.Dormant;
            Phase = 1;
            pendingPhase = 1;
            hazards.Clear();
        }

        protected override float ResistanceTo(DamageType type) => Definition.Resistance(type);

        protected override void OnDamaged(float amount, DamageInfo info)
        {
            if (Phase < 3 && HealthRatio <= Definition.phase3At) pendingPhase = 3;
            else if (Phase < 2 && HealthRatio <= Definition.phase2At) pendingPhase = Mathf.Max(pendingPhase, 2);
        }

        public void Tick(float deltaTime, Vector3 commanderPosition)
        {
            if (State == BossState.Dead) return;
            TickStatus(deltaTime);
            if (!IsAlive) return;

            stateTimer += deltaTime;
            UpdateHazards(deltaTime);
            float moveRatio = 0f;
            float speed = Definition.PhaseSpeed(Phase);

            switch (State)
            {
                case BossState.Dormant:
                {
                    Vector3 delta = commanderPosition - transform.position;
                    delta.y = 0f;
                    if (delta.magnitude <= Definition.aggroRange) Awaken();
                    break;
                }
                case BossState.Intro:
                    if (stateTimer >= IntroSeconds) EnterMoving(0.8f);
                    break;
                case BossState.Moving:
                    if (pendingPhase > Phase)
                    {
                        BeginPhaseShift();
                        break;
                    }
                    moveRatio = MoveTowardArmy(deltaTime, speed);
                    nextAttackTimer -= deltaTime * speed;
                    if (nextAttackTimer <= 0f) BeginAttack();
                    break;
                case BossState.Telegraph:
                    FaceToward(targetPoint, deltaTime);
                    if (Visual != null) Visual.localRotation = Quaternion.Euler(-14f * Mathf.Clamp01(stateTimer / stateDuration), 0f, 0f);
                    if (stateTimer >= stateDuration) Execute();
                    break;
                case BossState.Recover:
                    if (Visual != null) Visual.localRotation = Quaternion.Euler(18f, 0f, 6f * Mathf.Sin(stateTimer * 9f));
                    SetFlash(0.25f + 0.2f * Mathf.Sin(stateTimer * 12f));
                    if (stateTimer >= stateDuration) EnterMoving(Definition.attackSpacing * 0.5f);
                    break;
                case BossState.PhaseShift:
                    if (Visual != null) Visual.localRotation = Quaternion.Euler(-10f * Mathf.Sin(Mathf.Clamp01(stateTimer / PhaseShiftSeconds) * Mathf.PI), 0f, 0f);
                    if (stateTimer >= PhaseShiftSeconds) EnterMoving(0.6f);
                    break;
                case BossState.Flying:
                    TickFlight();
                    moveRatio = 1f;
                    break;
            }

            bobPhase += deltaTime * (2f + 4f * moveRatio);
            TickVisual(deltaTime, moveRatio, Mathf.Abs(Mathf.Sin(bobPhase)) * 0.06f * Definition.scale * moveRatio);
        }

        private void Awaken()
        {
            State = BossState.Intro;
            stateTimer = 0f;
            GameAudio.Play(SoundId.BossRoar);
            if (cameraRig != null)
            {
                cameraRig.Shake(0.7f);
                cameraRig.SetZoom(1.3f);
            }
            Vfx.Ring(transform.position, Definition.glowColor, 8f, 1.2f);
            GameLog.Info($"Chefão desperto: {Definition.displayName}.");
            Awakened?.Invoke(this);
        }

        private void EnterMoving(float delay)
        {
            State = BossState.Moving;
            stateTimer = 0f;
            nextAttackTimer = delay;
            if (Visual != null) Visual.localRotation = Quaternion.identity;
        }

        private float MoveTowardArmy(float deltaTime, float speed)
        {
            Vector3 centroid = army.Centroid();
            Vector3 toArmy = centroid - transform.position;
            toArmy.y = 0f;
            float distance = toArmy.magnitude;
            FaceToward(centroid, deltaTime);
            float desired = Definition.preferredRange + army.FormationRadius * 0.5f;
            if (distance <= desired) return 0f;
            float step = Mathf.Min(distance - desired, Definition.moveSpeed * speed * MoveMultiplier * deltaTime);
            Vector3 next = transform.position + toArmy / distance * step;
            next.x = Mathf.Clamp(next.x, -halfWidth + 1f, halfWidth - 1f);
            next.y = 0f;
            transform.position = next;
            return MoveMultiplier;
        }

        private void FaceToward(Vector3 point, float deltaTime)
        {
            Vector3 direction = point - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 1f - Mathf.Exp(-5f * deltaTime));
        }

        private void BeginAttack()
        {
            currentAttack = PickAttack(out lastAttackIndex);
            if (currentAttack == null)
            {
                nextAttackTimer = Definition.attackSpacing;
                return;
            }

            Vector3 origin = transform.position;
            Vector3 centroid = army.Centroid();
            Soldier focus = army.Count > 0 ? army.Soldiers[Random.Range(0, army.Count)] : null;
            targetPoint = focus != null && Random.value < 0.5f ? focus.transform.position : centroid;
            targetPoint.y = 0f;
            lineDirection = targetPoint - origin;
            lineDirection.y = 0f;
            if (lineDirection.sqrMagnitude < 0.01f) lineDirection = Vector3.back;
            lineDirection.Normalize();

            float speed = Mathf.Min(Definition.PhaseSpeed(Phase), 1.3f);
            stateDuration = currentAttack.telegraph / speed;
            stateTimer = 0f;
            State = BossState.Telegraph;
            Color color = currentAttack.color;

            switch (currentAttack.type)
            {
                case BossAttackType.Slam:
                    Vfx.Telegraph(targetPoint, currentAttack.radius, stateDuration, color);
                    break;
                case BossAttackType.Spin:
                    Vfx.Telegraph(origin, currentAttack.radius, stateDuration, color);
                    break;
                case BossAttackType.Shockwave:
                    Vfx.Telegraph(origin, 2.5f, stateDuration, color);
                    Color faint = color;
                    faint.a *= 0.35f;
                    Vfx.Telegraph(origin, currentAttack.length, stateDuration, faint);
                    break;
                case BossAttackType.Rain:
                    PrepareRain(centroid, currentAttack.count);
                    for (int i = 0; i < rainCount; i++) Vfx.Telegraph(rainPoints[i], currentAttack.radius, stateDuration, color);
                    break;
                case BossAttackType.Line:
                    Vfx.TelegraphLine(origin + lineDirection * Definition.bodyRadius, lineDirection, currentAttack.length, currentAttack.radius, stateDuration, color);
                    break;
                case BossAttackType.Storm:
                    Vfx.Telegraph(centroid, army.FormationRadius + 4f, stateDuration, color);
                    break;
                case BossAttackType.Summon:
                    Vfx.Ring(origin, color, 4f, stateDuration);
                    GameAudio.Play(SoundId.BossRoar);
                    break;
                case BossAttackType.Flight:
                    GameAudio.Play(SoundId.BossRoar);
                    break;
            }
        }

        private BossAttack PickAttack(out int index)
        {
            index = -1;
            float total = 0f;
            for (int i = 0; i < Definition.attacks.Count; i++) total += EffectiveWeight(i);
            if (total <= 0f) return null;
            float roll = Random.value * total;
            for (int i = 0; i < Definition.attacks.Count; i++)
            {
                roll -= EffectiveWeight(i);
                if (roll > 0f) continue;
                index = i;
                return Definition.attacks[i];
            }
            index = Definition.attacks.Count - 1;
            return Definition.attacks[index];
        }

        private float EffectiveWeight(int i)
        {
            BossAttack attack = Definition.attacks[i];
            if (attack == null || attack.minPhase > Phase) return 0f;
            return attack.weight * (i == lastAttackIndex ? 0.35f : 1f);
        }

        private void PrepareRain(Vector3 centroid, int count)
        {
            rainCount = Mathf.Clamp(count, 1, rainPoints.Length);
            float spread = army.FormationRadius + 2.5f;
            for (int i = 0; i < rainCount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * spread;
                Vector3 p = centroid + new Vector3(offset.x, 0f, offset.y);
                p.x = Mathf.Clamp(p.x, -halfWidth, halfWidth);
                rainPoints[i] = p;
            }
        }

        private void Execute()
        {
            BossAttack attack = currentAttack;
            var info = new DamageInfo(attack.damage, attack.damageType);
            Vector3 origin = transform.position;
            PlayAttackAnimation(1f);
            if (Visual != null) Visual.localRotation = Quaternion.Euler(12f, 0f, 0f);
            GameAudio.PlayAt(attack.sound, origin);

            switch (attack.type)
            {
                case BossAttackType.Slam:
                    army.DamageInRadius(targetPoint, attack.radius, info, 0.3f, attack.burnDps, attack.burnSeconds,
                        attack.slowFactor, attack.slowFactor < 1f ? attack.duration : 0f);
                    Impact(targetPoint, attack.radius, attack.color, 0.45f);
                    break;
                case BossAttackType.Spin:
                    army.DamageInRadius(origin, attack.radius, info, 0f, attack.burnDps, attack.burnSeconds);
                    Impact(origin, attack.radius, attack.color, 0.3f);
                    break;
                case BossAttackType.Shockwave:
                    AddHazard(BossAttackType.Shockwave, origin, 1f, attack.length, attack);
                    Vfx.Ring(origin, attack.color, attack.length, 1f);
                    Shake(0.5f);
                    break;
                case BossAttackType.Rain:
                    for (int i = 0; i < rainCount; i++)
                    {
                        army.DamageInRadius(rainPoints[i], attack.radius, info, 0f, attack.burnDps, attack.burnSeconds,
                            attack.slowFactor, attack.slowFactor < 1f ? attack.duration : 0f);
                        Vfx.Burst(rainPoints[i] + Vector3.up * 0.5f, attack.color, attack.radius, 0.5f);
                    }
                    Shake(0.35f);
                    break;
                case BossAttackType.Line:
                    army.DamageInLine(origin, lineDirection, attack.length, attack.radius, info, attack.burnDps, attack.burnSeconds);
                    Vfx.Beam(origin + lineDirection * Definition.bodyRadius, lineDirection, attack.length, attack.radius, attack.color, 0.5f);
                    Shake(0.3f);
                    break;
                case BossAttackType.Storm:
                    AddHazard(BossAttackType.Storm, army.Centroid(), attack.duration, army.FormationRadius + 6f, attack);
                    army.SlowAll(attack.slowFactor, attack.duration);
                    break;
                case BossAttackType.Summon:
                    level.SpawnSummons(origin + lineDirection * (Definition.bodyRadius + 2f), attack.count, Definition.summonColor, Definition.summonHealth);
                    Vfx.Ring(origin, attack.color, 5f, 0.7f);
                    break;
                case BossAttackType.Flight:
                    BeginFlight();
                    return;
            }

            if (attack.vulnerableAfter > 0f) EnterRecover(attack.vulnerableAfter);
            else EnterMoving(Definition.attackSpacing);
        }

        private void EnterRecover(float seconds)
        {
            State = BossState.Recover;
            stateTimer = 0f;
            stateDuration = seconds;
        }

        private void BeginFlight()
        {
            State = BossState.Flying;
            stateTimer = 0f;
            stateDuration = currentAttack.duration;
            flightBaseX = transform.position.x;
            flightTargetX = Random.Range(-halfWidth + 1.5f, halfWidth - 1.5f);
            flightBarrageDone = false;
        }

        // Em voo o chefão fica fora do alcance corpo a corpo: só arqueiros, magos e habilidades o atingem.
        private void TickFlight()
        {
            float t = Mathf.Clamp01(stateTimer / stateDuration);
            float height = Mathf.Sin(t * Mathf.PI) * FlightHeight;
            Vector3 p = transform.position;
            p.x = Mathf.Lerp(flightBaseX, flightTargetX, Mathf.SmoothStep(0f, 1f, t));
            p.y = height;
            transform.position = p;
            FaceToward(army.Centroid(), Time.deltaTime);

            if (!flightBarrageDone && t >= 0.45f)
            {
                flightBarrageDone = true;
                PrepareRain(army.Centroid(), currentAttack.count);
                var info = new DamageInfo(currentAttack.damage, currentAttack.damageType);
                for (int i = 0; i < rainCount; i++)
                {
                    Vfx.Telegraph(rainPoints[i], currentAttack.radius, 0.5f, currentAttack.color);
                    army.DamageInRadius(rainPoints[i], currentAttack.radius, info, 0f, currentAttack.burnDps, currentAttack.burnSeconds);
                    Vfx.Burst(rainPoints[i] + Vector3.up * 0.5f, currentAttack.color, currentAttack.radius, 0.6f);
                }
                GameAudio.PlayAt(SoundId.FireExplosion, army.Centroid());
            }

            if (t < 1f) return;
            p.y = 0f;
            transform.position = p;
            army.DamageInRadius(p, Definition.bodyRadius + 2f, new DamageInfo(currentAttack.damage * 0.8f), 0.4f);
            Impact(p, Definition.bodyRadius + 2f, currentAttack.color, 0.6f);
            GameAudio.PlayAt(SoundId.BossStomp, p);
            if (currentAttack.vulnerableAfter > 0f) EnterRecover(currentAttack.vulnerableAfter);
            else EnterMoving(Definition.attackSpacing);
        }

        private void BeginPhaseShift()
        {
            Phase = pendingPhase;
            State = BossState.PhaseShift;
            stateTimer = 0f;
            GameAudio.Play(SoundId.BossRoar);
            Shake(0.8f);
            SetFlash(1f);
            Vfx.Ring(transform.position, Definition.glowColor, 10f, 1.2f);
            Vfx.Burst(transform.position + Vector3.up * Definition.scale * 0.5f, Definition.glowColor, Definition.scale, 0.8f);
            if (Definition.phaseSummonCount > 0)
                level.SpawnSummons(transform.position + transform.forward * (Definition.bodyRadius + 2f), Definition.phaseSummonCount, Definition.summonColor, Definition.summonHealth);
            GameLog.Info($"{Definition.displayName}: fase {Phase}.");
            PhaseChanged?.Invoke(this, Phase);
        }

        private void AddHazard(BossAttackType type, Vector3 center, float duration, float reach, BossAttack attack)
        {
            hazards.Add(new Hazard
            {
                Type = type,
                Center = center,
                Age = 0f,
                Duration = duration,
                Reach = reach,
                TickTimer = 0f,
                Attack = attack,
                Hit = type == BossAttackType.Shockwave ? new HashSet<Soldier>() : null
            });
        }

        private void UpdateHazards(float deltaTime)
        {
            for (int i = hazards.Count - 1; i >= 0; i--)
            {
                Hazard hazard = hazards[i];
                hazard.Age += deltaTime;
                var info = new DamageInfo(hazard.Attack.damage, hazard.Attack.damageType);
                if (hazard.Type == BossAttackType.Shockwave)
                {
                    float radius = hazard.Reach * Mathf.Clamp01(hazard.Age / hazard.Duration);
                    army.DamageInRing(hazard.Center, Mathf.Max(0f, radius - 0.9f), radius + 0.3f, info, hazard.Hit);
                }
                else
                {
                    hazard.TickTimer -= deltaTime;
                    if (hazard.TickTimer <= 0f)
                    {
                        hazard.TickTimer = 0.5f;
                        info.Silent = true;
                        army.DamageInRadius(army.Centroid(), hazard.Reach, info);
                        Vfx.Ring(army.Centroid(), hazard.Attack.color, hazard.Reach, 0.5f);
                    }
                }

                if (hazard.Age >= hazard.Duration)
                {
                    hazards.RemoveAt(i);
                    continue;
                }
                hazards[i] = hazard;
            }
        }

        private void Impact(Vector3 point, float radius, Color color, float shake)
        {
            Vfx.Burst(point + Vector3.up * 0.5f, color, radius, 0.5f);
            Vfx.Ring(point, color, radius * 1.3f, 0.5f);
            Shake(shake);
        }

        private void Shake(float amount)
        {
            if (cameraRig != null) cameraRig.Shake(amount);
        }

        protected override void OnDeath()
        {
            State = BossState.Dead;
            stateTimer = 0f;
            hazards.Clear();
            CombatRegistry.Unregister(this);
            Vfx.Burst(transform.position + Vector3.up * Definition.scale * 0.5f, Definition.glowColor, Definition.scale * 1.2f, 1.2f);
            Vfx.Ring(transform.position, Definition.glowColor, 12f, 1.5f);
            Shake(1f);
            GameAudio.Play(SoundId.BossRoar);
            level.HandleBossDefeated(this);
        }

        private void Update()
        {
            if (State != BossState.Dead) return;
            stateTimer += Time.deltaTime;
            float t = Mathf.Clamp01(stateTimer / DeathSeconds);
            Vector3 p = transform.position;
            p.y = Mathf.MoveTowards(p.y, 0f, Time.deltaTime * 8f);
            if (Visual != null)
            {
                Visual.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 80f, Mathf.SmoothStep(0f, 1f, t * 2.5f)), 0f, 0f);
                Visual.localPosition = Vector3.down * (Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, t)) * Definition.scale);
            }
            transform.position = p;
            if (t >= 1f) gameObject.SetActive(false);
        }
    }
}
