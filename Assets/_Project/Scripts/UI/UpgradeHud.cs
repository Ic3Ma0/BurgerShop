using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class UpgradeHud : MonoBehaviour
    {
        GrillUpgradeZone upgrade;
        Text label;
        Image progress;
        CanvasGroup panel;

        public void Configure(GrillUpgradeZone zone, Text text, Image fill, CanvasGroup group)
        {
            upgrade = zone;
            label = text;
            progress = fill;
            panel = group;
            Refresh();
        }

        void LateUpdate() => Refresh();

        void OnDestroy()
        {
            // The bootstrap creates this sprite specifically for the progress bar.
            if (progress != null && progress.sprite != null) BurgerVisual.Release(progress.sprite);
        }

        void Refresh()
        {
            if (upgrade == null || label == null) return;
            panel.alpha = upgrade.IsInRange ? 1f : 0f;
            progress.fillAmount = upgrade.Progress;
            if (upgrade.IsMaxLevel)
            {
                label.text = $"GRILL LV {upgrade.Level} - MAX\n{upgrade.CurrentProductionSeconds:0.0}s per burger\nFully upgraded";
                return;
            }
            if (upgrade.PurchasedThisVisit)
            {
                label.text = $"GRILL LV {upgrade.Level} - Upgraded!\nNow {upgrade.CurrentProductionSeconds:0.0}s per burger\nNext: {upgrade.NextCost} COINS - leave and return";
                return;
            }
            string hint = !upgrade.IsAvailable ? "Upgrade unavailable"
                : upgrade.MissingCoins > 0 ? $"Need {upgrade.MissingCoins} more coins"
                : $"Stay here to upgrade - {Mathf.FloorToInt(upgrade.Progress * 100f)}%";
            label.text = $"GRILL LV {upgrade.Level} > {upgrade.Level + 1}\n{upgrade.NextCost} COINS  |  {upgrade.CurrentProductionSeconds:0.0}s > {upgrade.NextProductionSeconds:0.0}s\n{hint}";
        }
    }
}
