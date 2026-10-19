using TMPro;
using UnityEngine;
using LegionRush.Environments;

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

            // Kit de ambiente disponível: portal ornamentado (pilares, arco, cortina de energia). Senão, primitivas.
            bool ornate = EnvGatePortal.Available;
            if (ornate)
            {
                EnvGatePortal.Build(gate, width, color, Mathf.RoundToInt(row.z * 13f + centerX * 7f), 1f, option.category);
            }
            else
            {
                float half = width * 0.5f - 0.15f;
                PrimitiveFactory.Part(PrimitiveType.Cube, gate, new Vector3(-half, 1.6f, 0f), new Vector3(0.25f, 3.2f, 0.25f), frame);
                PrimitiveFactory.Part(PrimitiveType.Cube, gate, new Vector3(half, 1.6f, 0f), new Vector3(0.25f, 3.2f, 0.25f), frame);
                PrimitiveFactory.Part(PrimitiveType.Cube, gate, new Vector3(0f, 0.03f, 0f), new Vector3(width - 0.4f, 0.06f, 1.4f), glow, default, false);
                PrimitiveFactory.Part(PrimitiveType.Cube, gate, new Vector3(0f, 1.55f, 0f), new Vector3(width - 0.45f, 3.0f, 0.04f), curtain, default, false);
            }

            Transform sign = new GameObject("Sign").transform;
            sign.SetParent(gate, false);
            sign.localPosition = new Vector3(0f, ornate ? 5.75f : 3.75f, ornate ? 0.1f : 0f);
            sign.localRotation = Quaternion.Euler(30f, 0f, 0f);
            // Placa na largura do portal, na cor do efeito (azul soma, verde multiplica, laranja melhora, vermelho perde),
            // só com o valor em fonte grande e o ícone da arte. Sem subtítulo nem placas ATALHO / CAMINHO LONGO.
            Material plate = PlateMaterial(PlateColor(option));
            float textX = 0f;
            float textWidth = width - 0.5f;
            float textHeight = 1.3f;
            if (plate != null)
            {
                float plateWidth = width - 0.1f;
                PrimitiveFactory.Part(PrimitiveType.Quad, sign, new Vector3(0f, 0f, -0.02f), new Vector3(plateWidth, plateWidth / 2.75f, 1f), plate, default, false);
                textX = plateWidth * 0.12f;
                textWidth = plateWidth * 0.68f;
                textHeight = plateWidth / 2.75f * 0.8f;
            }
            else if (ornate)
            {
                Material plaque = MaterialLibrary.Get(color * 0.55f, 0.5f, 0f);
                Material gold = MaterialLibrary.Get(new Color(1f, 0.76f, 0.26f), 0.15f, 0.2f);
                PrimitiveFactory.Part(PrimitiveType.Cube, sign, new Vector3(0f, 0f, 0.03f), new Vector3(width - 0.05f, 1.8f, 0.14f), gold);
                PrimitiveFactory.Part(PrimitiveType.Cube, sign, Vector3.zero, new Vector3(width - 0.3f, 1.58f, 0.18f), plaque);
            }
            else PrimitiveFactory.Part(PrimitiveType.Cube, sign, Vector3.zero, new Vector3(width - 0.15f, 1.7f, 0.18f), glow);
            string value = PlateText(option);
            float titleSize = Mathf.Clamp(0.64f / Mathf.Max(3, value.Length) * width, 0.07f, 0.15f);
            title = PrimitiveFactory.Label(sign, new Vector3(textX, 0f, -0.12f), value, 128, titleSize, Color.white);
            FitWidth(title, textWidth, textHeight);
            return gate;
        }

        // Placas recortadas da prancha: azul (tropas e defesa), verde (multiplicar), laranja (melhorias e recursos),
        // vermelha (perdas e rotas). Material compartilhado por categoria.
        private static readonly System.Collections.Generic.Dictionary<string, Material> plates =
            new System.Collections.Generic.Dictionary<string, Material>();

        private enum Plate { Blue, Green, Orange, Red }

        // A cor diz o que o portal faz: soma (azul), multiplica (verde), melhora (laranja), perde (vermelho).
        // Rotas: o atalho traz emboscada (vermelho); o caminho longo dá ouro e tropas (azul).
        private static Plate PlateColor(GateOption option)
        {
            switch (option.category)
            {
                case GateCategory.Multiply: return Plate.Green;
                case GateCategory.Upgrade:
                case GateCategory.Resource: return Plate.Orange;
                case GateCategory.Negative: return Plate.Red;
                case GateCategory.Route: return HasEffect(option, GateEffectType.SkipAhead) ? Plate.Red : Plate.Blue;
                default: return Plate.Blue;
            }
        }

        // Só o valor: rotas viram "-40m" (atalho) ou "+120" (ouro do caminho longo); os demais já têm o valor no título.
        private static string PlateText(GateOption option)
        {
            if (option.category != GateCategory.Route || option.effects == null || option.effects.Count == 0) return option.title;
            GateEffect first = option.effects[0];
            return first.type == GateEffectType.SkipAhead ? $"-{first.value:0}m" : $"+{first.value:0}";
        }

        private static bool HasEffect(GateOption option, GateEffectType type)
        {
            if (option.effects == null) return false;
            foreach (GateEffect effect in option.effects)
                if (effect.type == type) return true;
            return false;
        }

        private static Material PlateMaterial(Plate color)
        {
            string name = color switch
            {
                Plate.Green => "Gate_Green",
                Plate.Orange => "Gate_Orange",
                Plate.Red => "Gate_Red",
                _ => "Gate_Blue"
            };
            if (plates.TryGetValue(name, out Material material)) return material;
            Texture2D texture = BattleArt.Texture(name);
            material = texture != null ? MaterialLibrary.CreateGlow(texture, Color.white, false, 1f) : null;
            // Antes do texto: sem isso a ordenação por distância às vezes desenha a placa por cima do rótulo.
            if (material != null) material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 20;
            plates[name] = material;
            return material;
        }

        private static void FitWidth(TMP_Text text, float width, float height)
        {
            if (text == null) return;
            float size = text.fontSize;
            text.rectTransform.sizeDelta = new Vector2(Mathf.Max(0.5f, width), height);
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = size * 0.35f;
        }
    }
}
