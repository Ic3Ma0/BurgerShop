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
        TrashInventory trash;
        Text label;
        int shownCount = -1;
        int shownCapacity = -1;
        int shownTrash = -1;
        int shownCola = -1;
        bool shownInRange;
        bool shownAtUpgrade;
        bool shownAtHiring;

        public void Configure(BurgerInventory carrier, BurgerPickupZone zone, Text text, GrillUpgradeZone upgradeZone = null, WorkerHiringZone hiringZone = null, TrashInventory trashBag = null)
        {
            inventory = carrier;
            pickup = zone;
            upgrade = upgradeZone;
            hiring = hiringZone;
            trash = trashBag;
            label = text;
            shownCount = -1;
            if (label != null) label.raycastTarget = false;
            HudChrome.HideFromDefaultScreen(gameObject);
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
            int trashCount = trash != null ? trash.Count : 0;
            int colaCount = inventory.ColaCount;
            if (shownCount == inventory.Count && shownCapacity == inventory.Capacity && shownTrash == trashCount
                && shownCola == colaCount
                && shownInRange == inRange && shownAtUpgrade == atUpgrade && shownAtHiring == atHiring)
                return;

            shownCount = inventory.Count;
            shownCapacity = inventory.Capacity;
            shownTrash = trashCount;
            shownCola = colaCount;
            shownInRange = inRange;
            shownAtUpgrade = atUpgrade;
            shownAtHiring = atHiring;
            bool onlyCola = colaCount > 0 && inventory.LooseCount == 0 && inventory.BoxedCount == 0;
            string hint = atHiring ? "Staff details below"
                : atUpgrade ? "Upgrade details below"
                : inventory.IsFull ? (onlyCola ? "FULL - Take cola to the cola counter" : "FULL - Take burgers to the counter")
                : inRange ? "Picking up - wait for the grill"
                : onlyCola ? "Take cola to the cola counter"
                : inventory.Count > 0 ? "Take burgers to the burger counter"
                : "Stand on the green pickup spot";
            string colaLine = colaCount > 0 ? $"\nCOLA  {colaCount}" : "";
            string trashLine = trashCount > 0 ? $"\nTRASH  {trashCount}" : "";
            label.text = $"BURGERS  {inventory.Count}/{inventory.Capacity}{colaLine}{trashLine}\n{hint}";
            label.color = inventory.IsFull ? new Color(1f, 0.79f, 0.3f) : Color.white;
        }
    }
}
