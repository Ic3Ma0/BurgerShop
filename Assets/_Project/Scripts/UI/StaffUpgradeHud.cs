using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.UI
{
    public sealed class StaffUpgradeHud : MonoBehaviour
    {
        StaffUpgradeBoard board;
        StatUpgradePopup popup;

        public StatUpgradePopup Popup => popup;
        public bool IsVisible => popup != null && popup.IsVisible;

        public static StaffUpgradeHud Build(Transform parent, StaffUpgradeBoard upgrades)
        {
            StatUpgradePopup sheet = StatUpgradePopup.Build(parent, "StaffUpgradePopup",
                "Staff upgrades", "Speed", "Carry");
            var hud = sheet.gameObject.AddComponent<StaffUpgradeHud>();
            hud.Configure(upgrades, sheet);
            return hud;
        }

        public void Configure(StaffUpgradeBoard upgrades, StatUpgradePopup sheet)
        {
            board = upgrades;
            popup = sheet;
            popup?.Bind(ClickSpeed, ClickCarry, ClickClose);
            Refresh();
        }

        public void ClickSpeed() => board?.TryBuySpeed();

        public void ClickCarry() => board?.TryBuyCarry();

        public void ClickClose() => popup?.Dismiss();

        public void RefreshNow() => Refresh();

        void LateUpdate() => Refresh();

        void Refresh()
        {
            if (popup == null) return;
            bool inRange = board != null && board.IsPlayerInRange;
            if (!inRange)
            {
                popup.ResetDismissed();
                popup.SetVisible(false);
                return;
            }
            if (popup.IsDismissed)
            {
                popup.SetVisible(false);
                return;
            }
            popup.SetVisible(true);
            popup.PaintStat(true, "Speed", board.SpeedTier,
                Mathf.RoundToInt(Restaurant.StaffBoost.WalkSpeed(board.SpeedTier) / Restaurant.StaffBoost.BaseWalkSpeed * 100) + "%",
                Mathf.RoundToInt(Restaurant.StaffBoost.WalkSpeed(board.SpeedTier + 1) / Restaurant.StaffBoost.BaseWalkSpeed * 100) + "%",
                board.SpeedIsMax, board.SpeedCost, board.Coins);
            popup.PaintStat(false, "Carry", board.CarryTier, Restaurant.StaffBoost.CarryCapacity(board.CarryTier).ToString(),
                Restaurant.StaffBoost.CarryCapacity(board.CarryTier + 1).ToString(), board.CarryIsMax, board.CarryCost, board.Coins);
        }
    }
}
