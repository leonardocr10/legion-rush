using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Acesso por nome aos sprites da tela inicial (catálogo MainMenuArtCatalog em Resources/MainMenuArt).
    public static class MainMenuArt
    {
        public const string CatalogResource = "MainMenuArt";

        private static Dictionary<string, Sprite> map;

        public static Sprite Get(string name)
        {
            if (map == null) Load();
            if (map.TryGetValue(name, out Sprite sprite)) return sprite;
            GameLog.Warn("Arte da tela inicial ausente: " + name);
            map[name] = null;
            return null;
        }

        // Existe no catálogo? (sem aviso; usado para trocar pela arte nova quando ela chegar)
        public static bool Has(string name)
        {
            if (map == null) Load();
            return map.TryGetValue(name, out Sprite sprite) && sprite != null;
        }

        private static void Load()
        {
            map = new Dictionary<string, Sprite>();
            var catalog = Resources.Load<MainMenuArtCatalog>(CatalogResource);
            if (catalog == null) return;
            foreach (Sprite sprite in catalog.sprites)
            {
                if (sprite != null) map[sprite.name] = sprite;
            }
        }

        // Imagem sem toque. Sprites com bordas 9-slice ficam fatiados na escala da arte de referência.
        public static Image Image(Transform parent, string sprite, string name = null, bool sliced = false)
        {
            RectTransform rect = UI.Rect(parent, name ?? sprite);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = Get(sprite);
            image.raycastTarget = false;
            if (image.sprite == null)
            {
                image.color = Color.clear;
                return image;
            }
            if (sliced && image.sprite.border != Vector4.zero)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f / LegionUI.Px;
            }
            else
            {
                image.preserveAspect = true;
            }
            return image;
        }
    }
}
