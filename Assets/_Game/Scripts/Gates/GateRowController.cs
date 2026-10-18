using UnityEngine;

namespace LegionRush
{
    public class GateRowController : MonoBehaviour
    {
        private static readonly Color AddColor = new Color(0.2f, 0.55f, 1f);
        private static readonly Color MultiplyColor = new Color(1f, 0.74f, 0.15f);
        private static readonly Color NegativeColor = new Color(0.95f, 0.18f, 0.18f);
        private const float ResolveAnimSeconds = 0.45f;

        private GateRow row;
        private Transform leftGate;
        private Transform rightGate;
        private Transform chosenGate;
        private Transform otherGate;
        private bool consumed;
        private bool animationDone;
        private float animTime;

        public void Initialize(GateRow gateRow, float halfWidth)
        {
            row = gateRow;
            transform.localPosition = new Vector3(0f, 0f, row.z);
            leftGate = BuildGate(row.left, -halfWidth * 0.5f, halfWidth);
            rightGate = BuildGate(row.right, halfWidth * 0.5f, halfWidth);
        }

        public bool TryResolve(Vector3 commanderPosition, out GateDef chosen)
        {
            chosen = default;
            if (consumed || row == null || commanderPosition.z < row.z) return false;

            consumed = true;
            bool left = commanderPosition.x < 0f;
            chosen = left ? row.left : row.right;
            chosenGate = left ? leftGate : rightGate;
            otherGate = left ? rightGate : leftGate;
            return true;
        }

        private void Update()
        {
            if (!consumed || animationDone) return;
            animTime += Time.deltaTime;
            float t = Mathf.Clamp01(animTime / ResolveAnimSeconds);
            chosenGate.localScale = Vector3.one * ((1f + 0.35f * Mathf.Sin(t * Mathf.PI)) * (1f - t * t * t));
            otherGate.localScale = Vector3.one * (1f - t);
            if (t < 1f) return;

            chosenGate.gameObject.SetActive(false);
            otherGate.gameObject.SetActive(false);
            animationDone = true;
        }

        private Transform BuildGate(GateDef def, float centerX, float width)
        {
            Color color = !def.IsPositive ? NegativeColor : def.op == GateOp.Multiply ? MultiplyColor : AddColor;
            Material frame = MaterialLibrary.Get(new Color(0.85f, 0.88f, 0.95f), 0f, 0.6f);
            Material glow = MaterialLibrary.Get(color, 1.6f, 0f, false);

            Transform gate = new GameObject("Gate_" + def.Label).transform;
            gate.SetParent(transform, false);
            gate.localPosition = new Vector3(centerX, 0f, 0f);

            float half = width * 0.5f - 0.15f;
            PrimitiveFactory.Part(PrimitiveType.Cube, gate, new Vector3(-half, 1.6f, 0f), new Vector3(0.25f, 3.2f, 0.25f), frame);
            PrimitiveFactory.Part(PrimitiveType.Cube, gate, new Vector3(half, 1.6f, 0f), new Vector3(0.25f, 3.2f, 0.25f), frame);
            PrimitiveFactory.Part(PrimitiveType.Cube, gate, new Vector3(0f, 0.03f, 0f), new Vector3(width - 0.4f, 0.06f, 1.4f), glow, default, false);

            Transform sign = new GameObject("Sign").transform;
            sign.SetParent(gate, false);
            sign.localPosition = new Vector3(0f, 3.5f, 0f);
            sign.localRotation = Quaternion.Euler(30f, 0f, 0f);
            PrimitiveFactory.Part(PrimitiveType.Cube, sign, Vector3.zero, new Vector3(width - 0.2f, 1.2f, 0.18f), glow);
            PrimitiveFactory.Label(sign, new Vector3(0f, 0f, -0.12f), def.Label, 128, 0.07f, Color.white);
            return gate;
        }
    }
}
