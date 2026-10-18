using UnityEngine;
using UnityEngine.Rendering;

namespace LegionRush.Bosses.DarkGolem
{
    public enum GolemBone
    {
        Root, Pelvis, Spine, Chest, Neck, Head,
        ShoulderL, UpperArmL, ForearmL, HandL,
        ShoulderR, UpperArmR, ForearmR, HandR,
        UpperLegL, LowerLegL, FootL,
        UpperLegR, LowerLegR, FootR,
        SkirtFront, SkirtBack,
        Count
    }

    // Esqueleto + referências do modelo montado.
    public sealed class DarkGolemRig
    {
        public Transform Model;
        public Transform[] Bones;
        public Quaternion[] BindRotations;
        public Vector3[] BindPositions;
        public SkinnedMeshRenderer Body;
        public Transform MaceSocket;
        public Transform Mace;
        public Transform MaceHead;
        public Transform HeadTop;
        public DarkGolemMaterialSet Materials;
        public float ModelScale;

        public Transform this[GolemBone bone] => Bones[(int)bone];
    }

    // Golem Sombrio gerado por código (o Blender não está disponível nesta máquina): placas de armadura chanfradas,
    // ombreiras grandes com cristais vermelhos, olhos emissivos, saiote vermelho e maça cravada separada na mão direita.
    // É uma malha única com pele rígida (cada placa segue um osso) + a maça como objeto próprio (Boss_DarkGolem_Mace).
    // Medidas em unidades do modelo: o golem tem ~2 unidades de altura, escalado para BossDefinition.scale metros.
    public static class DarkGolemModel
    {
        // Pivôs dos ossos no espaço do modelo (pose de ligação, rotações identidade). Direita = +X (lado da maça).
        private static readonly Vector3[] Pivots = BuildPivots();
        private static readonly GolemBone[] Parents = BuildParents();

        public static DarkGolemRig Build(Transform parent, DarkGolemProfile profile, int variant, float heightMeters)
        {
            var rig = new DarkGolemRig
            {
                Bones = new Transform[(int)GolemBone.Count],
                BindRotations = new Quaternion[(int)GolemBone.Count],
                BindPositions = new Vector3[(int)GolemBone.Count],
                Materials = DarkGolemMaterialSet.Get(profile, variant)
            };

            var model = new GameObject("Boss_DarkGolem").transform;
            model.SetParent(parent, false);
            rig.ModelScale = heightMeters / Mathf.Max(0.5f, profile.modelHeight);
            model.localScale = Vector3.one * rig.ModelScale;
            rig.Model = model;

            for (int i = 0; i < (int)GolemBone.Count; i++)
            {
                var bone = new GameObject(((GolemBone)i).ToString()).transform;
                Transform boneParent = i == 0 ? model : rig.Bones[(int)Parents[i]];
                bone.SetParent(boneParent, false);
                Vector3 parentPivot = i == 0 ? Vector3.zero : Pivots[(int)Parents[i]];
                bone.localPosition = Pivots[i] - parentPivot;
                bone.localRotation = Quaternion.identity;
                rig.Bones[i] = bone;
                rig.BindRotations[i] = Quaternion.identity;
                rig.BindPositions[i] = bone.localPosition;
            }

            var bodyGo = new GameObject("Boss_DarkGolem_Body");
            bodyGo.transform.SetParent(model, false);
            var body = bodyGo.AddComponent<SkinnedMeshRenderer>();
            body.sharedMesh = SharedBodyMesh;
            body.bones = rig.Bones;
            body.rootBone = rig.Bones[(int)GolemBone.Root];
            body.sharedMaterials = rig.Materials.BodyMaterials;
            body.localBounds = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(3.2f, 3.2f, 3.2f));
            body.updateWhenOffscreen = false;
            body.quality = SkinQuality.Bone1;
            body.shadowCastingMode = ShadowCastingMode.On;
            rig.Body = body;

