using System;
using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    public enum CharacterId
    {
        Commander, Archer, Warrior, Mage, Lancer, Healer, MountedKnight,
        EnemySoldier, EnemyArcher, Berserker, HeavyWarrior, Pikeman, DarkMage,
        BossDarkTitan, BossLavaGolem, BossIceKing, BossFireDragon
    }

    [Serializable]
    public class CharacterVisualEntry
    {
        public CharacterId id;
        [Tooltip("Modelo final: malha rigada + Animator com Override Controller + CharacterAnimatorDriver.")]
        public GameObject prefab;
        [Min(0.01f)] public float scale = 1f;
        public Vector3 localOffset;
        public Vector3 localEuler;
    }

    [CreateAssetMenu(fileName = "CharacterVisualLibrary", menuName = "Legion Rush/Character Visual Library")]
    public class CharacterVisualLibrary : ScriptableObject
    {
        public List<CharacterVisualEntry> entries = new List<CharacterVisualEntry>();
        [Tooltip("Material aditivo para rastros de flecha. Vazio = material de runtime com LegionRush/AdditiveGlow.")]
        public Material arrowTrailMaterial;

        private Dictionary<CharacterId, CharacterVisualEntry> lookup;

        public bool TryGet(CharacterId id, out CharacterVisualEntry entry)
        {
            if (lookup == null) BuildLookup();
            return lookup.TryGetValue(id, out entry) && entry.prefab != null;
        }

        public void Validate()
        {
            BuildLookup();
            foreach (CharacterVisualEntry entry in lookup.Values)
            {
                if (entry.prefab == null)
                {
                    GameLog.Warn($"Visual '{entry.id}': prefab ausente; placeholder será usado.");
                    continue;
                }
                Animator animator = entry.prefab.GetComponentInChildren<Animator>(true);
                if (animator == null) GameLog.Warn($"Visual '{entry.id}': prefab sem Animator.");
                else if (animator.runtimeAnimatorController == null) GameLog.Warn($"Visual '{entry.id}': Animator sem controller.");
            }
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<CharacterId, CharacterVisualEntry>();
            foreach (CharacterVisualEntry entry in entries)
            {
                if (entry == null) continue;
                if (lookup.ContainsKey(entry.id))
                {
                    GameLog.Warn($"Visual '{entry.id}' duplicado; usando o primeiro.");
                    continue;
                }
                lookup.Add(entry.id, entry);
            }
        }

        private void OnValidate() => lookup = null;
    }
}
