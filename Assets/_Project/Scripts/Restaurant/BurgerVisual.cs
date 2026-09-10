using UnityEngine;

namespace BurgerShop.Restaurant
{
    // Materials travel with the burger when it moves from grill to player.
    [ExecuteAlways]
    public sealed class BurgerVisual : MonoBehaviour
    {
        Material[] materials;

        internal void OwnMaterials(params Material[] ownedMaterials)
        {
            materials = ownedMaterials;
        }

        void OnDestroy()
        {
            if (materials == null)
                return;
            foreach (Material material in materials)
                if (material != null)
                    Release(material);
        }

        internal static void Release(Object instance)
        {
            if (Application.isPlaying)
                Destroy(instance);
            else
                DestroyImmediate(instance);
        }
    }
}
