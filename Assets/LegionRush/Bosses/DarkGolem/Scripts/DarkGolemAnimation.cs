using System.Collections.Generic;
using UnityEngine;

namespace LegionRush.Bosses.DarkGolem
{
    public enum GolemClipId { Idle, Walk, Attack1, Attack2, GroundSlam, Hit, Stun, Roar, Death, Spawn, Dormant, Count }

    // Pose = rotação extra (graus, Euler local) de cada osso + deslocamento/rotação da raiz.
    // Convenções (golem olhando para +Z, direita = +X): X+ inclina para a frente / dobra; braço X−90 aponta para a frente,
    // X−170 acima da cabeça; braço direito Z+ abre para fora (esquerdo Z−).
    public sealed class GolemPose
    {
        public readonly Vector3[] Euler = new Vector3[(int)GolemBone.Count];
        public Vector3 RootOffset;

        public void Clear()
        {
            for (int i = 0; i < Euler.Length; i++) Euler[i] = Vector3.zero;
            RootOffset = Vector3.zero;
        }

        public void CopyFrom(GolemPose other)
        {
            System.Array.Copy(other.Euler, Euler, Euler.Length);
            RootOffset = other.RootOffset;
        }

        public void Lerp(GolemPose a, GolemPose b, float t)
        {
            for (int i = 0; i < Euler.Length; i++) Euler[i] = Vector3.LerpUnclamped(a.Euler[i], b.Euler[i], t);
            RootOffset = Vector3.LerpUnclamped(a.RootOffset, b.RootOffset, t);
        }

        public void Add(GolemPose other, float weight)
        {
            for (int i = 0; i < Euler.Length; i++) Euler[i] += other.Euler[i] * weight;
            RootOffset += other.RootOffset * weight;
        }

        // Ângulos acumulados (giro de 360°) voltam para (−180, 180] antes de misturar com a próxima animação.
        public void Wrap()
        {
            for (int i = 0; i < Euler.Length; i++)
            {
                Vector3 e = Euler[i];
                Euler[i] = new Vector3(Mathf.DeltaAngle(0f, e.x), Mathf.DeltaAngle(0f, e.y), Mathf.DeltaAngle(0f, e.z));
            }
        }
    }

    public sealed class GolemKey
    {
        public float Time;
        public readonly Dictionary<GolemBone, Vector3> Bones = new Dictionary<GolemBone, Vector3>();
        public Vector3 Root;

        public GolemKey(float time) => Time = time;

        public GolemKey Set(GolemBone bone, float x, float y = 0f, float z = 0f)
        {
            Bones[bone] = new Vector3(x, y, z);
            return this;
        }

        // Os dois lados com espelhamento (Y e Z invertidos no lado esquerdo).
        public GolemKey Both(GolemBone right, GolemBone left, float x, float y = 0f, float z = 0f)
        {
            Bones[right] = new Vector3(x, y, z);
            Bones[left] = new Vector3(x, -y, -z);
            return this;
        }

        public GolemKey Move(float x, float y, float z)
        {
            Root = new Vector3(x, y, z);
            return this;
        }
    }

    // Clipe gerado por código: poses-chave com interpolação suave. 'Impact' marca o quadro do golpe
    // (o apresentador estica o trecho até ele para coincidir com o fim do aviso do ataque).
    public sealed class GolemClip
    {
        public GolemClipId Id;
        public float Length;
        public bool Loop;
        public float Impact = -1f;
        public readonly List<GolemKey> Keys = new List<GolemKey>();

        public GolemKey Key(float time)
        {
            var key = new GolemKey(time);
            Keys.Add(key);
            return key;
        }

