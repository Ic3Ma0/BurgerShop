using System.Collections.Generic;
using BurgerShop.Building;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.UI
{
    public static class TableSetThumbnails
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        public static Sprite Get(FacilityKind kind, TableSetId set)
        {
            string key = kind + "_" + set;
            if (cache.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            var texture = Resources.Load<Texture2D>("TableSets/" + key);
            if (texture == null) return FacilityThumbnails.Get(kind);
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one*.5f,100);
            cache[key] = sprite; return sprite;
        }
    }
}
