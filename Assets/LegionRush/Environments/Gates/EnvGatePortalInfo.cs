using UnityEngine;

namespace LegionRush.Environments
{
    // Vai nos prefabs Gate_* para ligar o visual ao sistema real de portais: a categoria define a cor
    // (GateOption.ColorOf). No jogo, o GateRowController monta este mesmo visual (EnvGatePortal) para cada
    // GateOption da fase e decide a escolha pela faixa do comandante ao cruzar a linha; o trigger do prefab
    // serve para montar cenas à mão e para ferramentas, não para a regra do jogo.
    public class EnvGatePortalInfo : MonoBehaviour
    {
        public GateCategory category = GateCategory.Defense;
        [TextArea] public string note = "Valores e textos vêm do GateOption da fase (GateRowController).";
    }
}
