using TMPro;
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
        private CharacterAnimatorDriver animDriver;
        private TMP_Text countLabel;
        private int shownCount = -1;

        private float Limit => Mathf.Max(0f, halfWidth - edgeMargin);

        public void Initialize(float speed, float trackHalfWidth)
        {
            forwardSpeed = Mathf.Max(0.1f, speed);
            halfWidth = Mathf.Max(1f, trackHalfWidth);
            targetX = Mathf.Clamp(transform.position.x, -Limit, Limit);
            visual = transform.childCount > 0 ? transform.GetChild(0) : null;
            animDriver = visual != null ? visual.GetComponentInChildren<CharacterAnimatorDriver>(true) : null;
            if (animDriver != null) animDriver.ResetState();

            // Sem contador fixo: o total fica no retrato do rei no HUD. Ganho ou perda aparece como número flutuante.
            countLabel = PrimitiveFactory.Label(transform, new Vector3(0f, LabelHeight, 0f), string.Empty, 110, 0.06f, Color.white);
            countLabel.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            countLabel.gameObject.SetActive(false);
        }

        private const float LabelHeight = 3.1f;
        private const float FloatSeconds = 0.8f;
        private float floatTimer;

        public void SetEngaged(bool engaged) => desiredFactor = engaged ? 0f : 1f;

        // Comandante com modelo final: "Comando" ao usar uma habilidade e comemoração na vitória.
        public void PlayCommand()
        {
            if (animDriver != null) animDriver.PlaySkill();
        }

        public void PlayVictory()
        {
            if (animDriver != null) animDriver.PlayVictory();
        }

        public void SetCountLabel(int count)
        {
            if (countLabel == null || count == shownCount) return;
            int delta = shownCount < 0 ? 0 : count - shownCount;
            shownCount = count;
            if (delta == 0) return;
            countLabel.text = delta > 0 ? "+" + delta : "-" + (-delta);
            countLabel.color = delta > 0 ? new Color(0.55f, 1f, 0.45f) : new Color(1f, 0.38f, 0.32f);
            countLabel.gameObject.SetActive(true);
            floatTimer = FloatSeconds;
            LegionTextFx.Play(countLabel, LegionTextAnimation.Punch);
        }

        private void UpdateFloatingCount(float dt)
        {
            if (floatTimer <= 0f || countLabel == null) return;
            floatTimer -= dt;
            float t = 1f - Mathf.Clamp01(floatTimer / FloatSeconds);
            countLabel.transform.localPosition = new Vector3(0f, LabelHeight + t * 0.9f, 0f);
            countLabel.alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            if (floatTimer <= 0f) countLabel.gameObject.SetActive(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            UpdateFloatingCount(dt);
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
            float lean = Mathf.Clamp((targetX - position.x) * 8f, -12f, 12f);
            if (animDriver != null)
            {
                // o Animator já anima a corrida; só a inclinação lateral continua procedural
                animDriver.SetMoveSpeed(speedFactor);
                visual.localRotation = Quaternion.Euler(0f, lean, -lean * 0.3f);
                return;
            }
            bobPhase += dt * (4f + 8f * speedFactor);
            visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(bobPhase)) * 0.08f * Mathf.Max(0.2f, speedFactor), 0f);
            visual.localRotation = Quaternion.Euler(0f, lean, -lean * 0.5f);
        }
    }
}
