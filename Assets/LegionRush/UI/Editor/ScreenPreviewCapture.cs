using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush.EditorTools
{
    // Prévias em PNG das telas de vitória, derrota e pausa, sem entrar no Play: monta cada tela num canvas e renderiza.
    // Os textos dinâmicos recebem valores de exemplo (sem save nem partida carregados).
    // Lote: Unity.exe -batchmode -projectPath . -executeMethod LegionRush.EditorTools.ScreenPreviewCapture.CaptureBatch -captureDir <pasta>
    public static class ScreenPreviewCapture
    {
        private static readonly (string name, int w, int h)[] Sizes =
        {
            ("ref_1080x1920", 1080, 1920), ("phone_1080x2400", 1080, 2400)
        };

        private static readonly (string name, Func<UIScreen> create, (string field, string text)[] samples, bool gameBehind)[] Screens =
        {
            ("victory", () => new VictoryScreen(),
                new[] { ("goldValue", "+1.250"), ("gemsValue", "+50"), ("scoreValue", "1.674"), ("scoreLabel", "Novo recorde!") }, false),
            ("defeat", () => new DefeatScreen(),
                new[] { ("goldValue", "+250"), ("scoreValue", "1.674"), ("scoreLabel", "Novo recorde!") }, false),
            ("pause", () => new PauseScreen(),
                new[] { ("missionTitle", "Vença 5 batalhas"), ("missionCount", "3/5") }, true)
        };

        [MenuItem("Legion Rush/UI/Capturar prévias de telas (PNG)")]
        public static void CaptureMenu()
        {
            string dir = Path.GetFullPath("Builds/ScreenPreview");
            Capture(dir);
            EditorUtility.RevealInFinder(dir);
        }

        public static void CaptureBatch()
        {
            int code = 0;
            try
            {
                AssetDatabase.Refresh();
                Capture(Argument("-captureDir") ?? Path.GetFullPath("Builds/ScreenPreview"));
            }
            catch (Exception exception)
            {
                Debug.LogError("[ScreenPreview] Captura falhou: " + exception);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        private static void Capture(string dir)
        {
            Directory.CreateDirectory(dir);
            MethodInfo create = typeof(UIScreen).GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var size in Sizes)
            foreach (var entry in Screens)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Camera cam = new GameObject("Camera").AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.orthographic = true;
                var target = new RenderTexture(size.w, size.h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = target;

                var canvasGo = new GameObject("Canvas", typeof(RectTransform)) { layer = 5 };
                Canvas canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 10f;
                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f;

                // Telas sobrepostas à partida (pausa) ganham um cenário de exemplo por trás.
                if (entry.gameBehind)
                {
                    Image behind = LegionArt.Image(canvasGo.transform, "Backgrounds/bg_frozen_a", false, "GameStandIn");
                    behind.preserveAspect = false;
                    UI.Stretch(behind.rectTransform);
                }

                UIScreen screen = entry.create();
                create.Invoke(screen, new object[] { null, canvasGo.transform });
                foreach (var (field, text) in entry.samples) Fill(screen, field, text);
                foreach (UISlideIn slide in canvasGo.GetComponentsInChildren<UISlideIn>(true)) UnityEngine.Object.DestroyImmediate(slide);
                foreach (CanvasGroup group in canvasGo.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1f;
                foreach (Image fill in canvasGo.GetComponentsInChildren<Image>(true))
                {
                    if (fill.name == "Fill" && fill.transform.parent.name == "Progress") LegionUI.SetProgress(fill, 0.6f);
                }

                for (int i = 0; i < 3; i++) Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = target;
                var texture = new Texture2D(size.w, size.h, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, size.w, size.h), 0, 0);
                texture.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(dir, $"{entry.name}_{size.name}.png"), texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                cam.targetTexture = null;
                target.Release();
                Debug.Log($"[ScreenPreview] Captura {entry.name} {size.name} salva em {dir}");
            }
        }

        private static void Fill(UIScreen screen, string field, string text)
        {
            FieldInfo info = screen.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (info?.GetValue(screen) is TMP_Text label) label.text = text;
        }

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }
    }
}
