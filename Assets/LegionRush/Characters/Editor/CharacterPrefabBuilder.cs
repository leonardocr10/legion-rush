using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LegionRush.Characters.EditorTools
{
    // Monta, a partir dos FBX gerados no Blender, o material compartilhado, um Animator Controller por personagem,
    // os prefabs Ally_*/Enemy_*, os prefabs das armas e a CharacterVisualLibrary (Resources) usada pelo jogo.
    // Menu: Legion Rush › Personagens › Montar prefabs. Batch: -executeMethod LegionRush.Characters.EditorTools.CharacterPrefabBuilder.BuildBatch
    public static class CharacterPrefabBuilder
    {
        public const string Root = "Assets/LegionRush/Characters";
        public const string MaterialPath = Root + "/Materials/LR_CharacterAtlas.mat";
        public const string LibraryPath = Root + "/Resources/CharacterVisualLibrary.asset";
        private const string ToonShaderPath = "Assets/_Game/Shaders/LegionToon.shader";

        // Nome do modelo exportado pelo Blender -> prefab e vaga no jogo.
        public static readonly (string model, string prefab, CharacterId id, bool enemy)[] Roster =
        {
            ("Warrior", "Ally_Warrior", CharacterId.Warrior, false),
            ("RoyalArcher", "Ally_RoyalArcher", CharacterId.Archer, false),
            ("RoyalCommander", "Ally_RoyalCommander", CharacterId.Commander, false),
            ("ArcaneMage", "Ally_ArcaneMage", CharacterId.Mage, false),
            ("MountedKnight", "Ally_MountedKnight", CharacterId.MountedKnight, false),
            ("Spearman", "Ally_Spearman", CharacterId.Lancer, false),
            ("Healer", "Ally_Healer", CharacterId.Healer, false),
            ("EnemySoldier", "Enemy_Soldier", CharacterId.EnemySoldier, true),
            ("EnemyArcher", "Enemy_Archer", CharacterId.EnemyArcher, true),
            ("Berserker", "Enemy_Berserker", CharacterId.Berserker, true),
            ("HeavyWarrior", "Enemy_HeavyWarrior", CharacterId.HeavyWarrior, true),
            ("Pikeman", "Enemy_Pikeman", CharacterId.Pikeman, true),
            ("DarkMage", "Enemy_DarkMage", CharacterId.DarkMage, true),
        };

        [MenuItem("Legion Rush/Personagens/Montar prefabs")]
        public static void BuildMenu()
        {
            string report = BuildAll();
            EditorUtility.DisplayDialog("Legion Rush", report, "OK");
        }

        public static void BuildBatch()
        {
            int code = 0;
            try
            {
                Debug.Log("[LegionRush] " + BuildAll());
            }
            catch (Exception exception)
            {
                Debug.LogError("[LegionRush] Montagem dos personagens falhou: " + exception);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        public static string BuildAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Material material = EnsureMaterial();
            BuildWeaponPrefabs(material);
            CharacterVisualLibrary library = EnsureLibrary();
            var built = new List<string>();
            foreach (var entry in Roster)
            {
                string folder = $"{Root}/{(entry.enemy ? "Enemies" : "Allies")}/{entry.model}";
                string modelPath = $"{folder}/Models/{entry.model}.fbx";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(modelPath) == null) continue;
                GameObject prefab = BuildCharacter(folder, modelPath, entry.prefab, material);
                if (prefab == null) continue;
                SetLibraryEntry(library, entry.id, prefab);
                built.Add(entry.prefab);
            }
            library.arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Weapons/Prefabs/Weapon_RoyalArrow.prefab");
            library.enemyArrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Weapons/Prefabs/Weapon_EnemyArrow.prefab");
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            return built.Count == 0 ? "Nenhum modelo encontrado." : "Prefabs: " + string.Join(", ", built);
        }

        // ---------- Material ----------

        private static Material EnsureMaterial()
        {
            EnsureFolder(Root + "/Materials");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ToonShaderPath);
            if (material == null)
            {
                material = new Material(shader) { name = "LR_CharacterAtlas" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/LR_CharacterAtlas.png"));
            material.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/LR_CharacterAtlas_Emission.png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", Color.white * 1.8f);
            material.SetFloat("_OutlineWidth", 0.014f);
            material.SetFloat("_RimIntensity", 0.5f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------- Armas ----------

        private static void BuildWeaponPrefabs(Material material)
        {
            string models = Root + "/Weapons/Models";
            if (!AssetDatabase.IsValidFolder(models)) return;
            EnsureFolder(Root + "/Weapons/Prefabs");
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { models }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                AssignMaterial(instance, material);
                string outPath = $"{Root}/Weapons/Prefabs/{model.name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(instance, outPath);
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        // ---------- Personagem ----------

        private static GameObject BuildCharacter(string folder, string modelPath, string prefabName, Material material)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            CharacterMeta meta = CharacterMeta.LoadNextTo(modelPath);
            Dictionary<string, AnimationClip> clips = AssetDatabase.LoadAllAssetsAtPath(modelPath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal))
                .GroupBy(c => c.name)
                .ToDictionary(g => g.Key, g => g.First());
            if (!clips.ContainsKey("Idle"))
            {
                Debug.LogWarning($"[LegionRush] {modelPath}: sem clip Idle; prefab não montado.");
                return null;
            }

            EnsureFolder(folder + "/Controllers");
            EnsureFolder(folder + "/Prefabs");
            AnimatorController controller = BuildController($"{folder}/Controllers/{meta?.name ?? model.name}.controller", clips, meta);

            var root = new GameObject(prefabName);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            instance.name = "Model";
            // o FBX do Blender chega olhando para -Z; o jogo move as unidades para +Z
            instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            AssignMaterial(instance, material);

            Animator animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var driver = instance.AddComponent<CharacterAnimatorDriver>();
            var so = new SerializedObject(driver);
            CharacterMeta.Clip attack = meta?.FindClip(meta.SlotClip("Attack") ?? "Attack");
            CharacterMeta.Clip death = meta?.FindClip("Death");
            so.FindProperty("impactDelay").floatValue = attack != null && attack.impact > 0f ? attack.impact : 0.18f;
            so.FindProperty("corpseDuration").floatValue = (death != null ? death.length : 1f) + 0.7f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var sockets = root.AddComponent<CharacterSockets>();
            sockets.rightHand = Find(instance.transform, "RightHandSocket");
            sockets.leftHand = Find(instance.transform, "LeftHandSocket");
            sockets.head = Find(instance.transform, "HeadSocket");
            sockets.back = Find(instance.transform, "BackSocket");
            sockets.projectileSpawn = Find(instance.transform, "ProjectileSpawn");
            sockets.vfx = Find(instance.transform, "VFXSocket");

            ConfigureRenderers(instance);

            string prefabPath = $"{folder}/Prefabs/{prefabName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void ConfigureRenderers(GameObject instance)
        {
            LODGroup group = instance.GetComponentInChildren<LODGroup>();
            var lod = new Renderer[3][];
            for (int i = 0; i < 3; i++)
            {
                string suffix = "_LOD" + i;
                lod[i] = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(r => r.name.EndsWith(suffix, StringComparison.Ordinal)).Cast<Renderer>().ToArray();
            }
            // as armas (MeshRenderer filhas dos encaixes) entram nos LOD 0 e 1
            Renderer[] weapons = instance.GetComponentsInChildren<MeshRenderer>(true).Cast<Renderer>().ToArray();
            if (group == null) group = instance.AddComponent<LODGroup>();
            var lods = new List<LOD>();
            float[] thresholds = { 0.16f, 0.055f, 0.004f };
            for (int i = 0; i < 3; i++)
            {
                if (lod[i].Length == 0) continue;
                Renderer[] set = i < 2 ? lod[i].Concat(weapons).ToArray() : lod[i].Concat(weapons).ToArray();
                lods.Add(new LOD(thresholds[i], set));
            }
            group.SetLODs(lods.ToArray());
            group.fadeMode = LODFadeMode.None;
            group.RecalculateBounds();

            foreach (SkinnedMeshRenderer smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.updateWhenOffscreen = false;
                smr.skinnedMotionVectors = false;
                smr.quality = SkinQuality.Bone2;
                smr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                smr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                smr.shadowCastingMode = smr.name.EndsWith("_LOD2", StringComparison.Ordinal)
                    ? UnityEngine.Rendering.ShadowCastingMode.Off
                    : UnityEngine.Rendering.ShadowCastingMode.On;
                // corpo um pouco maior evita sumiço por culling durante saltos e mortes
                Bounds b = smr.localBounds;
                b.Expand(0.4f);
                smr.localBounds = b;
            }
            foreach (MeshRenderer mr in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }
        }

        // ---------- Animator ----------

        private static AnimatorController BuildController(string path, Dictionary<string, AnimationClip> clips, CharacterMeta meta)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) AssetDatabase.DeleteAsset(path);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = "AttackSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f
            });
            controller.AddParameter("AttackVariant", AnimatorControllerParameterType.Int);
            foreach (string trigger in new[] { "Attack", "Hit", "Die", "Victory", "Skill", "Spawn", "Block" })
                controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

            AnimationClip Clip(string slot, string fallback)
            {
                string name = meta?.SlotClip(slot) ?? fallback;
                return name != null && clips.TryGetValue(name, out AnimationClip c) ? c : null;
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(Clip("Idle", "Idle"), 0f);
            AnimationClip walk = Clip("Walk", "Walk");
            if (walk != null) tree.AddChild(walk, 0.35f);
            tree.AddChild(Clip("Run", "Run") ?? walk ?? Clip("Idle", "Idle"), 1f);
            machine.defaultState = locomotion;

            AnimatorState attack = AddState(machine, "Attack", Clip("Attack", "Attack"));
            attack.speedParameter = "AttackSpeed";
            attack.speedParameterActive = true;
            AnimationClip altClip = Clip("AttackAlt", null);
            AnimatorState attackAlt = null;
            if (altClip != null)
            {
                attackAlt = AddState(machine, "AttackAlt", altClip);
                attackAlt.speedParameter = "AttackSpeed";
                attackAlt.speedParameterActive = true;
            }
            AnimatorState hit = AddState(machine, "Hit", Clip("Hit", "Hit"));
            AnimatorState death = AddState(machine, "Death", Clip("Death", "Death"));
            AnimatorState victory = AddState(machine, "Victory", Clip("Victory", "Victory") ?? Clip("Idle", "Idle"));
            AnimationClip skillClip = Clip("Skill", null);
            AnimatorState skill = skillClip != null ? AddState(machine, "Skill", skillClip) : null;
            AnimationClip blockClip = Clip("Block", null);
            AnimatorState block = blockClip != null ? AddState(machine, "Block", blockClip) : null;
            AnimationClip spawnClip = Clip("Spawn", null);
            AnimatorState spawn = spawnClip != null ? AddState(machine, "Spawn", spawnClip) : null;

            AnimatorStateTransition toAttack = AnyState(machine, attack, "Attack", 0.06f, true);
            if (attackAlt != null)
            {
                toAttack.AddCondition(AnimatorConditionMode.Equals, 0f, "AttackVariant");
                AnimatorStateTransition toAlt = AnyState(machine, attackAlt, "Attack", 0.06f, true);
                toAlt.AddCondition(AnimatorConditionMode.Equals, 1f, "AttackVariant");
            }
            AnyState(machine, hit, "Hit", 0.05f, false);
            AnyState(machine, death, "Die", 0.05f, false);
            AnyState(machine, victory, "Victory", 0.15f, false);
            if (skill != null) AnyState(machine, skill, "Skill", 0.08f, false);
            if (block != null) AnyState(machine, block, "Block", 0.06f, false);
            if (spawn != null) AnyState(machine, spawn, "Spawn", 0f, false);
            foreach (AnimatorState state in new[] { attack, attackAlt, hit, skill, block, spawn })
                if (state != null) ReturnTo(state, locomotion);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion)
        {
            AnimatorState state = machine.AddState(name);
            state.motion = motion;
            state.writeDefaultValues = true;
            return state;
        }

        private static AnimatorStateTransition AnyState(AnimatorStateMachine machine, AnimatorState to, string trigger,
            float duration, bool toSelf)
        {
            AnimatorStateTransition t = machine.AddAnyStateTransition(to);
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            t.duration = duration;
            t.hasExitTime = false;
            t.canTransitionToSelf = toSelf;
            return t;
        }

        private static void ReturnTo(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = true;
            t.exitTime = 0.92f;
            t.duration = 0.12f;
        }

        // ---------- Biblioteca ----------

        private static CharacterVisualLibrary EnsureLibrary()
        {
            EnsureFolder(Root + "/Resources");
            var library = AssetDatabase.LoadAssetAtPath<CharacterVisualLibrary>(LibraryPath);
            if (library != null) return library;
            library = ScriptableObject.CreateInstance<CharacterVisualLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
            return library;
        }

        private static void SetLibraryEntry(CharacterVisualLibrary library, CharacterId id, GameObject prefab)
        {
            CharacterVisualEntry entry = library.entries.Find(e => e != null && e.id == id);
            if (entry == null)
            {
                entry = new CharacterVisualEntry { id = id, scale = 1f };
                library.entries.Add(entry);
            }
            entry.prefab = prefab;
        }

        // ---------- util ----------

        private static void AssignMaterial(GameObject go, Material material)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < mats.Length; i++) mats[i] = material;
                r.sharedMaterials = mats;
            }
        }

        public static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = Find(child, name);
                if (found != null) return found;
            }
            return null;
        }

        public static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
