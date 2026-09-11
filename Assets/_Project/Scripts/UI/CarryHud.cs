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
        WorkerHiringZone hiring;
        Text label;
        int shownCount = -1;
        int shownCapacity = -1;
        bool shownInRange;
        bool shownAtUpgrade;
        bool shownAtHiring;

        public void Configure(BurgerInventory carrier, BurgerPickupZone zone, Text text, GrillUpgradeZone upgradeZone = null, WorkerHiringZone hiringZone = null)
        {
            inventory = carrier;
            pickup = zone;
            upgrade = upgradeZone;
            hiring = hiringZone;
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
            bool atHiring = hiring != null && hiring.IsInRange;
            if (shownCount == inventory.Count && shownCapacity == inventory.Capacity && shownInRange == inRange && shownAtUpgrade == atUpgrade && shownAtHiring == atHiring)
                return;

            shownCount = inventory.Count;
            shownCapacity = inventory.Capacity;
            shownInRange = inRange;
            shownAtUpgrade = atUpgrade;
            shownAtHiring = atHiring;
            string hint = atHiring ? "Staff details below"
                : atUpgrade ? "Upgrade details below"
                : inventory.IsFull ? "FULL - Go to the gold serving spot"
                : inRange ? "Picking up - wait for the grill"
                : inventory.Count > 0 ? "Take burgers to the gold serving spot"
                : "Stand on the green pickup spot";
            label.text = $"BURGERS  {inventory.Count}/{inventory.Capacity}\n{hint}";
            label.color = inventory.IsFull ? new Color(1f, 0.79f, 0.3f) : Color.white;
        }
    }
}
