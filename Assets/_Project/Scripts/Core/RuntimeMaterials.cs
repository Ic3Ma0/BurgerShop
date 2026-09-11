using System;
using UnityEngine;

namespace BurgerShop.Core
{
    // Resources materials retain their URP shaders and opaque depth-writing variants in builds.
    public static class RuntimeMaterials
    {
        public static Material Create(Color color, bool unlit = false)
        {
            string resource = unlit ? "RestaurantUnlit" : "RestaurantLit";
            Material template = Resources.Load<Material>(resource);
            if (template == null || template.shader == null || !template.shader.isSupported)
                throw new InvalidOperationException("Required restaurant material unavailable: " + resource);
            var material = new Material(template);
            material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }
    }
}