            // Encaixe da maça na mão direita: o cabo atravessa o punho, a bola pende ao lado do pé.
            rig.MaceSocket = new GameObject("MaceSocket").transform;
            rig.MaceSocket.SetParent(rig[GolemBone.HandR], false);
            rig.MaceSocket.localPosition = new Vector3(0.01f, -0.04f, 0.02f);
            rig.MaceSocket.localRotation = Quaternion.Euler(0f, 0f, 8f);
            BuildMace(rig);

            rig.HeadTop = new GameObject("HeadTop").transform;
            rig.HeadTop.SetParent(rig[GolemBone.Head], false);
            rig.HeadTop.localPosition = new Vector3(0f, 0.42f, 0f);
            return rig;
        }

        private static Vector3[] BuildPivots()
        {
            var p = new Vector3[(int)GolemBone.Count];
            p[(int)GolemBone.Root] = Vector3.zero;
            p[(int)GolemBone.Pelvis] = new Vector3(0f, 0.8f, 0f);
            p[(int)GolemBone.Spine] = new Vector3(0f, 0.97f, -0.02f);
            p[(int)GolemBone.Chest] = new Vector3(0f, 1.18f, -0.04f);
            p[(int)GolemBone.Neck] = new Vector3(0f, 1.5f, 0.04f);
            p[(int)GolemBone.Head] = new Vector3(0f, 1.56f, 0.12f);
            for (int side = -1; side <= 1; side += 2)
            {
                bool right = side > 0;
                p[(int)(right ? GolemBone.ShoulderR : GolemBone.ShoulderL)] = new Vector3(side * 0.4f, 1.5f, -0.02f);
                p[(int)(right ? GolemBone.UpperArmR : GolemBone.UpperArmL)] = new Vector3(side * 0.63f, 1.45f, 0f);
                p[(int)(right ? GolemBone.ForearmR : GolemBone.ForearmL)] = new Vector3(side * 0.7f, 1.07f, 0.03f);
                p[(int)(right ? GolemBone.HandR : GolemBone.HandL)] = new Vector3(side * 0.74f, 0.7f, 0.08f);
                p[(int)(right ? GolemBone.UpperLegR : GolemBone.UpperLegL)] = new Vector3(side * 0.22f, 0.76f, 0f);
                p[(int)(right ? GolemBone.LowerLegR : GolemBone.LowerLegL)] = new Vector3(side * 0.25f, 0.45f, 0.03f);
                p[(int)(right ? GolemBone.FootR : GolemBone.FootL)] = new Vector3(side * 0.26f, 0.13f, 0f);
            }
            p[(int)GolemBone.SkirtFront] = new Vector3(0f, 0.78f, 0.22f);
            p[(int)GolemBone.SkirtBack] = new Vector3(0f, 0.78f, -0.24f);
            return p;
        }

        private static GolemBone[] BuildParents()
        {
            var parents = new GolemBone[(int)GolemBone.Count];
            parents[(int)GolemBone.Pelvis] = GolemBone.Root;
            parents[(int)GolemBone.Spine] = GolemBone.Pelvis;
            parents[(int)GolemBone.Chest] = GolemBone.Spine;
            parents[(int)GolemBone.Neck] = GolemBone.Chest;
            parents[(int)GolemBone.Head] = GolemBone.Neck;
            parents[(int)GolemBone.ShoulderL] = GolemBone.Chest;
            parents[(int)GolemBone.UpperArmL] = GolemBone.ShoulderL;
            parents[(int)GolemBone.ForearmL] = GolemBone.UpperArmL;
            parents[(int)GolemBone.HandL] = GolemBone.ForearmL;
            parents[(int)GolemBone.ShoulderR] = GolemBone.Chest;
            parents[(int)GolemBone.UpperArmR] = GolemBone.ShoulderR;
            parents[(int)GolemBone.ForearmR] = GolemBone.UpperArmR;
            parents[(int)GolemBone.HandR] = GolemBone.ForearmR;
            parents[(int)GolemBone.UpperLegL] = GolemBone.Pelvis;
            parents[(int)GolemBone.LowerLegL] = GolemBone.UpperLegL;
            parents[(int)GolemBone.FootL] = GolemBone.LowerLegL;
            parents[(int)GolemBone.UpperLegR] = GolemBone.Pelvis;
            parents[(int)GolemBone.LowerLegR] = GolemBone.UpperLegR;
            parents[(int)GolemBone.FootR] = GolemBone.LowerLegR;
            parents[(int)GolemBone.SkirtFront] = GolemBone.Pelvis;
            parents[(int)GolemBone.SkirtBack] = GolemBone.Pelvis;
            return parents;
        }

