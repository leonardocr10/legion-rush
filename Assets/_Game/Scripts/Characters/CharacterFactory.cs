using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    // Cria personagens: usa o prefab final da CharacterVisualLibrary quando existir; caso contrário, placeholder de primitivas.
    public static class CharacterFactory
    {
        public static CharacterVisualLibrary Library { get; set; }

        private static readonly HashSet<CharacterId> WarnedPlaceholders = new HashSet<CharacterId>();
        private static readonly Color SkinColor = new Color(0.96f, 0.78f, 0.62f);
        private static readonly Color LeatherColor = new Color(0.36f, 0.22f, 0.12f);
        private static readonly Color SteelColor = new Color(0.78f, 0.81f, 0.86f);
        private static readonly Color GoldColor = new Color(1f, 0.76f, 0.25f);
        private static readonly Color CapeColor = new Color(0.75f, 0.12f, 0.15f);
        private static readonly Color WoodColor = new Color(0.5f, 0.34f, 0.2f);

        public static CharacterId IdFor(UnitClass unitClass) => unitClass switch
        {
            UnitClass.Archer => CharacterId.Archer,
            UnitClass.Lancer => CharacterId.Lancer,
            UnitClass.Mage => CharacterId.Mage,
            UnitClass.Healer => CharacterId.Healer,
            UnitClass.Knight => CharacterId.MountedKnight,
            _ => CharacterId.Warrior
        };

        public static GameObject CreateSoldier(UnitClass unitClass, Transform parent, Color tunicColor)
        {
            return TryCreateFromLibrary(IdFor(unitClass), parent, out GameObject root)
                ? root
                : CreatePlaceholderSoldier(unitClass, parent, tunicColor);
        }

        public static GameObject CreateEnemy(Transform parent, Color armorColor) => CreateEnemy(parent, armorColor, EnemyKind.Soldier);

        public static GameObject CreateEnemy(Transform parent, Color armorColor, EnemyKind kind)
        {
            // tipo sem modelo próprio cai no soldado inimigo antes do placeholder
            if (kind != EnemyKind.Soldier && TryCreateFromLibrary(EnemyKinds.Visual(kind), parent, out GameObject typed)) return typed;
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

        public static GameObject CreateBoss(BossDefinition definition, Transform parent)
        {
            // Golem Sombrio: modelo procedural com animações próprias, até existir um prefab final na biblioteca.
            if (definition.visual == CharacterId.BossDarkGolem && (Library == null || !Library.TryGet(definition.visual, out _)))
            {
                Transform visual = CreateRoot("Boss_" + definition.id, parent, out GameObject golem);
                LegionRush.Bosses.DarkGolem.DarkGolemPresentation.Create(visual, definition);
                return golem;
            }
            return TryCreateFromLibrary(definition.visual, parent, out GameObject root)
                ? root
                : CreatePlaceholderBoss(definition, parent);
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

        // ---------- Placeholders das tropas ----------

        private static GameObject CreatePlaceholderSoldier(UnitClass unitClass, Transform parent, Color tunicColor)
        {
            Transform v = CreateRoot(unitClass + "Soldier", parent, out GameObject root);
            Material tunic = MaterialLibrary.Get(tunicColor);
            Material leather = MaterialLibrary.Get(LeatherColor);
            Material steel = MaterialLibrary.Get(SteelColor, 0f, 0.85f);
            Material wood = MaterialLibrary.Get(WoodColor);
            Material skin = MaterialLibrary.Get(SkinColor);

            if (unitClass == UnitClass.Knight)
            {
                BuildHorse(v, MaterialLibrary.Get(new Color(0.45f, 0.3f, 0.18f)), tunic);
                Transform rider = new GameObject("Rider").transform;
                rider.SetParent(v, false);
                rider.localPosition = new Vector3(0f, 0.62f, -0.05f);
                BuildHumanoid(rider, tunic, skin, leather, 0.9f);
                PrimitiveFactory.Part(PrimitiveType.Sphere, rider, new Vector3(0f, 1.05f, 0f), new Vector3(0.32f, 0.26f, 0.32f), steel);
                PrimitiveFactory.Part(PrimitiveType.Cube, rider, new Vector3(0.3f, 0.75f, 0.45f), new Vector3(0.06f, 0.06f, 1.3f), wood, new Vector3(-8f, 0f, 0f));
                PrimitiveFactory.Part(PrimitiveType.Cube, rider, new Vector3(0.3f, 0.82f, 1.1f), new Vector3(0.07f, 0.07f, 0.25f), steel, new Vector3(-8f, 0f, 0f));
                return root;
            }

            BuildHumanoid(v, tunic, skin, leather, 1f);
            switch (unitClass)
            {
                case UnitClass.Archer:
                    PrimitiveFactory.Part(PrimitiveType.Sphere, v, new Vector3(0f, 1.16f, -0.03f), new Vector3(0.36f, 0.3f, 0.36f), tunic);
                    PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.32f, 0.72f, 0.12f), new Vector3(0.05f, 0.78f, 0.07f), leather);
                    PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0f, 0.82f, -0.22f), new Vector3(0.15f, 0.42f, 0.12f), leather, new Vector3(-12f, 0f, 0f));
                    break;
                case UnitClass.Lancer:
                    PrimitiveFactory.Part(PrimitiveType.Sphere, v, new Vector3(0f, 1.17f, 0f), new Vector3(0.35f, 0.27f, 0.35f), steel);
                    PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0f, 1.36f, 0f), new Vector3(0.04f, 0.14f, 0.2f), tunic);
                    PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.3f, 0.95f, 0.15f), new Vector3(0.05f, 1.8f, 0.05f), wood, new Vector3(25f, 0f, 0f));
                    PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.3f, 1.8f, 0.55f), new Vector3(0.07f, 0.25f, 0.07f), steel, new Vector3(25f, 0f, 0f));
                    PrimitiveFactory.Part(PrimitiveType.Cylinder, v, new Vector3(-0.32f, 0.65f, 0.1f), new Vector3(0.38f, 0.03f, 0.38f), steel, new Vector3(0f, 0f, 90f));
                    break;
                case UnitClass.Mage:
                {
                    Material glow = MaterialLibrary.Get(new Color(0.65f, 0.4f, 1f), 3f, 0f, false);
                    PrimitiveFactory.Part(PrimitiveType.Capsule, v, new Vector3(0f, 1.3f, -0.02f), new Vector3(0.34f, 0.32f, 0.34f), tunic);
                    PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0f, 1.0f, 0.13f), new Vector3(0.18f, 0.2f, 0.05f), MaterialLibrary.Get(Color.white));
                    PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.32f, 0.75f, 0.1f), new Vector3(0.05f, 1.2f, 0.05f), wood);
                    PrimitiveFactory.Part(PrimitiveType.Sphere, v, new Vector3(0.32f, 1.4f, 0.1f), Vector3.one * 0.16f, glow, default, false);
                    break;
                }
                case UnitClass.Healer:
                {
                    Material holy = MaterialLibrary.Get(new Color(1f, 0.85f, 0.35f), 2.5f, 0f, false);
                    PrimitiveFactory.Part(PrimitiveType.Sphere, v, new Vector3(0f, 1.16f, -0.04f), new Vector3(0.34f, 0.3f, 0.34f), tunic);
                    PrimitiveFactory.Part(PrimitiveType.Cylinder, v, new Vector3(0f, 0.32f, 0f), new Vector3(0.42f, 0.2f, 0.42f), tunic);
                    PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.32f, 0.75f, 0.1f), new Vector3(0.05f, 1.15f, 0.05f), MaterialLibrary.Get(GoldColor, 0f, 0.7f));
                    PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.32f, 1.38f, 0.1f), new Vector3(0.05f, 0.22f, 0.05f), holy, default, false);
                    PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.32f, 1.38f, 0.1f), new Vector3(0.18f, 0.05f, 0.05f), holy, default, false);
                    break;
                }
                default:
                    PrimitiveFactory.Part(PrimitiveType.Sphere, v, new Vector3(0f, 1.17f, 0f), new Vector3(0.35f, 0.25f, 0.35f), steel);
                    PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(-0.33f, 0.68f, 0.12f), new Vector3(0.07f, 0.52f, 0.4f), steel);
                    PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.33f, 0.8f, 0.3f), new Vector3(0.06f, 0.06f, 0.7f), steel);
                    break;
            }
            return root;
        }

        private static void BuildHorse(Transform v, Material coat, Material barding)
        {
            Material dark = MaterialLibrary.Get(new Color(0.18f, 0.12f, 0.08f));
            PrimitiveFactory.Part(PrimitiveType.Capsule, v, new Vector3(0f, 0.62f, 0f), new Vector3(0.42f, 0.55f, 0.42f), coat, new Vector3(90f, 0f, 0f));
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0f, 0.68f, 0f), new Vector3(0.48f, 0.22f, 0.6f), barding);
            PrimitiveFactory.Part(PrimitiveType.Capsule, v, new Vector3(0f, 0.95f, 0.55f), new Vector3(0.2f, 0.3f, 0.2f), coat, new Vector3(35f, 0f, 0f));
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0f, 1.12f, 0.78f), new Vector3(0.16f, 0.16f, 0.32f), coat);
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0f, 1.1f, 0.5f), new Vector3(0.05f, 0.3f, 0.3f), dark, new Vector3(35f, 0f, 0f));
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.14f : 0.14f;
                float z = i < 2 ? 0.32f : -0.32f;
                PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(x, 0.2f, z), new Vector3(0.1f, 0.42f, 0.1f), coat);
            }
        }

        private static GameObject CreatePlaceholderEnemy(Transform parent, Color armorColor)
        {
            Transform v = CreateRoot("Enemy", parent, out GameObject root);
            Material armor = MaterialLibrary.Get(armorColor, 0f, 0.4f);
            Material dark = MaterialLibrary.Get(new Color(0.15f, 0.12f, 0.14f), 0f, 0.6f);
            Material eyes = MaterialLibrary.Get(new Color(1f, 0.35f, 0.1f), 3f, 0f, false);
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

        // ---------- Placeholders dos chefões (silhuetas distintas por chefão) ----------

        private static GameObject CreatePlaceholderBoss(BossDefinition d, Transform parent)
        {
            Transform v = CreateRoot("Boss_" + d.id, parent, out GameObject root);
            Transform body = new GameObject("Body").transform;
            body.SetParent(v, false);
            body.localScale = Vector3.one * (d.scale / 1.6f);

            Material primary = MaterialLibrary.Get(d.primaryColor, 0f, 0.5f);
            Material secondary = MaterialLibrary.Get(d.secondaryColor, 0f, 0.7f);
            Material glow = MaterialLibrary.Get(d.glowColor, 3f, 0f, false);

            if (d.id == BossId.AncientDragon)
            {
                BuildDragon(body, primary, secondary, glow);
                return root;
            }

            BuildHumanoid(body, primary, secondary, secondary, 1.15f);
            PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(-0.08f, 1.28f, 0.17f), new Vector3(0.08f, 0.04f, 0.03f), glow, default, false);
            PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(0.08f, 1.28f, 0.17f), new Vector3(0.08f, 0.04f, 0.03f), glow, default, false);

            switch (d.id)
            {
                case BossId.DarkTitan:
                    for (int side = -1; side <= 1; side += 2)
                    {
                        PrimitiveFactory.Part(PrimitiveType.Sphere, body, new Vector3(side * 0.42f, 1.0f, 0f), Vector3.one * 0.32f, secondary);
                        PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(side * 0.5f, 1.2f, 0f), new Vector3(0.07f, 0.28f, 0.07f), glow, new Vector3(0f, 0f, side * -30f), false);
                    }
                    PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(0.5f, 0.7f, 0.35f), new Vector3(0.06f, 0.06f, 0.8f), secondary);
                    PrimitiveFactory.Part(PrimitiveType.Sphere, body, new Vector3(0.5f, 0.7f, 0.8f), Vector3.one * 0.32f, primary);
                    PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(0.5f, 0.86f, 0.8f), new Vector3(0.05f, 0.16f, 0.05f), glow, default, false);
                    break;
                case BossId.MadPharaoh:
                    PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(0f, 1.3f, -0.05f), new Vector3(0.48f, 0.42f, 0.36f), primary);
                    PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(-0.22f, 1.05f, -0.02f), new Vector3(0.1f, 0.4f, 0.2f), secondary);
                    PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(0.22f, 1.05f, -0.02f), new Vector3(0.1f, 0.4f, 0.2f), secondary);
                    PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(0.38f, 0.8f, 0.15f), new Vector3(0.05f, 1.5f, 0.05f), primary);
                    PrimitiveFactory.Part(PrimitiveType.Sphere, body, new Vector3(0.38f, 1.58f, 0.15f), Vector3.one * 0.2f, glow, default, false);
                    break;
                case BossId.LordVulcan:
                    for (int side = -1; side <= 1; side += 2)
                    {
                        PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(side * 0.18f, 1.48f, 0f), new Vector3(0.07f, 0.32f, 0.07f), secondary, new Vector3(0f, 0f, side * -35f));
                        PrimitiveFactory.Part(PrimitiveType.Sphere, body, new Vector3(side * 0.42f, 1.0f, 0f), Vector3.one * 0.34f, primary);
                    }
                    PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(0f, 0.72f, 0.19f), new Vector3(0.3f, 0.05f, 0.02f), glow, default, false);
                    PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(0.05f, 0.6f, 0.19f), new Vector3(0.04f, 0.25f, 0.02f), glow, default, false);
                    PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(0.5f, 0.75f, 0.3f), new Vector3(0.36f, 0.36f, 0.36f), primary, new Vector3(20f, 30f, 0f));
                    break;
                default:
                    PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(0f, 0.72f, 0f), new Vector3(0.62f, 0.5f, 0.42f), primary);
                    PrimitiveFactory.Part(PrimitiveType.Cylinder, body, new Vector3(0f, 0.75f, 0.22f), new Vector3(0.22f, 0.02f, 0.22f), glow, new Vector3(90f, 0f, 0f), false);
                    PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(0f, 1.2f, 0f), new Vector3(0.32f, 0.26f, 0.3f), secondary);
                    for (int side = -1; side <= 1; side += 2)
                        PrimitiveFactory.Part(PrimitiveType.Cube, body, new Vector3(side * 0.45f, 0.62f, 0.05f), new Vector3(0.28f, 0.32f, 0.32f), secondary);
                    break;
            }
            return root;
        }

        private static void BuildDragon(Transform v, Material scales, Material belly, Material fire)
        {
            PrimitiveFactory.Part(PrimitiveType.Capsule, v, new Vector3(0f, 0.75f, 0f), new Vector3(0.6f, 0.75f, 0.6f), scales, new Vector3(75f, 0f, 0f));
            PrimitiveFactory.Part(PrimitiveType.Capsule, v, new Vector3(0f, 0.72f, 0.12f), new Vector3(0.45f, 0.6f, 0.4f), belly, new Vector3(75f, 0f, 0f));
            PrimitiveFactory.Part(PrimitiveType.Capsule, v, new Vector3(0f, 1.25f, 0.55f), new Vector3(0.24f, 0.4f, 0.24f), scales, new Vector3(30f, 0f, 0f));
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0f, 1.5f, 0.85f), new Vector3(0.3f, 0.24f, 0.5f), scales);
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(-0.08f, 1.58f, 1.08f), new Vector3(0.06f, 0.04f, 0.04f), fire, default, false);
            PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(0.08f, 1.58f, 1.08f), new Vector3(0.06f, 0.04f, 0.04f), fire, default, false);
            PrimitiveFactory.Part(PrimitiveType.Capsule, v, new Vector3(0f, 0.5f, -0.85f), new Vector3(0.18f, 0.6f, 0.18f), scales, new Vector3(-70f, 0f, 0f));
            for (int side = -1; side <= 1; side += 2)
            {
                PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(side * 0.8f, 1.35f, -0.1f), new Vector3(1.2f, 0.04f, 0.8f), belly, new Vector3(0f, 0f, side * -25f));
                PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(side * 0.25f, 0.25f, 0.3f), new Vector3(0.16f, 0.5f, 0.16f), scales);
                PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(side * 0.25f, 0.25f, -0.35f), new Vector3(0.18f, 0.5f, 0.18f), scales);
                PrimitiveFactory.Part(PrimitiveType.Cube, v, new Vector3(side * 0.1f, 1.72f, 0.7f), new Vector3(0.05f, 0.25f, 0.05f), belly, new Vector3(-30f, 0f, side * 15f));
            }
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
