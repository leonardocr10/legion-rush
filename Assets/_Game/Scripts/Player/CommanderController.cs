using UnityEngine;

namespace LegionRush
{
    public class CommanderController : MonoBehaviour
    {
        [SerializeField, Tooltip("Track widths traveled per full-screen-width drag.")]
        private float lateralSensitivity = 1.3f;
        [SerializeField] private float lateralSmoothing = 14f;
        [SerializeField] private float speedResponse = 3f;
        [SerializeField] private float edgeMargin = 0.8f;

        public bool ControlEnabled { get; set; }
        public bool AdvanceEnabled { get; set; }

        private float forwardSpeed;
        private float halfWidth;
        private float targetX;
        private float lastPointerX;
        private float desiredFactor = 1f;
        private float speedFactor;
        private float bobPhase;
        private bool dragging;
        private Transform visual;
        private TextMesh countLabel;

        private float Limit => Mathf.Max(0f, halfWidth - edgeMargin);

        public void Initialize(float speed, float trackHalfWidth)
        {
            forwardSpeed = Mathf.Max(0.1f, speed);
            halfWidth = Mathf.Max(1f, trackHalfWidth);
            targetX = Mathf.Clamp(transform.position.x, -Limit, Limit);
            visual = transform.childCount > 0 ? transform.GetChild(0) : null;

            countLabel = PrimitiveFactory.Label(transform, new Vector3(0f, 3.1f, 0f), "0", 110, 0.06f, Color.white);
            countLabel.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            PrimitiveFactory.Part(PrimitiveType.Cube, countLabel.transform, new Vector3(0f, 0f, 0.06f),
                new Vector3(1.6f, 0.9f, 0.04f), MaterialLibrary.Get(new Color(0.1f, 0.2f, 0.45f), 0f, 0f, false), default, false);
        }

        public void SetEngaged(bool engaged) => desiredFactor = engaged ? 0f : 1f;

        public void SetCountLabel(int count)
        {
            if (countLabel != null) countLabel.text = count.ToString();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (ControlEnabled && PointerInput.IsPressed(out Vector2 pointer))
            {
                if (!dragging)
                {
                    dragging = true;
                    lastPointerX = pointer.x;
                }
                else
                {
                    float normalizedDelta = (pointer.x - lastPointerX) / Mathf.Max(1f, Screen.width);
                    lastPointerX = pointer.x;
                    targetX = Mathf.Clamp(targetX + normalizedDelta * lateralSensitivity * halfWidth * 2f, -Limit, Limit);
                }
            }
            else
            {
                dragging = false;
            }

            Vector3 position = transform.position;
            position.x = Mathf.Lerp(position.x, targetX, 1f - Mathf.Exp(-lateralSmoothing * dt));
            speedFactor = Mathf.MoveTowards(speedFactor, AdvanceEnabled ? desiredFactor : 0f, speedResponse * dt);
            position.z += forwardSpeed * speedFactor * dt;
            transform.position = position;

            if (visual == null) return;
            bobPhase += dt * (4f + 8f * speedFactor);
            visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(bobPhase)) * 0.08f * Mathf.Max(0.2f, speedFactor), 0f);
            float lean = Mathf.Clamp((targetX - position.x) * 8f, -12f, 12f);
            visual.localRotation = Quaternion.Euler(0f, lean, -lean * 0.5f);
        }
    }
}
