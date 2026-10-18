using UnityEngine;

namespace LegionRush
{
    // Prévia da tela inicial sem o jogo (cena MainMenuPreview): monta a MainMenuView com os valores do mockup
    // e registra no Console os destinos tocados. Útil para ajustar layout e arte isoladamente.
    public class MainMenuPreview : MonoBehaviour
    {
        public RectTransform target;

        private MainMenuView view;
        private float next;

        private void Start()
        {
            if (target == null) target = (RectTransform)transform;
            view = new MainMenuView(target, new NullMainMenuNavigator());
            view.Bind(new SampleMainMenuData().Snapshot());
        }

        private void Update()
        {
            if (view == null || Time.unscaledTime < next) return;
            next = Time.unscaledTime + 1f;
            view.Tick();
        }
    }
}
