using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LegionRush.Bosses.DarkGolem
{
    // Efeitos do Golem com pooling (sem Instantiate/Destroy durante a luta):
    // cristais que brotam do chão (onda de rochas, pancada no solo), rochedos que caem (chuva de rochedos),
    // detritos balísticos, estrelas do atordoamento e rastro da maça. Fica fora do visual do chefão
    // (não herda o piscar de dano nem o giro do corpo).
    public class DarkGolemVfx : MonoBehaviour
    {
        private const int CrystalPool = 56;
        private const int BoulderPool = 12;
        private const int DebrisPool = 40;

        private struct Crystal
        {
            public Transform Body;
            public float Age;
            public float Life;
            public float Delay;
            public float Height;
            public Vector3 Position;
        }

        private struct Boulder
        {
            public Transform Body;
            public TrailRenderer Trail;
            public Vector3 Target;
            public Vector3 Spin;
            public float Delay;
            public float Fall;
            public float Age;
            public float Radius;
            public bool Landed;
        }

        private struct Debris
        {
            public Transform Body;
            public Vector3 Velocity;
            public Vector3 Spin;
            public float Age;
            public float Life;
        }

        private readonly List<Crystal> crystals = new List<Crystal>(CrystalPool);
        private readonly List<Boulder> boulders = new List<Boulder>(BoulderPool);
        private readonly List<Debris> debris = new List<Debris>(DebrisPool);
        private readonly Stack<Transform> idleCrystals = new Stack<Transform>(CrystalPool);
        private readonly Stack<Transform> idleBoulders = new Stack<Transform>(BoulderPool);
        private readonly Stack<Transform> idleDebris = new Stack<Transform>(DebrisPool);
        private readonly Transform[] stars = new Transform[3];

        private DarkGolemProfile profile;
        private DarkGolemRig rig;
        private BossController boss;
        private Material starMaterial;
        private Material trailMaterial;
        private TrailRenderer maceTrail;
        private Transform starAnchor;
        private float starsTimer;
        private float trailTimer;
        private float scale = 1f;

        private static Mesh crystalMesh, boulderMesh, debrisMesh, starMesh;

        public void Initialize(DarkGolemProfile golemProfile, DarkGolemRig golemRig, BossController owner)
        {
            profile = golemProfile;
            rig = golemRig;
            boss = owner;
            scale = rig.ModelScale;
            starAnchor = rig.HeadTop;
            BuildMeshes();
            for (int i = 0; i < CrystalPool; i++) idleCrystals.Push(NewBody("Crystal", crystalMesh, new[] { rig.Materials.Crystal }, false));
            for (int i = 0; i < BoulderPool; i++) idleBoulders.Push(NewBoulder());
            for (int i = 0; i < DebrisPool; i++) idleDebris.Push(NewBody("Debris", debrisMesh, new[] { rig.Materials.DarkMetal }, false));

            starMaterial = MaterialLibrary.CreateLit(new Color(1f, 0.85f, 0.25f), 2.2f, 0f, false);
            starMaterial.name = "MAT_Boss_StunStar";
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = NewBody("StunStar", starMesh, new[] { starMaterial }, false);
                stars[i].localScale = Vector3.one * scale * 0.55f;
            }

            trailMaterial = MaterialLibrary.CreateGlow(ProceduralTextures.SoftCircle, Color.white, true, 2.4f);
            if (trailMaterial != null && rig.MaceHead != null)
            {
                maceTrail = rig.MaceHead.gameObject.AddComponent<TrailRenderer>();
                maceTrail.sharedMaterial = trailMaterial;
                maceTrail.time = 0.28f;
                maceTrail.minVertexDistance = 0.15f;
                maceTrail.widthMultiplier = 0.9f * scale * 0.5f;
                maceTrail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                var gradient = new Gradient();
                Color glow = rig.Materials.Variant.crystal;
                gradient.SetKeys(new[] { new GradientColorKey(Color.Lerp(glow, Color.white, 0.4f), 0f), new GradientColorKey(glow, 1f) },
                    new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
                maceTrail.colorGradient = gradient;
                maceTrail.alignment = LineAlignment.View;
                maceTrail.shadowCastingMode = ShadowCastingMode.Off;
                maceTrail.receiveShadows = false;
                maceTrail.emitting = false;
            }
        }

        // ---------- API usada pela apresentação ----------

        public void MaceTrail(float seconds) => trailTimer = Mathf.Max(trailTimer, seconds);

        public void Stars(bool on) => starsTimer = on ? float.MaxValue : 0f;

        // Aglomerado de cristais brotando (onda de rochas avança passo a passo).
        public void CrystalCluster(Vector3 point, float halfWidth, float delay = 0f)
        {
            int count = halfWidth > 1f ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                Vector2 jitter = Random.insideUnitCircle * halfWidth * 0.6f;
                SpawnCrystal(point + new Vector3(jitter.x, 0f, jitter.y), Random.Range(0.75f, 1.15f), delay + i * 0.04f);
            }
            if (Random.value < 0.6f) SpawnDebris(point, 2, 3.5f);
        }

        // Anéis de cristais acompanhando a onda de choque da pancada no solo.
        public void CrystalWave(Vector3 center, float reach, float duration)
        {
            const int rings = 4;
            for (int r = 0; r < rings; r++)
            {
                float radius = Mathf.Lerp(2.6f, reach * 0.85f, r / (float)(rings - 1));
                int count = Mathf.Min(10, Mathf.RoundToInt(2f * Mathf.PI * radius / 2.6f));
                float delay = duration * radius / reach;
                float offset = Random.value * Mathf.PI * 2f;
                for (int i = 0; i < count; i++)
                {
                    float a = offset + i / (float)count * Mathf.PI * 2f;
                    var p = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                    SpawnCrystal(p, Mathf.Lerp(1.1f, 0.65f, r / (float)rings), delay);
                }
            }
            SpawnDebris(center, 10, 6f);
        }

        // Estilhaços e cristais no ponto do golpe de maça.
        public void SlamBurst(Vector3 point, float radius)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f + Random.value;
                SpawnCrystal(point + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius * 0.55f, 0.7f, 0.02f * i);
            }
            SpawnDebris(point, 8, 5f);
        }

        // Rochedos caem do céu e tocam o chão exatamente no fim do aviso (quando o dano é aplicado).
        public void ScheduleBoulders(IReadOnlyList<Vector3> targets, float windup, float fallTime, float radius)
        {
            float fall = Mathf.Clamp(fallTime, 0.2f, windup);
            for (int i = 0; i < targets.Count && idleBoulders.Count > 0; i++)
            {
                Transform body = idleBoulders.Pop();
                var boulder = new Boulder
                {
                    Body = body,
                    Trail = body.GetComponent<TrailRenderer>(),
                    Target = targets[i],
                    Spin = Random.onUnitSphere * Random.Range(180f, 420f),
                    Delay = windup - fall,
                    Fall = fall,
                    Age = 0f,
                    Radius = radius
                };
                body.gameObject.SetActive(false);
                boulders.Add(boulder);
            }
        }

        public void Dust(Vector3 point, float radius)
        {
            Vfx.Ring(point, new Color(0.92f, 0.95f, 1f, 0.55f), radius, 0.45f);
        }

        public void Clear()
        {
            for (int i = crystals.Count - 1; i >= 0; i--) Release(crystals[i].Body, idleCrystals);
            for (int i = boulders.Count - 1; i >= 0; i--) ReleaseBoulder(boulders[i]);
            for (int i = debris.Count - 1; i >= 0; i--) Release(debris[i].Body, idleDebris);
            crystals.Clear();
            boulders.Clear();
            debris.Clear();
            starsTimer = 0f;
            trailTimer = 0f;
        }

        // ---------- Atualização ----------

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            UpdateCrystals(dt);
            UpdateBoulders(dt);
            UpdateDebris(dt);
            UpdateStars(dt);
            if (maceTrail != null)
            {
                trailTimer -= dt;
                maceTrail.emitting = trailTimer > 0f;
            }
        }

        private void SpawnCrystal(Vector3 point, float size, float delay)
        {
            if (idleCrystals.Count == 0) return;
            Transform body = idleCrystals.Pop();
            point.y = 0f;
            body.SetPositionAndRotation(point, Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-12f, 12f)));
            body.gameObject.SetActive(false);
            crystals.Add(new Crystal
            {
                Body = body,
                Age = 0f,
                Delay = delay,
                Life = profile.crystalLifetime,
                Height = profile.crystalHeight * size,
                Position = point
            });
        }

        private void UpdateCrystals(float dt)
        {
            for (int i = crystals.Count - 1; i >= 0; i--)
            {
                Crystal c = crystals[i];
                if (c.Delay > 0f)
                {
                    c.Delay -= dt;
                    crystals[i] = c;
                    if (c.Delay > 0f) continue;
                    c.Body.gameObject.SetActive(true);
                }
                c.Age += dt;
                float t = c.Age / c.Life;
                if (t >= 1f)
                {
                    Release(c.Body, idleCrystals);
                    RemoveAt(crystals, i);
                    continue;
                }
                // Brota com exagero (0–12%), segura, afunda no chão no último terço.
                float rise = t < 0.12f ? Mathf.Sin(t / 0.12f * Mathf.PI * 0.6f) / Mathf.Sin(Mathf.PI * 0.6f) : 1f;
                float sink = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, t));
                float h = c.Height * Mathf.Max(0.02f, rise);
                c.Body.localScale = new Vector3(c.Height * 0.8f, h, c.Height * 0.8f);
                c.Body.position = c.Position + Vector3.down * (sink * c.Height * 1.05f);
                crystals[i] = c;
            }
        }

        private void UpdateBoulders(float dt)
        {
            for (int i = boulders.Count - 1; i >= 0; i--)
            {
                Boulder b = boulders[i];
                b.Age += dt;
                if (b.Age < b.Delay)
                {
                    boulders[i] = b;
                    continue;
                }
                float t = (b.Age - b.Delay) / b.Fall;
                if (!b.Body.gameObject.activeSelf)
                {
                    b.Body.gameObject.SetActive(true);
                    if (b.Trail != null) b.Trail.Clear();
                }
                Vector3 top = b.Target + new Vector3(2.5f, profile.boulderDropHeight, 3f);
                float size = b.Radius * 0.55f;
                b.Body.localScale = Vector3.one * size;
                if (!b.Landed)
                {
                    float eased = t * t;
                    b.Body.position = Vector3.Lerp(top, b.Target + Vector3.up * size * 0.6f, Mathf.Clamp01(eased));
                    b.Body.rotation *= Quaternion.Euler(b.Spin * dt);
                    if (b.Trail != null) b.Trail.emitting = true;
                    if (t >= 1f)
                    {
                        b.Landed = true;
                        if (b.Trail != null) b.Trail.emitting = false;
                        SpawnDebris(b.Target, 4, 4.5f);
                        SpawnCrystal(b.Target + Random.insideUnitSphere * 0.4f, 0.6f, 0f);
                    }
                }
                else
                {
                    // Fica cravado no chão e afunda.
                    float settle = (b.Age - b.Delay - b.Fall) / 0.9f;
                    b.Body.position = b.Target + Vector3.up * size * Mathf.Lerp(0.6f, -1.1f, Mathf.SmoothStep(0f, 1f, settle));
                    if (settle >= 1f)
                    {
                        ReleaseBoulder(b);
                        RemoveAt(boulders, i);
                        continue;
                    }
                }
                boulders[i] = b;
            }
        }

        private void SpawnDebris(Vector3 point, int count, float speed)
        {
            for (int i = 0; i < count && idleDebris.Count > 0; i++)
            {
                Transform body = idleDebris.Pop();
                body.gameObject.SetActive(true);
                body.position = point + Vector3.up * 0.3f;
                body.rotation = Random.rotation;
                body.localScale = Vector3.one * Random.Range(0.18f, 0.38f);
                Vector2 dir = Random.insideUnitCircle.normalized;
                debris.Add(new Debris
                {
                    Body = body,
                    Velocity = new Vector3(dir.x, 0f, dir.y) * speed * Random.Range(0.4f, 1f) + Vector3.up * Random.Range(4f, 8f),
                    Spin = Random.onUnitSphere * Random.Range(200f, 600f),
                    Age = 0f,
                    Life = Random.Range(0.9f, 1.3f)
                });
            }
        }

        private void UpdateDebris(float dt)
        {
            for (int i = debris.Count - 1; i >= 0; i--)
            {
                Debris d = debris[i];
                d.Age += dt;
                if (d.Age >= d.Life)
                {
                    Release(d.Body, idleDebris);
                    RemoveAt(debris, i);
                    continue;
                }
                d.Velocity += Physics.gravity * dt;
                Vector3 p = d.Body.position + d.Velocity * dt;
                if (p.y < 0.08f)
                {
                    p.y = 0.08f;
                    d.Velocity = new Vector3(d.Velocity.x * 0.5f, -d.Velocity.y * 0.3f, d.Velocity.z * 0.5f);
                    d.Spin *= 0.5f;
                }
                d.Body.position = p;
                d.Body.rotation *= Quaternion.Euler(d.Spin * dt);
                float shrink = Mathf.InverseLerp(d.Life, d.Life * 0.7f, d.Age);
                if (d.Age > d.Life * 0.7f) d.Body.localScale *= Mathf.Lerp(1f, 0.9f, 1f - shrink);
                debris[i] = d;
            }
        }

        private void UpdateStars(float dt)
        {
            bool on = starsTimer > 0f && starAnchor != null;
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i].gameObject.activeSelf != on) stars[i].gameObject.SetActive(on);
                if (!on) continue;
                float a = Time.time * 3.2f + i * Mathf.PI * 2f / stars.Length;
                Vector3 center = starAnchor.position;
                stars[i].position = center + new Vector3(Mathf.Cos(a), 0.15f * Mathf.Sin(a * 2f), Mathf.Sin(a)) * (0.45f * scale);
                stars[i].rotation = Quaternion.Euler(0f, Time.time * 240f + i * 40f, 0f);
            }
        }

        // ---------- Pool ----------

        private Transform NewBody(string label, Mesh mesh, Material[] materials, bool shadows)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            go.SetActive(false);
            return go.transform;
        }

        private Transform NewBoulder()
        {
            Transform body = NewBody("Boulder", boulderMesh, new[] { rig.Materials.DarkMetal, rig.Materials.Crystal }, true);
            if (trailMaterial == null) trailMaterial = MaterialLibrary.CreateGlow(ProceduralTextures.SoftCircle, Color.white, true, 2.4f);
            if (trailMaterial != null)
            {
                TrailRenderer trail = body.gameObject.AddComponent<TrailRenderer>();
                trail.sharedMaterial = trailMaterial;
                trail.time = 0.35f;
                trail.minVertexDistance = 0.3f;
                trail.widthMultiplier = 1.3f;
                trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                var gradient = new Gradient();
                Color glow = rig.Materials.Variant.crystal;
                gradient.SetKeys(new[] { new GradientColorKey(Color.Lerp(glow, Color.white, 0.3f), 0f), new GradientColorKey(glow, 1f) },
                    new[] { new GradientAlphaKey(0.75f, 0f), new GradientAlphaKey(0f, 1f) });
                trail.colorGradient = gradient;
                trail.shadowCastingMode = ShadowCastingMode.Off;
                trail.emitting = false;
            }
            return body;
        }

        private void ReleaseBoulder(Boulder b)
        {
            if (b.Trail != null) b.Trail.emitting = false;
            Release(b.Body, idleBoulders);
        }

        private static void Release(Transform body, Stack<Transform> pool)
        {
            if (body == null) return;
            body.gameObject.SetActive(false);
            pool.Push(body);
        }

        private static void RemoveAt<T>(List<T> list, int index)
        {
            int last = list.Count - 1;
            list[index] = list[last];
            list.RemoveAt(last);
        }

        private void OnDestroy()
        {
            if (starMaterial != null) Destroy(starMaterial);
            if (trailMaterial != null) Destroy(trailMaterial);
        }

        // ---------- Malhas dos efeitos (geradas uma vez) ----------

        private static void BuildMeshes()
        {
            if (crystalMesh == null)
            {
                var b = new GolemMeshBuilder(1);
                b.Place(Vector3.zero, Vector3.zero);
                b.Crystal(0.22f, 1f, 5, 0.3f);
                b.Place(new Vector3(0.2f, 0f, 0.08f), new Vector3(8f, 0f, -24f));
                b.Crystal(0.14f, 0.62f, 5, 1.1f);
                b.Place(new Vector3(-0.16f, 0f, -0.12f), new Vector3(-14f, 0f, 20f));
                b.Crystal(0.12f, 0.5f, 4, 0.7f);
                crystalMesh = b.ToMesh("VFX_Boss_CrystalCluster", false);
            }
            if (boulderMesh == null)
            {
                var b = new GolemMeshBuilder(2);
                b.Slot = 0;
                b.Place(Vector3.zero, Vector3.zero);
                b.FacetedSphere(1f, Swatch.Plate, Swatch.Dark, 0.22f, 7);
                b.Slot = 1;
                for (int i = 0; i < 5; i++)
                {
                    Vector3 d = Quaternion.Euler(i * 71f, i * 137f, 0f) * Vector3.up;
                    b.Matrix = Matrix4x4.TRS(d * 0.85f, Quaternion.FromToRotation(Vector3.up, d), Vector3.one);
                    b.Crystal(0.18f, 0.5f, 5, i);
                }
                boulderMesh = b.ToMesh("VFX_Boss_Boulder", false);
            }
            if (debrisMesh == null)
            {
                var b = new GolemMeshBuilder(1);
                b.Place(Vector3.zero, Vector3.zero);
                b.FacetedSphere(0.5f, Swatch.Plate, Swatch.Edge, 0.3f, 3);
                debrisMesh = b.ToMesh("VFX_Boss_Debris", false);
            }
            if (starMesh == null) starMesh = BuildStar();
        }

        private static Mesh BuildStar()
        {
            var b = new GolemMeshBuilder(1);
            b.Place(Vector3.zero, new Vector3(0f, 0f, 0f));
            const int points = 5;
            const float outer = 0.22f, inner = 0.09f, depth = 0.05f;
            var ring = new Vector3[points * 2];
            for (int i = 0; i < ring.Length; i++)
            {
                float a = i / (float)ring.Length * Mathf.PI * 2f + Mathf.PI * 0.5f;
                float r = (i & 1) == 0 ? outer : inner;
                ring[i] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
            }
            var front = new Vector3(0f, 0f, depth);
            var back = new Vector3(0f, 0f, -depth);
            for (int i = 0; i < ring.Length; i++)
            {
                Vector3 p0 = ring[i], p1 = ring[(i + 1) % ring.Length];
                b.Triangle(front, p1, p0, Swatch.Plate);
                b.Triangle(back, p0, p1, Swatch.Plate);
            }
            return b.ToMesh("VFX_Boss_StunStar", false);
        }
    }
}
