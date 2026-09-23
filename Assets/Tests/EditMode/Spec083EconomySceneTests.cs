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
using UnityEngine.UI;

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
            VerifyPlacementTransactions();
            Time.timeScale=1;LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }

        static void VerifyPlacementTransactions()
        {
            Time.timeScale=0;
            var layout=FacilityLayout.Current;
            Assert.That(layout != null, Is.True, "live layout");
            var goals=layout.GetComponent<SessionGoalTracker>();
            Assert.That(goals != null, Is.True, "goals");
            Assert.That(layout.GetComponent<MainHallExpansion>() != null, Is.True, "hall");
            layout.GetComponent<MainHallExpansion>().Restore(true,true,150);goals.Restore(7,0,0);goals.ApplyUnlocks();layout.Discover();
            layout.Wallet.RestoreProgress(5000,0);
            var candidate=layout.BeginPurchase(FacilityKind.PairTable);Assert.That(candidate != null,Is.True,"live candidate: "+layout.LastError);
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

        }

        [UnityTest] public IEnumerator CatalogQuotesFitPhoneAndLandscapeWithoutCoveringPurchaseControls()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var layout=FacilityLayout.Current;
            layout.GetComponent<MainHallExpansion>().Restore(true,true,MainHallExpansion.Cost);
            layout.Wallet.RestoreProgress(0,0);layout.Wallet.CollectCoins(5000);
            var goals=layout.GetComponent<SessionGoalTracker>();goals.Restore(7,0,0);goals.ApplyUnlocks();layout.Discover();
            // This fixture is funded for UI checks, not used in the natural-income trace.
            Object.FindFirstObjectByType<SalesHud>().Advance(2f);
            var shop=Object.FindFirstObjectByType<FacilityShopHud>();shop.Open();
            var camera=Camera.main;var canvas=shop.GetComponentInParent<Canvas>();
            var cameraData=camera.GetComponents<Component>().First(c=>c.GetType().Name=="UniversalAdditionalCameraData");
            var serialized=new UnityEditor.SerializedObject(cameraData);
            serialized.FindProperty("m_RenderPostProcessing").boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(1600,1000)})
            {
                var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
                yield return null;Canvas.ForceUpdateCanvases();shop.SendMessage("FitCatalog");Canvas.ForceUpdateCanvases();
                foreach(var label in shop.GetComponentsInChildren<Text>().Where(t=>t.name=="OpeningQuote"&&t.text.Length>0))
                {
                    Assert.That(label.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height),label.transform.parent.name);
                    var card=(RectTransform)label.transform.parent;
                    var quoteBounds=HudChrome.LocalRect(label.rectTransform,card);
                    var priceBounds=HudChrome.LocalRect((RectTransform)card.Find("Detail"),card);
                    Assert.That(quoteBounds.yMin,Is.GreaterThan(priceBounds.yMax),"quote must not cover price or Buy");
                }
                var cola=(RectTransform)GameObject.Find("Buy_ColaMachine").transform;
                Assert.That(cola.Find("OpeningQuote").GetComponent<Text>().text,Does.Contain("450"));
                Assert.That(cola.Find("OpeningQuote").GetComponent<Text>().text,Does.Contain("750"));
                var scroll=shop.GetComponentInChildren<ScrollRect>();
                scroll.content.anchoredPosition=new Vector2(0,-cola.anchoredPosition.y);
                Canvas.ForceUpdateCanvases();camera.Render();
                var previous=RenderTexture.active;RenderTexture.active=target;
                var pixels=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                System.IO.File.WriteAllBytes("/tmp/bs083-quotes-"+size.x+".png",pixels.EncodeToPNG());
                RenderTexture.active=previous;camera.targetTexture=null;Object.Destroy(pixels);Object.Destroy(target);
            }
            shop.Close();LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }
    }
}
