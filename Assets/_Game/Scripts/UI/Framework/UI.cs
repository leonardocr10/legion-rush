using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Identidade visual da interface: fantasia cartoon, azul profundo, dourado, painéis arredondados com brilho.
    public static class UIStyle
    {
        public static readonly Color Background = new Color(0.04f, 0.07f, 0.16f, 0.94f);
        public static readonly Color Panel = new Color(0.09f, 0.15f, 0.32f, 0.96f);
        public static readonly Color PanelLight = new Color(0.14f, 0.24f, 0.48f, 0.96f);
        public static readonly Color Blue = new Color(0.16f, 0.42f, 0.92f);
        public static readonly Color Gold = new Color(1f, 0.76f, 0.2f);
        public static readonly Color Green = new Color(0.26f, 0.78f, 0.32f);
        public static readonly Color Red = new Color(0.9f, 0.25f, 0.25f);
        public static readonly Color Purple = new Color(0.6f, 0.35f, 0.95f);
        public static readonly Color Gray = new Color(0.4f, 0.44f, 0.52f);
        public static readonly Color Text = Color.white;
        public static readonly Color TextDim = new Color(0.78f, 0.84f, 0.96f);
        public static readonly Color Outline = new Color(0.03f, 0.05f, 0.14f, 0.9f);

        private static Sprite rounded;
        private static Sprite circle;
        private static Font font;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Sprite Rounded
        {
            get
            {
                if (rounded == null) rounded = CreateRounded(96, 30);
                return rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle == null) circle = CreateRounded(64, 32);
                return circle;
            }
        }

        private static Sprite CreateRounded(int size, int radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "LR_UIRounded" + size,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            float border = Mathf.Min(radius, size / 2 - 1);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }
    }

    // Sprites recortados da arte do projeto (Resources/UIArt, gerados por tools/art/extract_ui_art.py).
    public static class UIArt
    {
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> cache =
            new System.Collections.Generic.Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out Sprite sprite)) return sprite;
            Texture2D texture = Resources.Load<Texture2D>("UIArt/" + name);
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Clamp;
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }
            else
            {
                GameLog.Warn("Arte de interface ausente: " + name);
            }
            cache[name] = sprite;
            return sprite;
        }

        public static Image Image(Transform parent, string name, bool preserveAspect = true)
        {
            RectTransform rect = UI.Rect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = Get(name);
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            if (image.sprite == null) image.color = Color.clear;
            return image;
        }
    }

    // Fábrica de elementos UGUI criados por código (sem prefabs), todos com âncoras relativas para qualquer resolução.
    public static class UI
    {
        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Stretch(rect);
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        // Posiciona por âncoras normalizadas (0–1) com margens em unidades de referência (1080×1920).
        public static RectTransform Place(RectTransform rect, float xMin, float yMin, float xMax, float yMax, float padding = 0f)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return rect;
        }

        public static Image Panel(Transform parent, Color color, string name = "Panel", bool shine = false)
        {
            RectTransform rect = Rect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = UIStyle.Rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            // Painéis da paleta padrão usam a moldura ornamentada do pacote de arte.
            if (color == UIStyle.Panel || color == UIStyle.PanelLight)
            {
                if (ApplyFrame(image, color == UIStyle.Panel ? "Frames/frame_gold" : "Frames/frame_gold_b", 0.35f)) return image;
            }
            if (shine) AddShine(rect);
            return image;
        }

        // Moldura 9-slice do pacote; heightRatio controla a espessura relativa das bordas.
        public static bool ApplyFrame(Image image, string path, float heightRatio)
        {
            Sprite sprite = LegionArt.Get(path);
            if (sprite == null) return false;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            UISliceFit fit = image.GetComponent<UISliceFit>() ?? image.gameObject.AddComponent<UISliceFit>();
            fit.heightRatio = 1f / Mathf.Max(0.05f, heightRatio);
            return true;
        }

        // Texto TextMeshPro com estilo do tema. Sem estilo explícito: sem contorno = Body, grande = Title, demais = Menu.
        // A cor passada é preservada (exceto estilos com gradiente, que usam a cor do tema).
        public static TMP_Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter,
            bool outline = true, string name = "Label", LegionTextStyle? style = null)
        {
            RectTransform rect = Rect(parent, name);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            LegionTextStyle chosen = style ?? (!outline ? LegionTextStyle.Body : size >= 64 ? LegionTextStyle.Title : LegionTextStyle.Menu);
            bool gradient = LegionTypography.HasGradient(chosen);
            LegionStyledText.Attach(label, chosen, gradient);
            if (!gradient) label.color = color;
            label.text = text;
            label.alignment = LegionTypography.Alignment(anchor);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableAutoSizing = true;
            label.fontSizeMax = size;
            label.fontSizeMin = Mathf.Max(10, size / 2);
            label.fontSize = size;
            label.raycastTarget = false;
            return label;
        }

        public static void SetStyle(TMP_Text label, LegionTextStyle style)
        {
            bool gradient = LegionTypography.HasGradient(style);
            Color color = label.color;
            LegionStyledText.Attach(label, style, gradient);
            if (!gradient) label.color = color;
        }

        public static Button Button(Transform parent, string label, Color color, Action onClick, int fontSize = 46, string name = "Button")
        {
            RectTransform rect = Rect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = UIStyle.Rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            Shadow shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -7f);
            bool art = ApplyButtonArt(image, color);
            if (!art) AddShine(rect);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = art ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.8f);
            button.colors = colors;
            if (art)
            {
                UIButtonArtState state = rect.gameObject.AddComponent<UIButtonArtState>();
                state.normal = image.sprite;
                state.disabled = LegionArt.Get("Buttons/btn_gray");
            }
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

            TMP_Text text = Label(rect, label, fontSize, Color.white, TextAnchor.MiddleCenter, true, "Label", LegionTextStyle.PrimaryButton);
            Stretch(text.rectTransform, 16f, 16f, 6f, 10f);
            return button;
        }

        public static void SetLabel(Button button, string text)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = text;
        }

        public static void SetColor(Button button, Color color)
        {
            if (!(button.targetGraphic is Image image)) return;
            UIButtonArtState state = button.GetComponent<UIButtonArtState>();
            if (state != null && ApplyButtonArt(image, color))
            {
                state.normal = image.sprite;
                if (!button.interactable) image.sprite = state.disabled;
                return;
            }
            image.color = color;
        }

        // Botões com a arte do pacote (dourado, verde, azul, roxo, laranja, vermelho, cinza), em 9-slice.
        private static bool ApplyButtonArt(Image image, Color color)
        {
            string path = LegionArt.ButtonFor(color);
            Sprite sprite = path != null ? LegionArt.Get(path) : null;
            if (sprite == null) return false;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            if (image.GetComponent<UISliceFit>() == null) image.gameObject.AddComponent<UISliceFit>();
            return true;
        }

        // Lista rolável vertical. Retorna o conteúdo, onde se adicionam linhas com Row().
        public static RectTransform ScrollList(Transform parent, float spacing = 18f, float padding = 12f)
        {
            RectTransform viewport = Rect(parent, "Viewport");
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);

            RectTransform content = Rect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            return content;
        }

        public static RectTransform Row(Transform list, float height, Color? background = null)
        {
            RectTransform row = Rect(list, "Row");
            LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            if (background.HasValue)
            {
                Image image = row.gameObject.AddComponent<Image>();
                image.sprite = UIStyle.Rounded;
                image.type = Image.Type.Sliced;
                image.color = background.Value;
                image.raycastTarget = false;
                if (background.Value == UIStyle.Panel) ApplyFrame(image, "Frames/frame_gold_b", 1f);
            }
            return row;
        }

        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }

        public static Image Bar(Transform parent, float ratio, Color fill)
        {
            Image back = Panel(parent, new Color(0f, 0f, 0f, 0.45f), "Bar");
            Image front = Panel(back.transform, fill, "Fill");
            front.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            return front;
        }

        public static Slider Slider(Transform parent, float value, Action<float> onChanged)
        {
            RectTransform root = Rect(parent, "Slider");
            Slider slider = root.gameObject.AddComponent<Slider>();
            Image background = Panel(root, new Color(0f, 0f, 0f, 0.45f), "Background");
            Place(background.rectTransform, 0f, 0.3f, 1f, 0.7f);

            RectTransform fillArea = Rect(root, "Fill Area");
            Place(fillArea, 0f, 0.3f, 1f, 0.7f);
            fillArea.offsetMin = new Vector2(10f, 0f);
            fillArea.offsetMax = new Vector2(-10f, 0f);
            Image fill = Panel(fillArea, UIStyle.Gold, "Fill");

            RectTransform handleArea = Rect(root, "Handle Slide Area");
            handleArea.offsetMin = new Vector2(30f, 0f);
            handleArea.offsetMax = new Vector2(-30f, 0f);
            Image handle = Panel(handleArea, Color.white, "Handle");
            handle.sprite = UIStyle.Circle;
            handle.raycastTarget = true;
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            handle.rectTransform.sizeDelta = new Vector2(64f, 0f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            if (onChanged != null) slider.onValueChanged.AddListener(v => onChanged(v));
            return slider;
        }

        private static void AddShine(RectTransform parent)
        {
            Image shine = Panel(parent, new Color(1f, 1f, 1f, 0.13f), "Shine");
            Place(shine.rectTransform, 0.02f, 0.52f, 0.98f, 0.96f);
        }
    }
}
