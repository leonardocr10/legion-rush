using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    // Máquina de estados do chefão:
    // Dormant → Intro → (Idle ⇄ Chase → Attack/SpecialAttack [aviso → janela ativa → recuperação] → Stunned) → Enraged (troca de fase) → Dead.
    // Todo ataque tem aviso visual antecipado e só causa dano na janela ativa; vários deixam o chefão atordoado
    // (vulnerável, dano extra). Todos os tempos e valores vêm do BossDefinition (ScriptableObject).
    public class BossController : Combatant
    {
        public enum BossState { Dormant, Intro, Idle, Chase, Attack, SpecialAttack, Stunned, Enraged, Flying, Dead }

        private enum AttackStage { Windup, Active, Recovery }

        private struct Hazard
        {
            public BossAttackType Type;
            public Vector3 Center;
            public Vector3 Direction;
            public float Age;
            public float Duration;
            public float Reach;
            public float TickTimer;
            public float Progress;
            public BossAttack Attack;
            public HashSet<Soldier> Hit;
        }

        public BossDefinition Definition { get; private set; }
        public BossState State { get; private set; } = BossState.Dormant;
        public int Phase { get; private set; } = 1;
        public bool IsVulnerable => State == BossState.Stunned;
        public bool IsEngaged => State != BossState.Dormant && State != BossState.Dead;
        public bool IsAttacking => State == BossState.Attack || State == BossState.SpecialAttack;
        public string CurrentAttack => currentAttack != null && IsAttacking && stage == AttackStage.Windup ? currentAttack.name : string.Empty;
        public override bool IsAirborne => State == BossState.Flying;
        public BossPresentation Presentation => presentation;
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
                    case BossState.Enraged:
                    case BossState.Dead:
                        return 0f;
                    case BossState.Stunned:
                        return Definition.vulnerableMultiplier;
                    default:
                        return 1f;
                }
            }
        }

        protected override float StunResistance => 1f;

        // Distância entre passos visuais da onda que percorre o chão.
        private const float StepSpacing = 1.25f;

        private readonly List<Hazard> hazards = new List<Hazard>(4);
        private readonly List<Vector3> attackPoints = new List<Vector3>(16);
        private readonly HashSet<Soldier> activeHits = new HashSet<Soldier>();
        private LevelManager level;
        private ArmyManager army;
        private ArmyCameraController cameraRig;
        private BossPresentation presentation;
        private BossAttack currentAttack;
        private AttackStage stage;
        private int lastAttackIndex = -1;
        private int pendingPhase = 1;
        private float stateTimer;
        private float stateDuration;
        private float stageTimer;
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
            presentation = GetComponentInChildren<BossPresentation>(true);
            if (presentation != null) presentation.Bind(this);
        }

        protected override float ResistanceTo(DamageType type) => Definition.Resistance(type);

        protected override void OnDamaged(float amount, DamageInfo info)
        {
            if (Phase < 3 && HealthRatio <= Definition.phase3At) pendingPhase = 3;
            else if (Phase < 2 && HealthRatio <= Definition.phase2At) pendingPhase = Mathf.Max(pendingPhase, 2);
            if (presentation != null && !info.Silent && Health > 0f) presentation.OnHit(info);
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
                    if (stateTimer >= Definition.introSeconds) EnterLocomotion(0.8f);
                    break;
                case BossState.Idle:
                case BossState.Chase:
                    if (pendingPhase > Phase)
                    {
                        BeginPhaseShift();
                        break;
                    }
                    moveRatio = MoveTowardArmy(deltaTime, speed);
                    SetState(moveRatio > 0.05f ? BossState.Chase : BossState.Idle, false);
                    nextAttackTimer -= deltaTime * speed;
                    if (nextAttackTimer <= 0f) BeginAttack();
                    break;
                case BossState.Attack:
                case BossState.SpecialAttack:
                    TickAttack(deltaTime);
                    break;
                case BossState.Stunned:
                    if (presentation == null && Visual != null) Visual.localRotation = Quaternion.Euler(18f, 0f, 6f * Mathf.Sin(stateTimer * 9f));
                    SetFlash(0.25f + 0.2f * Mathf.Sin(stateTimer * 12f));
                    if (stateTimer >= stateDuration) EnterLocomotion(Definition.attackSpacing * 0.5f);
                    break;
                case BossState.Enraged:
                    if (presentation == null && Visual != null)
                        Visual.localRotation = Quaternion.Euler(-10f * Mathf.Sin(Mathf.Clamp01(stateTimer / Definition.phaseShiftSeconds) * Mathf.PI), 0f, 0f);
                    if (stateTimer >= Definition.phaseShiftSeconds) EnterLocomotion(0.6f);
                    break;
                case BossState.Flying:
                    TickFlight();
                    moveRatio = 1f;
                    break;
            }

            if (presentation != null)
            {
                presentation.Tick(deltaTime, moveRatio);
                TickVisual(deltaTime, moveRatio, 0f);
                return;
            }
            bobPhase += deltaTime * (2f + 4f * moveRatio);
            TickVisual(deltaTime, moveRatio, Mathf.Abs(Mathf.Sin(bobPhase)) * 0.06f * Definition.scale * moveRatio);
        }

        private void SetState(BossState state, bool resetTimer = true)
        {
            BossState previous = State;
            State = state;
            if (resetTimer) stateTimer = 0f;
            if (previous != state && presentation != null) presentation.OnStateChanged(previous, state);
        }

        private void Awaken()
        {
            SetState(BossState.Intro);
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

        private void EnterLocomotion(float delay)
        {
            SetState(BossState.Idle);
            nextAttackTimer = delay;
            currentAttack = null;
            if (presentation == null && Visual != null) Visual.localRotation = Quaternion.identity;
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

        // ---------- Ataques: aviso → janela ativa → recuperação ----------

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
            if (lineDirection.sqrMagnitude < 0.01f) lineDirection = transform.forward;
            lineDirection.Normalize();

            float speed = Mathf.Min(Definition.PhaseSpeed(Phase), Definition.telegraphSpeedCap);
            stateDuration = currentAttack.telegraph / speed;
            stage = AttackStage.Windup;
            stageTimer = 0f;
            activeHits.Clear();
            SetState(currentAttack.special ? BossState.SpecialAttack : BossState.Attack);
            Color color = currentAttack.color;

            attackPoints.Clear();
            switch (currentAttack.type)
            {
                case BossAttackType.Slam:
                    attackPoints.Add(targetPoint);
                    Vfx.Telegraph(targetPoint, currentAttack.radius, stateDuration, color);
                    break;
                case BossAttackType.Spin:
                    attackPoints.Add(origin);
                    Vfx.Telegraph(origin, currentAttack.radius, stateDuration, color);
                    break;
                case BossAttackType.Shockwave:
                    attackPoints.Add(origin);
                    Vfx.Telegraph(origin, 2.5f, stateDuration, color);
                    Color faint = color;
                    faint.a *= 0.35f;
                    Vfx.Telegraph(origin, currentAttack.length, stateDuration, faint);
                    break;
                case BossAttackType.Rain:
                    PrepareRain(centroid, currentAttack.count);
                    for (int i = 0; i < attackPoints.Count; i++) Vfx.Telegraph(attackPoints[i], currentAttack.radius, stateDuration, color);
                    break;
                case BossAttackType.Line:
                    attackPoints.Add(origin + lineDirection * Definition.bodyRadius);
                    Vfx.TelegraphLine(origin + lineDirection * Definition.bodyRadius, lineDirection, currentAttack.length, currentAttack.radius, stateDuration, color);
                    break;
                case BossAttackType.Storm:
                    attackPoints.Add(centroid);
                    Vfx.Telegraph(centroid, army.FormationRadius + 4f, stateDuration, color);
                    break;
                case BossAttackType.Summon:
                    attackPoints.Add(origin);
                    Vfx.Ring(origin, color, 4f, stateDuration);
                    GameAudio.Play(SoundId.BossRoar);
                    break;
                case BossAttackType.Flight:
                    attackPoints.Add(origin);
                    GameAudio.Play(SoundId.BossRoar);
                    break;
            }

            if (presentation != null)
                presentation.OnAttackWindup(currentAttack, stateDuration, currentAttack.activeTime + currentAttack.recovery, attackPoints, lineDirection);
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
            return attack.weight * (i == lastAttackIndex ? Definition.repeatPenalty : 1f);
        }

        private void PrepareRain(Vector3 centroid, int count)
        {
            int rainCount = Mathf.Clamp(count, 1, 16);
            float spread = army.FormationRadius + 2.5f;
            for (int i = 0; i < rainCount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * spread;
                Vector3 p = centroid + new Vector3(offset.x, 0f, offset.y);
                p.x = Mathf.Clamp(p.x, -halfWidth, halfWidth);
                attackPoints.Add(p);
            }
        }

        private void TickAttack(float deltaTime)
        {
            stageTimer += deltaTime;
            switch (stage)
            {
                case AttackStage.Windup:
                    // Só acompanha o alvo no começo do aviso: o golpe final é legível e desviável.
                    if (stageTimer < stateDuration * 0.6f) FaceToward(targetPoint, deltaTime);
                    if (presentation == null && Visual != null)
                        Visual.localRotation = Quaternion.Euler(-14f * Mathf.Clamp01(stageTimer / stateDuration), 0f, 0f);
                    if (stageTimer >= stateDuration) Execute();
                    break;
                case AttackStage.Active:
                    if (currentAttack.type == BossAttackType.Spin)
                        army.DamageInRing(transform.position, 0f, currentAttack.radius, new DamageInfo(currentAttack.damage, currentAttack.damageType), activeHits);
                    if (stageTimer >= currentAttack.activeTime) EnterStage(AttackStage.Recovery);
                    break;
                case AttackStage.Recovery:
                    if (stageTimer >= currentAttack.recovery) FinishAttack();
                    break;
            }
        }

        private void EnterStage(AttackStage next)
        {
            stage = next;
            stageTimer = 0f;
        }

        private void FinishAttack()
        {
            if (currentAttack.vulnerableAfter > 0f) EnterStunned(currentAttack.vulnerableAfter);
            else EnterLocomotion(Definition.attackSpacing);
        }

        // Fim do aviso: aplica o dano do impacto e abre a janela ativa.
        private void Execute()
        {
            BossAttack attack = currentAttack;
            var info = new DamageInfo(attack.damage, attack.damageType);
            Vector3 origin = transform.position;
            if (presentation == null)
            {
                PlayAttackAnimation(1f);
                if (Visual != null) Visual.localRotation = Quaternion.Euler(12f, 0f, 0f);
            }
            GameAudio.PlayAt(attack.sound, origin);

            switch (attack.type)
            {
                case BossAttackType.Slam:
                    army.DamageInRadius(targetPoint, attack.radius, info, 0.3f, attack.burnDps, attack.burnSeconds,
                        attack.slowFactor, attack.slowFactor < 1f ? attack.duration : 0f);
                    Impact(targetPoint, attack.radius, attack.color, attack.cameraShake);
                    NotifyImpact(attack, targetPoint);
                    break;
                case BossAttackType.Spin:
                    army.DamageInRing(origin, 0f, attack.radius, info, activeHits);
                    Impact(origin, attack.radius, attack.color, attack.cameraShake);
                    NotifyImpact(attack, origin);
                    break;
                case BossAttackType.Shockwave:
                    AddHazard(BossAttackType.Shockwave, origin, Vector3.zero, 1f, attack.length, attack);
                    Vfx.Ring(origin, attack.color, attack.length, 1f);
                    Shake(attack.cameraShake);
                    NotifyImpact(attack, origin);
                    break;
                case BossAttackType.Rain:
                    for (int i = 0; i < attackPoints.Count; i++)
                    {
                        army.DamageInRadius(attackPoints[i], attack.radius, info, 0f, attack.burnDps, attack.burnSeconds,
                            attack.slowFactor, attack.slowFactor < 1f ? attack.duration : 0f);
                        Vfx.Burst(attackPoints[i] + Vector3.up * 0.5f, attack.color, attack.radius, 0.5f);
                        NotifyImpact(attack, attackPoints[i]);
                    }
                    Shake(attack.cameraShake);
                    break;
                case BossAttackType.Line:
                {
                    Vector3 start = origin + lineDirection * Definition.bodyRadius;
                    if (attack.travelSpeed > 0f)
                    {
                        // Onda que percorre o chão: o dano acompanha a frente da onda, uma vez por soldado.
                        AddHazard(BossAttackType.Line, start, lineDirection, attack.length / attack.travelSpeed, attack.length, attack);
                    }
                    else
                    {
                        army.DamageInLine(origin, lineDirection, attack.length, attack.radius, info, attack.burnDps, attack.burnSeconds);
                        Vfx.Beam(start, lineDirection, attack.length, attack.radius, attack.color, 0.5f);
                    }
                    Shake(attack.cameraShake);
                    NotifyImpact(attack, start);
                    break;
                }
                case BossAttackType.Storm:
                    AddHazard(BossAttackType.Storm, army.Centroid(), Vector3.zero, attack.duration, army.FormationRadius + 6f, attack);
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

            EnterStage(AttackStage.Active);
        }

        private void NotifyImpact(BossAttack attack, Vector3 point)
        {
            if (presentation != null) presentation.OnAttackImpact(attack, point);
        }

        private void EnterStunned(float seconds)
        {
            SetState(BossState.Stunned);
            stateDuration = seconds;
        }

        private void BeginFlight()
        {
            SetState(BossState.Flying);
            stateDuration = currentAttack.duration;
            flightBaseX = transform.position.x;
            flightTargetX = Random.Range(-halfWidth + 1.5f, halfWidth - 1.5f);
            flightBarrageDone = false;
        }

        // Em voo o chefão fica fora do alcance corpo a corpo: só arqueiros, magos e habilidades o atingem.
        private void TickFlight()
        {
            float t = Mathf.Clamp01(stateTimer / stateDuration);
            float height = Mathf.Sin(t * Mathf.PI) * Definition.flightHeight;
            Vector3 p = transform.position;
            p.x = Mathf.Lerp(flightBaseX, flightTargetX, Mathf.SmoothStep(0f, 1f, t));
            p.y = height;
            transform.position = p;
            FaceToward(army.Centroid(), Time.deltaTime);

            if (!flightBarrageDone && t >= 0.45f)
            {
                flightBarrageDone = true;
                attackPoints.Clear();
                PrepareRain(army.Centroid(), currentAttack.count);
                var info = new DamageInfo(currentAttack.damage, currentAttack.damageType);
                for (int i = 0; i < attackPoints.Count; i++)
                {
                    Vfx.Telegraph(attackPoints[i], currentAttack.radius, 0.5f, currentAttack.color);
                    army.DamageInRadius(attackPoints[i], currentAttack.radius, info, 0f, currentAttack.burnDps, currentAttack.burnSeconds);
                    Vfx.Burst(attackPoints[i] + Vector3.up * 0.5f, currentAttack.color, currentAttack.radius, 0.6f);
                }
                GameAudio.PlayAt(SoundId.FireExplosion, army.Centroid());
            }

            if (t < 1f) return;
            p.y = 0f;
            transform.position = p;
            army.DamageInRadius(p, Definition.bodyRadius + 2f, new DamageInfo(currentAttack.damage * 0.8f), 0.4f);
            Impact(p, Definition.bodyRadius + 2f, currentAttack.color, currentAttack.cameraShake);
            GameAudio.PlayAt(SoundId.BossStomp, p);
            FinishAttack();
        }

        private void BeginPhaseShift()
        {
            Phase = pendingPhase;
            SetState(BossState.Enraged);
            GameAudio.Play(SoundId.BossRoar);
            Shake(0.8f);
            SetFlash(1f);
            Vfx.Ring(transform.position, Definition.glowColor, 10f, 1.2f);
            Vfx.Burst(transform.position + Vector3.up * Definition.scale * 0.5f, Definition.glowColor, Definition.scale, 0.8f);
            if (Definition.phaseSummonCount > 0)
                level.SpawnSummons(transform.position + transform.forward * (Definition.bodyRadius + 2f), Definition.phaseSummonCount, Definition.summonColor, Definition.summonHealth);
            GameLog.Info($"{Definition.displayName}: fase {Phase}.");
            if (presentation != null) presentation.OnPhaseChanged(Phase);
            PhaseChanged?.Invoke(this, Phase);
        }

        // ---------- Perigos que duram (onda de choque, onda de rochas, tempestade) ----------

        private void AddHazard(BossAttackType type, Vector3 center, Vector3 direction, float duration, float reach, BossAttack attack)
        {
            hazards.Add(new Hazard
            {
                Type = type,
                Center = center,
                Direction = direction,
                Age = 0f,
                Duration = Mathf.Max(0.05f, duration),
                Reach = reach,
                TickTimer = 0f,
                Progress = 0f,
                Attack = attack,
                Hit = type == BossAttackType.Shockwave || type == BossAttackType.Line ? new HashSet<Soldier>() : null
            });
        }

        private void UpdateHazards(float deltaTime)
        {
            for (int i = hazards.Count - 1; i >= 0; i--)
            {
                Hazard hazard = hazards[i];
                hazard.Age += deltaTime;
                var info = new DamageInfo(hazard.Attack.damage, hazard.Attack.damageType);
                switch (hazard.Type)
                {
                    case BossAttackType.Shockwave:
                    {
                        float radius = hazard.Reach * Mathf.Clamp01(hazard.Age / hazard.Duration);
                        army.DamageInRing(hazard.Center, Mathf.Max(0f, radius - 0.9f), radius + 0.3f, info, hazard.Hit);
                        break;
                    }
                    case BossAttackType.Line:
                        TickTravellingLine(ref hazard, info);
                        break;
                    default:
                        hazard.TickTimer -= deltaTime;
                        if (hazard.TickTimer <= 0f)
                        {
                            hazard.TickTimer = 0.5f;
                            info.Silent = true;
                            army.DamageInRadius(army.Centroid(), hazard.Reach, info);
                            Vfx.Ring(army.Centroid(), hazard.Attack.color, hazard.Reach, 0.5f);
                        }
                        break;
                }

                if (hazard.Age >= hazard.Duration)
                {
                    hazards.RemoveAt(i);
                    continue;
                }
                hazards[i] = hazard;
            }
        }

        private void TickTravellingLine(ref Hazard hazard, DamageInfo info)
        {
            float front = hazard.Reach * Mathf.Clamp01(hazard.Age / hazard.Duration);
            float halfWidthLine = hazard.Attack.radius;
            IReadOnlyList<Soldier> soldiers = army.Soldiers;
            for (int s = soldiers.Count - 1; s >= 0; s--)
            {
                if (s >= soldiers.Count) continue;
                Soldier soldier = soldiers[s];
                Vector3 delta = soldier.transform.position - hazard.Center;
                delta.y = 0f;
                float along = Vector3.Dot(delta, hazard.Direction);
                if (along < front - 1.6f || along > front + 0.4f) continue;
                if ((delta - hazard.Direction * along).sqrMagnitude > halfWidthLine * halfWidthLine) continue;
                if (!hazard.Hit.Add(soldier)) continue;
                soldier.ApplyStun(0.35f);
                soldier.TakeDamage(info);
            }

            while (hazard.Progress <= front)
            {
                Vector3 point = hazard.Center + hazard.Direction * hazard.Progress;
                if (presentation != null) presentation.OnAttackStep(hazard.Attack, point, halfWidthLine);
                else Vfx.Burst(point + Vector3.up * 0.3f, hazard.Attack.color, halfWidthLine, 0.4f);
                hazard.Progress += StepSpacing;
            }
        }

        private void Impact(Vector3 point, float radius, Color color, float shake)
        {
            Vfx.Burst(point + Vector3.up * 0.5f, color, radius, 0.5f);
            Vfx.Ring(point, color, radius * 1.3f, 0.5f);
            Shake(shake);
        }

        // Tremor controlado pedido pelo modelo (passos, rugido). Limitado para não cansar a leitura da tela.
        public void ShakeCamera(float amount) => Shake(Mathf.Min(amount, 0.8f));

        private void Shake(float amount)
        {
            if (cameraRig != null && amount > 0f) cameraRig.Shake(amount);
        }

        protected override void OnDeath()
        {
            SetState(BossState.Dead);
            hazards.Clear();
            CombatRegistry.Unregister(this);
            Vfx.Burst(transform.position + Vector3.up * Definition.scale * 0.5f, Definition.glowColor, Definition.scale * 1.2f, 1.2f);
            Vfx.Ring(transform.position, Definition.glowColor, 12f, 1.5f);
            Shake(1f);
            GameAudio.Play(SoundId.BossRoar);
            if (presentation != null) presentation.OnDeath();
            level.HandleBossDefeated(this);
        }

        private void Update()
        {
            if (State != BossState.Dead) return;
            stateTimer += Time.deltaTime;
            float t = Mathf.Clamp01(stateTimer / Definition.deathSeconds);
            Vector3 p = transform.position;
            p.y = Mathf.MoveTowards(p.y, 0f, Time.deltaTime * 8f);
            transform.position = p;
            if (presentation != null)
                presentation.Tick(Time.deltaTime, 0f);
            else if (Visual != null)
            {
                Visual.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 80f, Mathf.SmoothStep(0f, 1f, t * 2.5f)), 0f, 0f);
                Visual.localPosition = Vector3.down * (Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, t)) * Definition.scale);
            }
            if (t >= 1f) gameObject.SetActive(false);
        }
    }
}
