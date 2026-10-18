using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LegionRush
{
    // Arte do HUD de batalha recortada da prancha (Assets/LegionRush/UI/Battle/Resources/BattleArt/<nome>).
    public static class BattleArt
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out Sprite sprite)) return sprite;
            sprite = Resources.Load<Sprite>("BattleArt/" + name);
            cache[name] = sprite;
            return sprite;
        }

        public static Texture2D Texture(string name) => Resources.Load<Texture2D>("BattleArt/" + name);

        public static Image Image(Transform parent, string sprite, string name = null)
        {
            RectTransform rect = UI.Rect(parent, name ?? sprite);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = Get(sprite);
            image.preserveAspect = false;
            image.raycastTarget = false;
            if (image.sprite == null)
            {
                image.color = Color.clear;
                GameLog.Warn("Arte do HUD de batalha ausente: " + sprite);
            }
            return image;
        }
    }
}
