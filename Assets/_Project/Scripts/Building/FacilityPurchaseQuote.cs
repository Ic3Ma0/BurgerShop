using System;

namespace BurgerShop.Building
{
    public readonly struct FacilityPurchaseQuote : IEquatable<FacilityPurchaseQuote>
    {
        public readonly int BasePrice, FullPrice, Invested, Due, Owned;
        public FacilityPurchaseQuote(FacilityKind kind, int owned, int invested)
        {
            BasePrice = FacilityCatalog.Get(kind).BasePrice;
            Owned = owned;
            FullPrice = FacilityCatalog.Price(kind, owned);
            Invested = Math.Max(0, invested);
            Due = Math.Max(0, FullPrice - Invested);
        }
        public bool Equals(FacilityPurchaseQuote other) => FullPrice == other.FullPrice
            && Invested == other.Invested && Owned == other.Owned;
    }
}
