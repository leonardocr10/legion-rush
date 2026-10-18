using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Componentes visuais reutilizáveis feitos com a arte real do jogo (Resources/UIArt).
    // Layout em "pixels de referência" do mockup (941×1672) convertidos para unidades do canvas (1080 de largura),
    // ancorados à borda certa (topo, base, esquerda, direita) para se adaptar a qualquer proporção de tela.
    public static class LegionUI
    {
        public const float RefWidth = 941f;
        public const float RefHeight = 1672f;
        public const float Px = 1080f / RefWidth;

        public enum Edge { Top, Bottom, TopLeft, TopRight }

        // Posiciona um retângulo do mockup (x0,y0,x1,y1) preso a uma borda da tela.
        public static RectTransform Place(RectTransform rect, Edge edge, float x0, float y0, float x1, float y1)
        {
            float w = (x1 - x0) * Px;
            float h = (y1 - y0) * Px;
            rect.sizeDelta = new Vector2(w, h);
            switch (edge)
            {
                case Edge.TopLeft:
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(x0 * Px, -y0 * Px);
                    break;
                case Edge.TopRight:
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
                    rect.anchoredPosition = new Vector2(-(RefWidth - x1) * Px, -y0 * Px);
                    break;
                case Edge.Bottom:
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = new Vector2(((x0 + x1) * 0.5f - RefWidth * 0.5f) * Px, (RefHeight - y1) * Px);
                    break;
                default:
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(((x0 + x1) * 0.5f - RefWidth * 0.5f) * Px, -y0 * Px);
                    break;
            }
            return rect;
        }

        // Filho posicionado em coordenadas do mockup relativas à caixa do pai (que também está em coordenadas do mockup).
        public static RectTransform Inside(RectTransform child, Vector4 parentBox, float x0, float y0, float x1, float y1)
        {
            float w = parentBox.z - parentBox.x;
            float h = parentBox.w - parentBox.y;
            child.anchorMin = new Vector2((x0 - parentBox.x) / w, 1f - (y1 - parentBox.y) / h);
            child.anchorMax = new Vector2((x1 - parentBox.x) / w, 1f - (y0 - parentBox.y) / h);
            child.offsetMin = Vector2.zero;
            child.offsetMax = Vector2.zero;
            return child;
        }

        // LegionIconButton / LegionPrimaryButton: botão cuja arte inteira é um sprite, com toque, som e vibração.
        public static Button SpriteButton(Transform parent, string sprite, Action onClick, string name = null)
        {
            RectTransform rect = UI.Rect(parent, name ?? sprite);
            Image image = rect.gameObject.AddComponent<Image>();
            // "Categoria/nome" = pacote de arte (LegionArt); nome simples = recortes da tela inicial (UIArt).
            image.sprite = sprite.Contains("/") ? LegionArt.Get(sprite) : UIArt.Get(sprite);
            if (image.sprite == null) image.color = new Color(1f, 1f, 1f, 0.15f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.86f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            rect.gameObject.AddComponent<UIButtonFeedback>();
            if (onClick != null)
            {
                button.onClick.AddListener(() =>
                {
                    GameAudio.Play(SoundId.UiClick);
                    Haptics.Tap();
                    onClick();
                });
            }
            return button;
        }

        // LegionNotificationBadge: círculo vermelho com borda branca e número ou "!".
        public static TMP_Text Badge(Transform parent, Vector4 parentBox, float cx, float cy, string text)
        {
            RectTransform rect = Inside(UI.Rect(parent, "Badge"), parentBox, cx - 19f, cy - 19f, cx + 19f, cy + 19f);
            Image ring = rect.gameObject.AddComponent<Image>();
            ring.sprite = UIStyle.Circle;
            ring.color = Color.white;
            ring.raycastTarget = false;
            Image fill = UI.Panel(rect, new Color(0.88f, 0.1f, 0.12f), "Fill");
            fill.sprite = UIStyle.Circle;
            fill.type = Image.Type.Simple;
            UI.Stretch(fill.rectTransform, 3f, 3f, 3f, 3f);
            TMP_Text label = UI.Label(rect, text, 30, Color.white, TextAnchor.MiddleCenter, true, "Count", LegionTextStyle.Hud);
            UI.Stretch(label.rectTransform, 2f, 2f, 0f, 2f);
            rect.gameObject.AddComponent<UIBadgeBounce>();
            return label;
        }

        // LegionProgressBar: trilho escuro arredondado + preenchimento com brilho.
        public static Image ProgressBar(Transform parent, Vector4 parentBox, float x0, float y0, float x1, float y1, Color fill)
        {
            RectTransform rect = Inside(UI.Rect(parent, "Progress"), parentBox, x0, y0, x1, y1);
            Image back = rect.gameObject.AddComponent<Image>();
            back.sprite = UIStyle.Rounded;
            back.type = Image.Type.Sliced;
            back.color = new Color(0.02f, 0.05f, 0.14f, 0.85f);
            back.raycastTarget = false;
            Image front = UI.Panel(rect, fill, "Fill", true);
            UI.Stretch(front.rectTransform, 3f, 3f, 3f, 3f);
            front.rectTransform.anchorMax = new Vector2(0f, 1f);
            return front;
        }

        public static void SetProgress(Image fill, float ratio)
        {
            RectTransform rect = fill.rectTransform;
            rect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            fill.enabled = ratio > 0.001f;
        }

        public static TMP_Text Text(Transform parent, Vector4 parentBox, float x0, float y0, float x1, float y1, string text,
            float size, LegionTextStyle style, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            RectTransform rect = Inside(UI.Rect(parent, "Text"), parentBox, x0, y0, x1, y1);
            TMP_Text label = UI.Label(rect, text, Mathf.RoundToInt(size * Px), color, anchor, true, "Value", style);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }
    }

    // Entrada suave: desliza a partir de um deslocamento e aparece (alfa) ao ativar.
    [RequireComponent(typeof(RectTransform))]
    public class UISlideIn : MonoBehaviour
    {
        public Vector2 offset = new Vector2(0f, -120f);
        public float delay;
        public float duration = 0.35f;

        private RectTransform rect;
        private CanvasGroup group;
        private Vector2 home;
        private float time;
        private bool captured;

        private void OnEnable()
        {
            rect = (RectTransform)transform;
            if (!captured)
            {
                home = rect.anchoredPosition;
                captured = true;
            }
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            time = -delay;
            Apply(0f);
        }

        private void OnDisable()
        {
            if (captured) rect.anchoredPosition = home;
            if (group != null) group.alpha = 1f;
        }

        private void Update()
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            Apply(t);
            if (t >= 1f) enabled = false;
        }

        private void Apply(float t)
        {
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            rect.anchoredPosition = home + offset * (1f - eased);
            group.alpha = eased;
        }

        public void Replay()
        {
            enabled = false;
            enabled = true;
        }
    }

    // Selo de alerta pulando de leve.
    public class UIBadgeBounce : MonoBehaviour
    {
        private void Update()
        {
            float s = 1f + 0.08f * Mathf.Max(0f, Mathf.Sin(Time.unscaledTime * 4f));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    // Escala respirando (botão JOGAR). Fica num contêiner pai para não brigar com o efeito de toque do botão.
    public class UIBreath : MonoBehaviour
    {
        public float amount = 0.025f;
        public float speed = 2.6f;

        private void OnDisable() => transform.localScale = Vector3.one;

        private void Update()
        {
            float s = 1f + amount * Mathf.Sin(Time.unscaledTime * speed);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    // Faixa de brilho atravessando o botão de tempos em tempos.
    public class UIShineSweep : MonoBehaviour
    {
        public float interval = 2.8f;
        public float sweep = 0.6f;

        private RectTransform band;
        private RectTransform area;

        public static void Attach(RectTransform target)
        {
            RectTransform clip = UI.Rect(target, "ShineClip");
            UI.Stretch(clip, 10f, 10f, 10f, 10f);
            clip.gameObject.AddComponent<RectMask2D>();
            RectTransform band = UI.Rect(clip, "Shine");
            band.anchorMin = new Vector2(0f, -0.3f);
            band.anchorMax = new Vector2(0f, 1.3f);
            band.sizeDelta = new Vector2(70f, 0f);
            band.localRotation = Quaternion.Euler(0f, 0f, -18f);
            Image image = band.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 0.9f, 0.35f);
            image.raycastTarget = false;
            UIShineSweep sweep = clip.gameObject.AddComponent<UIShineSweep>();
            sweep.band = band;
            sweep.area = clip;
        }

        private void Update()
        {
            if (band == null) return;
            float cycle = Time.unscaledTime % interval;
            float t = Mathf.Clamp01(cycle / sweep);
            float width = area.rect.width;
            band.anchoredPosition = new Vector2(Mathf.Lerp(-100f, width + 100f, t), 0f);
            band.gameObject.SetActive(cycle < sweep);
        }
    }

    // Neve leve sobre o cenário: poucos flocos reaproveitados, sem alocação por quadro.
    public class UISnow : MonoBehaviour
    {
        private const int Count = 42;
        private readonly RectTransform[] flakes = new RectTransform[Count];
        private readonly Vector3[] state = new Vector3[Count]; // x: velocidade, y: fase, z: amplitude
        private RectTransform area;

        public static void Attach(RectTransform parent)
        {
            RectTransform layer = UI.Rect(parent, "Snow");
            layer.gameObject.AddComponent<UISnow>().Build(layer);
        }

        private void Build(RectTransform layer)
        {
            area = layer;
            for (int i = 0; i < Count; i++)
            {
                RectTransform flake = UI.Rect(layer, "Flake");
                flake.anchorMin = flake.anchorMax = new Vector2(0f, 0f);
                float size = UnityEngine.Random.Range(6f, 16f);
                flake.sizeDelta = new Vector2(size, size);
                Image image = flake.gameObject.AddComponent<Image>();
                image.sprite = UIStyle.Circle;
                image.color = new Color(1f, 1f, 1f, UnityEngine.Random.Range(0.35f, 0.85f));
                image.raycastTarget = false;
                flakes[i] = flake;
                state[i] = new Vector3(UnityEngine.Random.Range(40f, 110f), UnityEngine.Random.Range(0f, 6.28f), UnityEngine.Random.Range(10f, 40f));
                flake.anchoredPosition = new Vector2(UnityEngine.Random.Range(0f, 1080f), UnityEngine.Random.Range(0f, 2000f));
            }
        }

        private void Update()
        {
            if (area == null) return;
            Rect bounds = area.rect;
            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;
            for (int i = 0; i < Count; i++)
            {
                Vector2 p = flakes[i].anchoredPosition;
                p.y -= state[i].x * dt;
                p.x += Mathf.Sin(time * 0.8f + state[i].y) * state[i].z * dt;
                if (p.y < -20f)
                {
                    p.y = bounds.height + 20f;
                    p.x = UnityEngine.Random.Range(0f, bounds.width);
                }
                flakes[i].anchoredPosition = p;
            }
        }
    }
}
