using System.Collections;
using System.Linq;
using BurgerShop.Building;
using BurgerShop.Core;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec083EconomySceneTests : SaveIsolatedGameplayTest
    {
        [TearDown] public void RestoreClock()=>Time.timeScale=1;
        [UnityTest] public IEnumerator QuotesWindowUpgradesAndSavedLevelsUseRealSceneOwners()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();for(int boot=0;boot<12;boot++)yield return null;
            Assert.That(FacilityLayout.Current,Is.Not.Null,"runtime layout booted");
            Time.timeScale=0;
            var layout=FacilityLayout.Current;var goals=layout.GetComponent<SessionGoalTracker>();
            var wallet=layout.Wallet;var growth=layout.GetComponent<GrowthUpgrades>();
            var hall=layout.GetComponent<MainHallExpansion>();
            // Explicitly funded fixture: verifies A, never used as natural earning-time evidence.
            wallet.RestoreProgress(5000,0);hall.Restore(true,true,MainHallExpansion.Cost);
            goals.Restore(7,0,0);goals.ApplyUnlocks();layout.Discover();
            var shop=layout.GetComponentInChildren<ShopExpansion>();
            var quote=new BusinessOpeningQuote(layout);
            Assert.That(shop.HasDriveThru,Is.True);Assert.That(wallet.Coins,Is.EqualTo(5000));Assert.That(goals.Stars,Is.Zero);
            Assert.That(quote.BlueBoxes,Is.EqualTo(150));Assert.That(quote.Cola,Is.EqualTo(450));Assert.That(quote.ColaWithWing,Is.EqualTo(750));
            shop.RestoreInvestments(0,0,0,0,0,cola:50);
            Assert.That(quote.Cola,Is.EqualTo(400));Assert.That(layout.Quote(FacilityKind.ColaMachine).Invested,Is.EqualTo(50));
            layout.BeginPurchase(FacilityKind.ColaMachine);
            Assert.That(layout.Confirm(new Vector3(-10,0,9),0),Is.True,layout.LastError);
            Assert.That(shop.HasWing,Is.False);Assert.That(shop.ColaInvested,Is.Zero,"credit consumed in owned space");
            Assert.That(wallet.Coins,Is.EqualTo(4850));Assert.That(goals.Stars,Is.EqualTo(2));
            Assert.That(quote.Cola,Is.EqualTo(250));Assert.That(goals.TeachingPrerequisite,Does.Not.Contain("Cola opening: land"));
            goals.Restore(10,0,0);goals.ApplyUnlocks();layout.Discover();
            Assert.That(quote.Bags,Is.EqualTo(750));
            var bags=layout.GetComponent<BagLine>();bags.MachinePad.RestoreInvestment(bags.MachinePad.Cost);layout.Discover();
            Assert.That(quote.Bags,Is.EqualTo(500));Assert.That(goals.Stars,Is.Zero,"restore/free opening never awards build stars");

            const string id="custom:08300000000000000000000000000001";
            layout.Restore(new[]{new FacilityPlacementRecord{id=id,kind=(int)FacilityKind.CarCounter,purchased=true,x=11,z=-26.3f,level=1}});
            var item=layout.Instances.Single(f=>f.Id==id);var lane=item.GetComponentInChildren<DriveThruLane>();
            Assert.That(growth.TryBuy(id,1),Is.True);Assert.That(wallet.Coins,Is.EqualTo(4750));Assert.That(goals.Stars,Is.EqualTo(2));
            Assert.That(lane.ServiceInterval,Is.EqualTo(.65f).Within(.0001f));
            Assert.That(growth.TryBuy(id,1),Is.False);Assert.That(wallet.Coins,Is.EqualTo(4750));
            Assert.That(growth.TryBuy(id,2),Is.True);Assert.That(wallet.Coins,Is.EqualTo(4550));Assert.That(goals.Stars,Is.EqualTo(4));
            Assert.That(item.Capture().level,Is.EqualTo(3));
            var persistence=layout.GetComponent<RestaurantPersistence>();Assert.That(persistence.Flush(),Is.True);
            Goal01Bootstrap.RequestInstalledShopRebuild();for(int i=0;i<30;i++)yield return null;
            layout=FacilityLayout.Current;item=layout.Instances.Single(f=>f.Id==id);
            Assert.That(item.GetComponentInChildren<DriveThruLane>().ServiceInterval,Is.EqualTo(.55f).Within(.0001f));
            Assert.That(layout.Wallet.Coins,Is.EqualTo(4550));Assert.That(layout.GetComponent<SessionGoalTracker>().Stars,Is.EqualTo(4));
            Time.timeScale=1;LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator PlacementRefreshesChangedQuoteBeforeChargingAndCancelIsFree()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();for(int boot=0;boot<12;boot++)yield return null;
            Assert.That(FacilityLayout.Current,Is.Not.Null,"runtime layout booted");
            Time.timeScale=0;var layout=FacilityLayout.Current;var goals=layout.GetComponent<SessionGoalTracker>();
            layout.GetComponent<MainHallExpansion>().Restore(true,true,150);goals.Restore(7,0,0);goals.ApplyUnlocks();layout.Discover();
            layout.Wallet.RestoreProgress(5000,0);
            var candidate=layout.BeginPurchase(FacilityKind.PairTable);Assert.That(candidate,Is.Not.Null);
            var position=new Vector3(-10,0,9);
            Assert.That(layout.CanPlace(candidate,position,0,true),Is.True,layout.LastError);
            layout.Restore(new[]{new FacilityPlacementRecord{id="custom:08300000000000000000000000000002",kind=(int)FacilityKind.PairTable,purchased=true,x=7,z=9,level=1}});
            int due=layout.Price(FacilityKind.PairTable);
            Assert.That(layout.Confirm(position,0),Is.False);Assert.That(layout.LastError,Does.Contain("Price updated"));
            Assert.That(layout.Wallet.Coins,Is.EqualTo(5000));Assert.That(goals.Stars,Is.Zero);
            Assert.That(layout.PendingQuote.Due,Is.EqualTo(due));
            Assert.That(layout.Confirm(position,0),Is.True,layout.LastError);
            Assert.That(layout.Wallet.Coins,Is.EqualTo(5000-due));Assert.That(goals.Stars,Is.EqualTo(2));
            Assert.That(layout.Confirm(position,0),Is.False);Assert.That(layout.Wallet.Coins,Is.EqualTo(5000-due));
            layout.BeginPurchase(FacilityKind.PairTable);layout.Cancel();
            Assert.That(layout.Wallet.Coins,Is.EqualTo(5000-due));Assert.That(goals.Stars,Is.EqualTo(2));
            var placed=layout.Instances.Single(f=>Vector3.Distance(f.transform.position,position)<.01f);
            Assert.That(layout.BeginMove(placed),Is.True);Assert.That(layout.Confirm(position,0),Is.True);
            Assert.That(layout.Wallet.Coins,Is.EqualTo(5000-due));Assert.That(goals.Stars,Is.EqualTo(2));
            Time.timeScale=1;LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }
    }
}