        // ---------- Corpo ----------

        private static Mesh BuildBodyMesh()
        {
            var b = new GolemMeshBuilder((int)GolemSlot.Count);
            BuildPelvis(b);
            BuildTorso(b);
            BuildHead(b);
            for (int side = -1; side <= 1; side += 2)
            {
                BuildShoulder(b, side);
                BuildArm(b, side);
                BuildLeg(b, side);
            }
            return b.ToMesh("Boss_DarkGolem_Body", true);
        }

        private static void Part(GolemMeshBuilder b, GolemBone bone, GolemSlot slot, Vector3 position, Vector3 euler)
        {
            b.Bone = (int)bone;
            b.Slot = (int)slot;
            b.Place(position, euler);
        }

        private static void BuildPelvis(GolemMeshBuilder b)
        {
            // Cinto largo + fivela com filete vermelho + escarcelas laterais.
            Part(b, GolemBone.Pelvis, GolemSlot.Metal, new Vector3(0f, 0.81f, 0f), Vector3.zero);
            b.ChamferBox(new Vector3(0.68f, 0.16f, 0.48f), 0.045f);
            Part(b, GolemBone.Pelvis, GolemSlot.Metal, new Vector3(0f, 0.8f, 0.245f), Vector3.zero);
            b.ChamferBox(new Vector3(0.24f, 0.15f, 0.06f), 0.025f, Swatch.Edge, Swatch.Dark);
            Part(b, GolemBone.Pelvis, GolemSlot.Metal, new Vector3(0f, 0.8f, 0.28f), Vector3.zero);
            b.ChamferBox(new Vector3(0.12f, 0.07f, 0.03f), 0.01f, Swatch.Trim, Swatch.Dark);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(b, GolemBone.Pelvis, GolemSlot.Metal, new Vector3(side * 0.35f, 0.7f, 0f), new Vector3(0f, 0f, side * 14f));
                b.ChamferBox(new Vector3(0.16f, 0.24f, 0.34f), 0.04f);
                Part(b, GolemBone.Pelvis, GolemSlot.Cloth, new Vector3(side * 0.3f, 0.6f, 0.04f), new Vector3(0f, 0f, side * 10f));
                b.ChamferBox(new Vector3(0.12f, 0.3f, 0.3f), 0.02f, Swatch.Plate, Swatch.Dark);
            }

