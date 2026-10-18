using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LegionRush.Characters.EditorTools
{
    // Importação dos personagens gerados no Blender (tools/characters/build.py):
    // rig Generic, 2 ossos por vértice, blend shapes sem normais, clips renomeados e loop conforme o .json ao lado do FBX.
    public class CharacterAssetPostprocessor : AssetPostprocessor
    {
        public const string Root = "Assets/LegionRush/Characters/";

        private bool IsCharacterAsset => assetPath.StartsWith(Root, StringComparison.Ordinal);
        private bool IsModel => IsCharacterAsset && assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);
        private bool IsWeapon => assetPath.Contains("/Weapons/");

        private void OnPreprocessModel()
        {
            if (!IsModel) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.isReadable = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.None;
            importer.weldVertices = true;

            if (IsWeapon)
            {
                importer.importBlendShapes = false;
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                return;
            }

            importer.importBlendShapes = true;
            importer.importBlendShapeNormals = ModelImporterNormals.None;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.skinWeights = ModelImporterSkinWeights.Custom;
            importer.maxBonesPerVertex = 2;
            importer.importAnimation = true;
            importer.resampleCurves = true;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.animationRotationError = 0.4f;
            importer.animationPositionError = 0.4f;
            importer.animationScaleError = 0.8f;
        }

        private void OnPreprocessAnimation()
        {
            if (!IsModel || IsWeapon) return;
            var importer = (ModelImporter)assetImporter;
            CharacterMeta meta = CharacterMeta.LoadNextTo(assetPath);
            TakeInfo[] takes = importer.importedTakeInfos;
            var clips = new List<ModelImporterClipAnimation>();
            var seen = new HashSet<string>();
            foreach (TakeInfo take in takes)
            {
                string name = take.name;
                int bar = name.LastIndexOf('|');
                if (bar >= 0) name = name.Substring(bar + 1);
                if (!seen.Add(name)) continue;
                float rate = take.sampleRate > 0f ? take.sampleRate : 30f;
                CharacterMeta.Clip info = meta?.FindClip(name);
                clips.Add(new ModelImporterClipAnimation
                {
                    name = name,
                    takeName = take.name,
                    firstFrame = Mathf.Round(take.startTime * rate),
                    lastFrame = Mathf.Round(take.stopTime * rate),
                    loopTime = info != null ? info.loop : name == "Idle" || name == "Walk" || name == "Run",
                    loopPose = false,
                    lockRootRotation = false,
                    keepOriginalOrientation = true,
                    keepOriginalPositionY = true,
                    keepOriginalPositionXZ = true
                });
            }
            if (clips.Count > 0) importer.clipAnimations = clips.ToArray();
        }

        private void OnPreprocessTexture()
        {
            if (!IsCharacterAsset || !assetPath.Contains("/Textures/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 512;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = 512, format = TextureImporterFormat.ASTC_4x4
            });
        }
    }

    // Espelho do .json gerado pelo Blender (clips, slots do Animator, encaixes e contagem de polígonos).
    [Serializable]
    public class CharacterMeta
    {
        [Serializable]
        public class Clip
        {
            public string name;
            public bool loop;
            public float length;
            public float impact;
        }

        [Serializable]
        public class Slot
        {
            public string slot;
            public string clip;
        }

        [Serializable]
        public class Stats
        {
            public int LOD0_tris;
            public int LOD1_tris;
            public int LOD2_tris;
            public int bones;
            public string shape_keys;
        }

        public string name;
        public List<Clip> clips = new List<Clip>();
        public List<Slot> slots = new List<Slot>();
        public List<string> sockets = new List<string>();
        public Stats stats;

        public Clip FindClip(string clipName) => clips.Find(c => c.name == clipName);

        public string SlotClip(string slotName)
        {
            Slot s = slots.Find(x => x.slot == slotName);
            return s?.clip;
        }

        public static CharacterMeta LoadNextTo(string modelPath)
        {
            string json = Path.ChangeExtension(modelPath, ".json");
            if (!File.Exists(json)) return null;
            return JsonUtility.FromJson<CharacterMeta>(File.ReadAllText(json));
        }
    }
}
