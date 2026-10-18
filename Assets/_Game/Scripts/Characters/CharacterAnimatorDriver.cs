using UnityEngine;

namespace LegionRush
{
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    public class CharacterAnimatorDriver : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int AttackSpeedId = Animator.StringToHash("AttackSpeed");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DieId = Animator.StringToHash("Die");
        private static readonly int VictoryId = Animator.StringToHash("Victory");
        private static readonly int SkillId = Animator.StringToHash("Skill");
        private static readonly int SpawnId = Animator.StringToHash("Spawn");
        private static readonly int BlockId = Animator.StringToHash("Block");
        private static readonly int AttackVariantId = Animator.StringToHash("AttackVariant");

        [SerializeField, Tooltip("Segundos entre o início do ataque e o frame de impacto.")]
        private float impactDelay = 0.18f;
        [SerializeField, Tooltip("Duração do corpo em cena após a morte, incluindo a animação.")]
        private float corpseDuration = 1.4f;
        [SerializeField, Range(0f, 0.3f)] private float playbackVariation = 0.08f;
        [SerializeField] private float speedDampTime = 0.1f;
        [SerializeField] private bool playSpawnOnReset;
        [SerializeField, Range(0f, 1f), Tooltip("Chance de usar o ataque alternativo (estado AttackAlt), quando existir.")]
        private float alternateAttackChance = 0.3f;

        public float ImpactDelay => Mathf.Max(0f, impactDelay);
        public float CorpseDuration => Mathf.Max(0.1f, corpseDuration);

        private Animator animator;
        private bool cached;
        private bool hasSpeed, hasAttackSpeed, hasAttack, hasHit, hasDie, hasVictory, hasSkill, hasSpawn, hasBlock, hasAttackVariant;

        private void Awake() => Cache();

        public void ResetState()
        {
            Cache();
            if (!IsReady) return;
            animator.Rebind();
            animator.Update(0f);
            animator.speed = 1f + Random.Range(-playbackVariation, playbackVariation);
            if (hasAttackSpeed) animator.SetFloat(AttackSpeedId, 1f);
            if (playSpawnOnReset && hasSpawn)
            {
                animator.SetTrigger(SpawnId);
                return;
            }
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            animator.Play(state.fullPathHash, 0, Random.value);
        }

        public void SetMoveSpeed(float normalized)
        {
            if (hasSpeed && IsReady) animator.SetFloat(SpeedId, Mathf.Clamp01(normalized), speedDampTime, Time.deltaTime);
        }

        public void PlayAttack(float attackSpeed)
        {
            if (!IsReady) return;
            if (hasAttackSpeed) animator.SetFloat(AttackSpeedId, Mathf.Clamp(attackSpeed, 0.25f, 4f));
            if (hasAttackVariant) animator.SetInteger(AttackVariantId, Random.value < alternateAttackChance ? 1 : 0);
            if (hasAttack) animator.SetTrigger(AttackId);
        }

        public void PlayHit()
        {
            if (hasHit && IsReady) animator.SetTrigger(HitId);
        }

        public void PlayDeath()
        {
            if (!IsReady) return;
            ClearCombatTriggers();
            if (hasDie) animator.SetTrigger(DieId);
        }

        public void PlayVictory()
        {
            if (!IsReady) return;
            ClearCombatTriggers();
            if (hasVictory) animator.SetTrigger(VictoryId);
        }

        public bool CanBlock => hasBlock && IsReady;

        public void PlayBlock()
        {
            if (hasBlock && IsReady) animator.SetTrigger(BlockId);
        }

        public void PlaySkill()
        {
            if (hasSkill && IsReady) animator.SetTrigger(SkillId);
        }

        private bool IsReady => animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null;

        private void ClearCombatTriggers()
        {
            if (hasAttack) animator.ResetTrigger(AttackId);
            if (hasHit) animator.ResetTrigger(HitId);
        }

        private void Cache()
        {
            if (cached) return;
            animator = GetComponent<Animator>();
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            animator.applyRootMotion = false;
            if (animator.runtimeAnimatorController == null)
            {
                GameLog.Warn($"{name}: Animator sem controller.");
                cached = true;
                return;
            }

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                int hash = parameter.nameHash;
                if (hash == SpeedId) hasSpeed = true;
                else if (hash == AttackSpeedId) hasAttackSpeed = true;
                else if (hash == AttackId) hasAttack = true;
                else if (hash == HitId) hasHit = true;
                else if (hash == DieId) hasDie = true;
                else if (hash == VictoryId) hasVictory = true;
                else if (hash == SkillId) hasSkill = true;
                else if (hash == SpawnId) hasSpawn = true;
                else if (hash == BlockId) hasBlock = true;
                else if (hash == AttackVariantId) hasAttackVariant = true;
            }
            if (!hasAttack || !hasDie)
                GameLog.Warn($"{name}: parâmetros Attack/Die ausentes. Use 'Legion Rush/Animation/Create Base Animator Controller'.");
            cached = true;
        }
    }
}
