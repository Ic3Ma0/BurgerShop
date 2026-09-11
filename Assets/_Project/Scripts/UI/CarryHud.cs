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
        GrillUpgradeZone upgrade;
        Text label;
        int shownCount = -1;
        int shownCapacity = -1;
        bool shownInRange;
        bool shownAtUpgrade;

        public void Configure(BurgerInventory carrier, BurgerPickupZone zone, Text text, GrillUpgradeZone upgradeZone = null)
        {
            inventory = carrier;
            pickup = zone;
            upgrade = upgradeZone;
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
            bool atUpgrade = upgrade != null && upgrade.IsInRange;
            if (shownCount == inventory.Count && shownCapacity == inventory.Capacity && shownInRange == inRange && shownAtUpgrade == atUpgrade)
                return;

            shownCount = inventory.Count;
            shownCapacity = inventory.Capacity;
            shownInRange = inRange;
            shownAtUpgrade = atUpgrade;
            string hint = atUpgrade ? "Upgrade details below"
                : inventory.IsFull ? "FULL - Go to the gold serving spot"
                : inRange ? "Picking up - wait for the grill"
                : inventory.Count > 0 ? "Take burgers to the gold serving spot"
                : "Stand on the green pickup spot";
            label.text = $"BURGERS  {inventory.Count}/{inventory.Capacity}\n{hint}";
            label.color = inventory.IsFull ? new Color(1f, 0.79f, 0.3f) : Color.white;
        }
    }
}
