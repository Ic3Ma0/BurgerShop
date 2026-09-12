using System.Collections;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
namespace BurgerShop.Tests.EditMode
{
    public sealed class SideWingIntegrationTests : SaveIsolatedGameplayTest
    {
        [UnityTest]
        public IEnumerator FreshSceneBuysLandThenBarThenMachineAndRestoresEverything()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            var shop=Object.FindFirstObjectByType<ShopExpansion>();
            var wallet=Object.FindFirstObjectByType<RestaurantWallet>();
            var player=Object.FindFirstObjectByType<PlayerMotor>(); player.enabled=false;
            Object.FindFirstObjectByType<CourierLine>().SetPaused(true);
            Assert.That(shop.HasWing || shop.HasColaMachine || shop.HasColaBar, Is.False);
            Assert.That(GameObject.Find("WingBackFloor"), Is.Null);
            wallet.RestoreProgress(1000,0);
            Pay(shop.WingPad,player.transform);
            Assert.That(shop.HasWing,Is.True);
            Assert.That(GameObject.Find("WingBackFloor"), Is.Not.Null);
            Assert.That(shop.HasColaMachine || shop.HasColaBar, Is.False);
            Pay(shop.ColaBarPad,player.transform); // Reverse order must also work.
            Assert.That(shop.HasColaBar,Is.True);
            Pay(shop.ColaPad,player.transform);
            Assert.That(shop.HasColaMachine,Is.True);
            Assert.That(wallet.Coins,Is.EqualTo(250));
            shop.ColaUpgrade.RestoreLevel(3);
            var parts=Object.FindFirstObjectByType<PartsWallet>(); parts.Restore(77);
            Assert.That(Object.FindFirstObjectByType<RestaurantPersistence>().Flush(),Is.True);
            yield return new ExitPlayMode();
            yield return new EnterPlayMode();
            shop=Object.FindFirstObjectByType<ShopExpansion>();
            Assert.That(shop.HasWing && shop.HasColaMachine && shop.HasColaBar,Is.True);
            Assert.That(shop.ColaLevel,Is.EqualTo(3));
            Assert.That(Object.FindFirstObjectByType<RestaurantWallet>().Coins,Is.EqualTo(250));
            Assert.That(Object.FindFirstObjectByType<PartsWallet>().Balance,Is.EqualTo(77));
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }
        static void Pay(FacilityUnlockZone pad,Transform player)
        {
            player.position=pad.transform.position+Vector3.up;
            for(int i=0;i<100&&!pad.IsPurchased;i++)pad.Advance(.1f);
            Assert.That(pad.IsPurchased,Is.True);
        }
        [Test]
        public void V12ColaAndPartsRightsMigrateWithoutRebuying()
        {
            var old=new RestaurantSaveData {version=12,grillLevel=1,coins=600,colaLevel=3,parts=41};
            var store=new LocalSaveStore(SaveDirectory);
            Assert.That(store.Save(old),Is.True);
            Assert.That(store.Load(out var loaded),Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(loaded.ResolvedBoughtSideWing && loaded.ResolvedBoughtColaMachine && loaded.ResolvedBoughtColaCounter,Is.True);
            Assert.That(loaded.ResolvedColaLevel,Is.EqualTo(3));
            Assert.That(loaded.ResolvedParts,Is.EqualTo(41));
            Assert.That(loaded.coins,Is.EqualTo(600));
        }
    }
}
