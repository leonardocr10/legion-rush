using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace LegionRush
{
    // Tela de abertura (cena SplashScreen, índice 0 da build). Carrega a cena Main de forma assíncrona com progresso
    // real, espera o GameBootstrap montar a fase e só então libera "TOQUE PARA COMEÇAR". Sobrevive à troca de cena
    // (DontDestroyOnLoad) para cobrir a montagem da fase e some com fade quando o jogador toca.
    // Aberta direto na cena Main (editor), não carrega nada: esconde a barra e mostra o botão.
    [DisallowMultipleComponent]
    public class SplashScreenController : MonoBehaviour
    {
        public enum LoadState { Initializing, LoadingResources, PreparingGame, Ready, Error }

        // Peso de cada etapa na barra. As duas etapas são operações reais: leitura da cena e montagem da fase.
        private const float LoadingWeight = 0.8f;

        /// <summary>Verdadeiro enquanto a tela de abertura cobre o jogo (o GameBootstrap não mostra a splash antiga).</summary>
        public static bool IsShowing { get; private set; }

        /// <summary>Disparado quando o jogador toca e a tela começa a sumir.</summary>
        public static event Action Finished;

        [Header("Fluxo")]
        [SerializeField, Tooltip("Cena do jogo (menu principal + fase). Precisa estar na lista de cenas da build.")]
        private string mainSceneName = "Main";
        [SerializeField, Tooltip("Tempo máximo para o GameBootstrap montar a fase depois que a cena ativa.")]
        private float prepareTimeout = 25f;
        [SerializeField] private float fadeOutSeconds = 0.45f;

        [Header("Referências")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Button startButton;
        [SerializeField] private CanvasGroup startGroup;
        [SerializeField] private RectTransform startArt;
        [SerializeField] private TMP_Text startLabel;
        [SerializeField] private CanvasGroup loadingGroup;
        [SerializeField] private Image loadingFill;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text versionLabel;
        [SerializeField, Tooltip("Animações de entrada; o botão só aparece quando todas terminam.")]
        private SplashEntrance[] entrances = new SplashEntrance[0];

        public LoadState State { get; private set; } = LoadState.Initializing;
        public float Progress { get; private set; }
        public event Action<LoadState> StateChanged;

        private const string StartText = "TOQUE PARA COMEÇAR";
        private const string RetryText = "TENTAR NOVAMENTE";

        private bool accepted;
        private float shownProgress;
        private Coroutine flow;

        private void Awake()
        {
            IsShowing = true;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
            EnsureEventSystem();
            if (versionLabel != null) versionLabel.text = "v" + Application.version;
            if (startButton != null) startButton.onClick.AddListener(OnStartPressed);
            SetButtonVisible(false, true);
            SetLoadingVisible(true, true);
            SetProgressImmediate(0f);
        }

        private void Start() => flow = StartCoroutine(Run());

        private void OnDestroy()
        {
            if (IsShowing) IsShowing = false;
        }

        // ---------- Carregamento ----------

        private IEnumerator Run()
        {
            accepted = false;
            SetState(LoadState.Initializing);
            SetLoadingVisible(true, false);
            SetButtonVisible(false, false);
            yield return null;

            if (FindGame() == null)
            {
                if (!Application.CanStreamedLevelBeLoaded(mainSceneName))
                {
                    Fail($"Cena '{mainSceneName}' não está na build.");
                    yield break;
                }

                AsyncOperation load = SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Single);
                if (load == null)
                {
                    Fail($"Não foi possível carregar '{mainSceneName}'.");
                    yield break;
                }
                load.allowSceneActivation = false;
                SetState(LoadState.LoadingResources);
                // Até 0.9 o Unity está lendo a cena e seus recursos; a ativação vem depois.
                while (load.progress < 0.9f)
                {
                    SetProgress(load.progress / 0.9f * LoadingWeight);
                    yield return null;
                }
                SetProgress(LoadingWeight);

                SetState(LoadState.PreparingGame);
                yield return null; // deixa a barra e o texto aparecerem antes do quadro pesado da ativação
                load.allowSceneActivation = true;
                while (!load.isDone) yield return null;
            }
            else
            {
                // Já estamos na cena do jogo (aberta direto no editor): nada a carregar.
                SetLoadingVisible(false, true);
            }

            float deadline = Time.realtimeSinceStartup + prepareTimeout;
            GameBootstrap game;
            while ((game = FindGame()) == null || game.CurrentLevel == null)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Fail("O jogo não terminou de montar a fase.");
                    yield break;
                }
                yield return null;
            }
            SetProgress(1f);

            while (!EntrancesDone()) yield return null;
            while (shownProgress < 0.999f && loadingGroup != null && loadingGroup.alpha > 0f) yield return null;

            SetState(LoadState.Ready);
            SetText(statusLabel, "");
            SetText(startLabel, StartText);
            SetLoadingVisible(false, false);
            SetButtonVisible(true, false);
            flow = null;
        }

        private void Fail(string reason)
        {
            GameLog.Error("[Splash] " + reason);
            SetState(LoadState.Error);
            SetText(startLabel, RetryText);
            SetButtonVisible(true, false);
            flow = null;
        }

        private static GameBootstrap FindGame() => FindAnyObjectByType<GameBootstrap>();

        private bool EntrancesDone()
        {
            foreach (SplashEntrance entrance in entrances)
                if (entrance != null && entrance.isActiveAndEnabled && !entrance.Done) return false;
            return true;
        }

        // ---------- Toque ----------

        private void OnStartPressed()
        {
            if (accepted || flow != null) return; // trava toques repetidos e toques durante o carregamento
            if (State == LoadState.Error)
            {
                SetButtonVisible(false, false);
                flow = StartCoroutine(Run());
                return;
            }
            if (State != LoadState.Ready) return;

            GameBootstrap game = FindGame();
            if (game == null || game.CurrentLevel == null)
            {
                Fail("Dados do jogo indisponíveis ao tocar.");
                return;
            }
            accepted = true;
            startButton.interactable = false;
            GameAudio.Play(SoundId.UiConfirm);
            Haptics.Tap();
            StartCoroutine(Leave());
        }

        private IEnumerator Leave()
        {
            // pressionamento: afunda e volta
            for (float t = 0f; t < 0.16f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.16f;
                float s = 1f - 0.07f * Mathf.Sin(k * Mathf.PI);
                if (startArt != null) startArt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (startArt != null) startArt.localScale = Vector3.one;

            IsShowing = false;
            Finished?.Invoke();

            float start = rootGroup != null ? rootGroup.alpha : 1f;
            if (rootGroup != null) rootGroup.blocksRaycasts = false;
            for (float t = 0f; t < fadeOutSeconds; t += Time.unscaledDeltaTime)
            {
                if (rootGroup != null) rootGroup.alpha = Mathf.Lerp(start, 0f, t / fadeOutSeconds);
                yield return null;
            }
            Destroy(gameObject);
            Resources.UnloadUnusedAssets(); // texturas da abertura saem da memória
        }

        // ---------- Visual ----------

        private void Update()
        {
            // a barra persegue o progresso real (só suaviza o movimento, nunca passa do valor real)
            if (Mathf.Abs(shownProgress - Progress) > 0.0005f)
            {
                shownProgress = Mathf.MoveTowards(shownProgress, Progress, Time.unscaledDeltaTime * 1.6f);
                if (loadingFill != null) loadingFill.fillAmount = shownProgress;
            }
        }

        private void SetState(LoadState state)
        {
            State = state;
            SetText(statusLabel, state switch
            {
                LoadState.Initializing => "Inicializando...",
                LoadState.LoadingResources => "Carregando recursos...",
                LoadState.PreparingGame => "Preparando o jogo...",
                LoadState.Ready => "Pronto para começar",
                _ => "Erro de carregamento"
            });
            if (statusLabel != null)
                statusLabel.color = state == LoadState.Error ? new Color(1f, 0.55f, 0.5f) : Color.white;
            GameLog.Info("[Splash] " + state);
            StateChanged?.Invoke(state);
        }

        private void SetProgress(float value) => Progress = Mathf.Clamp01(Mathf.Max(Progress, value));

        private void SetProgressImmediate(float value)
        {
            Progress = shownProgress = Mathf.Clamp01(value);
            if (loadingFill != null) loadingFill.fillAmount = shownProgress;
        }

        private void SetButtonVisible(bool visible, bool instant)
        {
            if (startButton != null) startButton.interactable = visible;
            if (startGroup == null) return;
            startGroup.blocksRaycasts = visible;
            StartFade(startGroup, visible ? 1f : 0f, instant ? 0f : 0.35f);
        }

        private void SetLoadingVisible(bool visible, bool instant)
        {
            if (loadingGroup == null) return;
            if (visible && State != LoadState.Error) SetProgressImmediate(0f);
            StartFade(loadingGroup, visible ? 1f : 0f, instant ? 0f : 0.3f);
        }

        private void StartFade(CanvasGroup group, float target, float seconds)
        {
            if (seconds <= 0f || !isActiveAndEnabled)
            {
                group.alpha = target;
                return;
            }
            StartCoroutine(Fade(group, target, seconds));
        }

        private static IEnumerator Fade(CanvasGroup group, float target, float seconds)
        {
            float from = group.alpha;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, target, t / seconds);
                yield return null;
            }
            group.alpha = target;
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null && label.text != value) label.text = value;
        }

        // Mesmo EventSystem que o UIRoot criaria: fica vivo depois que a abertura some.
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

#if UNITY_EDITOR
        // Usado pela captura de prévia no editor: mostra o estado final sem rodar o carregamento.
        public void PreviewState(LoadState state, float progress)
        {
            State = state;
            bool ready = state == LoadState.Ready || state == LoadState.Error;
            if (startGroup != null) startGroup.alpha = ready ? 1f : 0f;
            if (loadingGroup != null) loadingGroup.alpha = ready ? 0f : 1f;
            if (loadingFill != null) loadingFill.fillAmount = progress;
            if (versionLabel != null) versionLabel.text = "v" + Application.version;
            SetText(startLabel, state == LoadState.Error ? RetryText : StartText);
            SetText(statusLabel, state switch
            {
                LoadState.LoadingResources => "Carregando recursos...",
                LoadState.PreparingGame => "Preparando o jogo...",
                LoadState.Error => "Erro de carregamento",
                _ => ""
            });
        }
#endif
    }
}
