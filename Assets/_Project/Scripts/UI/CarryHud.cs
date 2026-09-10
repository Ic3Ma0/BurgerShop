using BurgerShop.Player;
using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class CarryHud : MonoBehaviour
    {
        BurgerInventory inventory;
        BurgerPickupZone pickup;
        Text label;
        int shownCount = -1;
        int shownCapacity = -1;
        bool shownInRange;

        public void Configure(BurgerInventory carrier, BurgerPickupZone zone, Text text)
        {
            inventory = carrier;
            pickup = zone;
            label = text;
            shownCount = -1;
            Refresh();
        }

        void LateUpdate() => Refresh();

        void Refresh()
        {
            if (inventory == null || label == null)
                return;
            bool inRange = pickup != null && pickup.IsInRange;
            if (shownCount == inventory.Count && shownCapacity == inventory.Capacity && shownInRange == inRange)
                return;

            shownCount = inventory.Count;
            shownCapacity = inventory.Capacity;
            shownInRange = inRange;
            string hint = inventory.IsFull ? "FULL - Go to the gold serving spot"
                : inRange ? "Picking up - wait for the grill"
                : inventory.Count > 0 ? "Take burgers to the gold serving spot"
                : "Stand on the green pickup spot";
            label.text = $"BURGERS  {inventory.Count}/{inventory.Capacity}\n{hint}";
            label.color = inventory.IsFull ? new Color(1f, 0.79f, 0.3f) : Color.white;
        }
    }
}
