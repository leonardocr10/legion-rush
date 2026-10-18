using UnityEngine;

namespace LegionRush.Environments
{
    // Folhas do portão do castelo. Ficam fechadas durante a fase e abrem para fora (na direção da câmera)
    // quando o exército vence. Só visual: a vitória continua sendo decidida pelo LevelManager.
    public class EnvGateDoors : MonoBehaviour
    {
        private const float OpenAngle = 82f;

        [SerializeField] private Transform left;
        [SerializeField] private Transform right;
        private float duration = 1.6f;
        private float elapsed = -1f;

        public bool IsOpen => elapsed >= 0f;

        public void Initialize(Transform leftLeaf, Transform rightLeaf)
        {
            left = leftLeaf;
            right = rightLeaf;
        }

        public void Open(float seconds = 1.6f)
        {
            if (IsOpen) return;
            duration = Mathf.Max(0.05f, seconds);
            elapsed = 0f;
        }

        private void Update()
        {
            if (elapsed < 0f || elapsed > duration || left == null || right == null) return;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Arranque pesado e leve balanço no fim.
            float k = 1f - Mathf.Pow(1f - t, 3f);
            k += Mathf.Sin(t * Mathf.PI) * 0.04f * t;
            SetOpenAmount(k);
        }

        // 0 = fechado, 1 = aberto (também usado pela prévia no editor, onde Update não roda).
        public void SetOpenAmount(float k)
        {
            if (left != null) left.localRotation = Quaternion.Euler(0f, OpenAngle * k, 0f);
            if (right != null) right.localRotation = Quaternion.Euler(0f, -OpenAngle * k, 0f);
        }
    }
}
