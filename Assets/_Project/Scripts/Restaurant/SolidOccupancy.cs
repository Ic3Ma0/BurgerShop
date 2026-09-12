using UnityEngine;

namespace BurgerShop.Restaurant
{
    public static class SolidOccupancy
    {
        public static void Apply(Collider collider, bool solid)
        {
            if (collider == null) return;
            if (solid)
            {
                collider.enabled = true;
                collider.isTrigger = false;
                return;
            }
            collider.enabled = false;
            BurgerVisual.Release(collider);
        }

        public static bool BlocksPlayer(Collider collider) =>
            collider != null && collider.enabled && !collider.isTrigger && collider.gameObject.activeInHierarchy;
    }
}
