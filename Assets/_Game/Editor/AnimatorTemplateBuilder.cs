using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LegionRush.EditorTools
{
    public static class AnimatorTemplateBuilder
    {
        private const string Folder = "Assets/_Game/Animations";
        private const string ControllerPath = Folder + "/LR_BaseCharacter.controller";
        private static readonly string[] Slots = { "Idle", "Run", "Attack", "Hit", "Death", "Victory", "Skill", "Spawn" };

        private static readonly Dictionary<string, string[]> Keywords = new Dictionary<string, string[]>
        {
            { "Idle", new[] { "idle" } },
            { "Run", new[] { "run", "walk", "move" } },
            { "Attack", new[] { "attack", "atk", "shoot", "slash", "cast", "strike" } },
            { "Hit", new[] { "hit", "damage", "hurt" } },
            { "Death", new[] { "death", "die", "dead" } },
            { "Victory", new[] { "victory", "win", "cheer" } },
            { "Skill", new[] { "skill", "special", "ability" } },
            { "Spawn", new[] { "spawn", "intro", "appear" } }
        };

        [MenuItem("Legion Rush/Animation/Create Base Animator Controller")]
        public static void CreateBaseController()
        {
            EnsureFolder(Folder);
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                bool recreate = EditorUtility.DisplayDialog("Legion Rush",
                    "LR_BaseCharacter.controller já existe. Recriar? Overrides existentes precisarão ser regenerados.",
                    "Recriar", "Cancelar");
                if (!recreate) return;
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = "AttackSpeed",
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 1f
            });
            foreach (string trigger in new[] { "Attack", "Hit", "Die", "Victory", "Skill", "Spawn" })
                controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

            var clips = new Dictionary<string, AnimationClip>();
            foreach (string slot in Slots)
            {
                var clip = new AnimationClip { name = "LR_" + slot };
                if (slot == "Idle" || slot == "Run" || slot == "Victory")
                {
                    AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                    settings.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(clip, settings);
                }
                AssetDatabase.AddObjectToAsset(clip, controller);
                clips[slot] = clip;
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(clips["Idle"], 0f);
            tree.AddChild(clips["Run"], 1f);
            machine.defaultState = locomotion;

            AnimatorState attack = AddState(machine, "Attack", clips["Attack"]);
            attack.speedParameter = "AttackSpeed";
            attack.speedParameterActive = true;
            AnimatorState hit = AddState(machine, "Hit", clips["Hit"]);
            AnimatorState skill = AddState(machine, "Skill", clips["Skill"]);
            AnimatorState spawn = AddState(machine, "Spawn", clips["Spawn"]);
            AnimatorState death = AddState(machine, "Death", clips["Death"]);
            AnimatorState victory = AddState(machine, "Victory", clips["Victory"]);

            AddAnyStateTrigger(machine, attack, "Attack", 0.05f, true);
            AddAnyStateTrigger(machine, hit, "Hit", 0.05f, false);
            AddAnyStateTrigger(machine, skill, "Skill", 0.08f, false);
            AddAnyStateTrigger(machine, spawn, "Spawn", 0f, false);
            AddAnyStateTrigger(machine, death, "Die", 0.05f, false);
            AddAnyStateTrigger(machine, victory, "Victory", 0.15f, false);
            foreach (AnimatorState state in new[] { attack, hit, skill, spawn }) ReturnTo(state, locomotion);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Selection.activeObject = controller;
            Debug.Log($"[LegionRush] Controller base criado em {ControllerPath}.");
        }

        [MenuItem("Legion Rush/Animation/Create Override From Selected Model")]
        public static void CreateOverrideFromSelection()
        {
            var baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (baseController == null)
            {
                Dialog("Crie primeiro o controller base (Legion Rush/Animation/Create Base Animator Controller).");
                return;
            }

            string modelPath = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : null;
            if (string.IsNullOrEmpty(modelPath))
            {
                Dialog("Selecione um FBX ou modelo no painel Project.");
                return;
            }

            AnimationClip[] sourceClips = AssetDatabase.LoadAllAssetsAtPath(modelPath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .ToArray();
            if (sourceClips.Length == 0)
            {
                Dialog("Nenhum AnimationClip encontrado em " + modelPath);
                return;
            }

            var overrideController = new AnimatorOverrideController(baseController)
            {
                name = Path.GetFileNameWithoutExtension(modelPath) + "_Override"
            };
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
            overrideController.GetOverrides(overrides);

            var missing = new List<string>();
            for (int i = 0; i < overrides.Count; i++)
            {
                string slot = overrides[i].Key.name.Replace("LR_", string.Empty);
                AnimationClip match = FindClip(sourceClips, slot);
                if (match == null) missing.Add(slot);
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, match);
            }
            overrideController.ApplyOverrides(overrides);

            string directory = Path.GetDirectoryName(modelPath)?.Replace('\\', '/') ?? Folder;
            string outPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{overrideController.name}.overrideController");
            AssetDatabase.CreateAsset(overrideController, outPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = overrideController;

            string report = missing.Count == 0 ? "Todos os slots mapeados." : "Slots sem clip: " + string.Join(", ", missing);
            Debug.Log($"[LegionRush] Override criado em {outPath}. {report}");
        }

        private static AnimationClip FindClip(AnimationClip[] clips, string slot)
        {
            if (!Keywords.TryGetValue(slot, out string[] words)) return null;
            foreach (string word in words)
                foreach (AnimationClip clip in clips)
                    if (clip.name.ToLowerInvariant().Contains(word)) return clip;
            return null;
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion)
        {
            AnimatorState state = machine.AddState(name);
            state.motion = motion;
            state.writeDefaultValues = true;
            return state;
        }

        private static void AddAnyStateTrigger(AnimatorStateMachine machine, AnimatorState to, string trigger,
            float duration, bool canTransitionToSelf)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            transition.duration = duration;
            transition.hasExitTime = false;
            transition.canTransitionToSelf = canTransitionToSelf;
            transition.interruptionSource = TransitionInterruptionSource.None;
        }

        private static void ReturnTo(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 0.9f;
            transition.duration = 0.12f;
        }

        private static void EnsureFolder(string path)
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

        private static void Dialog(string message) => EditorUtility.DisplayDialog("Legion Rush", message, "OK");
    }
}
