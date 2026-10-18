using System.Collections.Generic;
using UnityEngine;

namespace LegionRush.Bosses.DarkGolem
{
    // Apresentação do Golem Sombrio: toca as animações procedurais conforme o estado do BossController,
    // alinha o golpe ao fim do aviso, controla o brilho por fase e dispara os efeitos com pooling.
    // Não aplica dano nem decide nada de combate.
    public class DarkGolemPresentation : BossPresentation
    {
        private enum ActionMode { Normal, Timed, Loop }

        private DarkGolemProfile profile;
        private DarkGolemRig rig;
        private DarkGolemVfx vfx;

        private readonly GolemPose idlePose = new GolemPose();
        private readonly GolemPose walkPose = new GolemPose();
        private readonly GolemPose locomotion = new GolemPose();
        private readonly GolemPose actionPose = new GolemPose();
        private readonly GolemPose fadeFrom = new GolemPose();
        private readonly GolemPose output = new GolemPose();
        private readonly GolemPose hitPose = new GolemPose();
        private readonly GolemPose zero = new GolemPose();

        private GolemClip action;
        private ActionMode actionMode;
        private float actionTime;
        private float actionWindup;
        private float actionFollow;
        private float actionSpeed = 1f;
        private GolemClipId queued = GolemClipId.Count;
        private float queuedLength;
        private float fadeTimer;
        private float fadeDuration;
        private float moveBlend;
        private float idleTime;
        private float walkPhase;
        private float hitTimer;
        private float lastHit = -10f;
        private float glow;
        private float glowTarget = 1f;
        private float appliedGlow = -1f;
        private float flare;
        private float deathTimer;
        private bool deathImpactDone;
        private bool roarPeakDone;
        private int lastTickFrame = -1;

        public DarkGolemRig Rig => rig;

        // Monta o modelo dentro do visual do chefão. Chamado pela CharacterFactory antes do BossController existir.
        public static DarkGolemPresentation Create(Transform visual, BossDefinition definition)
        {
            DarkGolemProfile profile = DarkGolemProfile.LoadOrDefault();
            DarkGolemRig rig = DarkGolemModel.Build(visual, profile, definition.visualVariant, definition.scale);
            var presentation = rig.Model.gameObject.AddComponent<DarkGolemPresentation>();
            presentation.profile = profile;
            presentation.rig = rig;
            presentation.PlayLoop(GolemClipId.Dormant, false);
            presentation.glow = profile.dormantGlow;
            presentation.glowTarget = profile.dormantGlow;
            presentation.Apply(0f);
            return presentation;
        }

        protected override void OnBound()
        {
            if (vfx != null) return;
            var go = new GameObject("DarkGolem_VFX");
            go.transform.SetParent(Boss.transform.parent, false);
            vfx = go.AddComponent<DarkGolemVfx>();
            vfx.Initialize(profile, rig, Boss);
        }

        private void OnDestroy()
        {
            if (vfx != null) Destroy(vfx.gameObject);
        }

        // Fora da partida (menus) o controller não chama Tick: o golem continua respirando sozinho.
        private void LateUpdate()
        {
            if (lastTickFrame != Time.frameCount) Tick(Time.deltaTime, 0f);
        }

        public override void Tick(float deltaTime, float moveRatio)
        {
            if (lastTickFrame == Time.frameCount) return;
            lastTickFrame = Time.frameCount;
            Apply(deltaTime, moveRatio);
        }

        // ---------- Reações ao controller ----------

