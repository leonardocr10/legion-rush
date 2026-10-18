using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    public abstract class Combatant : MonoBehaviour
    {
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
        private static MaterialPropertyBlock flashBlock;
        private const float HitAnimationCooldown = 0.35f;
        private const float PlaceholderCorpseDuration = 0.35f;

        public float MaxHealth { get; private set; }
        public float Health { get; private set; }
        public bool IsAlive => Health > 0f && gameObject.activeInHierarchy;
        public float CorpseDuration => AnimDriver != null ? AnimDriver.CorpseDuration : PlaceholderCorpseDuration;

        protected CharacterAnimatorDriver AnimDriver { get; private set; }
        protected float ImpactDelay => AnimDriver != null ? AnimDriver.ImpactDelay : 0f;

        private Transform visual;
        private Vector3 visualBaseScale;
        private Vector3 visualBasePosition;
        private Quaternion visualBaseRotation;
        private Renderer[] renderers;
        private bool visualCaptured;
        private bool flashApplied;
        private float punch;
        private float flash;
        private float lastHitAnimationTime = -10f;

        protected void SetupCombatant(float maxHealth)
        {
            MaxHealth = Mathf.Max(1f, maxHealth);
            Health = MaxHealth;
            punch = 0f;
            flash = 0f;

            if (!visualCaptured && transform.childCount > 0)
            {
                visual = transform.GetChild(0);
                visualBaseScale = visual.localScale;
                visualBasePosition = visual.localPosition;
                visualBaseRotation = visual.localRotation;
                renderers = visual.GetComponentsInChildren<Renderer>(true);
                AnimDriver = visual.GetComponentInChildren<CharacterAnimatorDriver>(true);
                visualCaptured = true;
            }
            if (visualCaptured)
            {
                visual.localScale = visualBaseScale;
                visual.localPosition = visualBasePosition;
                visual.localRotation = visualBaseRotation;
                ApplyFlash(0f);
            }
            if (AnimDriver != null) AnimDriver.ResetState();
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || !IsAlive) return;
            Health = Mathf.Max(0f, Health - amount);
            punch = 1f;
            flash = 1f;

            if (Health <= 0f)
            {
                if (AnimDriver != null) AnimDriver.PlayDeath();
                OnDeath();
                return;
            }
            if (AnimDriver != null && Time.time - lastHitAnimationTime >= HitAnimationCooldown)
            {
                lastHitAnimationTime = Time.time;
                AnimDriver.PlayHit();
            }
        }

        public void TickCorpse(float normalizedTime, float deltaTime)
        {
            if (!visualCaptured) return;
            flash = Mathf.MoveTowards(flash, 0f, deltaTime * 8f);
            UpdateFlash();

            if (AnimDriver != null)
            {
                float sink = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, normalizedTime));
                visual.localPosition = visualBasePosition + Vector3.down * (sink * 1.2f);
                return;
            }
            visual.localScale = visualBaseScale * Mathf.Max(0f, 1f - normalizedTime);
            visual.localRotation = visualBaseRotation * Quaternion.Euler(normalizedTime * 80f, 0f, 0f);
        }

        protected void Punch(float strength) => punch = Mathf.Max(punch, Mathf.Clamp01(strength));

        protected void PlayAttackAnimation(float attackSpeed)
        {
            if (AnimDriver != null) AnimDriver.PlayAttack(attackSpeed);
            else Punch(0.6f);
        }

        protected void TickVisual(float deltaTime, float normalizedSpeed, float bobHeight)
        {
            if (!visualCaptured) return;
            punch = Mathf.MoveTowards(punch, 0f, deltaTime * 6f);
            flash = Mathf.MoveTowards(flash, 0f, deltaTime * 8f);
            visual.localScale = visualBaseScale * (1f + punch * 0.15f);

            if (AnimDriver != null) AnimDriver.SetMoveSpeed(normalizedSpeed);
            else visual.localPosition = visualBasePosition + Vector3.up * bobHeight;
            UpdateFlash();
        }

        protected abstract void OnDeath();

        private void UpdateFlash()
        {
            if (flash > 0.001f) ApplyFlash(flash);
            else if (flashApplied) ApplyFlash(0f);
        }

        private void ApplyFlash(float amount)
        {
            if (renderers == null) return;
            if (amount <= 0.001f)
            {
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null) renderers[i].SetPropertyBlock(null);
                flashApplied = false;
                return;
            }

            flashBlock ??= new MaterialPropertyBlock();
            flashBlock.Clear();
            flashBlock.SetFloat(FlashAmountId, amount);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].SetPropertyBlock(flashBlock);
            flashApplied = true;
        }
    }

    public static class CombatRegistry
    {
        public static readonly List<Enemy> Enemies = new List<Enemy>(256);

        public static void Reset() => Enemies.Clear();

        public static Enemy FindNearestEnemy(Vector3 from, float maxRange)
        {
            float best = maxRange * maxRange;
            Enemy result = null;
            for (int i = 0; i < Enemies.Count; i++)
            {
                Enemy enemy = Enemies[i];
                if (!enemy.IsAlive) continue;
                Vector3 delta = enemy.transform.position - from;
                delta.y = 0f;
                float sq = delta.sqrMagnitude;
                if (sq < best)
                {
                    best = sq;
                    result = enemy;
                }
            }
            return result;
        }
    }

    public sealed class CorpseQueue
    {
        private struct Entry
        {
            public Combatant Body;
            public float Elapsed;
            public float Duration;
        }

        private readonly List<Entry> entries = new List<Entry>(64);
        private readonly System.Action<Combatant> onFinished;

        public CorpseQueue(System.Action<Combatant> finished) => onFinished = finished;

        public void Add(Combatant body)
        {
            if (body == null) return;
            entries.Add(new Entry { Body = body, Elapsed = 0f, Duration = Mathf.Max(0.05f, body.CorpseDuration) });
        }

        public void Tick(float deltaTime)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                Entry entry = entries[i];
                if (entry.Body == null)
                {
                    RemoveAt(i);
                    continue;
                }
                entry.Elapsed += deltaTime;
                float t = Mathf.Clamp01(entry.Elapsed / entry.Duration);
                entry.Body.TickCorpse(t, deltaTime);
                if (t < 1f)
                {
                    entries[i] = entry;
                    continue;
                }
                RemoveAt(i);
                onFinished?.Invoke(entry.Body);
            }
        }

        private void RemoveAt(int index)
        {
            int last = entries.Count - 1;
            entries[index] = entries[last];
            entries.RemoveAt(last);
        }
    }
}
