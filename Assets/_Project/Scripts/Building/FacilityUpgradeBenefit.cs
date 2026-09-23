using BurgerShop.Restaurant;

namespace BurgerShop.Building
{
    public static class FacilityUpgradeBenefit
    {
        public static string Describe(FacilityKind kind, int level)
        {
            switch (kind)
            {
                case FacilityKind.CarCounter:
                    return $"Service interval {DriveThruLane.IntervalForLevel(level):0.00}s";
                case FacilityKind.BurgerCounter: case FacilityKind.ColaCounter:
                    return $"Service interval {BurgerServingZone.HandoffDuration + BurgerServingZone.CooldownForLevel(level):0.00}s";
                case FacilityKind.BagCounter:
                    return $"Service interval {BurgerServingZone.HandoffDuration + BagLine.CooldownForLevel(level):0.00}s";
                case FacilityKind.BlueBoxTable:
                    return $"Packing {BoxingStation.SecondsForLevel(level):0.00}s";
                case FacilityKind.BagMachine:
                    return $"Bag production {BagLine.ProductionForLevel(level):0.00}s";
                case FacilityKind.BagTable:
                    return $"Packing {BagLine.ProcessingForLevel(level):0.00}s";
                default: return "Facility level " + level;
            }
        }
    }
}