        public override void OnStateChanged(BossController.BossState previous, BossController.BossState state)
        {
            if (previous == BossController.BossState.Stunned) vfx?.Stars(false);
            switch (state)
            {
                case BossController.BossState.Dormant:
                    PlayLoop(GolemClipId.Dormant, false);
                    glowTarget = profile.dormantGlow;
                    break;
                case BossController.BossState.Intro:
                {
                    float total = Boss.Definition.introSeconds;
                    float spawn = Mathf.Min(profile.spawnSeconds, total * 0.5f);
                    PlayNormal(GolemClipId.Spawn, spawn);
                    Queue(GolemClipId.Roar, total - spawn);
                    glowTarget = Boss.Definition.PhaseGlow(1);
                    flare = 1f;
                    break;
                }
                case BossController.BossState.Idle:
                case BossController.BossState.Chase:
                    if (actionMode == ActionMode.Loop) StopAction();
                    glowTarget = Boss.Definition.PhaseGlow(Boss.Phase);
                    break;
                case BossController.BossState.Stunned:
                    PlayLoop(GolemClipId.Stun, true);
                    vfx?.Stars(true);
                    glowTarget = profile.stunnedGlow * Boss.Definition.PhaseGlow(Boss.Phase);
                    break;
                case BossController.BossState.Enraged:
                    PlayNormal(GolemClipId.Roar, Boss.Definition.phaseShiftSeconds);
                    flare = 1.5f;
                    break;
                case BossController.BossState.Dead:
                    PlayNormal(GolemClipId.Death, GolemClipLength(GolemClipId.Death));
                    vfx?.Stars(false);
                    deathTimer = 0f;
                    deathImpactDone = false;
                    glowTarget = 0f;
                    break;
            }
        }

        public override void OnAttackWindup(BossAttack attack, float windupSeconds, float followSeconds,
            IReadOnlyList<Vector3> points, Vector3 direction)
        {
            GolemClipId clip = attack.move switch
            {
                BossMove.Attack2 => GolemClipId.Attack2,
                BossMove.GroundSlam => GolemClipId.GroundSlam,
                BossMove.Roar => GolemClipId.Roar,
                _ => GolemClipId.Attack1
            };
            PlayTimed(clip, windupSeconds, Mathf.Max(0.35f, followSeconds + 0.25f));
            flare = attack.special ? 1.2f : 0.6f;
            if (attack.type == BossAttackType.Rain) vfx?.ScheduleBoulders(points, windupSeconds, attack.fallTime, attack.radius);
            if (attack.move == BossMove.Attack1 || attack.move == BossMove.Attack2)
                vfx?.MaceTrail(windupSeconds + Mathf.Max(0.3f, attack.activeTime));
        }

        public override void OnAttackImpact(BossAttack attack, Vector3 point)
        {
            if (vfx == null) return;
            switch (attack.type)
            {
                case BossAttackType.Slam:
                    vfx.SlamBurst(point, attack.radius);
                    break;
                case BossAttackType.Shockwave:
                    vfx.CrystalWave(point, attack.length, 1f);
                    break;
                case BossAttackType.Spin:
                    vfx.MaceTrail(attack.activeTime + 0.2f);
                    vfx.Dust(point, attack.radius);
                    break;
            }
        }

        public override void OnAttackStep(BossAttack attack, Vector3 point, float halfWidth)
        {
            vfx?.CrystalCluster(point, halfWidth);
        }

        public override void OnHit(DamageInfo info)
        {
            if (Time.time - lastHit < profile.hitCooldown) return;
            lastHit = Time.time;
            hitTimer = profile.hitReaction;
        }

        public override void OnPhaseChanged(int phase)
        {
            glowTarget = Boss.Definition.PhaseGlow(phase);
            flare = 2f;
        }

        public override void OnDeath()
        {
            vfx?.Stars(false);
        }

        // ---------- Reprodução ----------

        private float GolemClipLength(GolemClipId id) => DarkGolemAnimation.Get(id).Length;

        private void BeginFade()
        {
            fadeFrom.CopyFrom(output);
            fadeFrom.Wrap();
            fadeTimer = 0f;
            fadeDuration = profile != null ? profile.crossFade : 0.18f;
        }

        private void PlayNormal(GolemClipId id, float seconds)
        {
            BeginFade();
            action = DarkGolemAnimation.Get(id);
            actionMode = ActionMode.Normal;
            actionTime = 0f;
            actionSpeed = action.Length / Mathf.Max(0.05f, seconds);
            queued = GolemClipId.Count;
            roarPeakDone = false;
        }

