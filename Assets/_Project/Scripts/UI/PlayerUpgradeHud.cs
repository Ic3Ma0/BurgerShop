using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.UI
{
    public sealed class PlayerUpgradeHud : MonoBehaviour
    {
        BoostUpgradeZone board;
        StatUpgradePopup popup;

        public StatUpgradePopup Popup => popup;
        public bool IsVisible => popup != null && popup.IsVisible;

        public static PlayerUpgradeHud Build(Transform parent, BoostUpgradeZone upgrades)
        {
            StatUpgradePopup sheet = StatUpgradePopup.Build(parent, "PlayerUpgradePopup",
                "Player upgrades", "Speed", "Carry");
            var hud = sheet.gameObject.AddComponent<PlayerUpgradeHud>();
            hud.Configure(upgrades, sheet);
            return hud;
        }

        public void Configure(BoostUpgradeZone upgrades, StatUpgradePopup sheet)
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
            popup.PaintStat(true, "Speed (u/s)", board.SpeedTier,
                Player.PlayerBoost.MoveSpeed(board.SpeedTier, board.CarryTier).ToString("0.00"),
                Player.PlayerBoost.MoveSpeed(board.SpeedTier + 1, board.CarryTier).ToString("0.00"),
                board.SpeedIsMax, board.SpeedCost, board.Coins);
            string carryNext = board.CarryIsMax ? Player.PlayerBoost.CarryCapacity(board.CarryTier).ToString()
                : Player.PlayerBoost.IsEmptyCarryLevel(board.CarryTier + 1)
                    ? Player.PlayerBoost.CarryCapacity(board.CarryTier + 1) + " · +3% speed"
                    : Player.PlayerBoost.CarryCapacity(board.CarryTier + 1).ToString();
            popup.PaintStat(false, "Carry", board.CarryTier, Player.PlayerBoost.CarryCapacity(board.CarryTier).ToString(),
                carryNext, board.CarryIsMax, board.CarryCost, board.Coins);
        }
    }
}