            // Saiote vermelho em tiras: frente e costas em ossos próprios (balançam com as pernas).
            for (int i = -1; i <= 1; i++)
            {
                float len = i == 0 ? 0.42f : 0.36f;
                Part(b, GolemBone.SkirtFront, GolemSlot.Cloth, new Vector3(i * 0.16f, 0.78f - len * 0.5f, 0.245f), new Vector3(-6f, i * -8f, i * -4f));
                b.ChamferBox(new Vector3(0.155f, len, 0.045f), 0.015f, Swatch.Plate, Swatch.Dark, 1f, 1f, 0f);
                Part(b, GolemBone.SkirtFront, GolemSlot.Cloth, new Vector3(i * 0.16f, 0.78f - len + 0.03f, 0.262f), new Vector3(-6f, i * -8f, i * -4f));
                b.ChamferBox(new Vector3(0.155f, 0.04f, 0.02f), 0.008f, Swatch.Dark, Swatch.Dark);

                Part(b, GolemBone.SkirtBack, GolemSlot.Cloth, new Vector3(i * 0.17f, 0.78f - len * 0.5f, -0.255f), new Vector3(6f, i * 8f, i * 4f));
                b.ChamferBox(new Vector3(0.165f, len, 0.045f), 0.015f, Swatch.Plate, Swatch.Dark);
            }
        }

        private static void BuildTorso(GolemMeshBuilder b)
        {
            // Abdômen segmentado.
            Part(b, GolemBone.Spine, GolemSlot.Metal, new Vector3(0f, 0.99f, -0.01f), Vector3.zero);
            b.ChamferBox(new Vector3(0.54f, 0.26f, 0.42f), 0.05f, Swatch.Dark, Swatch.Dark, 1.12f, 1.05f);
            for (int i = 0; i < 2; i++)
            {
                Part(b, GolemBone.Spine, GolemSlot.Metal, new Vector3(0f, 0.93f + i * 0.1f, 0.2f), new Vector3(-4f, 0f, 0f));
                b.ChamferBox(new Vector3(0.44f - i * 0.02f, 0.09f, 0.08f), 0.025f);
            }

            // Peitoral massivo, mais largo no topo.
            Part(b, GolemBone.Chest, GolemSlot.Metal, new Vector3(0f, 1.32f, -0.05f), Vector3.zero);
            b.ChamferBox(new Vector3(0.78f, 0.46f, 0.52f), 0.08f, Swatch.Plate, Swatch.Edge, 1.22f, 1.12f, 0.05f);
            for (int side = -1; side <= 1; side += 2)
            {
                // Placas peitorais anguladas com filete vermelho embaixo.
                Part(b, GolemBone.Chest, GolemSlot.Metal, new Vector3(side * 0.2f, 1.37f, 0.22f), new Vector3(-8f, side * 14f, side * -4f));
                b.ChamferBox(new Vector3(0.36f, 0.27f, 0.11f), 0.045f, Swatch.Plate, Swatch.Edge, 1.1f);
                Part(b, GolemBone.Chest, GolemSlot.Metal, new Vector3(side * 0.2f, 1.225f, 0.255f), new Vector3(-8f, side * 14f, side * -4f));
                b.ChamferBox(new Vector3(0.33f, 0.035f, 0.05f), 0.012f, Swatch.Trim, Swatch.Trim);
                // Placas laterais das costelas.
                Part(b, GolemBone.Chest, GolemSlot.Metal, new Vector3(side * 0.4f, 1.24f, 0f), new Vector3(0f, 0f, side * -10f));
                b.ChamferBox(new Vector3(0.1f, 0.26f, 0.4f), 0.03f, Swatch.Dark, Swatch.Edge);
            }
            Part(b, GolemBone.Chest, GolemSlot.Metal, new Vector3(0f, 1.32f, 0.27f), Vector3.zero);
            b.ChamferBox(new Vector3(0.1f, 0.3f, 0.08f), 0.025f, Swatch.Edge, Swatch.Dark);
            // Gola.
            Part(b, GolemBone.Chest, GolemSlot.Metal, new Vector3(0f, 1.54f, 0f), Vector3.zero);
            b.ChamferBox(new Vector3(0.52f, 0.1f, 0.38f), 0.04f, Swatch.Dark, Swatch.Edge);

            // Corcunda das costas com fileira de cristais.
            Part(b, GolemBone.Chest, GolemSlot.Metal, new Vector3(0f, 1.42f, -0.29f), new Vector3(18f, 0f, 0f));
            b.ChamferBox(new Vector3(0.66f, 0.4f, 0.26f), 0.07f, Swatch.Plate, Swatch.Edge, 0.85f);
            Part(b, GolemBone.Chest, GolemSlot.Metal, new Vector3(0f, 1.25f, -0.33f), new Vector3(8f, 0f, 0f));
            b.ChamferBox(new Vector3(0.56f, 0.12f, 0.12f), 0.03f, Swatch.Edge, Swatch.Dark);
            Spike(b, GolemBone.Chest, new Vector3(0f, 1.58f, -0.36f), new Vector3(-40f, 0f, 0f), 0.075f, 0.36f);
            Spike(b, GolemBone.Chest, new Vector3(0f, 1.42f, -0.43f), new Vector3(-62f, 0f, 0f), 0.065f, 0.28f);
            Spike(b, GolemBone.Chest, new Vector3(0f, 1.27f, -0.42f), new Vector3(-75f, 0f, 0f), 0.05f, 0.2f);
            for (int side = -1; side <= 1; side += 2)
            {
                Spike(b, GolemBone.Chest, new Vector3(side * 0.2f, 1.53f, -0.38f), new Vector3(-48f, 0f, side * -20f), 0.06f, 0.27f);
                Spike(b, GolemBone.Chest, new Vector3(side * 0.18f, 1.36f, -0.43f), new Vector3(-68f, 0f, side * -18f), 0.05f, 0.2f);
            }
        }

        private static void BuildHead(GolemMeshBuilder b)
        {
            Part(b, GolemBone.Neck, GolemSlot.Metal, new Vector3(0f, 1.52f, 0.05f), Vector3.zero);
            b.ChamferBox(new Vector3(0.24f, 0.14f, 0.22f), 0.03f, Swatch.Dark, Swatch.Dark);

            // Elmo: bloco afunilado, viseira escura, sobrancelhas em V, olhos rasgados emissivos e grade da boca.
            Part(b, GolemBone.Head, GolemSlot.Metal, new Vector3(0f, 1.67f, 0.15f), Vector3.zero);
            b.ChamferBox(new Vector3(0.36f, 0.3f, 0.34f), 0.06f, Swatch.Plate, Swatch.Edge, 0.82f, 0.9f, 0.04f);
            Part(b, GolemBone.Head, GolemSlot.Metal, new Vector3(0f, 1.645f, 0.3f), Vector3.zero);
            b.ChamferBox(new Vector3(0.27f, 0.09f, 0.06f), 0.015f, Swatch.Dark, Swatch.Dark);
            Part(b, GolemBone.Head, GolemSlot.Metal, new Vector3(0f, 1.555f, 0.29f), new Vector3(-6f, 0f, 0f));
            b.ChamferBox(new Vector3(0.28f, 0.12f, 0.08f), 0.02f, Swatch.Dark, Swatch.Edge);
            for (int i = 0; i < 4; i++)
            {
                Part(b, GolemBone.Head, GolemSlot.Metal, new Vector3(-0.09f + i * 0.06f, 1.545f, 0.335f), new Vector3(-6f, 0f, 0f));
                b.ChamferBox(new Vector3(0.042f, 0.075f, 0.03f), 0.008f, Swatch.Edge, Swatch.Plate);
            }
            Part(b, GolemBone.Head, GolemSlot.Metal, new Vector3(0f, 1.49f, 0.22f), Vector3.zero);
            b.ChamferBox(new Vector3(0.3f, 0.07f, 0.24f), 0.025f, Swatch.Plate, Swatch.Edge);
            Part(b, GolemBone.Head, GolemSlot.Metal, new Vector3(0f, 1.72f, 0.315f), new Vector3(-20f, 0f, 0f));
            b.ChamferBox(new Vector3(0.07f, 0.12f, 0.05f), 0.015f, Swatch.Edge, Swatch.Plate);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(b, GolemBone.Head, GolemSlot.Metal, new Vector3(side * 0.085f, 1.705f, 0.32f), new Vector3(-12f, 0f, side * 20f));
                b.ChamferBox(new Vector3(0.17f, 0.055f, 0.09f), 0.015f, Swatch.Edge, Swatch.Plate);
                Part(b, GolemBone.Head, GolemSlot.Eyes, new Vector3(side * 0.08f, 1.655f, 0.335f), new Vector3(0f, 0f, side * 16f));
                b.ChamferBox(new Vector3(0.095f, 0.035f, 0.02f), 0.006f, Swatch.Plate, Swatch.Edge);
                Part(b, GolemBone.Head, GolemSlot.Metal, new Vector3(side * 0.18f, 1.62f, 0.18f), new Vector3(0f, side * 10f, 0f));
                b.ChamferBox(new Vector3(0.07f, 0.18f, 0.24f), 0.02f, Swatch.Plate, Swatch.Edge);
                Spike(b, GolemBone.Head, new Vector3(side * 0.13f, 1.79f, 0.1f), new Vector3(-10f, 0f, side * -32f), 0.045f, 0.17f);
            }
            // Crista de cristal no topo do elmo.
            Spike(b, GolemBone.Head, new Vector3(0f, 1.8f, 0.13f), new Vector3(-8f, 0f, 0f), 0.075f, 0.36f);
        }

        private static void BuildShoulder(GolemMeshBuilder b, int side)
        {
            GolemBone bone = side > 0 ? GolemBone.ShoulderR : GolemBone.ShoulderL;
            // Ombreira grande em duas camadas, caindo para fora, com quatro cristais.
            Part(b, bone, GolemSlot.Metal, new Vector3(side * 0.6f, 1.62f, -0.01f), new Vector3(0f, 0f, side * -18f));
            b.ChamferBox(new Vector3(0.48f, 0.3f, 0.52f), 0.11f, Swatch.Plate, Swatch.Edge, 0.85f, 0.88f);
            Part(b, bone, GolemSlot.Metal, new Vector3(side * 0.68f, 1.49f, -0.01f), new Vector3(0f, 0f, side * -30f));
            b.ChamferBox(new Vector3(0.4f, 0.11f, 0.5f), 0.04f, Swatch.Edge, Swatch.Dark);
            Part(b, bone, GolemSlot.Metal, new Vector3(side * 0.74f, 1.42f, -0.01f), new Vector3(0f, 0f, side * -38f));
            b.ChamferBox(new Vector3(0.3f, 0.04f, 0.46f), 0.012f, Swatch.Trim, Swatch.Trim);
            Spike(b, bone, new Vector3(side * 0.5f, 1.76f, 0.12f), new Vector3(14f, 0f, side * -12f), 0.08f, 0.36f);
            Spike(b, bone, new Vector3(side * 0.64f, 1.73f, -0.03f), new Vector3(0f, 0f, side * -28f), 0.09f, 0.44f);
            Spike(b, bone, new Vector3(side * 0.53f, 1.74f, -0.17f), new Vector3(-20f, 0f, side * -16f), 0.075f, 0.32f);
            Spike(b, bone, new Vector3(side * 0.79f, 1.63f, 0.06f), new Vector3(8f, 0f, side * -58f), 0.06f, 0.26f);
            Spike(b, bone, new Vector3(side * 0.77f, 1.62f, -0.14f), new Vector3(-14f, 0f, side * -62f), 0.05f, 0.2f);
        }

        private static void BuildArm(GolemMeshBuilder b, int side)
        {
            bool right = side > 0;
            GolemBone upper = right ? GolemBone.UpperArmR : GolemBone.UpperArmL;
            GolemBone fore = right ? GolemBone.ForearmR : GolemBone.ForearmL;
            GolemBone hand = right ? GolemBone.HandR : GolemBone.HandL;

            Part(b, upper, GolemSlot.Metal, new Vector3(side * 0.66f, 1.27f, 0.01f), new Vector3(0f, 0f, side * 6f));
            b.ChamferBox(new Vector3(0.26f, 0.38f, 0.27f), 0.05f, Swatch.Dark, Swatch.Edge);
            Part(b, upper, GolemSlot.Metal, new Vector3(side * 0.69f, 1.3f, 0.02f), new Vector3(0f, 0f, side * 6f));
            b.ChamferBox(new Vector3(0.3f, 0.16f, 0.3f), 0.04f);
            Part(b, upper, GolemSlot.Metal, new Vector3(side * 0.71f, 1.08f, -0.06f), Vector3.zero);
            b.ChamferBox(new Vector3(0.2f, 0.14f, 0.2f), 0.04f, Swatch.Edge, Swatch.Dark);

            // Manopla larga (mais grossa embaixo) com braçadeira escura e cristais.
            Part(b, fore, GolemSlot.Metal, new Vector3(side * 0.73f, 0.88f, 0.06f), Vector3.zero);
            b.ChamferBox(new Vector3(0.34f, 0.4f, 0.34f), 0.065f, Swatch.Plate, Swatch.Edge, 1.0f, 1.0f);
            Part(b, fore, GolemSlot.Metal, new Vector3(side * 0.73f, 0.79f, 0.06f), Vector3.zero);
            b.ChamferBox(new Vector3(0.38f, 0.07f, 0.38f), 0.02f, Swatch.Dark, Swatch.Dark);
            Part(b, fore, GolemSlot.Metal, new Vector3(side * 0.73f, 1.0f, 0.06f), Vector3.zero);
            b.ChamferBox(new Vector3(0.36f, 0.05f, 0.36f), 0.015f, Swatch.Trim, Swatch.Trim);
            Spike(b, fore, new Vector3(side * 0.88f, 0.92f, 0.05f), new Vector3(0f, 0f, side * -72f), 0.06f, 0.26f);
            Spike(b, fore, new Vector3(side * 0.82f, 0.95f, -0.1f), new Vector3(-50f, 0f, side * -40f), 0.05f, 0.2f);

            // Punho fechado: dorso, nós dos dedos e polegar.
            Part(b, hand, GolemSlot.Metal, new Vector3(side * 0.74f, 0.6f, 0.08f), Vector3.zero);
            b.ChamferBox(new Vector3(0.3f, 0.25f, 0.3f), 0.06f, Swatch.Plate, Swatch.Edge);
            for (int i = 0; i < 4; i++)
            {
                Part(b, hand, GolemSlot.Metal, new Vector3(side * (0.65f + i * 0.06f), 0.56f, 0.23f), Vector3.zero);
                b.ChamferBox(new Vector3(0.062f, 0.09f, 0.08f), 0.018f, Swatch.Edge, Swatch.Dark);
            }
            Part(b, hand, GolemSlot.Metal, new Vector3(side * 0.62f, 0.62f, 0.17f), new Vector3(0f, 0f, side * 20f));
            b.ChamferBox(new Vector3(0.08f, 0.13f, 0.1f), 0.02f, Swatch.Plate, Swatch.Edge);
        }

        private static void BuildLeg(GolemMeshBuilder b, int side)
        {
            bool right = side > 0;
            GolemBone upper = right ? GolemBone.UpperLegR : GolemBone.UpperLegL;
            GolemBone lower = right ? GolemBone.LowerLegR : GolemBone.LowerLegL;
            GolemBone foot = right ? GolemBone.FootR : GolemBone.FootL;

            Part(b, upper, GolemSlot.Metal, new Vector3(side * 0.23f, 0.6f, 0f), Vector3.zero);
            b.ChamferBox(new Vector3(0.3f, 0.34f, 0.32f), 0.06f, Swatch.Dark, Swatch.Edge);
            Part(b, lower, GolemSlot.Metal, new Vector3(side * 0.25f, 0.45f, 0.17f), new Vector3(-10f, 0f, 0f));
            b.ChamferBox(new Vector3(0.24f, 0.17f, 0.12f), 0.04f, Swatch.Edge, Swatch.Plate);
            Part(b, lower, GolemSlot.Metal, new Vector3(side * 0.26f, 0.3f, 0.03f), Vector3.zero);
            b.ChamferBox(new Vector3(0.32f, 0.3f, 0.34f), 0.06f, Swatch.Plate, Swatch.Edge, 0.9f, 0.9f);
            Part(b, foot, GolemSlot.Metal, new Vector3(side * 0.27f, 0.075f, 0.06f), Vector3.zero);
            b.ChamferBox(new Vector3(0.36f, 0.15f, 0.48f), 0.045f, Swatch.Plate, Swatch.Edge, 0.92f, 0.95f);
            for (int i = -1; i <= 1; i++)
            {
                Part(b, foot, GolemSlot.Metal, new Vector3(side * 0.27f + i * 0.1f, 0.05f, 0.31f), Vector3.zero);
                b.ChamferBox(new Vector3(0.09f, 0.09f, 0.09f), 0.025f, Swatch.Edge, Swatch.Dark);
            }
        }

        private static void Spike(GolemMeshBuilder b, GolemBone bone, Vector3 position, Vector3 euler, float radius, float height)
        {
            Part(b, bone, GolemSlot.Crystal, position, euler);
            b.Crystal(radius, height, 5, position.x * 3f);
        }

        // ---------- Maça (objeto separado, preso ao encaixe da mão) ----------

        public static Mesh BuildMaceMesh()
        {
            var b = new GolemMeshBuilder(1);
            b.Place(new Vector3(0f, 0.02f, 0f), Vector3.zero);
            b.Prism(0.045f, 0.46f, 8, Swatch.Dark, Swatch.Edge);
            b.Place(new Vector3(0f, 0.27f, 0f), Vector3.zero);
            b.ChamferBox(new Vector3(0.11f, 0.08f, 0.11f), 0.02f, Swatch.Edge, Swatch.Dark);
            b.Place(new Vector3(0f, -0.17f, 0f), Vector3.zero);
            b.ChamferBox(new Vector3(0.12f, 0.06f, 0.12f), 0.02f, Swatch.Edge, Swatch.Dark);

            const float ballY = -0.4f;
            const float ballRadius = 0.22f;
            b.Place(new Vector3(0f, ballY, 0f), Vector3.zero);
            b.FacetedSphere(ballRadius, Swatch.Plate, Swatch.Dark);
            // Cravos: nas direções do icosaedro (12) + 6 eixos, alternando tamanhos.
            var dirs = new[]
            {
                Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back,
                new Vector3(1, 1, 1), new Vector3(-1, 1, 1), new Vector3(1, 1, -1), new Vector3(-1, 1, -1),
                new Vector3(1, -1, 1), new Vector3(-1, -1, 1), new Vector3(1, -1, -1), new Vector3(-1, -1, -1)
            };
            for (int i = 0; i < dirs.Length; i++)
            {
                if (i == 0) continue;
                Vector3 d = dirs[i].normalized;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, d);
                b.Matrix = Matrix4x4.TRS(new Vector3(0f, ballY, 0f) + d * ballRadius * 0.85f, rotation, Vector3.one);
                float length = i < 6 ? 0.17f : 0.13f;
                SpikeCone(b, i < 6 ? 0.075f : 0.06f, length);
            }
            return b.ToMesh("Boss_DarkGolem_Mace", false);
        }

        private static void SpikeCone(GolemMeshBuilder b, float radius, float height)
        {
            const int sides = 4;
            var tip = new Vector3(0f, height, 0f);
            for (int i = 0; i < sides; i++)
            {
                float a0 = i / (float)sides * Mathf.PI * 2f + Mathf.PI * 0.25f;
                float a1 = (i + 1) / (float)sides * Mathf.PI * 2f + Mathf.PI * 0.25f;
                var p0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
                var p1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
                if (Vector3.Dot(Vector3.Cross(p1 - p0, tip - p0), p0 + p1) < 0f) b.Triangle(p0, tip, p1, (i & 1) == 0 ? Swatch.Trim : Swatch.Edge);
                else b.Triangle(p0, p1, tip, (i & 1) == 0 ? Swatch.Trim : Swatch.Edge);
            }
        }

        private static Mesh maceMesh;
        private static Mesh bodyMeshCache;

        private static void BuildMace(DarkGolemRig rig)
        {
            if (maceMesh == null) maceMesh = BuildMaceMesh();
            var mace = new GameObject("Boss_DarkGolem_Mace");
            mace.transform.SetParent(rig.MaceSocket, false);
            mace.AddComponent<MeshFilter>().sharedMesh = maceMesh;
            var renderer = mace.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = rig.Materials.Weapon;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            rig.Mace = mace.transform;
            rig.MaceHead = new GameObject("MaceHead").transform;
            rig.MaceHead.SetParent(mace.transform, false);
            rig.MaceHead.localPosition = new Vector3(0f, -0.4f, 0f);
        }

        // Malha do corpo é a mesma para todas as instâncias e variações (só os materiais mudam).
        public static Mesh SharedBodyMesh
        {
            get
            {
                if (bodyMeshCache == null)
                {
                    bodyMeshCache = BuildBodyMesh();
                    var bindposes = new Matrix4x4[(int)GolemBone.Count];
                    for (int i = 0; i < bindposes.Length; i++) bindposes[i] = Matrix4x4.Translate(-Pivots[i]);
                    bodyMeshCache.bindposes = bindposes;
                }
                return bodyMeshCache;
            }
        }
    }
}
