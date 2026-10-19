using UnityEngine;

namespace LegionRush
{
    // Barra de vida curta sobre elites e escudeiros (Berserker, Guerreiro Pesado): trilho escuro e preenchimento vermelho,
    // sempre virada para a câmera. Só aparece depois do primeiro dano, para não poluir a pista.
    public class EnemyHealthBar : MonoBehaviour
    {
        private const float Width = 0.9f;
        private const float Height = 0.12f;

        private Combatant owner;
        private Transform fill;
        private Transform root;

        public static bool Wants(EnemyKind kind) => kind == EnemyKind.Berserker || kind == EnemyKind.HeavyWarrior;

        public static void Attach(Combatant enemy, float height)
        {
            EnemyHealthBar bar = enemy.GetComponent<EnemyHealthBar>();
            if (bar == null)
            {
                bar = enemy.gameObject.AddComponent<EnemyHealthBar>();
                bar.Build(height);
            }
            bar.owner = enemy;
            bar.enabled = true;
            bar.root.gameObject.SetActive(false);
        }

        public static void Detach(Combatant enemy)
        {
            EnemyHealthBar bar = enemy.GetComponent<EnemyHealthBar>();
            if (bar == null) return;
            bar.root.gameObject.SetActive(false);
            bar.enabled = false;
        }

        private void Build(float height)
        {
            root = new GameObject("HealthBar").transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(0f, height, 0f);
            Material back = MaterialLibrary.Get(new Color(0.05f, 0.06f, 0.12f), 0.2f, 0f, false);
            Material red = MaterialLibrary.Get(new Color(0.95f, 0.18f, 0.15f), 0.9f, 0f, false);
            PrimitiveFactory.Part(PrimitiveType.Quad, root, new Vector3(0f, 0f, 0.01f), new Vector3(Width + 0.06f, Height + 0.06f, 1f), back,
                default, false);
            Transform pivot = new GameObject("Pivot").transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(-Width * 0.5f, 0f, 0f);
            PrimitiveFactory.Part(PrimitiveType.Quad, pivot, new Vector3(Width * 0.5f, 0f, 0f), new Vector3(Width, Height, 1f), red, default, false);
            fill = pivot;
            foreach (Collider c in root.GetComponentsInChildren<Collider>()) Destroy(c);
        }

        private void LateUpdate()
        {
            if (owner == null) return;
            float ratio = owner.HealthRatio;
            bool show = owner.IsAlive && ratio < 0.999f;
            if (root.gameObject.activeSelf != show) root.gameObject.SetActive(show);
            if (!show) return;
            fill.localScale = new Vector3(Mathf.Clamp01(ratio), 1f, 1f);
            Camera cam = Camera.main;
            if (cam != null) root.rotation = cam.transform.rotation;
        }
    }
}
