using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush.EditorTools
{
    // Teste real do fluxo em Play Mode (também em batch): abre a cena SplashScreen, espera o carregamento da Main,
    // confere os estados, toca duas vezes seguidas em "TOQUE PARA COMEÇAR" (o segundo toque deve ser ignorado),
    // espera o menu principal assumir e salva capturas da abertura pronta e do menu.
    // Batch: Unity.exe -batchmode -projectPath . -executeMethod LegionRush.EditorTools.SplashPlayModeCheck.RunBatch -captureDir <pasta>
    [InitializeOnLoad]
    public static class SplashPlayModeCheck
    {
        private const string Key = "LR_SplashCheck";
        private const string DirKey = "LR_SplashCheckDir";
        private const string BatchKey = "LR_SplashCheckBatch";
        private const float TimeoutSeconds = 120f;

        private static double startTime;
        private static int step;
        private static double stepTime;
        private static SplashScreenController.LoadState lastState = (SplashScreenController.LoadState)(-1);
        private static string log = "";
        private static int clicks;

        static SplashPlayModeCheck()
        {
            if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
        }

        [MenuItem("Legion Rush/UI/Splash Screen/Run Play Mode Check")]
        public static void RunMenu() => Begin(Path.GetFullPath("Builds/SplashPreview"), false);

        public static void RunBatch() => Begin(GetArgument("-captureDir") ?? Path.GetFullPath("Builds/SplashPreview"), true);

        private static void Begin(string dir, bool batch)
        {
            Directory.CreateDirectory(dir);
            SessionState.SetBool(Key, true);
            SessionState.SetString(DirKey, dir);
            SessionState.SetBool(BatchKey, batch);
            EditorSceneManager.OpenScene(SplashScreenBuilder.ScenePath, OpenSceneMode.Single);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (startTime <= 0) startTime = EditorApplication.timeSinceStartup;
            double now = EditorApplication.timeSinceStartup;
            if (now - startTime > TimeoutSeconds)
            {
                Finish(false, $"tempo esgotado no passo {step}");
                return;
            }

            SplashScreenController splash = UnityEngine.Object.FindAnyObjectByType<SplashScreenController>();
            if (splash != null && splash.State != lastState)
            {
                lastState = splash.State;
                Note($"estado {splash.State} (progresso {splash.Progress:0.00}) em {now - startTime:0.0}s");
            }

            switch (step)
            {
                case 0: // espera o botão liberar
                    if (splash == null) break;
                    if (splash.State == SplashScreenController.LoadState.Error)
                    {
                        Finish(false, "a abertura entrou em erro");
                        return;
                    }
                    if (splash.State != SplashScreenController.LoadState.Ready)
                    {
                        stepTime = now;
                        break;
                    }
                    if (now - stepTime < 0.8) break; // espera o botão terminar de aparecer
                    Note($"menu por baixo: {UIRoot.Instance?.Top?.GetType().Name ?? "nenhum"}");
                    Shot(splash.GetComponent<Canvas>(), "playmode_splash_ready");
                    Button button = splash.GetComponentInChildren<Button>();
                    button.onClick.AddListener(() => clicks++);
                    button.onClick.Invoke();
                    button.onClick.Invoke(); // toque duplo: o controlador deve ignorar o segundo
                    Note($"toques enviados: {clicks}, botão interativo depois do toque: {button.interactable}");
                    step = 1;
                    stepTime = now;
                    break;
                case 1: // espera a abertura sumir e o menu assumir
                    if (splash != null) break;
                    UIScreen top = UIRoot.Instance != null ? UIRoot.Instance.Top : null;
                    Note($"abertura destruída em {now - stepTime:0.00}s; tela atual: {top?.GetType().Name ?? "nenhuma"}; IsShowing={SplashScreenController.IsShowing}");
                    if (top == null || top.GetType().Name != "MainMenuScreen")
                    {
                        Finish(false, "o menu principal não ficou no topo");
                        return;
                    }
                    step = 2;
                    stepTime = now;
                    break;
                case 2: // deixa as entradas do menu tocarem e captura
                    if (now - stepTime < 1.5) break;
                    GameObject uiCanvas = GameObject.Find("LR_UICanvas");
                    Shot(uiCanvas != null ? uiCanvas.GetComponent<Canvas>() : null, "playmode_main_menu");
                    Finish(true, "fluxo abertura → menu ok");
                    return;
            }
        }

        // Canvas em overlay não aparece em câmera; troca por um instante para ScreenSpaceCamera numa RT 1080x1920.
        private static void Shot(Canvas canvas, string name)
        {
            const int w = 1080, h = 1920;
            Camera source = Camera.main;
            var go = new GameObject("CheckCamera");
            Camera cam = go.AddComponent<Camera>();
            if (source != null) cam.CopyFrom(source);
            else
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            RenderMode mode = RenderMode.ScreenSpaceOverlay;
            if (canvas != null)
            {
                mode = canvas.renderMode;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = Mathf.Max(cam.nearClipPlane + 0.5f, 1f);
            }
            Canvas.ForceUpdateCanvases();
            cam.Render();
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            string path = Path.Combine(SessionState.GetString(DirKey, "Builds/SplashPreview"), name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            if (canvas != null)
            {
                canvas.renderMode = mode;
                canvas.worldCamera = null;
            }
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(go);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            Note("captura " + path);
        }

        private static void Note(string message)
        {
            log += message + "\n";
            Debug.Log("[LegionRush] SplashCheck: " + message);
        }

        private static void Finish(bool ok, string message)
        {
            Note((ok ? "PASSOU: " : "FALHOU: ") + message);
            EditorApplication.update -= Tick;
            bool batch = SessionState.GetBool(BatchKey, false);
            string dir = SessionState.GetString(DirKey, "Builds/SplashPreview");
            File.WriteAllText(Path.Combine(dir, "playmode_check.txt"), log);
            SessionState.EraseBool(Key);
            EditorApplication.isPlaying = false;
            if (batch) EditorApplication.Exit(ok ? 0 : 1);
        }

        private static string GetArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }
    }
}
