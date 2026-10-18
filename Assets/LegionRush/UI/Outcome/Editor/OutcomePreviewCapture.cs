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
    // Prévia da tela de derrota em PNG, sem entrar no Play: monta a DefeatScreen num canvas e renderiza.
    // Lote: Unity.exe -batchmode -projectPath . -executeMethod LegionRush.EditorTools.DefeatPreviewCapture.CaptureBatch -captureDir <pasta>
    public static class DefeatPreviewCapture
    {
        private static readonly (string name, int w, int h)[] Sizes =
        {
            ("ref_1080x1920", 1080, 1920), ("phone_1080x2400", 1080, 2400)
        };

        [MenuItem("Legion Rush/UI/Derrota/Capturar prévias (PNG)")]
        public static void CaptureMenu()
        {
            string dir = Path.GetFullPath("Builds/DefeatPreview");
            Capture(dir);
            EditorUtility.RevealInFinder(dir);
        }

        public static void CaptureBatch()
        {
            int code = 0;
            try
            {
                AssetDatabase.Refresh();
                Capture(Argument("-captureDir") ?? Path.GetFullPath("Builds/DefeatPreview"));
            }
            catch (Exception exception)
            {
                Debug.LogError("[Defeat] Captura falhou: " + exception);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        private static void Capture(string dir)
        {
            Directory.CreateDirectory(dir);
            MethodInfo create = typeof(UIScreen).GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var size in Sizes)
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

                var screen = new DefeatScreen();
                create.Invoke(screen, new object[] { null, canvasGo.transform });
                Fill(screen, "goldValue", "+250");
                Fill(screen, "scoreValue", "1674");
                Fill(screen, "scoreLabel", "Novo recorde!");
                foreach (UISlideIn slide in canvasGo.GetComponentsInChildren<UISlideIn>(true)) UnityEngine.Object.DestroyImmediate(slide);
                foreach (CanvasGroup group in canvasGo.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1f;

                for (int i = 0; i < 3; i++) Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = target;
                var texture = new Texture2D(size.w, size.h, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, size.w, size.h), 0, 0);
                texture.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(dir, $"defeat_{size.name}.png"), texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                cam.targetTexture = null;
                target.Release();
                Debug.Log($"[Defeat] Captura {size.name} salva em {dir}");
            }
        }

        private static void Fill(DefeatScreen screen, string field, string text)
        {
            FieldInfo info = typeof(DefeatScreen).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
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
