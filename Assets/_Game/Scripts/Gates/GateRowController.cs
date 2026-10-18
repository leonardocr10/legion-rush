using TMPro;
using UnityEngine;

namespace LegionRush
{
    // Fileira de 2 ou 3 portais. A faixa escolhida é a da posição lateral do comandante ao cruzar a linha.
    public class GateRowController : MonoBehaviour
    {
        private const float ResolveAnimSeconds = 0.45f;

        private GateChoiceRow row;
        private Transform[] gates;
        private TMP_Text[] titles;
        private float halfWidth;
        private int chosenIndex = -1;
        private bool consumed;
        private bool animationDone;
        private float animTime;

        public float Z => row != null ? row.z : 0f;
        public bool Consumed => consumed;

        public void Initialize(GateChoiceRow gateRow, float trackHalfWidth)
        {
            row = gateRow;
            halfWidth = trackHalfWidth;
            transform.localPosition = new Vector3(0f, 0f, row.z);
            int count = Mathf.Max(1, row.options.Count);
            float laneWidth = halfWidth * 2f / count;
            gates = new Transform[count];
            titles = new TMP_Text[count];
            for (int i = 0; i < count; i++)
                gates[i] = BuildGate(row.options[i], -halfWidth + laneWidth * (i + 0.5f), laneWidth, out titles[i]);
        }

        public bool TryResolve(Vector3 commanderPosition, out GateOption chosen)
        {
            chosen = null;
            if (consumed || row == null || row.options.Count == 0 || commanderPosition.z < row.z) return false;
            consumed = true;
            int count = row.options.Count;
            float laneWidth = halfWidth * 2f / count;
            chosenIndex = Mathf.Clamp(Mathf.FloorToInt((commanderPosition.x + halfWidth) / laneWidth), 0, count - 1);
            chosen = row.options[chosenIndex];
            Vector3 center = gates[chosenIndex].position + Vector3.up * 1.6f;
            Vfx.Burst(center, GateOption.ColorOf(chosen.category), laneWidth * 0.6f, 0.6f);
            Vfx.Ring(gates[chosenIndex].position, GateOption.ColorOf(chosen.category), laneWidth * 0.8f, 0.6f);
            LegionTextFx.Play(titles[chosenIndex], LegionTextAnimation.Highlight);
            return true;
        }

        // Usado quando o exército pula um trecho (atalho): os portais do trecho somem sem efeito.
        public void Skip()
        {
            if (consumed) return;
            consumed = true;
            chosenIndex = -1;
        }

        private void Update()
        {
            if (!consumed || animationDone || gates == null) return;
            animTime += Time.deltaTime;
            float t = Mathf.Clamp01(animTime / ResolveAnimSeconds);
            for (int i = 0; i < gates.Length; i++)
            {
                float scale = i == chosenIndex
                    ? (1f + 0.35f * Mathf.Sin(t * Mathf.PI)) * (1f - t * t * t)
                    : 1f - t;
                gates[i].localScale = Vector3.one * Mathf.Max(0f, scale);
            }
            if (t < 1f) return;
            foreach (Transform gate in gates) gate.gameObject.SetActive(false);
            animationDone = true;
        }

        private Transform BuildGate(GateOption option, float centerX, float width, out TMP_Text title)
        {
            Color color = GateOption.ColorOf(option.category);
            Material frame = MaterialLibrary.Get(new Color(0.85f, 0.88f, 0.95f), 0f, 0.6f);
            Material glow = MaterialLibrary.Get(color, 1.6f, 0f, false);
            Material curtain = MaterialLibrary.Get(color * 0.6f, 0.9f, 0f, false);

            Transform gate = new GameObject("Gate_" + option.title).transform;
            gate.SetParent(transform, false);
            gate.localPosition = new Vector3(centerX, 0f, 0f);

            float half = width * 0.5f - 0.15f;
            PrimitiveFactory.Part(PrimitiveType.Cube, gate, new Vector3(-half, 1.6f, 0f), new Vector3(0.25f, 3.2f, 0.25f), frame);
            PrimitiveFactory.Part(PrimitiveType.Cube, gate, new Vector3(half, 1.6f, 0f), new Vector3(0.25f, 3.2f, 0.25f), frame);
            PrimitiveFactory.Part(PrimitiveType.Cube, gate, new Vector3(0f, 0.03f, 0f), new Vector3(width - 0.4f, 0.06f, 1.4f), glow, default, false);
            PrimitiveFactory.Part(PrimitiveType.Cube, gate, new Vector3(0f, 1.55f, 0f), new Vector3(width - 0.45f, 3.0f, 0.04f), curtain, default, false);

            Transform sign = new GameObject("Sign").transform;
            sign.SetParent(gate, false);
            sign.localPosition = new Vector3(0f, 3.75f, 0f);
            sign.localRotation = Quaternion.Euler(30f, 0f, 0f);
            PrimitiveFactory.Part(PrimitiveType.Cube, sign, Vector3.zero, new Vector3(width - 0.15f, 1.7f, 0.18f), glow);
            float titleSize = Mathf.Clamp(0.32f / Mathf.Max(3, option.title.Length) * width, 0.035f, 0.075f);
            float subtitleSize = Mathf.Clamp(0.5f / Mathf.Max(6, option.subtitle.Length) * width, 0.02f, 0.035f);
            title = PrimitiveFactory.Label(sign, new Vector3(0f, 0.28f, -0.12f), option.title, 128, titleSize, Color.white);
            PrimitiveFactory.Label(sign, new Vector3(0f, -0.48f, -0.12f), option.subtitle, 96, subtitleSize, new Color(1f, 1f, 1f, 0.95f),
                LegionTextStyle.Menu);
            return gate;
        }
    }
}
