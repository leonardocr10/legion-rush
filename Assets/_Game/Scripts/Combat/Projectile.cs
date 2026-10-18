using UnityEngine;
using UnityEngine.Rendering;

namespace LegionRush
{
    // Flecha, orbe mágico ou flecha da Chuva de Flechas. Segue um alvo ou voa até um ponto; pode causar dano em área.
    public class Projectile : MonoBehaviour
    {
        private const float MaxLifetime = 3.5f;
        private static readonly Vector3 AimOffset = new Vector3(0f, 0.7f, 0f);
        private static AnimationCurve trailWidth;

        private Combatant target;
        private Vector3 aimPoint;
        private DamageInfo damage;
        private float speed;
        private float life;
        private float areaRadius;
        private bool homing;
        private bool active;
        private bool hasImpactSound;
        private SoundId impactSound;
        private Color impactColor;
        private System.Action<Projectile> release;
        private TrailRenderer trail;

        // Dano em área alternativo (projéteis inimigos atingem o exército, não os hostis).
        public System.Func<Vector3, float, DamageInfo, int> AreaDamage { get; set; }

        public void ConfigureTrail(Material material, Color headColor, Color tailColor, float width)
        {
            if (material == null || trail != null) return;
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = 0.16f;
            trail.minVertexDistance = 0.12f;
            trail.widthMultiplier = width;
            trail.widthCurve = TrailWidth;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(headColor, 0f), new GradientColorKey(tailColor, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.numCapVertices = 2;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.autodestruct = false;
            trail.emitting = false;
        }

        public void ConfigureImpact(bool playSound, SoundId sound, Color color)
        {
            hasImpactSound = playSound;
            impactSound = sound;
            impactColor = color;
        }

        public void Launch(Vector3 origin, Combatant newTarget, DamageInfo hit, float travelSpeed, float radius,
            System.Action<Projectile> onRelease)
        {
            target = newTarget;
            homing = true;
            aimPoint = target.transform.position + AimOffset;
            Begin(origin, hit, travelSpeed, radius, onRelease);
        }

        public void LaunchAtPoint(Vector3 origin, Vector3 point, DamageInfo hit, float travelSpeed, float radius,
            System.Action<Projectile> onRelease)
        {
            target = null;
            homing = false;
            aimPoint = point;
            Begin(origin, hit, travelSpeed, radius, onRelease);
        }

        private void Begin(Vector3 origin, DamageInfo hit, float travelSpeed, float radius, System.Action<Projectile> onRelease)
        {
            damage = hit;
            speed = Mathf.Max(1f, travelSpeed);
            areaRadius = Mathf.Max(0f, radius);
            release = onRelease;
            life = MaxLifetime;
            active = true;
            transform.position = origin;
            Face(aimPoint - origin);
            if (trail != null)
            {
                trail.Clear();
                trail.emitting = true;
            }
        }

        private void Update()
        {
            if (!active) return;
            float dt = Time.deltaTime;
            life -= dt;
            if (homing && target != null && target.IsAlive) aimPoint = target.transform.position + AimOffset;

            Vector3 toAim = aimPoint - transform.position;
            float step = speed * dt;
            if (life <= 0f)
            {
                Finish();
                return;
            }
            if (toAim.sqrMagnitude <= step * step)
            {
                Impact();
                Finish();
                return;
            }
            transform.position += toAim.normalized * step;
            Face(toAim);
        }

        private void Impact()
        {
            if (areaRadius > 0f)
            {
                if (AreaDamage != null) AreaDamage(aimPoint, areaRadius, damage);
                else CombatRegistry.DamageInRadius(aimPoint, areaRadius, damage);
                Vfx.Burst(aimPoint, impactColor, areaRadius, 0.4f);
            }
            else if (homing && target != null && target.IsAlive)
            {
                target.TakeDamage(damage);
            }
            else
            {
                return;
            }
            if (hasImpactSound) GameAudio.PlayAt(impactSound, aimPoint);
        }

        private void Face(Vector3 direction)
        {
            if (direction.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(direction);
        }

        private void Finish()
        {
            active = false;
            target = null;
            if (trail != null) trail.emitting = false;
            release?.Invoke(this);
        }

        private static AnimationCurve TrailWidth
        {
            get
            {
                trailWidth ??= new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
                return trailWidth;
            }
        }
    }
}
