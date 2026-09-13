using System.Collections.Generic;
using UnityEngine;

namespace BurgerShop.Building
{
    // Baked from FacilityFactory's real meshes. Catalog browsing never constructs gameplay objects.
    public static class FacilityThumbnails
    {
        static readonly Dictionary<FacilityKind, Sprite> cache = new Dictionary<FacilityKind, Sprite>();
        static Sprite restroom;
        public static Sprite Restroom=>restroom!=null?restroom:restroom=Load("Restroom");
        public static Sprite Get(FacilityKind kind)
        {
            if (cache.TryGetValue(kind, out var sprite) && sprite != null) return sprite;
            sprite=Load(kind.ToString());cache[kind]=sprite;return sprite;
        }
        static Sprite Load(string name)
        {
            var texture=Resources.Load<Texture2D>("FacilityCatalog/"+name);
            return texture==null?null:Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,100);
        }
    }
}
