using System.Collections.Generic;
using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class UpgradeHud : MonoBehaviour
    {
        GrillUpgradeZone upgrade;
        readonly List<GrillUpgradeZone> extras = new List<GrillUpgradeZone>();
        Text label;
        Image progress;
        CanvasGroup panel;

        public void Configure(GrillUpgradeZone zone, Text text, Image fill, CanvasGroup group)
        {
            upgrade = zone;
            label = text;
            progress = fill;
            panel = group;
            if (label != null) label.raycastTarget = false;
            HudChrome.Mute(fill);
            Refresh();
        }

        public void AddZone(GrillUpgradeZone zone)
        {
            if (zone == null || extras.Contains(zone)) return;
            extras.Add(zone);
        }

        void LateUpdate() => Refresh();

        GrillUpgradeZone Active()
        {
            if (upgrade != null && upgrade.IsInRange) return upgrade;
            for (int i = 0; i < extras.Count; i++)
                if (extras[i] != null && extras[i].IsInRange) return extras[i];
            return upgrade;
        }

        void Refresh()
        {
            GrillUpgradeZone zone = Active();
            if (zone == null || label == null) return;
            if (panel != null)
            {
                panel.alpha = zone.IsInRange ? 1f : 0f;
                panel.blocksRaycasts = false;
                panel.interactable = false;
            }
            if (progress != null) progress.fillAmount = zone.Progress;
            if (zone.IsMaxLevel)
            {
                label.text = $"{zone.ProductNoun} LV {zone.Level} - MAX\n{zone.CurrentProductionSeconds:0.0}s per {zone.ItemNoun}\nFully upgraded";
                return;
            }
            if (zone.PurchasedThisVisit)
            {
                label.text = $"{zone.ProductNoun} LV {zone.Level} - Upgraded!\nNow {zone.CurrentProductionSeconds:0.0}s per {zone.ItemNoun}\nNext: {zone.NextCost} COINS - leave and return";
                return;
            }
            string hint = !zone.IsAvailable ? "Upgrade unavailable"
                : zone.MissingCoins > 0 ? $"Need {zone.MissingCoins} more coins"
                : $"Stay here to upgrade - {Mathf.FloorToInt(zone.Progress * 100f)}%";
            label.text = $"{zone.ProductNoun} LV {zone.Level} > {zone.Level + 1}\n{zone.NextCost} COINS  |  {zone.CurrentProductionSeconds:0.0}s > {zone.NextProductionSeconds:0.0}s\n{hint}";
        }
    }
}
