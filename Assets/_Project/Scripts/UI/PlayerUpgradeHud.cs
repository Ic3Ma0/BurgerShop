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
            popup.PaintOption(true, "Speed", board.SpeedIsMax, board.SpeedCost, board.CanAffordSpeed,
                StatUpgradePopup.ReadyLeft);
            popup.PaintOption(false, "Carry", board.CarryIsMax, board.CarryCost, board.CanAffordCarry,
                StatUpgradePopup.ReadyRight);
        }
    }
}
