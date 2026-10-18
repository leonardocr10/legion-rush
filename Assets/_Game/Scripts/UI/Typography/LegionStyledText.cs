using TMPro;
using UnityEngine;

namespace LegionRush
{
    // Marca um TMP_Text com um estilo do tema. Reaplica ao ativar, ao mudar no Inspector e quando o tema muda.
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public class LegionStyledText : MonoBehaviour
    {
        [SerializeField] private LegionTextStyle style = LegionTextStyle.Menu;
        [Tooltip("Desligue para manter a cor definida pelo código/Inspector (o estilo aplica só fonte e material).")]
        [SerializeField] private bool applyColor = true;

        private TMP_Text text;

        public LegionTextStyle Style
        {
            get => style;
            set
            {
                style = value;
                Apply();
            }
        }

        public bool ApplyColor
        {
            get => applyColor;
            set => applyColor = value;
        }

        public TMP_Text Text => text != null ? text : text = GetComponent<TMP_Text>();

        // Adiciona (ou reaproveita) o componente e aplica o estilo; usado pelos textos criados por código.
        public static LegionStyledText Attach(TMP_Text target, LegionTextStyle style, bool applyColor)
        {
            LegionStyledText styled = target.GetComponent<LegionStyledText>();
            if (styled == null) styled = target.gameObject.AddComponent<LegionStyledText>();
            styled.applyColor = applyColor;
            styled.Style = style;
            return styled;
        }

        public void Apply() => LegionTypography.Apply(Text, style, applyColor);

        private void OnEnable()
        {
            LegionTypographyTheme.Changed += Apply;
            Apply();
        }

        private void OnDisable() => LegionTypographyTheme.Changed -= Apply;

        private void OnValidate()
        {
            if (isActiveAndEnabled) Apply();
        }
    }
}
