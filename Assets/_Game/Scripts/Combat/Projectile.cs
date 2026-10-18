using UnityEngine;
using UnityEngine.Rendering;

namespace LegionRush
{
    public class Projectile : MonoBehaviour
    {
        private const float MaxLifetime = 3f;
        private static readonly Vector3 AimOffset = new Vector3(0f, 0.7f, 0f);
        private static Gradient trailGradient;
        private static AnimationCurve trailWidth;

        private Combatant target;
        private Vector3 aimPoint;
        private float damage;
        private float speed;
        private float life;
        private bool active;
        private System.Action<Projectile> release;
        private TrailRenderer trail;

        // Rastro dourado luminoso. Chamado uma vez quando o projétil é criado pelo pool.
        public void ConfigureTrail(Material material)
        {
            if (material == null || trail != null) return;
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = 0.16f;
            trail.minVertexDistance = 0.12f;
            trail.widthMultiplier = 0.14f;
            trail.widthCurve = TrailWidth;
            trail.colorGradient = TrailGradient;
            trail.numCapVertices = 2;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.autodestruct = false;
            trail.emitting = false;
        }

        public void Launch(Vector3 origin, Combatant newTarget, float hitDamage, float travelSpeed,
            System.Action<Projectile> onRelease)
        {
            target = newTarget;
            damage = hitDamage;
            speed = Mathf.Max(1f, travelSpeed);
            release = onRelease;
            life = MaxLifetime;
            active = true;
            transform.position = origin;
            aimPoint = target.transform.position + AimOffset;
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
            if (target != null && target.IsAlive) aimPoint = target.transform.position + AimOffset;

            Vector3 toAim = aimPoint - transform.position;
            float step = speed * dt;
            if (life <= 0f)
            {
                Finish();
                return;
            }
            if (toAim.sqrMagnitude <= step * step)
            {
                if (target != null && target.IsAlive)
                {
                    target.TakeDamage(damage);
                    GameAudio.PlayAt(SoundId.ArrowImpact, aimPoint);
                }
                Finish();
                return;
            }
            transform.position += toAim.normalized * step;
            Face(toAim);
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

        private static Gradient TrailGradient
        {
            get
            {
                if (trailGradient != null) return trailGradient;
                trailGradient = new Gradient();
                trailGradient.SetKeys(
                    new[] { new GradientColorKey(new Color(1f, 0.92f, 0.55f), 0f), new GradientColorKey(new Color(1f, 0.55f, 0.15f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
                return trailGradient;
            }
        }
    }
}
