using BurgerShop.Building;
using BurgerShop.Restaurant;

namespace BurgerShop.Economy
{
    // Read-only missing-equipment totals. Ownership and credits stay in the existing layout.
    public sealed class BusinessOpeningQuote
    {
        readonly FacilityLayout layout;
        public BusinessOpeningQuote(FacilityLayout facilities) { layout = facilities; }
        public int Missing(FacilityKind kind) => layout.Owned(kind) > 0 ? 0 : layout.Price(kind);
        public int Cola => Missing(FacilityKind.ColaMachine) + Missing(FacilityKind.ColaCounter);
        public int Wing => layout.GetComponentInChildren<ShopExpansion>() is ShopExpansion shop && !shop.HasWing
            ? shop.WingPad?.Remaining ?? ShopExpansion.WingCost : 0;
        public int ColaWithWing => Cola + Wing;
        // First car window is included at rank 6, never a paid missing component.
        public int BlueBoxes => Missing(FacilityKind.BlueBoxTable);
        public int Bags => Missing(FacilityKind.BagMachine) + Missing(FacilityKind.BagTable) + Missing(FacilityKind.BagCounter);
        public int FirstStaff => (layout.GetComponent<MainHallExpansion>()?.Remaining ?? 0)
            + ((layout.GetComponent<WorkerHiringZone>()?.HiredCount ?? 0) > 0 ? 0 : WorkerHiringZone.HireCosts[0]);
        public string ColaCopy => $"Own space: machine {Missing(FacilityKind.ColaMachine)} + counter {Missing(FacilityKind.ColaCounter)} = {Cola}\nOptional wing {Wing} · total {ColaWithWing}";
        public string BagCopy => $"Machine {Missing(FacilityKind.BagMachine)} + packing {Missing(FacilityKind.BagTable)} + pickup {Missing(FacilityKind.BagCounter)}\nRemaining to open {Bags} · land included";
        public string BlueCopy => $"Packing still needed {BlueBoxes}\nFirst car window included at Lv.{ShopRanks.DriveThruRank}";
    }
}
