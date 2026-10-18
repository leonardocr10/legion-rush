using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace LegionRush
{
    // Tela da interface. Construída uma única vez (Build) e reaproveitada; Refresh atualiza os dados ao mostrar.
    public abstract class UIScreen
    {
        public RectTransform Root { get; private set; }
        public CanvasGroup Group { get; private set; }
        protected UIRoot Ui { get; private set; }
        public virtual bool BlocksGame => true;
        public virtual bool AllowBack => true;

        internal void Create(UIRoot ui, Transform layer)
        {
            Ui = ui;
            Root = UI.Rect(layer, GetType().Name);
            Group = Root.gameObject.AddComponent<CanvasGroup>();
            Build(Root);
        }

        protected abstract void Build(RectTransform root);
        public virtual void Refresh() { }
        public virtual void OnBack() => Ui.Pop();
    }

    // Raiz da interface: canvas, área segura, pilha de navegação, transições, diálogos de confirmação e notificações.
    public class UIRoot : MonoBehaviour
    {
        private const float TransitionSeconds = 0.18f;

        public static UIRoot Instance { get; private set; }

        public GameBootstrap Game { get; private set; }
        public bool IsBlocking => stack.Count > 0 && stack[stack.Count - 1].BlocksGame || modalOpen;
        public UIScreen Top => stack.Count > 0 ? stack[stack.Count - 1] : null;

        private readonly List<UIScreen> stack = new List<UIScreen>();
        private readonly Dictionary<Type, UIScreen> cache = new Dictionary<Type, UIScreen>();
        private readonly Queue<string> toasts = new Queue<string>();
        private Canvas canvas;
        private CanvasScaler scaler;
        private RectTransform safeRoot;
        private RectTransform screenLayer;
        private RectTransform modalLayer;
        private RectTransform toastLayer;
        private TMP_Text toastText;
        private CanvasGroup toastGroup;
        private float toastTimer;
        private UIScreen animating;
        private float animTime;
        private bool modalOpen;
        private GameObject modal;
        private Rect lastSafeArea;
        private Vector2Int lastScreen;
        private float resultDelay = -1f;

        public void Initialize(GameBootstrap game)
        {
            Instance = this;
            Game = game;
            Game.LevelBuilt += HandleLevelBuilt;
            EnsureEventSystem();

            var canvasGo = new GameObject("LR_UICanvas", typeof(RectTransform));
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasGo.AddComponent<GraphicRaycaster>();

            safeRoot = UI.Rect(canvasGo.transform, "SafeArea");
            screenLayer = UI.Rect(safeRoot, "Screens");
            modalLayer = UI.Rect(safeRoot, "Modals");
            toastLayer = UI.Rect(safeRoot, "Toasts");
            BuildToast();
            ApplySafeArea();
        }

        public T Get<T>() where T : UIScreen, new()
        {
            if (cache.TryGetValue(typeof(T), out UIScreen existing)) return (T)existing;
            var screen = new T();
            screen.Create(this, screenLayer);
            screen.Root.gameObject.SetActive(false);
            cache[typeof(T)] = screen;
            return screen;
        }

        public T Push<T>() where T : UIScreen, new()
        {
            T screen = Get<T>();
            if (Top != null && Top != (UIScreen)screen) Top.Root.gameObject.SetActive(false);
            stack.Remove(screen);
            stack.Add(screen);
            Show(screen);
            return screen;
        }

        public T Replace<T>() where T : UIScreen, new()
        {
            CloseAll();
            return Push<T>();
        }

        public void Pop()
        {
            if (stack.Count == 0) return;
            UIScreen top = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            top.Root.gameObject.SetActive(false);
            if (Top != null) Show(Top);
            else if (Game != null && Game.CurrentLevel != null && Game.CurrentLevel.Current == LevelManager.State.Ready) Push<MainMenuScreen>();
            GameAudio.Play(SoundId.UiBack);
        }

        // Ao fim da partida (vitória ou derrota), mostra o resultado após a celebração.
        private void HandleLevelBuilt(LevelManager level)
        {
            resultDelay = -1f;
            level.StateChanged += state =>
            {
                if (state == LevelManager.State.Victory || state == LevelManager.State.Defeat) resultDelay = 1.6f;
            };
        }

        public void CloseAll()
        {
            foreach (UIScreen screen in stack) screen.Root.gameObject.SetActive(false);
            stack.Clear();
        }

        public void RefreshTop()
        {
            if (Top != null) Top.Refresh();
        }

        private void Show(UIScreen screen)
        {
            screen.Root.gameObject.SetActive(true);
            screen.Root.SetAsLastSibling();
            screen.Refresh();
            animating = screen;
            animTime = 0f;
            screen.Group.alpha = 0f;
        }

        // ---------- Diálogo de confirmação ----------

        public void Confirm(string title, string message, string confirmLabel, Action onConfirm, Action onCancel = null)
        {
            CloseModal();
            modalOpen = true;
            modal = UI.Rect(modalLayer, "Confirm").gameObject;
            Image dim = modal.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);

            Image panel = UI.Panel(modal.transform, UIStyle.Panel, "Dialog");
            UI.Place(panel.rectTransform, 0.06f, 0.36f, 0.94f, 0.64f);
            TMP_Text heading = PopupHeader(panel.rectTransform, title);
            TMP_Text body = UI.Label(panel.transform, message, 40, UIStyle.Text);
            UI.Place(body.rectTransform, 0.06f, 0.36f, 0.94f, 0.7f);

            Button cancel = UI.Button(panel.transform, "CANCELAR", UIStyle.Gray, () =>
            {
                CloseModal();
                onCancel?.Invoke();
            }, 40);
            UI.Place((RectTransform)cancel.transform, 0.05f, 0.06f, 0.48f, 0.3f);
            Button ok = UI.Button(panel.transform, confirmLabel, UIStyle.Green, () =>
            {
                CloseModal();
                onConfirm?.Invoke();
            }, 40);
            UI.Place((RectTransform)ok.transform, 0.52f, 0.06f, 0.95f, 0.3f);
        }

        public void Info(string title, string message)
        {
            CloseModal();
            modalOpen = true;
            modal = UI.Rect(modalLayer, "Info").gameObject;
            Image dim = modal.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            Image panel = UI.Panel(modal.transform, UIStyle.Panel, "Dialog");
            UI.Place(panel.rectTransform, 0.06f, 0.3f, 0.94f, 0.7f);
            TMP_Text heading = PopupHeader(panel.rectTransform, title);
            TMP_Text body = UI.Label(panel.transform, message, 42, UIStyle.Text);
            UI.Place(body.rectTransform, 0.06f, 0.26f, 0.94f, 0.78f);
            Button ok = UI.Button(panel.transform, "OK", UIStyle.Blue, CloseModal, 44);
            UI.Place((RectTransform)ok.transform, 0.25f, 0.05f, 0.75f, 0.22f);
        }

        // LegionPopup: coroa e faixa do pacote sobre a moldura, com o título ao vivo na faixa; entra com escala.
        private static TMP_Text PopupHeader(RectTransform panel, string title)
        {
            panel.gameObject.AddComponent<UIPopIn>();
            Image header = LegionArt.Image(panel, "Popups/popup_header", false, "Header");
            header.rectTransform.anchorMin = new Vector2(0.02f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.98f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 0.35f);
            header.rectTransform.sizeDelta = new Vector2(0f, 250f);
            header.rectTransform.anchoredPosition = Vector2.zero;
            TMP_Text heading = UI.Label(header.transform, title, 58, Color.white, TextAnchor.MiddleCenter, true, "Title", LegionTextStyle.Title);
            UI.Place(heading.rectTransform, 0.27f, 0.12f, 0.74f, 0.58f);
            heading.textWrappingMode = TextWrappingModes.NoWrap;
            return heading;
        }

        public void CloseModal()
        {
            if (modal != null) Destroy(modal);
            modal = null;
            modalOpen = false;
        }

        // ---------- Notificações ----------

        public void Toast(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (toasts.Count < 4) toasts.Enqueue(message);
        }

        private void BuildToast()
        {
            Image panel = UI.Panel(toastLayer, new Color(0.05f, 0.08f, 0.2f, 0.92f), "Toast");
            UI.Place(panel.rectTransform, 0.08f, 0.72f, 0.92f, 0.79f);
            toastGroup = panel.gameObject.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            toastGroup.alpha = 0f;
            toastText = UI.Label(panel.transform, string.Empty, 44, UIStyle.Gold);
            UI.Stretch(toastText.rectTransform, 20f, 20f, 8f, 8f);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (resultDelay >= 0f)
            {
                resultDelay -= dt;
                if (resultDelay < 0f) Replace<ResultScreen>();
            }
            if (animating != null)
            {
                animTime += dt;
                float t = Mathf.Clamp01(animTime / TransitionSeconds);
                animating.Group.alpha = t;
                animating.Root.localScale = Vector3.one * Mathf.Lerp(0.97f, 1f, t);
                if (t >= 1f) animating = null;
            }

            if (toastTimer > 0f)
            {
                toastTimer -= dt;
                toastGroup.alpha = Mathf.Clamp01(Mathf.Min(toastTimer / 0.3f, (2.4f - toastTimer) / 0.2f));
            }
            else if (toasts.Count > 0)
            {
                toastText.text = toasts.Dequeue();
                toastTimer = 2.4f;
            }
            else
            {
                toastGroup.alpha = 0f;
            }

            if (Screen.safeArea != lastSafeArea || Screen.width != lastScreen.x || Screen.height != lastScreen.y) ApplySafeArea();
            if (BackPressed()) HandleBack();
        }

        private void HandleBack()
        {
            if (modalOpen)
            {
                CloseModal();
                return;
            }
            if (Top != null && Top.AllowBack) Top.OnBack();
        }

        private void ApplySafeArea()
        {
            lastSafeArea = Screen.safeArea;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            safeRoot.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
            safeRoot.offsetMin = Vector2.zero;
            safeRoot.offsetMax = Vector2.zero;
            float aspect = Screen.width / (float)Screen.height;
            scaler.matchWidthOrHeight = aspect > 0.7f ? 1f : 0f;
        }

        private static bool BackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            DontDestroyOnLoad(go);
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (Game != null) Game.LevelBuilt -= HandleLevelBuilt;
        }
    }

    // Entrada de popup: cresce de 85% com leve ultrapassagem.
    public class UIPopIn : MonoBehaviour
    {
        private float time;

        private void OnEnable()
        {
            time = 0f;
            transform.localScale = Vector3.one * 0.85f;
        }

        private void Update()
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / 0.25f);
            float x = t - 1f;
            float eased = 1f + 2.70158f * x * x * x + 1.70158f * x * x;
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, eased);
            if (t >= 1f) enabled = false;
        }
    }

    // Leve "apertar" visual ao tocar em botões.
    public class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private float target = 1f;

        public void OnPointerDown(PointerEventData eventData) => target = 0.94f;
        public void OnPointerUp(PointerEventData eventData) => target = 1f;
        public void OnPointerExit(PointerEventData eventData) => target = 1f;

        private void OnDisable()
        {
            target = 1f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            float current = transform.localScale.x;
            if (Mathf.Abs(current - target) < 0.001f) return;
            float next = Mathf.MoveTowards(current, target, Time.unscaledDeltaTime * 2f);
            transform.localScale = new Vector3(next, next, 1f);
        }
    }

    // Vibração curta no Android quando habilitada nas configurações.
    public static class Haptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject vibrator;
        private static int sdk = -1;
#endif

        public static bool Enabled => !SaveManager.IsLoaded || SaveManager.Current.settings.haptics;

        public static void Tap() => Vibrate(12, 60);
        public static void Impact() => Vibrate(35, 180);

        private static void Vibrate(long milliseconds, int amplitude)
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (sdk < 0)
                {
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION")) sdk = version.GetStatic<int>("SDK_INT");
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                if (vibrator == null) return;
                if (sdk >= 26)
                {
                    using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
                        vibrator.Call("vibrate", effect);
                }
                else if (milliseconds > 30)
                {
                    Handheld.Vibrate();
                }
            }
            catch (Exception exception)
            {
                GameLog.Warn("Vibração indisponível: " + exception.Message);
                sdk = 0;
                vibrator = null;
            }
#endif
        }
    }
}
