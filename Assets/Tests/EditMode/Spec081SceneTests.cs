using System.Collections;
using System.IO;
using BurgerShop.Building;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec081SceneTests : SaveIsolatedGameplayTest
    {
        [UnityTest] public IEnumerator NewSlotBootsTradesAndExpandsWithoutMovingStarterEquipment()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();yield return null;
            var hall=MainHallExpansion.Current;var goals=Object.FindFirstObjectByType<SessionGoalTracker>();
            var wallet=goals.GetComponent<RestaurantWallet>();var persistence=goals.GetComponent<RestaurantPersistence>();
            var player=Object.FindFirstObjectByType<PlayerMotor>();var inventory=player.GetComponent<BurgerInventory>();
            Assert.That(persistence.Phase,Is.EqualTo(SaveSessionPhase.Running));
            Assert.That(hall.CompactStart,Is.True);Assert.That(hall.Built,Is.False);Assert.That(wallet.Coins,Is.Zero);
            Assert.That(goals.GetComponent<FacilityLayout>().Floors()[0],Is.EqualTo(hall.UsableBounds));
            Capture("/tmp/bs081-start.png");
            var queue=MainKitchen<CustomerQueue>();var serving=MainKitchen<BurgerServingZone>();var station=MainKitchen<ProductionStation>();
            player.enabled=false;player.GetComponent<CharacterController>().enabled=false;
            queue.SendMessage("OnApplicationFocus",true);
            for(int i=0;i<3600&&queue.ReadyCustomer==null;i++)queue.Advance(1f/60f);
            Assert.That(queue.ReadyCustomer,Is.Not.Null,$"count={queue.Count}, front={queue.FrontCustomer?.transform.position}");Assert.That(queue.ReadyCustomer.OrderSize,Is.EqualTo(1));
            station.Advance(3);Assert.That(inventory.TryCollectFrom(station),Is.True);
            player.transform.position=serving.ServingPosition+Vector3.up;
            serving.Advance(.01f);serving.Advance(BurgerServingZone.HandoffDuration);
            Assert.That(wallet.CompletedSales,Is.EqualTo(1));Assert.That(wallet.Coins+goals.GetComponent<CashFloor>().GroundValue,Is.EqualTo(10));Assert.That(goals.Stars,Is.EqualTo(2));
            Assert.That(goals.FirstOrderComplete,Is.True);Assert.That(goals.TryUpgradeRank(1),Is.True);
            var table=goals.GetComponentInChildren<DiningTable>();var trash=player.GetComponent<TrashInventory>();
            table.LeaveMealTrash(0);Assert.That(table.TryPickupTrash(trash),Is.True);
            while(table.TrashCount>0)Assert.That(table.TryPickupTrash(trash),Is.True);
            Assert.That(goals.Stars,Is.EqualTo(2));Assert.That(goals.Investments.Current.Cost,Is.EqualTo(30));
            wallet.CollectCoins(30);Assert.That(MainKitchen<GrillUpgradeZone>().TryUpgrade(1),Is.True);
            Assert.That(goals.TryUpgradeRank(2),Is.True);Assert.That(hall.Built,Is.False);
            Vector3 original=station.transform.position;
            wallet.CollectCoins(150);Assert.That(hall.TryContribute(),Is.True);yield return null;
            Assert.That(station.transform.position,Is.EqualTo(original));Assert.That(goals.Allows(3),Is.True);
            Assert.That(goals.Stars,Is.EqualTo(2));
            Capture("/tmp/bs081-expanded.png");
            persistence.Flush();Assert.That(RestaurantPersistence.PeekActiveSnapshot().mainHallBuilt,Is.True);
            BurgerShop.Core.Goal01Bootstrap.RequestInstalledShopRebuild();
            for(int i=0;i<30;i++)yield return null;
            hall=MainHallExpansion.Current;goals=Object.FindFirstObjectByType<SessionGoalTracker>();
            Assert.That(hall.Built,Is.True);Assert.That(hall.CompactStart,Is.True);Assert.That(goals.Stars,Is.EqualTo(2));
            Assert.That(MainKitchen<ProductionStation>().transform.position,Is.EqualTo(original));
            persistence=goals.GetComponent<RestaurantPersistence>();int oldSlot=persistence.ActiveSlotId;
            Assert.That(persistence.StartNewGame(),Is.True);
            for(int i=0;i<30;i++)yield return null;
            hall=MainHallExpansion.Current;goals=Object.FindFirstObjectByType<SessionGoalTracker>();
            Assert.That(hall.Built,Is.False);Assert.That(hall.Invested,Is.Zero);Assert.That(goals.Rank,Is.EqualTo(1));
            Assert.That(goals.FirstOrderComplete,Is.False);Assert.That(goals.Stars,Is.Zero);
            persistence=goals.GetComponent<RestaurantPersistence>();Assert.That(persistence.SwitchToSlot(oldSlot),Is.True);
            for(int i=0;i<30;i++)yield return null;
            hall=MainHallExpansion.Current;goals=Object.FindFirstObjectByType<SessionGoalTracker>();
            Assert.That(hall.Built,Is.True);Assert.That(goals.Rank,Is.EqualTo(3));Assert.That(goals.Stars,Is.EqualTo(2));
            Assert.That(goals.FirstOrderComplete,Is.True);
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }
        static void Capture(string path)
        {
            var camera=Camera.main;var old=camera.targetTexture;var active=RenderTexture.active;
            var rt=new RenderTexture(1280,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(1280,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,900),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
            camera.targetTexture=old;RenderTexture.active=active;Object.Destroy(texture);Object.Destroy(rt);
        }
        [UnityTearDown] public IEnumerator ExitIfFailed(){if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
