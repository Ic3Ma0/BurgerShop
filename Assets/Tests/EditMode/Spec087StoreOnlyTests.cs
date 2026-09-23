using System.Collections;
using BurgerShop.Building;
using BurgerShop.Core;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
namespace BurgerShop.Tests.EditMode
{
 public sealed class Spec087StoreOnlyTests : SaveIsolatedGameplayTest
 {
  [TearDown] public void RestoreClock()=>Time.timeScale=1;
  static void AssertNoPurchaseCircles(FacilityLayout layout)
  {
   var pads=layout.GetComponentsInChildren<FacilityUnlockZone>(true);
   Assert.That(pads.Length,Is.GreaterThan(0));
   foreach(var pad in pads)
   {
    int invested=pad.Invested;long cash=layout.Wallet.Coins;
    pad.SetRankVisible(true);pad.RestoreInvestment(invested);
    Assert.That(pad.IsAvailable,Is.False,pad.Title);
    Assert.That(pad.RankVisible,Is.False,pad.Title);
    foreach(var renderer in pad.GetComponentsInChildren<Renderer>(true))
     Assert.That(renderer.enabled && renderer.gameObject.activeInHierarchy,Is.False,pad.Title);
    if(pad.Player!=null)pad.Player.transform.position=pad.PadPosition+Vector3.up;
    pad.Advance(10);
    Assert.That(layout.Wallet.Coins,Is.EqualTo(cash),pad.Title);
    Assert.That(pad.Invested,Is.EqualTo(invested),pad.Title);
   }
   Assert.That(GameObject.Find("WingBuildGuide"),Is.Null);
  }
  [UnityTest] public IEnumerator HiddenPadsPreserveCreditAndStorePurchaseSurvivesReload()
  {
   EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
   for(int i=0;i<12;i++)yield return null;
   Time.timeScale=0;
   var layout=FacilityLayout.Current;var goals=layout.GetComponent<BurgerShop.UI.SessionGoalTracker>();
   layout.GetComponent<MainHallExpansion>().Restore(true,true,150);
   goals.Restore(7,0,0);goals.ApplyUnlocks();layout.Wallet.RestoreProgress(1000,0);
   var expansion=layout.GetComponentInChildren<ShopExpansion>();
   expansion.RestoreInvestments(0,0,0,0,0,wing:108,cola:50);
   AssertNoPurchaseCircles(layout);
   Assert.That(expansion.WingPad.Remaining,Is.EqualTo(192));
   var shop=Object.FindFirstObjectByType<FacilityShopHud>();shop.Open();shop.SendMessage("PurchaseExpansion",(object)0);shop.Close();
   Assert.That(expansion.HasWing,Is.True);Assert.That(layout.Wallet.Coins,Is.EqualTo(808));
   Assert.That(goals.Stars,Is.EqualTo(2));
   Assert.That(layout.Quote(FacilityKind.ColaMachine).Invested,Is.EqualTo(50));
   Assert.That(expansion.TryPurchaseWing(),Is.False);Assert.That(layout.Wallet.Coins,Is.EqualTo(808));
   AssertNoPurchaseCircles(layout);
   goals.Restore(10,0,0);goals.ApplyUnlocks();
   shop.Open();shop.SendMessage("PurchaseExpansion",(object)2);shop.Close();
   Assert.That(layout.GetComponent<BagLine>().Expanded,Is.True);AssertNoPurchaseCircles(layout);
   Assert.That(layout.GetComponent<RestaurantPersistence>().Flush(),Is.True);
   Goal01Bootstrap.RequestInstalledShopRebuild();for(int i=0;i<30;i++)yield return null;
   layout=FacilityLayout.Current;AssertNoPurchaseCircles(layout);
   Assert.That(layout.GetComponentInChildren<ShopExpansion>().HasWing,Is.True);
   Assert.That(layout.Quote(FacilityKind.ColaMachine).Invested,Is.EqualTo(50));
   Assert.That(layout.Wallet.Coins,Is.EqualTo(808));
   Time.timeScale=1;LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
  }
 }
}
