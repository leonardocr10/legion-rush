using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    public static class CharacterFactory
    {
        public static CharacterVisualLibrary Library { get; set; }

        private static readonly HashSet<CharacterId> WarnedPlaceholders = new HashSet<CharacterId>();
        private static readonly Color SkinColor = new Color(0.96f, 0.78f, 0.62f);
        private static readonly Color LeatherColor = new Color(0.36f, 0.22f, 0.12f);
        private static readonly Color SteelColor = new Color(0.78f, 0.81f, 0.86f);
        private static readonly Color GoldColor = new Color(1f, 0.76f, 0.25f);
        private static readonly Color CapeColor = new Color(0.75f, 0.12f, 0.15f);

        public static GameObject CreateSoldier(UnitClass unitClass, Transform parent, Color tunicColor)
        {
            CharacterId id = unitClass == UnitClass.Archer ? CharacterId.Archer : CharacterId.Warrior;
            return TryCreateFromLibrary(id, parent, out GameObject root)
                ? root
                : CreatePlaceholderSoldier(unitClass, parent, tunicColor);
        }

        public static GameObject CreateEnemy(Transform parent, Color armorColor)
        {
            return TryCreateFromLibrary(CharacterId.EnemySoldier, parent, out GameObject root)
                ? root
                : CreatePlaceholderEnemy(parent, armorColor);
        }

        public static GameObject CreateCommander(Transform parent)
        {
            return TryCreateFromLibrary(CharacterId.Commander, parent, out GameObject root)
                ? root
                : CreatePlaceholderCommander(parent);
        }

        private static bool TryCreateFromLibrary(CharacterId id, Transform parent, out GameObject root)
        {
            root = null;
            if (Library == null || !Library.TryGet(id, out CharacterVisualEntry entry))
            {
                if (WarnedPlaceholders.Add(id)) GameLog.Warn($"Sem modelo final para '{id}'; usando placeholder.");
                return false;
            }

            Transform visual = CreateRoot(id.ToString(), parent, out root);
            GameObject model = Object.Instantiate(entry.prefab, visual, false);
            model.transform.localPosition = entry.localOffset;
            model.transform.localRotation = Quaternion.Euler(entry.localEuler);
            model.transform.localScale = Vector3.one * entry.scale;

            Animator animator = model.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.GetComponent<CharacterAnimatorDriver>() == null)
                animator.gameObject.AddComponent<CharacterAnimatorDriver>();
            return true;
        }

        private static GameObject CreatePlaceholderSoldier(UnitClass unitClass, Transform parent, Color tunicColor)
        {
            Transform v = CreateRoot(unitClass + "Soldier", parent, out GameObject root);
            Material tunic = MaterialLibrary.Get(tunicColor);
            Material leather = MaterialLibrary.Get(LeatherColor);
            Material steel = MaterialLibrary.Get(SteelColor, 0f, 0.85f);
            BuildHumanoid(v, tunic, MaterialLibrary.Get(SkinColor), leather, 1f);

            if (unitClass == UnitClass.Archer)
            {
                PrimitiveFactory.Part(PrimitiveType.Sphere, v, new Vector3(0f, 1.16f, -0.03f), new Vector3(0.36f, 0.3f, 0.36f), tunic);
                PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.32f, 0.72f, 0.12f), new Vector3(0.05f, 0.78f, 0.07f), leather);
                PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0f, 0.82f, -0.22f), new Vector3(0.15f, 0.42f, 0.12f), leather, new Vector3(-12f, 0f, 0f));
            }
            else
            {
                PrimitiveFactory.Part(PrimitiveType.Sphere, v, new Vector3(0f, 1.17f, 0f), new Vector3(0.35f, 0.25f, 0.35f), steel);
                PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(-0.33f, 0.68f, 0.12f), new Vector3(0.07f, 0.52f, 0.4f), steel);
                PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.33f, 0.8f, 0.3f), new Vector3(0.06f, 0.06f, 0.7f), steel);
            }
            return root;
        }

        private static GameObject CreatePlaceholderEnemy(Transform parent, Color armorColor)
        {
            Transform v = CreateRoot("Enemy", parent, out GameObject root);
            Material armor = MaterialLibrary.Get(armorColor, 0f, 0.4f);
            Material dark = MaterialLibrary.Get(new Color(0.15f, 0.12f, 0.14f), 0f, 0.6f);
            Material eyes = MaterialLibrary.Get(new Color(1f, 0.35f, 0.1f), 3f);
            Material steel = MaterialLibrary.Get(SteelColor, 0f, 0.85f);
            BuildHumanoid(v, armor, dark, dark, 1.1f);

            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(-0.14f, 1.38f, 0f), new Vector3(0.06f, 0.26f, 0.06f), dark, new Vector3(0f, 0f, 25f));
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.14f, 1.38f, 0f), new Vector3(0.06f, 0.26f, 0.06f), dark, new Vector3(0f, 0f, -25f));
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(-0.06f, 1.24f, 0.155f), new Vector3(0.06f, 0.03f, 0.02f), eyes, default, false);
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.06f, 1.24f, 0.155f), new Vector3(0.06f, 0.03f, 0.02f), eyes, default, false);
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.36f, 0.85f, 0.28f), new Vector3(0.05f, 0.05f, 0.75f), dark);
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.36f, 0.85f, 0.62f), new Vector3(0.04f, 0.3f, 0.18f), steel);
            return root;
        }

        private static GameObject CreatePlaceholderCommander(Transform parent)
        {
            Transform v = CreateRoot("Commander", parent, out GameObject root);
            Material gold = MaterialLibrary.Get(GoldColor, 0f, 0.8f);
            Material cape = MaterialLibrary.Get(CapeColor);
            Material blade = MaterialLibrary.Get(new Color(0.7f, 0.85f, 1f), 1.2f, 0.9f);
            BuildHumanoid(v, gold, MaterialLibrary.Get(SkinColor), MaterialLibrary.Get(LeatherColor), 1.35f);

            PrimitiveFactory.Part(PrimitiveType.Sphere, v, new Vector3(0f, 1.56f, 0f), new Vector3(0.46f, 0.34f, 0.46f), gold);
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0f, 1.8f, -0.05f), new Vector3(0.06f, 0.25f, 0.35f), cape);
            PrimitiveFactory.Part(PrimitiveType.Sphere, v, new Vector3(-0.38f, 1.12f, 0f), Vector3.one * 0.24f, gold);
            PrimitiveFactory.Part(PrimitiveType.Sphere, v, new Vector3(0.38f, 1.12f, 0f), Vector3.one * 0.24f, gold);
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0f, 0.85f, -0.26f), new Vector3(0.55f, 1f, 0.05f), cape, new Vector3(8f, 0f, 0f));
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.42f, 1.05f, 0.35f), new Vector3(0.08f, 0.08f, 1f), blade);
            return root;
        }

        private static Transform CreateRoot(string name, Transform parent, out GameObject root)
        {
            root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            return visual;
        }

        private static void BuildHumanoid(Transform v, Material body, Material skin, Material boots, float s)
        {
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(-0.1f, 0.17f, 0f) * s, new Vector3(0.13f, 0.34f, 0.14f) * s, boots);
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.1f, 0.17f, 0f) * s, new Vector3(0.13f, 0.34f, 0.14f) * s, boots);
            PrimitiveFactory.Part(PrimitiveType.Capsule, v, new Vector3(0f, 0.62f, 0f) * s, new Vector3(0.42f, 0.32f, 0.32f) * s, body);
            PrimitiveFactory.Part(PrimitiveType.Capsule, v, new Vector3(-0.27f, 0.66f, 0f) * s, new Vector3(0.11f, 0.2f, 0.11f) * s, body);
            PrimitiveFactory.Part(PrimitiveType.Capsule, v, new Vector3(0.27f, 0.66f, 0f) * s, new Vector3(0.11f, 0.2f, 0.11f) * s, body);
            PrimitiveFactory.Part(PrimitiveType.Sphere, v, new Vector3(0f, 1.1f, 0f) * s, Vector3.one * (0.3f * s), skin);
        }
    }
}
