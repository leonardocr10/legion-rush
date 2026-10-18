using TMPro;
using UnityEngine;

namespace LegionRush
{
    public enum LegionTextAnimation
    {
        Pop,        // moedas recebidas
        Punch,      // exército aumentou
        Highlight,  // portão atravessado
        Entrance,   // VITÓRIA
        Shake,      // dano crítico
        FadeScale   // recompensas
    }

    // Animação leve de um texto (escala, tremor, alfa). Fica desligado quando parado: sem custo por quadro nem alocações.
    [DisallowMultipleComponent]
    public class LegionTextFx : MonoBehaviour
    {
        [SerializeField] private float popScale = 1.3f;
        [SerializeField] private float duration = 0.35f;

        private TMP_Text text;
        private Transform cached;
        private Vector3 baseScale = Vector3.one;
        private Vector3 basePosition;
        private LegionTextAnimation current;
        private float time;
        private float length;
        private bool captured;

        public static LegionTextFx Play(TMP_Text target, LegionTextAnimation animation)
        {
            if (target == null) return null;
            LegionTextFx fx = target.GetComponent<LegionTextFx>();
            if (fx == null) fx = target.gameObject.AddComponent<LegionTextFx>();
            fx.Play(animation);
            return fx;
        }

        public void Play(LegionTextAnimation animation)
        {
            if (text == null) text = GetComponent<TMP_Text>();
            cached = transform;
            if (!captured || !enabled)
            {
                baseScale = cached.localScale;
                basePosition = cached.localPosition;
                captured = true;
            }
            current = animation;
            time = 0f;
            length = animation switch
            {
                LegionTextAnimation.Entrance => 0.6f,
                LegionTextAnimation.FadeScale => 0.45f,
                LegionTextAnimation.Highlight => 0.5f,
                LegionTextAnimation.Shake => 0.3f,
                LegionTextAnimation.Punch => 0.2f,
                _ => duration
            };
            enabled = true;
            Evaluate(0f);
        }

        private void Update()
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / length);
            Evaluate(t);
            if (t >= 1f) Stop();
        }

        private void OnDisable()
        {
            if (captured) Stop();
        }

        private void Stop()
        {
            if (cached != null)
            {
                cached.localScale = baseScale;
                cached.localPosition = basePosition;
            }
            if (text != null) text.alpha = 1f;
            enabled = false;
        }

        private void Evaluate(float t)
        {
            float scale = 1f;
            Vector3 offset = Vector3.zero;
            switch (current)
            {
                case LegionTextAnimation.Pop:
                    scale = 1f + (popScale - 1f) * Mathf.Sin(t * Mathf.PI);
                    break;
                case LegionTextAnimation.Punch:
                    scale = 1f + 0.18f * Mathf.Sin(t * Mathf.PI);
                    break;
                case LegionTextAnimation.Highlight:
                    scale = 1f + 0.35f * Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f);
                    break;
                case LegionTextAnimation.Entrance:
                    scale = BackOut(t, 2.2f);
                    if (text != null) text.alpha = Mathf.Clamp01(t * 3f);
                    break;
                case LegionTextAnimation.FadeScale:
                    scale = Mathf.Lerp(0.6f, 1f, BackOutCurve(t));
                    if (text != null) text.alpha = t;
                    break;
                case LegionTextAnimation.Shake:
                    float strength = 9f * (1f - t);
                    offset = new Vector3(Mathf.Sin(t * 95f) * strength, Mathf.Cos(t * 70f) * strength * 0.5f, 0f);
                    scale = 1f + 0.12f * (1f - t);
                    break;
            }
            if (cached == null) return;
            cached.localScale = baseScale * scale;
            cached.localPosition = basePosition + offset;
        }

        // Começa grande e assenta com leve ultrapassagem (entrada de VITÓRIA).
        private static float BackOut(float t, float from) => Mathf.LerpUnclamped(from, 1f, BackOutCurve(t));

        private static float BackOutCurve(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }
    }

    // Números flutuantes de dano no mundo, com pool fixo (sem criar objetos durante o combate).
    public class LegionFloatingText : MonoBehaviour
    {
        private const int PoolSize = 24;
        private const int MaxPerFrame = 3;
        private const float Life = 0.8f;

        private static LegionFloatingText instance;

        private readonly TextMeshPro[] labels = new TextMeshPro[PoolSize];
        private readonly float[] ages = new float[PoolSize];
        private readonly Vector3[] origins = new Vector3[PoolSize];
        private readonly bool[] critical = new bool[PoolSize];
        private int next;
        private int frame = -1;
        private int spawnedThisFrame;
        private Camera view;

        public static void Damage(Vector3 position, float amount, bool isCritical)
        {
            if (amount < 1f || LegionTypography.Theme == null) return;
            if (instance == null)
            {
                var go = new GameObject("LR_FloatingText");
                instance = go.AddComponent<LegionFloatingText>();
                instance.Build();
            }
            instance.Spawn(position, Mathf.RoundToInt(amount), isCritical);
        }

        private void Build()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Damage");
                go.transform.SetParent(transform, false);
                TextMeshPro label = go.AddComponent<TextMeshPro>();
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.rectTransform.sizeDelta = new Vector2(4f, 1.5f);
                label.fontSize = 6f;
                go.SetActive(false);
                labels[i] = label;
                ages[i] = Life;
            }
        }

        private void Spawn(Vector3 position, int amount, bool isCritical)
        {
            if (frame != Time.frameCount)
            {
                frame = Time.frameCount;
                spawnedThisFrame = 0;
            }
            if (spawnedThisFrame >= MaxPerFrame && !isCritical) return;
            spawnedThisFrame++;

            int i = next;
            next = (next + 1) % PoolSize;
            TextMeshPro label = labels[i];
            LegionTypography.Apply(label, isCritical ? LegionTextStyle.Epic : LegionTextStyle.Negative);
            label.fontSize = isCritical ? 8.5f : 6f;
            if (isCritical) label.SetText("{0}!", amount);
            else label.SetText("{0}", amount);
            origins[i] = position + new Vector3(Random.Range(-0.4f, 0.4f), 2.2f, 0f);
            critical[i] = isCritical;
            ages[i] = 0f;
            label.gameObject.SetActive(true);
            Place(i);
        }

        private void LateUpdate()
        {
            if (view == null) view = Camera.main;
            float dt = Time.deltaTime;
            for (int i = 0; i < PoolSize; i++)
            {
                if (ages[i] >= Life) continue;
                ages[i] += dt;
                if (ages[i] >= Life)
                {
                    labels[i].gameObject.SetActive(false);
                    continue;
                }
                Place(i);
            }
        }

        private void Place(int i)
        {
            float t = ages[i] / Life;
            Transform label = labels[i].transform;
            label.position = origins[i] + Vector3.up * (1.2f * t);
            if (view != null) label.rotation = view.transform.rotation;
            float pop = critical[i] ? 1f + 0.5f * Mathf.Sin(Mathf.Min(1f, t * 3f) * Mathf.PI) : 1f;
            if (critical[i] && t < 0.3f) label.position += new Vector3(Mathf.Sin(t * 120f) * 0.08f, 0f, 0f);
            label.localScale = Vector3.one * pop;
            labels[i].alpha = 1f - Mathf.Clamp01((t - 0.6f) / 0.4f);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