        // Amostra o clipe no tempo normalizado (0..1) somando à pose base.
        public void Sample(float normalized, GolemPose basePose, GolemPose output)
        {
            output.CopyFrom(basePose);
            if (Keys.Count == 0) return;
            normalized = Loop ? Mathf.Repeat(normalized, 1f) : Mathf.Clamp01(normalized);

            int next = 0;
            while (next < Keys.Count && Keys[next].Time < normalized) next++;
            GolemKey a, b;
            float t;
            if (next == 0)
            {
                a = Loop ? Keys[Keys.Count - 1] : Keys[0];
                b = Keys[0];
                float span = Loop ? (1f - a.Time) + b.Time : 1f;
                t = Loop && span > 1e-4f ? (normalized + 1f - a.Time) / span : 1f;
            }
            else if (next >= Keys.Count)
            {
                a = Keys[Keys.Count - 1];
                b = Loop ? Keys[0] : a;
                float span = (1f - a.Time) + b.Time;
                t = Loop && span > 1e-4f ? (normalized - a.Time) / span : 1f;
            }
            else
            {
                a = Keys[next - 1];
                b = Keys[next];
                float span = b.Time - a.Time;
                t = span > 1e-4f ? (normalized - a.Time) / span : 1f;
            }
            t = Mathf.Clamp01(t);
            t = t * t * (3f - 2f * t);

            foreach (KeyValuePair<GolemBone, Vector3> pair in a.Bones)
            {
                Vector3 to = b.Bones.TryGetValue(pair.Key, out Vector3 v) ? v : Vector3.zero;
                output.Euler[(int)pair.Key] += Vector3.LerpUnclamped(pair.Value, to, t);
            }
            foreach (KeyValuePair<GolemBone, Vector3> pair in b.Bones)
            {
                if (a.Bones.ContainsKey(pair.Key)) continue;
                output.Euler[(int)pair.Key] += pair.Value * t;
            }
            output.RootOffset += Vector3.LerpUnclamped(a.Root, b.Root, t);
        }
    }

    // Biblioteca das animações do Golem (procedurais, por poses-chave). Postura base: corcunda, braços abertos pelo volume.
    public static class DarkGolemAnimation
    {
        public static readonly GolemPose BasePose = CreateBase();
        private static GolemClip[] clips;

        public static GolemClip Get(GolemClipId id)
        {
            if (clips == null) Build();
            return clips[(int)id];
        }

        private static GolemPose CreateBase()
        {
            var p = new GolemPose();
            Set(p, GolemBone.Spine, 6f, 0f, 0f);
            Set(p, GolemBone.Chest, 12f, 0f, 0f);
            Set(p, GolemBone.Neck, -6f, 0f, 0f);
            Set(p, GolemBone.Head, -10f, 0f, 0f);
            Set(p, GolemBone.UpperArmR, -8f, 0f, 9f);
            Set(p, GolemBone.UpperArmL, -8f, 0f, -9f);
            Set(p, GolemBone.ForearmR, -16f, 0f, -4f);
            Set(p, GolemBone.ForearmL, -16f, 0f, 4f);
            Set(p, GolemBone.UpperLegR, -6f, 0f, 3f);
            Set(p, GolemBone.UpperLegL, -6f, 0f, -3f);
            Set(p, GolemBone.LowerLegR, 12f, 0f, 0f);
            Set(p, GolemBone.LowerLegL, 12f, 0f, 0f);
            Set(p, GolemBone.FootR, -6f, 0f, 0f);
            Set(p, GolemBone.FootL, -6f, 0f, 0f);
            p.RootOffset = new Vector3(0f, -0.025f, 0f);
            return p;
        }

        private static void Set(GolemPose p, GolemBone bone, float x, float y, float z) => p.Euler[(int)bone] = new Vector3(x, y, z);

        private static GolemClip New(GolemClipId id, float length, bool loop, float impact = -1f)
        {
            var clip = new GolemClip { Id = id, Length = length, Loop = loop, Impact = impact };
            clips[(int)id] = clip;
            return clip;
        }

