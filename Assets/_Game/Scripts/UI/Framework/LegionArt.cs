using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Sprites do pacote de arte (Assets/LegionRush/Art/UI/Resources/<Categoria>/<nome>), com bordas 9-slice.
    // O importador do editor já marca as texturas como Sprite com bordas; se faltar algo (ex.: arquivo ainda
    // não reimportado), o sprite é criado aqui a partir da textura e de borders.json.
    public static class LegionArt
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        private static Dictionary<string, int[]> borders;

        public static Sprite Get(string path)
        {
            if (cache.TryGetValue(path, out Sprite sprite)) return sprite;
            Vector4 border = Border(path);
            sprite = Resources.Load<Sprite>(path);
            if (sprite == null || (border != Vector4.zero && sprite.border == Vector4.zero))
            {
                Texture2D texture = Resources.Load<Texture2D>(path);
                if (texture != null)
                {
                    texture.wrapMode = TextureWrapMode.Clamp;
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0,
                        SpriteMeshType.FullRect, border);
                }
            }
            if (sprite == null) GameLog.Warn("Arte do pacote ausente: " + path);
            cache[path] = sprite;
            return sprite;
        }

        public static Vector4 Border(string path)
        {
            if (borders == null)
            {
                TextAsset json = Resources.Load<TextAsset>("borders");
                borders = json != null ? ParseBorders(json.text) : new Dictionary<string, int[]>();
            }
            return borders.TryGetValue(path, out int[] b) ? new Vector4(b[0], b[1], b[2], b[3]) : Vector4.zero;
        }

        // {"Categoria/nome": [esq, baixo, dir, topo], ...} — uma entrada por linha.
        public static Dictionary<string, int[]> ParseBorders(string json)
        {
            var table = new Dictionary<string, int[]>();
            foreach (string line in json.Split('\n'))
            {
                int start = line.IndexOf('"');
                int colon = line.IndexOf("\":");
                if (start < 0 || colon < 0) continue;
                string key = line.Substring(start + 1, colon - start - 1);
                string[] parts = line.Substring(colon + 2).Trim().TrimEnd(',').Trim('[', ']', ' ').Split(',');
                if (parts.Length != 4) continue;
                var values = new int[4];
                for (int i = 0; i < 4; i++) int.TryParse(parts[i].Trim(), out values[i]);
                table[key] = values;
            }
            return table;
        }

        public static Image Image(Transform parent, string path, bool sliced = false, string name = null)
        {
            RectTransform rect = UI.Rect(parent, name ?? path);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = Get(path);
            image.raycastTarget = false;
            if (image.sprite == null) image.color = Color.clear;
            else if (sliced && image.sprite.border != Vector4.zero)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
                rect.gameObject.AddComponent<UISliceFit>();
            }
            else image.preserveAspect = true;
            return image;
        }

        // Sprite de botão do pacote para uma cor da paleta antiga (mantém as chamadas existentes de UI.Button).
        public static string ButtonFor(Color color)
        {
            if (Near(color, UIStyle.Gold)) return "Buttons/btn_gold";
            if (Near(color, UIStyle.Green)) return "Buttons/btn_green";
            if (Near(color, UIStyle.Purple)) return "Buttons/btn_purple";
            if (Near(color, UIStyle.Red)) return "Buttons/btn_red";
            if (Near(color, UIStyle.Gray)) return "Buttons/btn_gray";
            if (Near(color, UIStyle.Blue)) return "Buttons/btn_blue";
            if (color.r > 0.8f && color.g > 0.45f && color.b < 0.3f) return "Buttons/btn_orange";
            return null;
        }

        private static bool Near(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.08f;
    }

    // Ajusta a escala das bordas 9-slice à altura do elemento, para as pontas do botão/moldura
    // manterem a proporção da arte em qualquer tamanho.
    [RequireComponent(typeof(Image))]
    public class UISliceFit : MonoBehaviour
    {
        [Tooltip("Altura de referência relativa ao sprite (1 = pontas na altura do sprite).")]
        public float heightRatio = 1f;

        private Image image;

        private void OnEnable() => Fit();
        private void OnRectTransformDimensionsChange() => Fit();

        private void Fit()
        {
            if (image == null) image = GetComponent<Image>();
            if (image == null || image.sprite == null) return;
            float height = ((RectTransform)transform).rect.height;
            if (height <= 1f) return;
            image.pixelsPerUnitMultiplier = Mathf.Max(0.2f, image.sprite.rect.height / (height * heightRatio));
        }
    }

    // Troca o sprite do botão para o cinza quando desabilitado (estado "DESABILITADO" do pacote).
    [RequireComponent(typeof(Button))]
    public class UIButtonArtState : MonoBehaviour
    {
        public Sprite normal;
        public Sprite disabled;

        private Button button;
        private Image image;
        private bool last = true;

        private void Awake()
        {
            button = GetComponent<Button>();
            image = button.targetGraphic as Image;
        }

        private void LateUpdate()
        {
            if (button == null || image == null || button.interactable == last) return;
            last = button.interactable;
            image.sprite = last ? normal : disabled;
        }
    }
}