        private void PlayTimed(GolemClipId id, float windup, float follow)
        {
            BeginFade();
            action = DarkGolemAnimation.Get(id);
            actionMode = ActionMode.Timed;
            actionTime = 0f;
            actionWindup = Mathf.Max(0.05f, windup);
            actionFollow = Mathf.Max(0.05f, follow);
            queued = GolemClipId.Count;
            roarPeakDone = false;
        }

        private void PlayLoop(GolemClipId id, bool fade)
        {
            if (fade) BeginFade();
            else fadeTimer = fadeDuration = 0f;
            action = DarkGolemAnimation.Get(id);
            actionMode = ActionMode.Loop;
            actionTime = Random.value * action.Length;
            actionSpeed = 1f;
            queued = GolemClipId.Count;
        }

        private void Queue(GolemClipId id, float seconds)
        {
            queued = id;
            queuedLength = Mathf.Max(0.1f, seconds);
        }

        private void StopAction()
        {
            if (action == null) return;
            BeginFade();
            action = null;
        }

        // Tempo normalizado do clipe. No modo Timed, 0..Impact dura exatamente o aviso e Impact..1 dura a recuperação.
        private float ActionNormalized()
        {
            switch (actionMode)
            {
                case ActionMode.Timed:
                {
                    float impact = action.Impact > 0f ? action.Impact : 0.5f;
                    if (actionTime <= actionWindup) return impact * actionTime / actionWindup;
                    return impact + (1f - impact) * Mathf.Clamp01((actionTime - actionWindup) / actionFollow);
                }
                case ActionMode.Loop:
                    return actionTime / action.Length;
                default:
                    return actionTime * actionSpeed / action.Length;
            }
        }

        private void Apply(float deltaTime, float moveRatio = 0f)
        {
            if (rig == null) return;
            moveBlend = Mathf.MoveTowards(moveBlend, Mathf.Clamp01(moveRatio), deltaTime * 3f);
            idleTime += deltaTime / Mathf.Max(0.1f, profile.idleCycle);
            float previousWalk = walkPhase;
            walkPhase += deltaTime * Mathf.Lerp(0.35f, 1f, moveBlend) / Mathf.Max(0.1f, profile.walkCycle);

            DarkGolemAnimation.Get(GolemClipId.Idle).Sample(idleTime, DarkGolemAnimation.BasePose, idlePose);
            DarkGolemAnimation.Get(GolemClipId.Walk).Sample(walkPhase, DarkGolemAnimation.BasePose, walkPose);
            float walkWeight = Mathf.SmoothStep(0f, 1f, moveBlend);
            locomotion.Lerp(idlePose, walkPose, walkWeight);
            if (walkWeight > 0.4f && action == null) Footsteps(previousWalk, walkPhase, walkWeight);

            GolemPose target = locomotion;
            if (action != null)
            {
                actionTime += deltaTime;
                float n = ActionNormalized();
                action.Sample(n, DarkGolemAnimation.BasePose, actionPose);
                target = actionPose;
                TickActionEvents(n);
                if (actionMode != ActionMode.Loop && n >= 1f) FinishAction();
            }

            if (fadeTimer < fadeDuration)
            {
                fadeTimer += deltaTime;
                output.Lerp(fadeFrom, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fadeTimer / fadeDuration)));
            }
            else output.CopyFrom(target);

            if (hitTimer > 0f)
            {
                hitTimer -= deltaTime;
                DarkGolemAnimation.Get(GolemClipId.Hit).Sample(1f - hitTimer / profile.hitReaction, zero, hitPose);
                output.Add(hitPose, 1f);
            }

            // Saiote acompanha as pernas (balanço secundário barato).
            float legR = output.Euler[(int)GolemBone.UpperLegR].x, legL = output.Euler[(int)GolemBone.UpperLegL].x;
            output.Euler[(int)GolemBone.SkirtFront].x += Mathf.Min(0f, Mathf.Min(legR, legL)) * 0.7f;
            output.Euler[(int)GolemBone.SkirtBack].x += Mathf.Max(0f, Mathf.Max(legR, legL)) * 0.7f;

