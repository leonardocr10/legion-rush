using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Componentes compartilhados do redesign (entrega/PROMPT_1_CODIGO.md, Tarefa 0), no grid do mockup (941×1672).
    // Toda a arte é 9-slice sem texto; os rótulos são TMP. Os nomes novos (Tabs/Tab_On, Toggles/Toggle_On, Panels/Panel_Hero,
    // Popups/Modal_Frame, Popups/Header_Ribbon) são os do PROMPT_2_IMAGENS.md: enquanto não existirem, vale a arte atual
    // do pacote (btn_blue, frame_gold_b, frame_gold, popup_header).
    // O estado nunca é pintado em Graphic.color: cada estado tem o próprio sprite, então nada sai branco.

    // ---------------------------------------------------------------- 0.1 cabeçalho

    public static class LegionHeader
    {
        public const float BackX0 = 18f, BackY0 = 134f, BackX1 = 102f, BackY1 = 216f;
        public const float TitleX0 = 223f, TitleY0 = 130f, TitleX1 = 718f, TitleY1 = 226f;
        public const float ContentTop = 240f;

        // Voltar à esquerda e faixa de título abaixo da carteira (que termina em y 120).
        public static TMP_Text Build(RectTransform root, string title, Action onBack)
        {
            Image back = HeroesArt.Image(root, "Button_Back", "Back");
            LegionUI.Place(back.rectTransform, LegionUI.Edge.TopLeft, BackX0, BackY0, BackX1, BackY1);
            if (onBack != null) MainMenuButtons.Make(back.rectTransform, back, onBack);
            return Title(root, title);
        }

        public static TMP_Text Title(RectTransform root, string title)
        {
            string art = LegionArt.Pick("Popups/Header_Ribbon", "Popups/popup_header");
            bool clean = art == "Popups/Header_Ribbon";
            Image ribbon = LegionArt.Image(root, art, false, "Header");
            ribbon.preserveAspect = !clean;
            LegionUI.Place(ribbon.rectTransform, LegionUI.Edge.Top, TitleX0, TitleY0, TitleX1, TitleY1);
            TMP_Text label = UI.Label(root, title, Mathf.RoundToInt(42f * LegionUI.Px), Color.white,
                TextAnchor.MiddleCenter, true, "Title", LegionTextStyle.Title);
            // Faixa limpa: texto no miolo. Faixa atual (com coroa em cima): texto na placa da metade de baixo.
            if (clean) LegionUI.Place(label.rectTransform, LegionUI.Edge.Top, TitleX0 + 70, TitleY0 + 14, TitleX1 - 70, TitleY1 - 14);
            else LegionUI.Place(label.rectTransform, LegionUI.Edge.Top, 330, 174, 612, 218);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontSizeMin = Mathf.RoundToInt(22f * LegionUI.Px);
            return label;
        }
    }

    // ---------------------------------------------------------------- 0.3 abas e seletores

    public class LegionTabs : MonoBehaviour
    {
        private static string OnSprite => LegionArt.Pick("Tabs/Tab_On", "Buttons/btn_blue");
        private static string OffSprite => LegionArt.Pick("Tabs/Tab_Off", "Frames/frame_gold_b");

        private class Entry
        {
            public Image frame;
            public TMP_Text label;
            public Image icon;
            public GameObject badge;
            public GameObject soon;
            public bool locked;
        }

        private readonly List<Entry> entries = new List<Entry>();
        private Action<int> onSelect;
        private Action<int> onLocked;

        public int Selected { get; private set; } = -1;
        public int Count => entries.Count;

        // Preenche o retângulo (já posicionado pelo chamador) com abas iguais lado a lado.
        // fontSize e gap no grid do mockup; icons é opcional (caminhos do LegionArt, null = sem ícone).
        public static LegionTabs Create(RectTransform area, string[] labels, Action<int> onSelect, float fontSize = 26f,
            float gap = 8f, string[] icons = null, Action<int> onLocked = null)
        {
            LegionTabs tabs = area.gameObject.AddComponent<LegionTabs>();
            tabs.onSelect = onSelect;
            tabs.onLocked = onLocked;
            HorizontalLayoutGroup row = area.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = gap * LegionUI.Px;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = true;
            for (int i = 0; i < labels.Length; i++) tabs.Add(area, labels[i], icons != null && i < icons.Length ? icons[i] : null, fontSize);
            return tabs;
        }

        // Mesma coisa, mas em coluna (coluna lateral de Configurações).
        public static LegionTabs CreateVertical(RectTransform area, string[] labels, Action<int> onSelect, float fontSize = 24f,
            float gap = 10f, string[] icons = null, Action<int> onLocked = null)
        {
            LegionTabs tabs = area.gameObject.AddComponent<LegionTabs>();
            tabs.onSelect = onSelect;
            tabs.onLocked = onLocked;
            VerticalLayoutGroup column = area.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = gap * LegionUI.Px;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = column.childForceExpandHeight = true;
            for (int i = 0; i < labels.Length; i++) tabs.Add(area, labels[i], icons != null && i < icons.Length ? icons[i] : null, fontSize);
            return tabs;
        }

        private void Add(RectTransform area, string text, string iconPath, float fontSize)
        {
            int index = entries.Count;
            var entry = new Entry();
            entry.frame = LegionArt.Image(area, OffSprite, true, "Tab_" + index);
            entry.frame.preserveAspect = false;
            RectTransform inner = UI.Rect(entry.frame.transform, "Inner");
            UI.Stretch(inner, 10f, 10f, 6f, 10f);
            HorizontalLayoutGroup content = inner.gameObject.AddComponent<HorizontalLayoutGroup>();
            content.childAlignment = TextAnchor.MiddleCenter;
            content.spacing = 8f;
            content.childControlWidth = content.childControlHeight = true;
            content.childForceExpandWidth = false;
            content.childForceExpandHeight = true;
            if (!string.IsNullOrEmpty(iconPath))
            {
                entry.icon = LegionArt.Image(inner, iconPath, false, "Icon");
                LayoutElement size = entry.icon.gameObject.AddComponent<LayoutElement>();
                size.preferredWidth = fontSize * 1.9f * LegionUI.Px;
                size.flexibleWidth = 0f;
            }
            entry.label = UI.Label(inner, text, Mathf.RoundToInt(fontSize * LegionUI.Px), Color.white, TextAnchor.MiddleCenter, true,
                "Label", LegionTextStyle.PrimaryButton);
            entry.label.textWrappingMode = TextWrappingModes.NoWrap;
            entry.label.fontSizeMin = Mathf.RoundToInt(Mathf.Min(fontSize, 22f) * LegionUI.Px);
            LayoutElement grow = entry.label.gameObject.AddComponent<LayoutElement>();
            grow.flexibleWidth = 1f;
            MainMenuButtons.Make(entry.frame.rectTransform, entry.frame, () => Tap(index));
            entries.Add(entry);
            Paint(index);
        }

        private void Tap(int index)
        {
            if (entries[index].locked)
            {
                if (onLocked != null) onLocked(index);
                else UIRoot.Instance?.Toast("Em breve");
                return;
            }
            Select(index, true);
        }

        public void Select(int index, bool notify = false)
        {
            int previous = Selected;
            Selected = index;
            if (previous >= 0 && previous < entries.Count) Paint(previous);
            if (index >= 0 && index < entries.Count) Paint(index);
            if (notify) onSelect?.Invoke(index);
        }

        public void SetLabel(int index, string text) => entries[index].label.text = text;

        // Selo vermelho "!" no canto (missão para resgatar, por exemplo).
        public void SetAlert(int index, bool on)
        {
            Entry entry = entries[index];
            if (on && entry.badge == null)
            {
                RectTransform rect = UI.Rect(entry.frame.transform, "Alert");
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(40f, 40f);
                rect.anchoredPosition = new Vector2(-12f, -8f);
                Image ring = rect.gameObject.AddComponent<Image>();
                ring.sprite = UIStyle.Circle;
                ring.raycastTarget = false;
                Image fill = UI.Panel(rect, new Color(0.88f, 0.1f, 0.12f), "Fill");
                fill.sprite = UIStyle.Circle;
                fill.type = Image.Type.Simple;
                UI.Stretch(fill.rectTransform, 3f, 3f, 3f, 3f);
                TMP_Text mark = UI.Label(rect, "!", 30, Color.white, TextAnchor.MiddleCenter, true, "Mark", LegionTextStyle.Hud);
                UI.Stretch(mark.rectTransform);
                rect.gameObject.AddComponent<UIBadgeBounce>();
                entry.badge = rect.gameObject;
            }
            if (entry.badge != null) entry.badge.SetActive(on);
        }

        // Aba de recurso que ainda não existe: escurecida, com selo EM BREVE, e não seleciona.
        public void SetLocked(int index, bool locked)
        {
            Entry entry = entries[index];
            entry.locked = locked;
            if (locked && entry.soon == null) entry.soon = LegionBadge.Soon(entry.frame.transform).gameObject;
            if (entry.soon != null) entry.soon.SetActive(locked);
            Paint(index);
        }

        private void Paint(int index)
        {
            Entry entry = entries[index];
            bool on = index == Selected && !entry.locked;
            entry.frame.sprite = LegionArt.Get(on ? OnSprite : OffSprite);
            entry.frame.color = Color.white;
            float a = entry.locked ? 0.55f : 1f;
            entry.label.color = on ? Color.white : new Color(0.82f, 0.88f, 1f, a);
            entry.label.fontStyle = FontStyles.Normal;
            if (entry.icon != null) entry.icon.color = new Color(1f, 1f, 1f, entry.locked ? 0.5f : 1f);
        }
    }

    // ---------------------------------------------------------------- 0.4 chave liga/desliga

    public class LegionToggle : MonoBehaviour
    {
        private Image image;
        private Func<bool> get;
        private Action<bool> set;

        public bool On { get; private set; }

        private static readonly Color TrackOn = new Color(0.2f, 0.72f, 0.24f);
        private static readonly Color TrackOff = new Color(0.06f, 0.13f, 0.34f);
        private static readonly Color KnobOff = new Color(0.62f, 0.66f, 0.74f);

        private Image knob;
        private bool art;

        // Ligado: verde com bolinha branca à direita. Desligado: azul-escuro com bolinha cinza à esquerda.
        // Com Toggles/Toggle_On e Toggle_Off (Prompt 2) usa a arte; sem ela, trilho e bolinha da própria UI.
        public static LegionToggle Create(Transform parent, Func<bool> get, Action<bool> set, string name = "Toggle")
        {
            bool art = LegionArt.Pick("Toggles/Toggle_On", string.Empty) == "Toggles/Toggle_On";
            Image image;
            Image knob = null;
            if (art)
            {
                image = LegionArt.Image(parent, "Toggles/Toggle_Off", false, name);
                image.preserveAspect = true;
            }
            else
            {
                image = UI.Panel(parent, TrackOff, name);
                Outline ring = image.gameObject.AddComponent<Outline>();
                ring.effectColor = new Color(0.02f, 0.04f, 0.12f, 0.95f);
                ring.effectDistance = new Vector2(2f, -2f);
                knob = UI.Panel(image.transform, Color.white, "Knob");
                knob.sprite = UIStyle.Circle;
                knob.type = Image.Type.Simple;
                knob.preserveAspect = true;
            }
            LegionToggle toggle = image.gameObject.AddComponent<LegionToggle>();
            toggle.image = image;
            toggle.knob = knob;
            toggle.art = art;
            toggle.get = get;
            toggle.set = set;
            MainMenuButtons.Make(image.rectTransform, image, toggle.Flip);
            toggle.Refresh();
            return toggle;
        }

        private void Flip()
        {
            set?.Invoke(!On);
            Refresh();
        }

        public void Refresh()
        {
            On = get != null && get();
            if (art)
            {
                image.sprite = LegionArt.Get(On ? "Toggles/Toggle_On" : "Toggles/Toggle_Off");
                image.color = Color.white;
                return;
            }
            image.color = On ? TrackOn : TrackOff;
            knob.color = On ? Color.white : KnobOff;
            UI.Place(knob.rectTransform, On ? 0.5f : 0.04f, 0.1f, On ? 0.96f : 0.5f, 0.9f);
        }

        private void OnEnable()
        {
            if (image != null) Refresh();
        }
    }

    // ---------------------------------------------------------------- 0.5 botão de compra

    public enum LegionCurrency { None, Gold, Gems }

    public class LegionBuyButton : MonoBehaviour
    {
        public enum Variant { Green, Gold, Blue }

        private Image frame;
        private Image icon;
        private TMP_Text price;
        private TMP_Text action;
        private RectTransform priceRow;
        private GameObject bought;
        private GameObject soon;
        private Button button;
        private Variant variant;

        // Linha 1: ícone da moeda + valor. Linha 2: a ação (MELHORAR, COMPRAR...). O retângulo é posicionado pelo chamador.
        public static LegionBuyButton Create(Transform parent, Variant variant, LegionCurrency currency, int amount, string actionText,
            Action onClick, string name = "Buy")
        {
            Image frame = LegionArt.Image(parent, SpriteFor(variant), true, name);
            frame.preserveAspect = false;
            LegionBuyButton buy = frame.gameObject.AddComponent<LegionBuyButton>();
            buy.frame = frame;
            buy.variant = variant;
            buy.button = MainMenuButtons.Make(frame.rectTransform, frame, onClick);

            buy.priceRow = UI.Rect(frame.transform, "Price");
            UI.Place(buy.priceRow, 0.06f, 0.44f, 0.94f, 0.94f);
            HorizontalLayoutGroup row = buy.priceRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.spacing = 6f;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;
            buy.icon = LegionArt.Image(buy.priceRow, "Icons/coin", false, "Icon");
            LayoutElement iconSize = buy.icon.gameObject.AddComponent<LayoutElement>();
            iconSize.preferredWidth = 40f;
            buy.price = UI.Label(buy.priceRow, string.Empty, Mathf.RoundToInt(28f * LegionUI.Px), Color.white, TextAnchor.MiddleLeft, true,
                "Value", LegionTextStyle.Hud);
            buy.price.textWrappingMode = TextWrappingModes.NoWrap;
            buy.price.enableAutoSizing = false;

            buy.action = UI.Label(frame.transform, actionText, Mathf.RoundToInt(22f * LegionUI.Px), Color.white, TextAnchor.MiddleCenter,
                true, "Action", LegionTextStyle.PrimaryButton);
            UI.Place(buy.action.rectTransform, 0.04f, 0.08f, 0.96f, 0.5f);
            buy.action.textWrappingMode = TextWrappingModes.NoWrap;
            buy.Set(currency, amount, actionText);
            return buy;
        }

        public static string SpriteFor(Variant variant) =>
            variant == Variant.Gold ? "Buttons/btn_gold" : variant == Variant.Blue ? "Buttons/btn_blue" : "Buttons/btn_green";

        public void Set(LegionCurrency currency, int amount, string actionText)
        {
            SetBought(false);
            icon.gameObject.SetActive(currency != LegionCurrency.None);
            if (currency != LegionCurrency.None) icon.sprite = LegionArt.Get(currency == LegionCurrency.Gems ? "Icons/gem" : "Icons/coin");
            price.text = currency == LegionCurrency.None && amount <= 0 ? string.Empty : MenuChrome.Format(amount);
            priceRow.gameObject.SetActive(price.text.Length > 0);
            action.text = actionText;
            UI.Place(action.rectTransform, 0.04f, priceRow.gameObject.activeSelf ? 0.08f : 0.12f, 0.96f, priceRow.gameObject.activeSelf ? 0.5f : 0.88f);
            action.fontSize = action.fontSizeMax = Mathf.RoundToInt((priceRow.gameObject.activeSelf ? 22f : 28f) * LegionUI.Px);
        }

        // Texto livre na linha de cima (ex.: preço em R$) sem ícone.
        public void SetPriceText(string text)
        {
            icon.gameObject.SetActive(false);
            price.text = text;
            priceRow.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public void SetVariant(Variant value)
        {
            variant = value;
            if (bought == null || !bought.activeSelf) frame.sprite = LegionArt.Get(SpriteFor(value));
        }

        public void SetInteractable(bool on)
        {
            button.interactable = on;
            frame.sprite = LegionArt.Get(on ? SpriteFor(variant) : "Buttons/btn_gray");
        }

        // "✓ COMPRADO": selo escuro, sem cara de botão desabilitado.
        public void SetBought(bool on, string text = "COMPRADO")
        {
            if (on && bought == null)
            {
                Image seal = UI.Panel(frame.transform, new Color(0.03f, 0.08f, 0.22f, 0.96f), "Bought");
                UI.Stretch(seal.rectTransform);
                Outline ring = seal.gameObject.AddComponent<Outline>();
                ring.effectColor = new Color(0.3f, 0.85f, 0.45f, 0.9f);
                ring.effectDistance = new Vector2(2f, -2f);
                seal.raycastTarget = true;
                TMP_Text label = UI.Label(seal.transform, "✓ " + text, Mathf.RoundToInt(24f * LegionUI.Px), new Color(0.45f, 1f, 0.55f),
                    TextAnchor.MiddleCenter, true, "Label", LegionTextStyle.PrimaryButton);
                UI.Stretch(label.rectTransform, 6f, 6f, 4f, 4f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                bought = seal.gameObject;
            }
            if (bought != null) bought.SetActive(on);
            if (on) frame.color = Color.clear;
            else frame.color = Color.white;
            button.interactable = !on;
        }

        // Pagamento real (R$) ainda não existe: selo EM BREVE e botão desligado.
        public void SetComingSoon(bool on)
        {
            if (on && soon == null) soon = LegionBadge.Soon(frame.transform).gameObject;
            if (soon != null) soon.SetActive(on);
            SetInteractable(!on);
        }
    }

    // ---------------------------------------------------------------- selos

    public static class LegionBadge
    {
        // Pílula "EM BREVE" no canto superior direito do pai.
        public static RectTransform Soon(Transform parent, string text = "EM BREVE")
        {
            Image pill = UI.Panel(parent, new Color(0.95f, 0.55f, 0.1f), "Soon");
            RectTransform rect = pill.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(132f, 36f);
            rect.anchoredPosition = new Vector2(6f, 0f);
            Outline ring = pill.gameObject.AddComponent<Outline>();
            ring.effectColor = new Color(0.25f, 0.1f, 0f, 0.9f);
            ring.effectDistance = new Vector2(2f, -2f);
            TMP_Text label = UI.Label(rect, text, 24, Color.white, TextAnchor.MiddleCenter, true, "Label", LegionTextStyle.PrimaryButton);
            UI.Stretch(label.rectTransform, 4f, 4f, 2f, 2f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return rect;
        }

        // Selo de texto centralizado num retângulo do grid (ex.: "LIGA DE RIVAIS · OFFLINE").
        public static TMP_Text Pill(Transform parent, Vector4 box, float x0, float y0, float x1, float y1, string text, Color fill,
            float size = 22f)
        {
            Image pill = UI.Panel(parent, fill, "Pill");
            LegionUI.Inside(pill.rectTransform, box, x0, y0, x1, y1);
            TMP_Text label = UI.Label(pill.transform, text, Mathf.RoundToInt(size * LegionUI.Px), Color.white, TextAnchor.MiddleCenter, true,
                "Label", LegionTextStyle.PrimaryButton);
            UI.Stretch(label.rectTransform, 10f, 10f, 2f, 2f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }
    }

    // ---------------------------------------------------------------- 0.6 painel e janela

    public static class LegionPanel
    {
        public static string Sprite => LegionArt.Pick("Panels/Panel_Hero", "Frames/frame_gold");

        // Painel 9-slice com borda azul #2F6AD8 e friso dourado interno, em coordenadas relativas à caixa do pai.
        public static Image Create(Transform parent, Vector4 box, float x0, float y0, float x1, float y1, string name = "Panel",
            float edgeScale = 1f)
        {
            Image panel = Create(parent, name, edgeScale);
            LegionUI.Inside(panel.rectTransform, box, x0, y0, x1, y1);
            return panel;
        }

        public static Image Create(Transform parent, string name = "Panel", float edgeScale = 1f)
        {
            Image panel = LegionArt.Image(parent, Sprite, false, name);
            panel.preserveAspect = false;
            if (panel.sprite != null && panel.sprite.border != Vector4.zero)
            {
                panel.type = Image.Type.Sliced;
                // Bordas de 72 px do sprite viram ~36 unidades; edgeScale aumenta ou reduz.
                panel.pixelsPerUnitMultiplier = 2f / Mathf.Max(0.1f, edgeScale);
            }
            return panel;
        }
    }

    public class LegionModal : MonoBehaviour
    {
        public static readonly Color Dim = new Color(4f / 255f, 12f / 255f, 36f / 255f, 0.75f);

        public RectTransform Content { get; private set; }
        public RectTransform Window { get; private set; }
        public TMP_Text Heading { get; private set; }
        public Action OnClosed;

        // Fundo #040C24 a 75 %, moldura 9-slice e o X dentro da moldura (inset 16).
        public static LegionModal Create(Transform parent, string title, float yMin = 0.24f, float yMax = 0.74f)
        {
            RectTransform root = UI.Rect(parent, "Modal_" + title);
            LegionModal modal = root.gameObject.AddComponent<LegionModal>();
            Image dim = root.gameObject.AddComponent<Image>();
            dim.color = Dim;
            Button outside = dim.gameObject.AddComponent<Button>();
            outside.transition = Selectable.Transition.None;
            outside.onClick.AddListener(modal.Close);

            Image window = LegionArt.Image(root, LegionArt.Pick("Popups/Modal_Frame", "Frames/frame_gold"), false, "Window");
            window.preserveAspect = false;
            if (window.sprite != null && window.sprite.border != Vector4.zero)
            {
                window.type = Image.Type.Sliced;
                window.pixelsPerUnitMultiplier = 1.6f;
            }
            window.raycastTarget = true; // toques dentro da janela não fecham
            UI.Place(window.rectTransform, 0.06f, yMin, 0.94f, yMax);
            window.gameObject.AddComponent<UIPopIn>();
            modal.Window = window.rectTransform;

            modal.Heading = UI.Label(window.transform, title, Mathf.RoundToInt(40f * LegionUI.Px), Color.white, TextAnchor.MiddleCenter, true,
                "Title", LegionTextStyle.Title);
            modal.Heading.rectTransform.anchorMin = new Vector2(0.14f, 1f);
            modal.Heading.rectTransform.anchorMax = new Vector2(0.86f, 1f);
            modal.Heading.rectTransform.pivot = new Vector2(0.5f, 1f);
            modal.Heading.rectTransform.sizeDelta = new Vector2(0f, 96f);
            modal.Heading.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            modal.Heading.textWrappingMode = TextWrappingModes.NoWrap;

            Image x = LegionArt.Image(window.transform, "Buttons/btn_close", false, "Close");
            RectTransform xr = x.rectTransform;
            xr.anchorMin = xr.anchorMax = xr.pivot = new Vector2(1f, 1f);
            xr.sizeDelta = new Vector2(84f, 84f);
            xr.anchoredPosition = new Vector2(-16f * LegionUI.Px - 18f, -16f * LegionUI.Px - 18f);
            MainMenuButtons.Make(xr, x, modal.Close);

            modal.Content = UI.Rect(window.transform, "Content");
            UI.Stretch(modal.Content, 48f, 48f, 150f, 48f);
            root.gameObject.SetActive(false);
            return modal;
        }

        public void Open()
        {
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            if (!gameObject.activeSelf) return;
            GameAudio.Play(SoundId.UiBack);
            gameObject.SetActive(false);
            OnClosed?.Invoke();
        }
    }

    // ---------------------------------------------------------------- números curtos

    public static class LegionFormat
    {
        // 2500 -> "2.5K", 1200000 -> "1.2M" (carteira do HUD).
        public static string Short(int value)
        {
            if (value >= 1000000) return Trim(value / 1000000f) + "M";
            if (value >= 10000) return Mathf.FloorToInt(value / 1000f) + "K";
            if (value >= 1000) return Trim(value / 1000f) + "K";
            return value.ToString();
        }

        private static string Trim(float v) => (Mathf.Floor(v * 10f) / 10f).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
    }
}
