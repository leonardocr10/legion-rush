using UnityEngine;

namespace LegionRush
{
    [RequireComponent(typeof(Camera))]
    public class ArmyCameraController : MonoBehaviour
    {
        [SerializeField] private float baseHeight = 10f;
        [SerializeField] private float baseDistance = 8.5f;
        [SerializeField] private float heightPerRadius = 0.9f;
        [SerializeField] private float distancePerRadius = 0.6f;
        [SerializeField] private float lookAhead = 7f;
        [SerializeField] private float lateralFollow = 0.5f;
        [SerializeField] private float followSmoothTime = 0.22f;
        [SerializeField] private float horizontalFov = 46f;
        [SerializeField] private Vector2 verticalFovLimits = new Vector2(40f, 75f);

        private Camera cam;
        private Transform target;
        private ArmyManager army;
        private Vector3 basePosition;
        private Vector3 velocity;
        private float shake;
        private float zoom = 1f;
        private float zoomTarget = 1f;

        public void Initialize(Transform followTarget, ArmyManager followArmy)
        {
            cam = GetComponent<Camera>();
            target = followTarget;
            army = followArmy;
            shake = 0f;
            zoom = zoomTarget = 1f;
            velocity = Vector3.zero;
            basePosition = DesiredPosition();
            transform.SetPositionAndRotation(basePosition, LookRotation(basePosition));
            UpdateFieldOfView();
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, Mathf.Clamp01(amount));
        public void SetZoom(float value) => zoomTarget = Mathf.Clamp(value, 0.4f, 2f);

        private void LateUpdate()
        {
            if (target == null || cam == null) return;
            float dt = Time.deltaTime;
            UpdateFieldOfView();
            zoom = Mathf.Lerp(zoom, zoomTarget, 1f - Mathf.Exp(-3f * dt));
            basePosition = Vector3.SmoothDamp(basePosition, DesiredPosition(), ref velocity, followSmoothTime);

            Vector3 offset = Vector3.zero;
            if (shake > 0f)
            {
                offset = Random.insideUnitSphere * (shake * 0.35f);
                shake = Mathf.MoveTowards(shake, 0f, dt * 2.5f);
            }
            transform.SetPositionAndRotation(basePosition + offset, LookRotation(basePosition));
        }

        private Vector3 DesiredPosition()
        {
            float radius = army != null ? army.FormationRadius : 0f;
            Vector3 t = target.position;
            return new Vector3(
                t.x * lateralFollow,
                (baseHeight + radius * heightPerRadius) * zoom,
                t.z - (baseDistance + radius * distancePerRadius) * zoom);
        }

        private Quaternion LookRotation(Vector3 from)
        {
            Vector3 focus = new Vector3(target.position.x * lateralFollow, 0f, target.position.z + lookAhead * zoom);
            Vector3 direction = focus - from;
            return direction.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(direction) : transform.rotation;
        }

        private void UpdateFieldOfView()
        {
            float aspect = Mathf.Max(0.1f, cam.aspect);
            float halfHorizontal = horizontalFov * 0.5f * Mathf.Deg2Rad;
            float vertical = 2f * Mathf.Atan(Mathf.Tan(halfHorizontal) / aspect) * Mathf.Rad2Deg;
            cam.fieldOfView = Mathf.Clamp(vertical, verticalFovLimits.x, verticalFovLimits.y);
        }
    }
}
