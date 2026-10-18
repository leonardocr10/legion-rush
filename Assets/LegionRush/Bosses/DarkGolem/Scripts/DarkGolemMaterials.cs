using System.Collections.Generic;
using UnityEngine;

namespace LegionRush.Bosses.DarkGolem
{
    public enum GolemSlot { Metal = 0, Crystal = 1, Eyes = 2, Cloth = 3, Count = 4 }

    // Conjunto de materiais de uma variação de cor. Todos usam o LegionRush/ToonLit (URP, mobile, SRP Batcher)
    // com uma textura-paleta 4×1 (sem filtro): a UV de cada face escolhe a cor, então uma malha só serve às 4 variações.
    public sealed class DarkGolemMaterialSet
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly string[] VariantSuffix = { string.Empty, "_Roxo", "_Azul", "_Laranja" };
        private static readonly Dictionary<int, DarkGolemMaterialSet> Cache = new Dictionary<int, DarkGolemMaterialSet>();

        public Material DarkMetal { get; private set; }
        public Material Crystal { get; private set; }
        public Material Eyes { get; private set; }
        public Material Cloth { get; private set; }
        public Material Weapon { get; private set; }
        public Material[] BodyMaterials { get; private set; }
        public DarkGolemVariant Variant { get; private set; }

        private float crystalEmission;
        private float eyeEmission;

        public static DarkGolemMaterialSet Get(DarkGolemProfile profile, int variantIndex)
        {
            int key = Mathf.Clamp(variantIndex, 0, 64);
            if (Cache.TryGetValue(key, out DarkGolemMaterialSet cached) && cached.DarkMetal != null)
            {
                cached.crystalEmission = profile.crystalEmission;
                cached.eyeEmission = profile.eyeEmission;
                return cached;
            }
            var set = new DarkGolemMaterialSet(profile, profile.Variant(variantIndex), key);
            Cache[key] = set;
            return set;
        }

        private DarkGolemMaterialSet(DarkGolemProfile profile, DarkGolemVariant v, int index)
        {
            Variant = v;
            crystalEmission = profile.crystalEmission;
            eyeEmission = profile.eyeEmission;
            string suffix = index < VariantSuffix.Length ? VariantSuffix[index] : "_" + index;
            float outline = profile.outlineWidth;

            DarkMetal = Create("MAT_Boss_DarkMetal" + suffix, Palette("TEX_Boss_DarkMetal" + suffix, v.metal, v.metalEdge, v.metalDark, v.cloth),
                0.55f, outline, new Color(0.75f, 0.8f, 1f), 0.35f);
            Crystal = Create("MAT_Boss_RedCrystal" + suffix,
                Palette("TEX_Boss_Crystal" + suffix, v.crystal * 0.85f, Color.Lerp(v.crystal, Color.white, 0.45f), v.crystal * 0.45f, Color.white),
                0.2f, outline * 0.6f, Color.Lerp(v.crystal, Color.white, 0.5f), 0.8f);
            Eyes = Create("MAT_Boss_GlowingEyes" + suffix, Palette("TEX_Boss_Eyes" + suffix, v.eyes, Color.Lerp(v.eyes, Color.white, 0.6f), v.eyes, Color.white),
                0f, 0f, Color.white, 0f);
            Cloth = Create("MAT_Boss_RedCloth" + suffix, Palette("TEX_Boss_Cloth" + suffix, v.cloth, Color.Lerp(v.cloth, Color.white, 0.18f), v.clothDark, v.metalEdge),
                0f, outline, new Color(1f, 0.85f, 0.8f), 0.25f);
            Weapon = Create("MAT_Boss_Weapon" + suffix, Palette("TEX_Boss_Weapon" + suffix, v.metal * 0.85f, v.metalEdge, v.metalDark, v.weaponSpikes),
                0.65f, outline, new Color(0.8f, 0.85f, 1f), 0.4f);

            Crystal.SetTexture(EmissionMapId, Crystal.GetTexture(BaseMapId));
            Eyes.SetTexture(EmissionMapId, Eyes.GetTexture(BaseMapId));
            BodyMaterials = new Material[(int)GolemSlot.Count];
            BodyMaterials[(int)GolemSlot.Metal] = DarkMetal;
            BodyMaterials[(int)GolemSlot.Crystal] = Crystal;
            BodyMaterials[(int)GolemSlot.Eyes] = Eyes;
            BodyMaterials[(int)GolemSlot.Cloth] = Cloth;
            SetGlow(1f);
        }

        // Intensidade dos olhos e cristais (dormente, fases, atordoado). 1 = brilho normal da fase 1.
        public void SetGlow(float amount)
        {
            amount = Mathf.Max(0f, amount);
            Crystal.SetColor(EmissionColorId, Variant.crystal * (crystalEmission * amount));
            Eyes.SetColor(EmissionColorId, Variant.eyes * (eyeEmission * Mathf.Max(0.15f, amount)));
        }

        private static Material Create(string name, Texture2D palette, float metallic, float outline, Color rim, float rimIntensity)
        {
            Material m = MaterialLibrary.CreateLit(Color.white, 0f, metallic, outline > 0f);
            m.name = name;
            m.SetTexture(BaseMapId, palette);
            m.mainTexture = palette;
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", outline);
            if (m.HasProperty("_RimColor")) m.SetColor("_RimColor", rim);
            if (m.HasProperty("_RimIntensity")) m.SetFloat("_RimIntensity", rimIntensity);
            if (m.HasProperty("_OutlineColor")) m.SetColor("_OutlineColor", new Color(0.05f, 0.03f, 0.05f));
            if (m.HasProperty("_EmissionColor")) m.SetColor(EmissionColorId, Color.black);
            return m;
        }

        public static Texture2D Palette(string name, Color plate, Color edge, Color dark, Color trim)
        {
            var texture = new Texture2D(GolemMeshBuilder.PaletteColumns, 1, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new[] { plate, edge, dark, trim });
            texture.Apply(false, false);
            return texture;
        }
    }
}
