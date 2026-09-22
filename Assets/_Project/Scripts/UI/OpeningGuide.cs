using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.UI
{
    // A view of the actual first-order flow. It never awards progress or purchases anything.
    public sealed class OpeningGuide
    {
        public enum Step { Upgrade, RankUp, Done, Pickup, Stock, Serve }
        GrillUpgradeZone grill;
        SessionGoalTracker tracker;
        BurgerInventory inventory;
        CounterStock stock;
        ProductionStation station;
        BurgerServingZone cashier;
        public void Bind(RestaurantWallet wallet, GrillUpgradeZone upgrade, SessionGoalTracker goals,
            BurgerInventory carrier=null, CounterStock counter=null, ProductionStation source=null, BurgerServingZone serving=null)
        { grill=upgrade;tracker=goals;inventory=carrier;stock=counter;station=source;cashier=serving; }
        public bool HasPaidUpgrade=>grill!=null&&grill.Level>1;
        public bool IsActive=>tracker!=null&&tracker.Rank==1;
        public Step Current=>!IsActive?Step.Done:tracker.CanUpgrade?Step.RankUp:
            stock!=null&&stock.Count>0?Step.Serve:inventory!=null&&inventory.Count>0?Step.Stock:Step.Pickup;
        public Vector3 Target=>Current==Step.Pickup?(station!=null&&station.GetComponent<BurgerPickupZone>()!=null?station.GetComponent<BurgerPickupZone>().PickupPosition:ShopLayout.GrillPickup):
            Current==Step.Stock?(cashier!=null&&cashier.DropZone!=null?cashier.DropZone.DropPosition:ShopLayout.CounterTop):
            cashier!=null?cashier.ServingPosition:ShopLayout.ServingCircle;
        public string Title=>Current==Step.RankUp?"First order complete · Upgrade to unlock dining":
            Current==Step.Pickup?"Sell your first burger · Pick up a burger":
            Current==Step.Stock?"Put the burger on the counter":
            Current==Step.Serve?"Stand at the register · Serve your first order":tracker?.Title??"Sell your first burger";
        public int Progress=>tracker!=null&&tracker.FirstOrderComplete?1:0;
        public int Required=>1;
    }
}
