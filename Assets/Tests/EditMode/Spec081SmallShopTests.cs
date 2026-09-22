using BurgerShop.Building;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;
namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec081SmallShopTests
    {
        GameObject root;MainHallExpansion hall;SessionGoalTracker goals;RestaurantWallet wallet;
        [SetUp] public void SetUp()
        {
            root=new GameObject("Spec081");wallet=root.AddComponent<RestaurantWallet>();goals=root.AddComponent<SessionGoalTracker>();
            goals.Configure(null,null,null,null,wallet,null);hall=root.AddComponent<MainHallExpansion>();ShopLayout.CompactStart=true;hall.Initialize(true,false,0);
        }
        [TearDown] public void TearDown(){Object.DestroyImmediate(root);ShopLayout.CompactStart=false;}
        [Test] public void StarterWorkingPointsAndFloorShareOwnedBounds()
        {
            Assert.That(hall.Bounds.width*hall.Bounds.height,Is.LessThanOrEqualTo(900*.55));
            foreach(var p in new[]{ShopLayout.Grill,ShopLayout.GrillPickup,ShopLayout.UpgradeSpot,ShopLayout.Counter,ShopLayout.ServingCircle,ShopLayout.CounterCash,ShopLayout.TrashBin,ShopLayout.BoostPoint,ShopLayout.PlayerSpawn})
                Assert.That(ShopLayout.ContainsHall(p),Is.True,p.ToString());
            foreach(var p in ShopLayout.Tables)Assert.That(ShopLayout.ContainsHall(p),Is.True);
            Assert.That(ShopLayout.ContainsPlayable(new Vector3(12,0,10)),Is.False);
            var layout=root.AddComponent<FacilityLayout>();Assert.That(layout.Floors()[0],Is.EqualTo(hall.UsableBounds));
            Physics.SyncTransforms();Assert.That(Physics.CheckBox(new Vector3(11,.5f,0),Vector3.one*.2f),Is.True);
        }
        [Test] public void PartialPaymentOnlyAtThreeOpensOnceAndKeepsPositions()
        {
            wallet.RestoreProgress(100,0);goals.Restore(2,0,0);Assert.That(hall.TryContribute(),Is.False);Assert.That(wallet.Coins,Is.EqualTo(100));
            goals.Restore(3,0,0);Assert.That(goals.Allows(3),Is.False);Assert.That(hall.TryContribute(),Is.True);
            Assert.That(hall.Remaining,Is.EqualTo(50));Assert.That(hall.Built,Is.False);Assert.That(goals.Stars,Is.Zero);
            Vector3 grill=ShopLayout.Grill;wallet.CollectCoins(60);Assert.That(hall.TryContribute(),Is.True);
            Assert.That(wallet.Coins,Is.EqualTo(10));Assert.That(goals.Stars,Is.EqualTo(2));Assert.That(goals.Allows(3),Is.True);
            Assert.That(hall.Bounds,Is.EqualTo(MainHallExpansion.FullBounds));Assert.That(ShopLayout.Grill,Is.EqualTo(grill));
            Assert.That(hall.TryContribute(),Is.False);Assert.That(goals.Stars,Is.EqualTo(2));
        }
        [Test] public void RestoreNeverAwardsAndPartialInvestmentIsPreserved()
        {
            hall.Restore(true,false,100);Assert.That(hall.Remaining,Is.EqualTo(50));Assert.That(goals.Stars,Is.Zero);
            hall.Restore(true,true,150);hall.Restore(true,true,150);Assert.That(goals.Stars,Is.Zero);Assert.That(hall.Built,Is.True);
        }
        [TestCase(1)][TestCase(14)][TestCase(15)][TestCase(16)][TestCase(17)]
        public void EveryOlderSchemaRetainsFullHall(int version)
        {
            var data=new RestaurantSaveData{version=version};Assert.That(data.ResolvedMainHallBuilt,Is.True);
            hall.Restore(false,data.ResolvedMainHallBuilt,0);Assert.That(hall.Bounds,Is.EqualTo(MainHallExpansion.FullBounds));Assert.That(goals.Stars,Is.Zero);
        }
        [Test] public void NewFieldsAreCoveredByChecksum()
        {
            var data=new RestaurantSaveData{version=18,compactStart=true,mainHallInvestment=100};string before=data.Checksum();
            data.mainHallInvestment=101;Assert.That(data.Checksum(),Is.Not.EqualTo(before));before=data.Checksum();
            data.mainHallBuilt=true;Assert.That(data.Checksum(),Is.Not.EqualTo(before));before=data.Checksum();
            data.firstOrderComplete=true;Assert.That(data.Checksum(),Is.Not.EqualTo(before));
        }
    }
}
