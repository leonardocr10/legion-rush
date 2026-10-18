using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    public enum DamageType { Physical, Magic, Fire, Ice }

    public struct DamageInfo
    {
        public float Amount;
        public DamageType Type;
        public bool Critical;
        public bool IgnoreShield;
        public bool Silent;

        public DamageInfo(float amount, DamageType type = DamageType.Physical, bool critical = false)
        {
            Amount = amount;
            Type = type;
            Critical = critical;
            IgnoreShield = false;
            Silent = false;
        }
    }

    public abstract class Combatant : MonoBehaviour
    {
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
        private static MaterialPropertyBlock flashBlock;
        private const float HitAnimationCooldown = 0.35f;
        private const float PlaceholderCorpseDuration = 0.35f;

        public float MaxHealth { get; private set; }
        public float Health { get; private set; }
        public float Armor { get; private set; }
        public float BodyRadius { get; private set; } = 0.3f;
        public bool IsAlive => Health > 0f && gameObject.activeInHierarchy;
        public float HealthRatio => MaxHealth > 0f ? Health / MaxHealth : 0f;
        public float CorpseDuration => AnimDriver != null ? AnimDriver.CorpseDuration : PlaceholderCorpseDuration;
        public bool IsStunned => stunTimer > 0f;
        public bool HasShield => shieldTimer > 0f;
        public float MoveMultiplier => IsStunned ? 0f : slowTimer > 0f ? slowFactor : 1f;
        public virtual bool IsAirborne => false;

        protected CharacterAnimatorDriver AnimDriver { get; private set; }
        protected float ImpactDelay => AnimDriver != null ? AnimDriver.ImpactDelay : 0f;
        protected virtual float ArmorValue => Armor;
        protected virtual float DamageTakenMultiplier => 1f;
        protected virtual float StunResistance => 0f;
        protected Transform Visual => visual;

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
        private float shieldTimer;
        private float shieldFactor;
        private float slowTimer;
        private float slowFactor = 1f;
        private float stunTimer;
        private float burnTimer;
        private float burnDps;

        protected void SetupCombatant(float maxHealth, float armor = 0f, float bodyRadius = 0.3f)
        {
            MaxHealth = Mathf.Max(1f, maxHealth);
            Health = MaxHealth;
            Armor = Mathf.Max(0f, armor);
            BodyRadius = Mathf.Max(0.1f, bodyRadius);
            punch = 0f;
            flash = 0f;
            ClearStatus();

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

        public void TakeDamage(float amount) => TakeDamage(new DamageInfo(amount));

        public void TakeDamage(DamageInfo info)
        {
            if (info.Amount <= 0f || !IsAlive) return;
            float amount = info.Amount * DamageTakenMultiplier;
            if (info.Type == DamageType.Physical) amount *= 100f / (100f + Mathf.Max(0f, ArmorValue));
            amount *= 1f - Mathf.Clamp(ResistanceTo(info.Type), -1f, 1f);
            if (shieldTimer > 0f && !info.IgnoreShield) amount *= 1f - shieldFactor;
            if (amount <= 0f) return;

            Health = Mathf.Max(0f, Health - amount);
            if (!info.Silent)
            {
                punch = 1f;
                flash = 1f;
            }
            OnDamaged(amount, info);

            if (Health <= 0f)
            {
                ClearStatus();
                if (AnimDriver != null) AnimDriver.PlayDeath();
                OnDeath();
                return;
            }
            if (!info.Silent && AnimDriver != null && Time.time - lastHitAnimationTime >= HitAnimationCooldown)
            {
                lastHitAnimationTime = Time.time;
                AnimDriver.PlayHit();
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || !IsAlive) return;
            Health = Mathf.Min(MaxHealth, Health + amount);
        }

        public void ApplyShield(float reduction, float duration)
        {
            if (!IsAlive || duration <= 0f) return;
            shieldFactor = Mathf.Max(shieldTimer > 0f ? shieldFactor : 0f, Mathf.Clamp01(reduction));
            shieldTimer = Mathf.Max(shieldTimer, duration);
        }

        public void ApplySlow(float factor, float duration)
        {
            if (!IsAlive || duration <= 0f) return;
            factor = Mathf.Clamp(factor, 0.05f, 1f);
            slowFactor = slowTimer > 0f ? Mathf.Min(slowFactor, factor) : factor;
            slowTimer = Mathf.Max(slowTimer, duration);
        }

        public void ApplyStun(float duration)
        {
            if (!IsAlive || duration <= 0f) return;
            float resistance = Mathf.Clamp01(StunResistance);
            if (resistance >= 0.999f)
            {
                ApplySlow(0.55f, duration);
                return;
            }
            stunTimer = Mathf.Max(stunTimer, duration * (1f - resistance));
        }

        public void ApplyBurn(float damagePerSecond, float duration)
        {
            if (!IsAlive || duration <= 0f || damagePerSecond <= 0f) return;
            burnDps = Mathf.Max(burnTimer > 0f ? burnDps : 0f, damagePerSecond);
            burnTimer = Mathf.Max(burnTimer, duration);
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

        // Atualiza escudo, lentidão, atordoamento e queimadura. Pode matar a unidade (queimadura).
        protected void TickStatus(float deltaTime)
        {
            if (shieldTimer > 0f) shieldTimer -= deltaTime;
            if (slowTimer > 0f) slowTimer -= deltaTime;
            if (stunTimer > 0f) stunTimer -= deltaTime;
            if (burnTimer > 0f)
            {
                burnTimer -= deltaTime;
                TakeDamage(new DamageInfo(burnDps * deltaTime, DamageType.Fire) { Silent = true, IgnoreShield = true });
            }
        }

        protected virtual float ResistanceTo(DamageType type) => 0f;
        protected virtual void OnDamaged(float amount, DamageInfo info) { }

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

        protected void SetFlash(float amount)
        {
            flash = Mathf.Max(flash, Mathf.Clamp01(amount));
            UpdateFlash();
        }

        protected abstract void OnDeath();

        private void ClearStatus()
        {
            shieldTimer = 0f;
            shieldFactor = 0f;
            slowTimer = 0f;
            slowFactor = 1f;
            stunTimer = 0f;
            burnTimer = 0f;
            burnDps = 0f;
        }

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

    // Registro de alvos hostis (inimigos comuns + chefões). Enemies é a lista atualizada pelo LevelManager.
    public static class CombatRegistry
    {
        public static readonly List<Enemy> Enemies = new List<Enemy>(256);
        public static readonly List<Combatant> Hostiles = new List<Combatant>(256);

        public static void Reset()
        {
            Enemies.Clear();
            Hostiles.Clear();
        }

        public static void Register(Enemy enemy)
        {
            if (enemy == null) return;
            Enemies.Add(enemy);
            Hostiles.Add(enemy);
        }

        public static void RegisterHostile(Combatant hostile)
        {
            if (hostile != null && !Hostiles.Contains(hostile)) Hostiles.Add(hostile);
        }

        public static void Unregister(Combatant hostile)
        {
            Hostiles.Remove(hostile);
            if (hostile is Enemy enemy) Enemies.Remove(enemy);
        }

        // Distância de superfície (centro menos raio do corpo), para alvos grandes como chefões.
        public static Combatant FindNearestHostile(Vector3 from, float maxRange, bool groundOnly)
        {
            float best = maxRange;
            Combatant result = null;
            for (int i = 0; i < Hostiles.Count; i++)
            {
                Combatant hostile = Hostiles[i];
                if (!hostile.IsAlive || (groundOnly && hostile.IsAirborne)) continue;
                Vector3 delta = hostile.transform.position - from;
                delta.y = 0f;
                float distance = delta.magnitude - hostile.BodyRadius;
                if (distance < best)
                {
                    best = distance;
                    result = hostile;
                }
            }
            return result;
        }

        public static int DamageInRadius(Vector3 center, float radius, DamageInfo info, float stun = 0f,
            float slowFactor = 1f, float slowDuration = 0f)
        {
            int hits = 0;
            for (int i = Hostiles.Count - 1; i >= 0; i--)
            {
                if (i >= Hostiles.Count) continue;
                Combatant hostile = Hostiles[i];
                if (!hostile.IsAlive) continue;
                Vector3 delta = hostile.transform.position - center;
                delta.y = 0f;
                float reach = radius + hostile.BodyRadius;
                if (delta.sqrMagnitude > reach * reach) continue;
                if (stun > 0f) hostile.ApplyStun(stun);
                if (slowDuration > 0f) hostile.ApplySlow(slowFactor, slowDuration);
                hostile.TakeDamage(info);
                hits++;
            }
            return hits;
        }

        public static int DamageInLine(Vector3 origin, Vector3 direction, float length, float halfWidth, DamageInfo info, int maxTargets)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-5f) return 0;
            direction.Normalize();
            int hits = 0;
            for (int i = Hostiles.Count - 1; i >= 0 && hits < maxTargets; i--)
            {
                if (i >= Hostiles.Count) continue;
                Combatant hostile = Hostiles[i];
                if (!hostile.IsAlive) continue;
                Vector3 delta = hostile.transform.position - origin;
                delta.y = 0f;
                float along = Vector3.Dot(delta, direction);
                if (along < -hostile.BodyRadius || along > length + hostile.BodyRadius) continue;
                float side = (delta - direction * along).magnitude;
                if (side > halfWidth + hostile.BodyRadius) continue;
                hostile.TakeDamage(info);
                hits++;
            }
            return hits;
        }

        // Centro do maior agrupamento de hostis dentro da janela [minZ, maxZ]. Amostragem limitada para custo fixo.
        public static bool FindBestCluster(float minZ, float maxZ, float radius, out Vector3 center, out int count)
        {
            center = Vector3.zero;
            count = 0;
            float radiusSq = radius * radius;
            int stride = Mathf.Max(1, Hostiles.Count / 48);
            for (int i = 0; i < Hostiles.Count; i += stride)
            {
                Combatant candidate = Hostiles[i];
                if (!candidate.IsAlive) continue;
                Vector3 p = candidate.transform.position;
                if (p.z < minZ || p.z > maxZ) continue;

                int neighbours = 0;
                Vector3 sum = Vector3.zero;
                for (int j = 0; j < Hostiles.Count; j++)
                {
                    Combatant other = Hostiles[j];
                    if (!other.IsAlive) continue;
                    Vector3 delta = other.transform.position - p;
                    delta.y = 0f;
                    if (delta.sqrMagnitude > radiusSq) continue;
                    int weight = other.BodyRadius > 1f ? 6 : 1;
                    neighbours += weight;
                    sum += other.transform.position * weight;
                }
                if (neighbours <= count) continue;
                count = neighbours;
                center = sum / neighbours;
            }
            center.y = 0f;
            return count > 0;
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
