using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.UI
{
    public sealed class StaffUpgradeHud : MonoBehaviour
    {
        StaffUpgradeBoard board;
        StatUpgradePopup popup;
        int expectedSpeed, expectedCarry;
        float nextPurchase;

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
            if (popup != null) popup.Subtitle.text = "Upgrades apply to all hired staff";
            popup?.Bind(ClickSpeed, ClickCarry, ClickClose);
            Refresh();
        }

        public void ClickSpeed() { if (IsVisible && Time.unscaledTime >= nextPurchase) { nextPurchase = Time.unscaledTime + .3f; board.TryBuySpeed(expectedSpeed); Refresh(); } }

        public void ClickCarry() { if (IsVisible && Time.unscaledTime >= nextPurchase) { nextPurchase = Time.unscaledTime + .3f; board.TryBuyCarry(expectedCarry); Refresh(); } }

        public void ClickClose() => popup?.Dismiss();

        public bool CanSelect(RestaurantWorker worker) => board != null && board.Owns(worker);
        public bool Open(RestaurantWorker worker = null)
        {
            if (board == null || (worker != null && !CanSelect(worker)) || !popup.Open()) return false;
            nextPurchase = 0; Refresh(); return true;
        }

        void OnDisable() => ClickClose();

        public void RefreshNow() => Refresh();

        void LateUpdate() => Refresh();

        void Refresh()
        {
            if (popup == null) return;
            if (!IsVisible || board == null) return;
            expectedSpeed = board.SpeedTier; expectedCarry = board.CarryTier;
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
