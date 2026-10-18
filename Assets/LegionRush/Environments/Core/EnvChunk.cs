using UnityEngine;
using UnityEngine.Rendering;

namespace LegionRush.Environments
{
    // Um trecho do cenário: um construtor por material. Vira 1 objeto por material com geometria
    // (culling por trecho, poucas draw calls e nenhum GameObject por peça).
    public class EnvChunk
    {
        private readonly EnvMeshBuilder[] builders = new EnvMeshBuilder[(int)EnvMaterial.Count];

        public EnvMeshBuilder Opaque => this[EnvMaterial.Opaque];
        public EnvMeshBuilder Masonry => this[EnvMaterial.Masonry];
        public EnvMeshBuilder Road => this[EnvMaterial.Road];
        public EnvMeshBuilder Glow => this[EnvMaterial.Glow];
        public EnvMeshBuilder Banner => this[EnvMaterial.Banner];
        public EnvMeshBuilder Ice => this[EnvMaterial.Ice];
        public EnvMeshBuilder Distant => this[EnvMaterial.Distant];

        public EnvMeshBuilder this[EnvMaterial id]
        {
            get
            {
                EnvMeshBuilder b = builders[(int)id];
                if (b == null)
                {
                    b = new EnvMeshBuilder();
                    if (id == EnvMaterial.Masonry)
                    {
                        b.UvMode = EnvUvMode.WorldBox;
                        b.WorldUvScale = 0.5f;
                    }
                    builders[(int)id] = b;
                }
                return b;
            }
        }

        // Aplica a mesma matriz em todos os construtores (peças que usam vários materiais).
        public Matrix4x4 Matrix
        {
            set
            {
                for (int i = 0; i < builders.Length; i++) this[(EnvMaterial)i].Matrix = value;
            }
        }

        public int VertexCount
        {
            get
            {
                int total = 0;
                foreach (EnvMeshBuilder b in builders) if (b != null) total += b.VertexCount;
                return total;
            }
        }

        public void Build(Transform parent, string name, EnvShadowMode shadows)
        {
            for (int i = 0; i < builders.Length; i++)
            {
                EnvMeshBuilder b = builders[i];
                if (b == null || b.IsEmpty) continue;
                var id = (EnvMaterial)i;
                Material material = EnvMaterials.Get(id);
                if (material == null) continue;
                var go = new GameObject($"{name}_{id}");
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = b.ToMesh($"{name}_{id}");
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                bool casts = (shadows != EnvShadowMode.None && (id == EnvMaterial.Opaque || id == EnvMaterial.Masonry))
                    || (shadows == EnvShadowMode.All && id == EnvMaterial.Banner);
                renderer.shadowCastingMode = casts ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = id != EnvMaterial.Ice && id != EnvMaterial.Glow;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                b.Clear();
            }
        }
    }

    public enum EnvShadowMode { None, Large, All }
}
