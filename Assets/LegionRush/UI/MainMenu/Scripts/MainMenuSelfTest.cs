using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Autoteste da tela inicial no jogo real. Só liga com pedido explícito:
    //   desktop: LegionRush.exe -lrMenuSelfTest <pasta>      navegador (WebGL): index.html?lrMenuSelfTest
    // Passa pela abertura, captura o menu, toca cada botão e confere a tela/janela aberta.
    // Relatório em <pasta>/report.txt (desktop) ou no console do navegador ("[MenuSelfTest]").
    public class MainMenuSelfTest : MonoBehaviour
    {
        private string dir;
        private readonly StringBuilder report = new StringBuilder();
        private int failures;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string target = null;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                if (Application.absoluteURL.Contains("lrMenuSelfTest")) target = string.Empty;
            }
            else
            {
                string[] args = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(args, "-lrMenuSelfTest");
                if (i >= 0 && i + 1 < args.Length) target = args[i + 1];
            }
            if (target == null) return;
            var go = new GameObject("MainMenuSelfTest");
            DontDestroyOnLoad(go);
            go.AddComponent<MainMenuSelfTest>().dir = target;
        }

        private IEnumerator Start()
        {
            if (ToDisk) Directory.CreateDirectory(dir);
            Log($"Tela {Screen.width}x{Screen.height}, área segura {Screen.safeArea}");
            float deadline = Time.realtimeSinceStartup + 90f;
            while (!MenuOnTop() && Time.realtimeSinceStartup < deadline)
            {
                PressSplashStart();
                yield return new WaitForSecondsRealtime(0.5f);
            }
            if (!MenuOnTop())
            {
                Fail("menu inicial não apareceu em 90 s");
                Finish();
                yield break;
            }
            yield return new WaitForSecondsRealtime(2.5f); // entradas e logo terminam
            yield return Capture("menu.png");

            UIRoot ui = UIRoot.Instance;
            yield return Check("Perfil", "Profile", () => ui.Top is HeroScreen);
            yield return Check("Moedas +", "Gold/Add", () => ui.Top is ShopScreen);
            yield return Check("Gemas +", "Gems/Add", () => ui.Top is ShopScreen);
            yield return Check("Configurações", "Settings", () => ui.Top is SettingsScreen);
            yield return Check("Loja", "Side_Icon_Store", () => ui.Top is ShopScreen);
            yield return Check("Missões", "Side_Icon_Missions", () => ui.Top is MissionsScreen);
            yield return Check("Eventos (em breve)", "Side_Icon_Events", () => ui.ModalOpen);
            yield return Check("Melhorias", "Side_Icon_Upgrades", () => ui.Top is UpgradeScreen);
            yield return Check("Baú diário", "Side_Icon_TimedChest", () => ui.Top is DailyRewardScreen);
            yield return Check("Ranking", "Side_Icon_Ranking", () => ui.Top is RecordsScreen);
            yield return Check("Recompensas", "Side_Icon_Rewards", () => ui.Top is AchievementsScreen);
            yield return Check("Temporada (em breve)", "Side_Icon_Season", () => ui.ModalOpen);
            yield return Check("Amigos (em breve)", "Side_Icon_Friends", () => ui.ModalOpen);
            yield return Check("Mensagens (em breve)", "Side_Icon_Messages", () => ui.ModalOpen);
            yield return Check("Painel do mundo", "WorldPanel", () => ui.Top is LevelSelectScreen);
            yield return Check("Baú do mundo", "WorldPanel/Chest", () => ui.Top is LevelSelectScreen);
            yield return Check("Heróis", "Nav_Icon_Heroes", () => ui.Top is HeroScreen);
            yield return Check("Tropas", "Nav_Icon_Troops", () => ui.Top is TroopScreen);
            yield return Check("Passe (em breve)", "Nav_Icon_BattlePass", () => ui.ModalOpen);
            yield return Check("Início", "Nav_Icon_Home", () => ui.Top is MainMenuScreen);
            yield return Check("Mais", "Nav_Icon_More", () => Find("Modal_MAIS") != null && Find("Modal_MAIS").gameObject.activeInHierarchy, true);
            yield return Capture("menu_mais.png");
            yield return Check("Mais > Mundos", "Modal_MAIS/Window/Content/Side_Icon_Home", () => ui.Top is WorldSelectScreen);
            yield return Check("Modo Boss", "BossHolder/Boss", () => ui.Top is PrepScreen || ui.Top is MainMenuScreen);
            yield return Check("JOGAR", "PlayHolder/Play", () => ui.Top is PrepScreen);
            yield return Capture("depois_jogar.png");

            Finish();
        }

        private bool ToDisk => !string.IsNullOrEmpty(dir);

        private static bool MenuOnTop() => UIRoot.Instance != null && UIRoot.Instance.Top is MainMenuScreen && !SplashScreenController.IsShowing;

        private static void PressSplashStart()
        {
            SplashScreenController splash = FindAnyObjectByType<SplashScreenController>();
            if (splash == null || splash.State != SplashScreenController.LoadState.Ready) return;
            foreach (Button button in splash.GetComponentsInChildren<Button>())
            {
                if (button.isActiveAndEnabled) button.onClick.Invoke();
            }
        }

        private IEnumerator Check(string label, string path, Func<bool> expected, bool keepOpen = false)
        {
            UIRoot ui = UIRoot.Instance;
            Transform target = Find(path);
            Button button = target != null ? target.GetComponent<Button>() : null;
            if (button == null)
            {
                Fail($"{label}: botão '{path}' não encontrado");
                yield break;
            }
            button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            string top = ui.Top != null ? ui.Top.GetType().Name : "nenhuma";
            if (expected()) Log($"OK   {label} -> {top}{(ui.ModalOpen ? " + janela" : string.Empty)}");
            else Fail($"{label}: abriu {top} (janela: {ui.ModalOpen})");
            if (keepOpen) yield break;
            ui.CloseModal();
            if (!(ui.Top is MainMenuScreen))
            {
                if (ui.Game.CurrentLevel != null && ui.Top is PrepScreen) ui.Game.ReturnToMenu();
                else ui.Replace<MainMenuScreen>();
            }
            yield return new WaitForSecondsRealtime(0.5f);
        }

        // Caminho relativo à tela inicial: "Nome" ou "Pai/Filho".
        private static Transform Find(string path)
        {
            UIRoot ui = UIRoot.Instance;
            if (ui == null || ui.Top == null) return null;
            Transform root = ui.Top.Root;
            Transform found = root.Find(path);
            if (found != null) return found;
            string first = path.Split('/')[0];
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != first) continue;
                found = path.Contains("/") ? t.Find(path.Substring(first.Length + 1)) : t;
                if (found != null) return found;
            }
            return null;
        }

        private IEnumerator Capture(string file)
        {
            yield return new WaitForEndOfFrame();
            if (!ToDisk) yield break;
            string path = Path.Combine(dir, file);
            // Sem o módulo ScreenCapture no projeto: lê o backbuffer no fim do quadro.
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Destroy(texture);
            yield return new WaitForSecondsRealtime(0.2f);
            Log("Captura: " + path);
        }

        private void Log(string line)
        {
            report.AppendLine(line);
            Debug.Log("[MenuSelfTest] " + line);
        }

        private void Fail(string line)
        {
            failures++;
            Log("FALHA " + line);
        }

        private void Finish()
        {
            Log(failures == 0 ? "RESULTADO: todos os botões OK" : $"RESULTADO: {failures} falha(s)");
            if (!ToDisk) return; // navegador: fica aberto para inspeção
            File.WriteAllText(Path.Combine(dir, "report.txt"), report.ToString());
            Application.Quit(failures == 0 ? 0 : 1);
        }
    }
}
