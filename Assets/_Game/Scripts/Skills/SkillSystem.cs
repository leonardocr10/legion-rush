using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    // Quatro espaços de habilidade com recarga. Mira automática no maior grupo inimigo à frente (ou no chefão).
    public class SkillSystem : MonoBehaviour
    {
        public const int SlotCount = 4;
        private const float TargetWindow = 26f;

        public static readonly SkillId[] DefaultLoadout = { SkillId.ArrowRain, SkillId.CollectiveShield, SkillId.ArcaneExplosion, SkillId.CommanderStrike };

        private struct Rider
        {
            public Transform Body;
            public float X;
        }

        public event System.Action<SkillDefinition> SkillUsed;
        public event System.Action<SkillDefinition> SkillFailed;

        private readonly SkillDefinition[] slots = new SkillDefinition[SlotCount];
        private readonly float[] cooldowns = new float[SlotCount];
        private readonly SkillDefinition[] library = new SkillDefinition[SkillDefinition.Count];
        private readonly bool[] runtimeCreated = new bool[SkillDefinition.Count];
        private readonly List<Rider> riders = new List<Rider>(8);
        private readonly HashSet<Combatant> chargeHits = new HashSet<Combatant>();

        private ArmyManager army;
        private LevelManager level;
        private CommanderController commander;
        private ArmyCameraController cameraRig;
        private SkillDefinition rainSkill;
        private Vector3 rainCenter;
        private float rainTimer;
        private float rainAccumulator;
        private float rainDamage;
        private SkillDefinition chargeSkill;
        private float chargeTimer;
        private float chargeZ;
        private float chargeDamage;

        public void Initialize(ArmyManager armyManager, LevelManager levelManager, CommanderController commanderController,
            ArmyCameraController camera, SkillDefinition[] customDefinitions, SkillId[] loadout)
        {
            army = armyManager;
            level = levelManager;
            commander = commanderController;
            cameraRig = camera;

            for (int i = 0; i < SkillDefinition.Count; i++)
            {
                library[i] = SkillDefinition.CreatePreset((SkillId)i);
                runtimeCreated[i] = true;
            }
            if (customDefinitions != null)
            {
                foreach (SkillDefinition custom in customDefinitions)
                {
                    if (custom == null) continue;
                    int index = (int)custom.id;
                    if (runtimeCreated[index] && library[index] != null) Destroy(library[index]);
                    library[index] = custom;
                    runtimeCreated[index] = false;
                }
            }
            for (int i = 0; i < SlotCount; i++)
            {
                SkillId id = loadout != null && i < loadout.Length ? loadout[i] : DefaultLoadout[i];
                slots[i] = library[(int)id];
                cooldowns[i] = 0f;
            }
        }

        public SkillDefinition Slot(int index) => slots[index];

        public void SetSlot(int index, SkillId id)
        {
            if (index < 0 || index >= SlotCount) return;
            slots[index] = library[(int)id];
            cooldowns[index] = 0f;
        }
        public float CooldownRemaining(int index) => cooldowns[index];
        public float CooldownRatio(int index) => slots[index] == null ? 0f : Mathf.Clamp01(cooldowns[index] / slots[index].cooldown);
        public bool IsReady(int index) => slots[index] != null && cooldowns[index] <= 0f;
        public SkillDefinition Definition(SkillId id) => library[(int)id];

        public bool TryUse(int index)
        {
            SkillDefinition skill = slots[index];
            if (skill == null || cooldowns[index] > 0f || level.Current != LevelManager.State.Running || level.IsPaused || army.Count == 0)
                return false;

            Vector3 target = Vector3.zero;
            if (skill.needsTarget && !FindTarget(skill.radius, out target))
            {
                GameAudio.Play(SoundId.UiBack);
                SkillFailed?.Invoke(skill);
                return false;
            }

            Execute(skill, target);
            cooldowns[index] = skill.cooldown;
            SkillUsed?.Invoke(skill);
            GameLog.Info($"Habilidade: {skill.displayName}.");
            return true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < SlotCount; i++)
            {
                if (cooldowns[i] > 0f) cooldowns[i] = Mathf.Max(0f, cooldowns[i] - dt);
            }
            TickArrowRain(dt);
            TickCharge(dt);
        }

        private bool FindTarget(float radius, out Vector3 point)
        {
            BossController boss = level.Boss;
            Vector3 c = commander.transform.position;
            if (boss != null && boss.IsEngaged && boss.IsAlive)
            {
                Vector3 toBoss = boss.transform.position - c;
                toBoss.y = 0f;
                if (toBoss.magnitude <= TargetWindow + 6f)
                {
                    point = boss.transform.position;
                    point.y = 0f;
                    return true;
                }
            }
            return CombatRegistry.FindBestCluster(c.z - 4f, c.z + TargetWindow, Mathf.Max(2f, radius), out point, out _);
        }

        private void Execute(SkillDefinition skill, Vector3 target)
        {
            Transform commanderTransform = commander.transform;
            switch (skill.id)
            {
                case SkillId.ArrowRain:
                    rainSkill = skill;
                    rainCenter = target;
                    rainTimer = skill.duration;
                    rainAccumulator = 0f;
                    rainDamage = skill.power * (1f + skill.perUnitBonus * army.CountOf(UnitClass.Archer));
                    Vfx.Telegraph(target, skill.radius, skill.duration, new Color(1f, 0.8f, 0.3f, 0.5f));
                    GameAudio.PlayAt(SoundId.ArrowShot, commanderTransform.position);
                    break;

                case SkillId.CollectiveShield:
                    army.ShieldAll(skill.power, skill.duration);
                    Vfx.Bubble(commanderTransform, army.FormationRadius + 1.8f, new Color(0.35f, 0.75f, 1f, 0.28f), skill.duration);
                    Vfx.Ring(commanderTransform.position, skill.color, army.FormationRadius + 2f, 0.6f);
                    GameAudio.Play(SoundId.MagicCast);
                    break;

                case SkillId.AreaHeal:
                {
                    float fraction = Mathf.Min(1f, skill.power * (1f + skill.perUnitBonus * army.CountOf(UnitClass.Healer)));
                    army.HealAllPercent(fraction);
                    Vfx.Ring(commanderTransform.position, skill.color, army.FormationRadius + 3f, 0.9f);
                    IReadOnlyList<Soldier> soldiers = army.Soldiers;
                    int step = Mathf.Max(1, soldiers.Count / 12);
                    for (int i = 0; i < soldiers.Count; i += step)
                        Vfx.Burst(soldiers[i].transform.position + Vector3.up * 0.7f, new Color(0.45f, 1f, 0.55f, 0.8f), 0.8f, 0.6f);
                    GameAudio.Play(SoundId.HealingSpell);
                    break;
                }

                case SkillId.ArcaneExplosion:
                {
                    float damage = skill.power * (1f + skill.perUnitBonus * army.CountOf(UnitClass.Mage));
                    CombatRegistry.DamageInRadius(target, skill.radius, new DamageInfo(damage, DamageType.Magic));
                    Vfx.Burst(target + Vector3.up, skill.color, skill.radius, 0.7f);
                    Vfx.Ring(target, skill.color, skill.radius * 1.4f, 0.8f);
                    GameAudio.PlayAt(SoundId.FireExplosion, target);
                    cameraRig.Shake(0.35f);
                    break;
                }

                case SkillId.CavalryCharge:
                    StartCharge(skill);
                    break;

                case SkillId.Freeze:
                    CombatRegistry.DamageInRadius(target, skill.radius, new DamageInfo(skill.power, DamageType.Ice), skill.duration, 0.45f, skill.duration + 2f);
                    Vfx.Ring(target, skill.color, skill.radius, 0.8f);
                    Vfx.Burst(target + Vector3.up * 0.5f, new Color(0.7f, 0.95f, 1f, 0.7f), skill.radius * 0.8f, 0.9f);
                    GameAudio.PlayAt(SoundId.IceBurst, target);
                    break;

                default:
                {
                    Vector3 center = commanderTransform.position;
                    float damage = skill.power * (1f + skill.perUnitBonus * army.Count);
                    CombatRegistry.DamageInRadius(center, skill.radius, new DamageInfo(damage, DamageType.Physical, true), skill.duration);
                    Vfx.Ring(center, skill.color, skill.radius * 1.3f, 0.6f);
                    Vfx.Burst(center + Vector3.up * 0.6f, skill.color, skill.radius * 0.7f, 0.5f);
                    GameAudio.PlayAt(SoundId.BossStomp, center);
                    cameraRig.Shake(0.6f);
                    break;
                }
            }
        }

        private void TickArrowRain(float dt)
        {
            if (rainTimer <= 0f || rainSkill == null) return;
            rainTimer -= dt;
            rainAccumulator += dt * 20f;
            while (rainAccumulator >= 1f)
            {
                rainAccumulator -= 1f;
                Vector2 offset = Random.insideUnitCircle * rainSkill.radius;
                Vector3 point = rainCenter + new Vector3(offset.x, 0.3f, offset.y);
                Vector3 origin = point + new Vector3(-2f, 14f, -6f);
                army.FireArrowAtPoint(origin, point, new DamageInfo(rainDamage, DamageType.Physical), 0.9f);
            }
        }

        private void StartCharge(SkillDefinition skill)
        {
            ClearRiders();
            chargeSkill = skill;
            chargeTimer = skill.duration;
            chargeZ = commander.transform.position.z - 1f;
            chargeDamage = skill.power * (1f + skill.perUnitBonus * army.CountOf(UnitClass.Knight));
            chargeHits.Clear();

            float half = level.Config.trackHalfWidth - 0.8f;
            const int count = 5;
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(-half, half, i / (float)(count - 1));
                GameObject rider = CharacterFactory.CreateSoldier(UnitClass.Knight, transform, new Color(0.55f, 0.7f, 1f));
                rider.name = "SpectralRider";
                rider.transform.SetPositionAndRotation(new Vector3(x, 0f, chargeZ), Quaternion.identity);
                riders.Add(new Rider { Body = rider.transform, X = x });
            }
            Vfx.Ring(commander.transform.position, skill.color, 6f, 0.5f);
            GameAudio.Play(SoundId.SwordSwing);
            GameAudio.Play(SoundId.BossStomp);
        }

        private void TickCharge(float dt)
        {
            if (chargeTimer <= 0f || chargeSkill == null) return;
            chargeTimer -= dt;
            chargeZ += 18f * dt;
            var info = new DamageInfo(chargeDamage, DamageType.Physical);
            for (int r = 0; r < riders.Count; r++)
            {
                Vector3 p = new Vector3(riders[r].X, 0f, chargeZ);
                riders[r].Body.position = p;
                List<Combatant> hostiles = CombatRegistry.Hostiles;
                for (int i = hostiles.Count - 1; i >= 0; i--)
                {
                    if (i >= hostiles.Count) continue;
                    Combatant hostile = hostiles[i];
                    if (!hostile.IsAlive || hostile.IsAirborne || chargeHits.Contains(hostile)) continue;
                    Vector3 delta = hostile.transform.position - p;
                    delta.y = 0f;
                    float reach = chargeSkill.radius + hostile.BodyRadius;
                    if (delta.sqrMagnitude > reach * reach) continue;
                    chargeHits.Add(hostile);
                    hostile.ApplyStun(0.8f);
                    hostile.TakeDamage(info);
                    GameAudio.PlayAt(SoundId.SwordHit, hostile.transform.position);
                }
            }
            if (chargeTimer > 0f) return;
            foreach (Rider rider in riders) Vfx.Burst(rider.Body.position + Vector3.up, chargeSkill.color, 1.2f, 0.4f);
            ClearRiders();
        }

        private void ClearRiders()
        {
            foreach (Rider rider in riders)
            {
                if (rider.Body != null) Destroy(rider.Body.gameObject);
            }
            riders.Clear();
        }

        private void OnDestroy()
        {
            ClearRiders();
            for (int i = 0; i < library.Length; i++)
            {
                if (runtimeCreated[i] && library[i] != null) Destroy(library[i]);
            }
        }
    }
}
