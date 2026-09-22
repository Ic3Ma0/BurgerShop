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
            if (!board.CanUpgradeStaff)
            {
                string reason = !board.HasStaff ? "Hire staff first" : "Expand main hall first";
                popup.SetOption(true, "Speed\n" + reason, false, StatUpgradePopup.Disabled);
                popup.SetOption(false, "Carry\n" + reason, false, StatUpgradePopup.Disabled);
                return;
            }
            popup.PaintStat(true, "Speed (u/s)", board.SpeedTier,
                Restaurant.StaffBoost.WalkSpeed(board.SpeedTier, board.CarryTier).ToString("0.00"),
                Restaurant.StaffBoost.WalkSpeed(board.SpeedTier + 1, board.CarryTier).ToString("0.00"),
                board.SpeedIsMax, board.SpeedCost, board.Coins);
            string carryNext = board.CarryIsMax ? Restaurant.StaffBoost.CarryCapacity(board.CarryTier).ToString()
                : Player.PlayerBoost.IsEmptyCarryLevel(board.CarryTier + 1)
                    ? Restaurant.StaffBoost.CarryCapacity(board.CarryTier + 1) + " · +3% speed"
                    : Restaurant.StaffBoost.CarryCapacity(board.CarryTier + 1).ToString();
            popup.PaintStat(false, "Carry", board.CarryTier, Restaurant.StaffBoost.CarryCapacity(board.CarryTier).ToString(),
                carryNext, board.CarryIsMax, board.CarryCost, board.Coins);
        }
    }
}
