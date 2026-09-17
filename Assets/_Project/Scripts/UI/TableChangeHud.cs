using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.UI
{
    public sealed class TableChangeHud : MonoBehaviour
    {
        TableUpgradeBoard board;
        TableChangePopup popup;
        TableUpgradeZone focused;

        public TableChangePopup Popup => popup;
        public bool IsVisible => popup != null && popup.IsVisible;

        public static TableChangeHud Build(Transform parent, TableUpgradeBoard upgrades)
        {
            TableChangePopup sheet = TableChangePopup.Build(parent);
            var hud = sheet.gameObject.AddComponent<TableChangeHud>();
            hud.Configure(upgrades, sheet);
            return hud;
        }

        public void Configure(TableUpgradeBoard upgrades, TableChangePopup sheet)
        {
            board = upgrades;
            popup = sheet;
            BindButtons();
            Refresh();
        }

        public void ClickSelect(int index)
        {
            if (focused == null || index < 0 || index >= TableSetCatalog.ChoiceCount) return;
            var id = TableSetCatalog.Choices[index];
            if (!focused.TryBuySet(id, focused.Invested)) return;
            popup?.SetVisible(false);
        }

        public void ClickClose() => popup?.Dismiss();

        public void RefreshNow() => Refresh();

        void LateUpdate() => Refresh();

        void Refresh()
        {
            if (popup == null) return;
            if (FacilityDetailsHud.Current != null) { popup.SetVisible(false); return; }
            TableUpgradeZone zone = board != null ? board.PendingZoneInRange : null;
            if (zone != focused)
            {
                focused = zone;
                popup.ResetDismissed();
                BindButtons();
            }
            if (zone == null)
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
            popup.PaintChoices(zone);
            popup.SetVisible(true);
        }

        void BindButtons()
        {
            popup?.Bind(() => ClickSelect(0), () => ClickSelect(1), () => ClickSelect(2), ClickClose);
        }
    }
}