            if (Boss != null && Boss.State == BossController.BossState.Dead) TickDeath(deltaTime);

            for (int i = 0; i < rig.Bones.Length; i++)
                rig.Bones[i].localRotation = rig.BindRotations[i] * Quaternion.Euler(output.Euler[i]);
            rig.Bones[(int)GolemBone.Root].localPosition = rig.BindPositions[(int)GolemBone.Root] + output.RootOffset;

            UpdateGlow(deltaTime);
        }

        private void FinishAction()
        {
            if (queued != GolemClipId.Count)
            {
                GolemClipId next = queued;
                PlayNormal(next, queuedLength);
                return;
            }
            if (Boss != null && Boss.State == BossController.BossState.Dead)
            {
                actionTime -= Time.deltaTime; // segura a última pose (corpo no chão)
                return;
            }
            StopAction();
        }

        private void TickActionEvents(float n)
        {
            if (action.Id == GolemClipId.Roar && !roarPeakDone && n >= (action.Impact > 0f ? action.Impact : 0.35f))
            {
                roarPeakDone = true;
                if (Boss != null && (Boss.State == BossController.BossState.Intro || Boss.State == BossController.BossState.Enraged))
                {
                    GameAudio.Play(SoundId.BossRoar);
                    Boss.ShakeCamera(0.5f);
                    Vfx.Ring(Boss.transform.position, rig.Materials.Variant.crystal, 9f, 0.9f);
                    vfx?.CrystalWave(Boss.transform.position, 7f, 0.6f);
                }
            }
        }

        private void Footsteps(float from, float to, float weight)
        {
            if (Boss == null || vfx == null) return;
            bool right = Mathf.Floor(to) > Mathf.Floor(from);
            bool left = Mathf.Floor(to + 0.5f) > Mathf.Floor(from + 0.5f);
            if (!right && !left) return;
            Transform foot = rig[right ? GolemBone.FootR : GolemBone.FootL];
            Vector3 p = foot.position;
            p.y = 0f;
            vfx.Dust(p, 1.4f * weight);
            if (profile.footstepShake > 0f) Boss.ShakeCamera(profile.footstepShake * weight);
            GameAudio.PlayAt(SoundId.BossStomp, p);
        }

        private void TickDeath(float deltaTime)
        {
            deathTimer += deltaTime;
            float deathLength = GolemClipLength(GolemClipId.Death);
            if (!deathImpactDone && deathTimer >= deathLength * 0.78f)
            {
                deathImpactDone = true;
                Vector3 p = rig[GolemBone.Chest].position;
                p.y = 0f;
                Boss.ShakeCamera(0.7f);
                vfx?.CrystalWave(p, 6f, 0.5f);
                GameAudio.PlayAt(SoundId.BossStomp, p);
            }
            // Depois de cair, o corpo afunda devagar até o fim do tempo de morte do BossDefinition.
            float total = Boss.Definition.deathSeconds;
            float sink = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(deathLength + 0.3f, total, deathTimer));
            output.RootOffset += Vector3.down * sink * 1.2f;
        }

        private void UpdateGlow(float deltaTime)
        {
            glow = Mathf.MoveTowards(glow, glowTarget, deltaTime * 1.5f);
            flare = Mathf.MoveTowards(flare, 0f, deltaTime * 1.8f);
            float value = glow * (1f + 0.6f * flare);
            if (Boss != null && Boss.Phase >= 3 && Boss.State != BossController.BossState.Dead)
                value *= 1f + 0.22f * Mathf.Sin(Time.time * profile.enragedPulse * Mathf.PI * 2f);
            if (Mathf.Abs(value - appliedGlow) < 0.01f) return;
            appliedGlow = value;
            rig.Materials.SetGlow(value);
        }
    }
}
