using System;
using System.Collections.Generic;
using BurgerShop.Restaurant;

namespace BurgerShop.Building
{
    public enum FacilityKind
    {
        PairTable, FourSeatTable, SquareTable, BurgerMachine, ColaMachine,
        BlueBoxTable, RedBoxMachine, BagMachine, BagTable,
        BurgerCounter, ColaCounter, CarCounter, BagCounter, CourierTray, TrashBin
    }

    public readonly struct FacilityOffer
    {
        public readonly FacilityKind Kind;
        public readonly string Name;
        public readonly int BasePrice;
        public readonly int Rank;
        public FacilityOffer(FacilityKind kind,string name,int price,int rank)
        {Kind=kind;Name=name;BasePrice=price;Rank=rank;}
    }

    // Catalog prices belong here; never infer a price from a rendered model or its upgrade tier.
    public static class FacilityCatalog
    {
        public const float RotationStep=15f;
        public const int PriceStep=10;
        public const double RepeatMultiplier=1.25;
        public const int RedMachinePrice=200;
        public const int CourierTrayPrice=150;
        public const int TrashBinPrice=50;
        public const int BagMachinePrice=250;
        public const int BagTablePrice=200;
        public const int BagCounterPrice=300;
        static readonly FacilityOffer[] offers={
            new FacilityOffer(FacilityKind.PairTable,"Dining table",ShopExpansion.TableCost,ShopRanks.DiningRank),
            new FacilityOffer(FacilityKind.FourSeatTable,"Four-seat table",ShopExpansion.FourSeatCost,ShopRanks.ColaWingRank),
            new FacilityOffer(FacilityKind.SquareTable,"Square table",ShopExpansion.SquareTableCost,ShopRanks.ColaWingRank),
            new FacilityOffer(FacilityKind.BurgerMachine,"Burger machine",ShopExpansion.GrillCost,ShopRanks.ExtraKitchenRank),
            new FacilityOffer(FacilityKind.ColaMachine,"Cola machine",ShopExpansion.GrillCost,ShopRanks.ColaWingRank),
            new FacilityOffer(FacilityKind.BlueBoxTable,"Blue-box packing",ShopExpansion.BoxingCost,ShopRanks.BoxingRank),
            new FacilityOffer(FacilityKind.RedBoxMachine,"Red-box packing",RedMachinePrice,ShopRanks.CourierRank),
            new FacilityOffer(FacilityKind.BagMachine,"Paper bag machine",BagMachinePrice,ShopRanks.WestRank),
            new FacilityOffer(FacilityKind.BagTable,"Bagging table",BagTablePrice,ShopRanks.WestRank),
            new FacilityOffer(FacilityKind.BurgerCounter,"Burger counter",ShopExpansion.CounterCost,ShopRanks.ExtraKitchenRank),
            new FacilityOffer(FacilityKind.ColaCounter,"Cola counter",ShopExpansion.CounterCost,ShopRanks.ColaWingRank),
            new FacilityOffer(FacilityKind.CarCounter,"Drive-thru counter",ShopExpansion.DriveThruCost,ShopRanks.DriveThruRank),
            new FacilityOffer(FacilityKind.BagCounter,"Pickup counter",BagCounterPrice,ShopRanks.WestRank),
            new FacilityOffer(FacilityKind.CourierTray,"Courier pickup tray",CourierTrayPrice,ShopRanks.CourierRank),
            new FacilityOffer(FacilityKind.TrashBin,"Trash bin",TrashBinPrice,ShopRanks.DiningRank)
        };
        public static IReadOnlyList<FacilityOffer> Offers=>Array.AsReadOnly(offers);
        public static FacilityOffer Get(FacilityKind kind)
        {
            int index=(int)kind;
            if(index<0||index>=offers.Length)throw new ArgumentOutOfRangeException(nameof(kind));
            return offers[index];
        }
        public static int Price(FacilityKind kind,int ownedCount)
        {
            if(ownedCount<0)throw new ArgumentOutOfRangeException(nameof(ownedCount));
            double price=Get(kind).BasePrice*Math.Pow(RepeatMultiplier,ownedCount);
            double rounded=Math.Ceiling(price/PriceStep)*PriceStep;
            return rounded>=int.MaxValue?int.MaxValue:(int)rounded;
        }
        public static int[] UpgradeCosts(FacilityKind kind)
        {
            switch(kind)
            {
                case FacilityKind.BurgerCounter:case FacilityKind.ColaCounter:case FacilityKind.CarCounter:return new[]{100,200};
                case FacilityKind.BlueBoxTable:return new[]{120,240};
                case FacilityKind.BagMachine:case FacilityKind.BagTable:case FacilityKind.BagCounter:return new[]{150,300};
                default:return Array.Empty<int>();
            }
        }
        public static bool IsUnlocked(FacilityKind kind,int rank,bool legacyAccess)=>legacyAccess||rank>=Get(kind).Rank;
    }
}
