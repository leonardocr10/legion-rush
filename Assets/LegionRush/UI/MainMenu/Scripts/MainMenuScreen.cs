using UnityEngine;

namespace LegionRush
{
    // Tela inicial. Liga a MainMenuView (apresentação) aos dados reais do jogo (GameMainMenuData)
    // e à navegação pelas telas existentes (UIRootMainMenuNavigator).
    public class MainMenuScreen : UIScreen
    {
        // Voltar do Android só fecha o menu MAIS; o menu inicial é a raiz da navegação.
        public override bool AllowBack => view != null && view.ModalOpen;

        private MainMenuView view;
        private IMainMenuData data;

        public MainMenuView View => view;

        protected override void Build(RectTransform root)
        {
            data = new GameMainMenuData(Ui.Game);
            view = new MainMenuView(root, new UIRootMainMenuNavigator(Ui));
            root.gameObject.AddComponent<UITicker>().Tick = () => view.Tick();
            Economy.WalletChanged += OnWalletChanged;
            MetaService.Changed += OnMetaChanged;
        }

        public override void Refresh()
        {
            if (!SaveManager.IsLoaded) return;
            view.Bind(data.Snapshot());
            GameAudio.PlayMusic(MusicTrack.Menu);
        }

        public override void OnBack()
        {
            view.CloseModal();
        }

        // Moedas e gemas recebidas com a tela aberta animam na hora (contagem e pulo do ícone).
        private void OnWalletChanged(int gold, int gems)
        {
            if (Root != null && Root.gameObject.activeInHierarchy) Refresh();
        }

        private void OnMetaChanged()
        {
            if (Root != null && Root.gameObject.activeInHierarchy) Refresh();
        }
    }
}
