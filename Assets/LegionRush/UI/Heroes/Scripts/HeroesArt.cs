using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Sprites da tela de heróis (Assets/LegionRush/UI/Heroes/Resources/HeroesArt/<nome>).
    public static class HeroesArt
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (cache.TryGetValue(name, out Sprite sprite)) return sprite;
            sprite = Resources.Load<Sprite>("HeroesArt/" + name);
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>("HeroesArt/" + name);
                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
                else
                {
                    GameLog.Warn("Arte da tela de heróis ausente: " + name);
                }
            }
            cache[name] = sprite;
            return sprite;
        }

        // Imagem sem toque, mantendo a proporção da arte.
        public static Image Image(Transform parent, string sprite, string name = null)
        {
            RectTransform rect = UI.Rect(parent, name ?? sprite);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = Get(sprite);
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (image.sprite == null) image.color = Color.clear;
            return image;
        }
    }
}