        private static void Build()
        {
            clips = new GolemClip[(int)GolemClipId.Count];
            const GolemBone UAR = GolemBone.UpperArmR, UAL = GolemBone.UpperArmL, FAR = GolemBone.ForearmR, FAL = GolemBone.ForearmL;
            const GolemBone ULR = GolemBone.UpperLegR, ULL = GolemBone.UpperLegL, LLR = GolemBone.LowerLegR, LLL = GolemBone.LowerLegL;
            const GolemBone FR = GolemBone.FootR, FL = GolemBone.FootL, SHR = GolemBone.ShoulderR, SHL = GolemBone.ShoulderL;
            const GolemBone Chest = GolemBone.Chest, Spine = GolemBone.Spine, Head = GolemBone.Head, Pelvis = GolemBone.Pelvis;
            const GolemBone Root = GolemBone.Root, Neck = GolemBone.Neck, HR = GolemBone.HandR, HL = GolemBone.HandL;

            // Respiração pesada: peito sobe, ombros acompanham, cabeça balança.
            GolemClip idle = New(GolemClipId.Idle, 2.6f, true);
            idle.Key(0f).Set(Chest, 0f).Both(SHR, SHL, 0f, 0f, 0f).Set(Head, 0f, 0f, 0f).Move(0f, 0f, 0f);
            idle.Key(0.5f).Set(Chest, -4f).Both(SHR, SHL, 0f, 0f, 4f).Both(UAR, UAL, -3f, 0f, 3f).Set(Head, 3f, 4f, 0f).Move(0f, 0.012f, 0f);

            // Passada pesada: pernas alternadas, pelve gira, braços balançam ao contrário, dois apoios por ciclo.
            GolemClip walk = New(GolemClipId.Walk, 1.25f, true);
            walk.Key(0f).Set(ULR, -26f).Set(LLR, 6f).Set(FR, 14f).Set(ULL, 22f).Set(LLL, 18f).Set(FL, -10f)
                .Set(Pelvis, 0f, 7f, 3f).Set(Chest, 4f, -8f, 0f).Set(UAR, 16f).Set(UAL, -18f).Set(FAL, -12f).Move(0f, -0.04f, 0f);
            walk.Key(0.25f).Set(ULR, 0f).Set(LLR, 8f).Set(ULL, -10f).Set(LLL, 55f).Set(FL, -12f)
                .Set(Pelvis, 0f, 0f, -4f).Set(Chest, 2f, 0f, 2f).Move(0f, 0.04f, 0f);
            walk.Key(0.5f).Set(ULL, -26f).Set(LLL, 6f).Set(FL, 14f).Set(ULR, 22f).Set(LLR, 18f).Set(FR, -10f)
                .Set(Pelvis, 0f, -7f, -3f).Set(Chest, 4f, 8f, 0f).Set(UAL, 16f).Set(UAR, -18f).Set(FAR, -12f).Move(0f, -0.04f, 0f);
            walk.Key(0.75f).Set(ULL, 0f).Set(LLL, 8f).Set(ULR, -10f).Set(LLR, 55f).Set(FR, -12f)
                .Set(Pelvis, 0f, 0f, 4f).Set(Chest, 2f, 0f, -2f).Move(0f, 0.04f, 0f);

            // Ataque 1: maça acima da cabeça, golpe vertical à frente (impacto em 45%).
            GolemClip attack1 = New(GolemClipId.Attack1, 1.6f, false, 0.45f);
            attack1.Key(0f);
            attack1.Key(0.15f).Set(UAR, -60f, 0f, 20f).Set(FAR, -40f).Set(Chest, -6f, -10f, 0f).Set(UAL, -20f, 0f, -20f);
            attack1.Key(0.38f).Set(UAR, -175f, 0f, 22f).Set(FAR, -55f).Set(HR, -20f).Set(Chest, -18f, -18f, 0f).Set(Spine, -6f, -6f, 0f)
                .Set(UAL, -30f, 0f, -35f).Set(Head, -10f).Set(ULR, -12f).Set(LLR, 10f).Move(0f, 0.04f, -0.05f);
            attack1.Key(0.45f).Set(UAR, -45f, 0f, 6f).Set(FAR, -10f).Set(HR, 20f).Set(Chest, 30f, 12f, 0f).Set(Spine, 12f, 6f, 0f)
                .Set(UAL, 10f, 0f, -30f).Set(Head, 10f).Set(ULR, -30f).Set(LLR, 40f).Set(ULL, 10f).Set(LLL, 25f).Move(0f, -0.1f, 0.12f);
            attack1.Key(0.7f).Set(UAR, -40f, 0f, 8f).Set(FAR, -8f).Set(HR, 15f).Set(Chest, 26f, 10f, 0f).Set(Spine, 10f, 5f, 0f)
                .Set(UAL, 5f, 0f, -25f).Set(ULR, -26f).Set(LLR, 34f).Set(LLL, 20f).Move(0f, -0.08f, 0.1f);
            attack1.Key(1f);

            // Ataque 2 (área): carrega para trás e gira 360° com os braços abertos (impacto em 38%).
            GolemClip attack2 = New(GolemClipId.Attack2, 1.7f, false, 0.38f);
            attack2.Key(0f);
            attack2.Key(0.3f).Set(Root, 0f, -55f, 0f).Set(Chest, -4f, -35f, 0f).Set(UAR, -70f, -30f, 70f).Set(FAR, -20f)
                .Set(UAL, -30f, 0f, -50f).Set(ULR, -14f).Set(LLR, 20f).Set(LLL, 16f).Move(0f, -0.06f, 0f);
            attack2.Key(0.38f).Set(Root, 0f, 20f, 0f).Set(Chest, 8f, 20f, 0f).Set(UAR, -80f, 0f, 85f).Set(FAR, -5f)
                .Set(UAL, -60f, 0f, -75f).Set(LLR, 18f).Set(LLL, 18f).Move(0f, -0.07f, 0f);
            attack2.Key(0.62f).Set(Root, 0f, 250f, 0f).Set(Chest, 8f, 15f, 0f).Set(UAR, -82f, 0f, 88f).Set(FAR, -5f)
                .Set(UAL, -60f, 0f, -80f).Set(LLR, 18f).Set(LLL, 18f).Move(0f, -0.06f, 0f);
            attack2.Key(0.8f).Set(Root, 0f, 360f, 0f).Set(Chest, 12f, 10f, 0f).Set(UAR, -50f, 0f, 50f).Set(UAL, -30f, 0f, -40f)
                .Set(LLR, 12f).Set(LLL, 12f).Move(0f, -0.05f, 0f);
            attack2.Key(1f).Set(Root, 0f, 360f, 0f);

            // Pancada no solo: os dois punhos (e a maça) acima da cabeça e esmagam o chão (impacto em 50%).
            GolemClip slam = New(GolemClipId.GroundSlam, 1.9f, false, 0.5f);
            slam.Key(0f);
            slam.Key(0.38f).Both(UAR, UAL, -172f, 0f, 14f).Both(FAR, FAL, -50f).Set(Chest, -24f).Set(Spine, -8f).Set(Head, -16f)
                .Both(ULR, ULL, 4f).Move(0f, 0.08f, -0.06f);
            slam.Key(0.5f).Both(UAR, UAL, -38f, 0f, 16f).Both(FAR, FAL, -6f).Set(Chest, 42f).Set(Spine, 16f).Set(Head, 12f)
                .Both(ULR, ULL, -38f, 0f, 6f).Both(LLR, LLL, 62f).Both(FR, FL, -22f).Move(0f, -0.24f, 0.08f);
            slam.Key(0.72f).Both(UAR, UAL, -42f, 0f, 18f).Both(FAR, FAL, -8f).Set(Chest, 38f).Set(Spine, 14f).Set(Head, 18f)
                .Both(ULR, ULL, -34f, 0f, 6f).Both(LLR, LLL, 56f).Both(FR, FL, -20f).Move(0f, -0.22f, 0.07f);
            slam.Key(1f);

            // Reação ao golpe (aditiva): tranco para trás.
            GolemClip hit = New(GolemClipId.Hit, 0.4f, false);
            hit.Key(0f);
            hit.Key(0.25f).Set(Chest, -12f, 6f, 4f).Set(Head, -14f, 0f, -6f).Both(UAR, UAL, 6f, 0f, 10f).Move(0f, 0f, -0.05f);
            hit.Key(1f);

            // Atordoado: cabeça pendendo e oscilando, joelhos dobrados, braços soltos.
            GolemClip stun = New(GolemClipId.Stun, 1.8f, true);
            stun.Key(0f).Set(Chest, 22f, 0f, 6f).Set(Head, 26f, 10f, 12f).Set(Neck, 8f).Both(UAR, UAL, 12f, 0f, -4f).Both(FAR, FAL, 10f)
                .Both(ULR, ULL, -18f).Both(LLR, LLL, 30f).Both(FR, FL, -12f).Set(Pelvis, 0f, 0f, 4f).Move(0.03f, -0.12f, 0f);
            stun.Key(0.5f).Set(Chest, 24f, 0f, -6f).Set(Head, 28f, -10f, -12f).Set(Neck, 8f).Both(UAR, UAL, 14f, 0f, -2f).Both(FAR, FAL, 12f)
                .Both(ULR, ULL, -20f).Both(LLR, LLL, 32f).Both(FR, FL, -12f).Set(Pelvis, 0f, 0f, -4f).Move(-0.03f, -0.13f, 0f);

            // Rugido: inclina para trás, abre os braços e flexiona (pico em 35%).
            GolemClip roar = New(GolemClipId.Roar, 1.6f, false, 0.35f);
            roar.Key(0f);
            roar.Key(0.2f).Set(Chest, 18f).Set(Head, 14f).Both(UAR, UAL, -20f, 0f, 10f).Both(ULR, ULL, -10f).Both(LLR, LLL, 14f).Move(0f, -0.06f, 0f);
            roar.Key(0.35f).Set(Chest, -26f).Set(Spine, -8f).Set(Head, -34f).Set(Neck, -10f).Both(UAR, UAL, -70f, 0f, 62f)
                .Both(FAR, FAL, -70f, 0f, 0f).Both(HR, HL, 0f, 0f, 20f).Move(0f, 0.03f, -0.04f);
            roar.Key(0.75f).Set(Chest, -24f).Set(Spine, -6f).Set(Head, -30f).Set(Neck, -10f).Both(UAR, UAL, -66f, 0f, 60f)
                .Both(FAR, FAL, -74f, 0f, 0f).Both(HR, HL, 0f, 0f, 20f).Move(0f, 0.03f, -0.04f);
            roar.Key(1f);

            // Morte: tranco, joelhos cedem, cai de frente e desmorona.
            GolemClip death = New(GolemClipId.Death, 2.4f, false);
            death.Key(0f);
            death.Key(0.12f).Set(Chest, -22f).Set(Head, -26f).Both(UAR, UAL, -30f, 0f, 30f).Move(0f, 0.02f, -0.08f);
            death.Key(0.4f).Set(Chest, 26f).Set(Head, 30f).Both(UAR, UAL, 10f, 0f, 14f).Both(ULR, ULL, -80f, 0f, 6f)
                .Both(LLR, LLL, 110f).Both(FR, FL, -30f).Move(0f, -0.42f, 0.05f);
            death.Key(0.75f).Set(Root, 62f, 0f, 0f).Set(Chest, 20f).Set(Head, 20f).Both(UAR, UAL, -100f, 0f, 30f).Both(FAR, FAL, -20f)
                .Both(ULR, ULL, -60f, 0f, 8f).Both(LLR, LLL, 90f).Move(0f, -0.55f, 0.35f);
            death.Key(1f).Set(Root, 78f, 0f, 4f).Set(Chest, 14f).Set(Head, 30f, 20f, 0f).Both(UAR, UAL, -120f, 0f, 45f).Both(FAR, FAL, -10f)
                .Both(ULR, ULL, -50f, 0f, 10f).Both(LLR, LLL, 80f).Move(0f, -0.62f, 0.42f);

            // Dormente: ajoelhado como estátua, cabeça baixa (pose inicial do Spawn).
            GolemClip dormant = New(GolemClipId.Dormant, 4f, true);
            dormant.Key(0f).Set(Chest, 30f).Set(Head, 30f).Set(Spine, 10f).Both(UAR, UAL, -30f, 0f, 8f).Both(FAR, FAL, -40f)
                .Set(ULR, -70f, 0f, 4f).Set(LLR, 70f).Set(FR, 0f).Set(ULL, 20f, 0f, -4f).Set(LLL, 95f).Set(FL, -25f).Move(0f, -0.3f, 0f);
            dormant.Key(0.5f).Set(Chest, 32f).Set(Head, 31f).Set(Spine, 10f).Both(UAR, UAL, -30f, 0f, 9f).Both(FAR, FAL, -40f)
                .Set(ULR, -70f, 0f, 4f).Set(LLR, 70f).Set(ULL, 20f, 0f, -4f).Set(LLL, 95f).Set(FL, -25f).Move(0f, -0.305f, 0f);

            // Despertar: levanta da pose dormente, finca o pé e ergue o tronco.
            GolemClip spawn = New(GolemClipId.Spawn, 1.3f, false);
            spawn.Key(0f).Set(Chest, 30f).Set(Head, 30f).Set(Spine, 10f).Both(UAR, UAL, -30f, 0f, 8f).Both(FAR, FAL, -40f)
                .Set(ULR, -70f, 0f, 4f).Set(LLR, 70f).Set(ULL, 20f, 0f, -4f).Set(LLL, 95f).Set(FL, -25f).Move(0f, -0.3f, 0f);
            spawn.Key(0.45f).Set(Chest, 22f).Set(Head, 6f).Both(UAR, UAL, -10f, 0f, 20f).Both(FAR, FAL, -30f)
                .Set(ULR, -40f).Set(LLR, 50f).Set(ULL, -10f).Set(LLL, 40f).Move(0f, -0.15f, 0f);
            spawn.Key(1f);
        }
    }
}
