using System.Collections;
using BurgerShop.Customer;
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
    public sealed class Spec024GameplayTests : SaveIsolatedGameplayTest
    {
        [UnityTest]
        public IEnumerator SceneShortagesCompleteAtLastItemAndRestartKeepsOnlyPermanentProgress()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            // Keep legacy kitchen scenarios independent of the autonomous courier economy.
            Object.FindFirstObjectByType<CourierLine>()?.SetPaused(true);
            Time.captureDeltaTime = 1f / 60;
            var player = Object.FindFirstObjectByType<PlayerMotor>(); player.enabled = false;
            var inventory = player.GetComponent<BurgerInventory>();
            var wallet = Object.FindFirstObjectByType<RestaurantWallet>();
            var queue = MainKitchen<CustomerQueue>(); queue.OrderQuantityFactory = () => 3;
            var serving = MainKitchen<BurgerServingZone>();
            var grill = MainKitchen<ProductionStation>();
            var upgrade = MainKitchen<GrillUpgradeZone>();
            var crew = Object.FindFirstObjectByType<WorkerHiringZone>();
            var expansion = Object.FindFirstObjectByType<ShopExpansion>();
            var persistence = Object.FindFirstObjectByType<RestaurantPersistence>();
            wallet.RestoreProgress(82, 4); upgrade.RestoreLevel(2);
            crew.RestoreWorkers(1, 2, 3); crew.Worker.enabled = false;
            expansion.Restore(true, false, false, 0, true, false);
            for (int frame = 0; frame < 1200 && queue.ReadyCustomer == null; frame++) yield return null;
            var customer = queue.ReadyCustomer;
            Assert.That(customer, Is.Not.Null);
            for (int i = 0; i < 2; i++)
            {
                Assert.That(inventory.TryCollectFrom(grill), Is.True);
                Assert.That(serving.Stock.TryPlaceFrom(inventory), Is.True);
            }
            player.transform.position = serving.ServingPosition + Vector3.up;
            for (int frame = 0; frame < 240 && customer.RemainingQuantity > 1; frame++) yield return null;
            Assert.That(customer.RemainingQuantity, Is.EqualTo(1));
            Assert.That(wallet.CompletedSales, Is.EqualTo(4));
            Assert.That(serving.Cash.GroundValue, Is.Zero);
            Assert.That(persistence.Flush(), Is.True);
            yield return new ExitPlayMode();
            yield return new EnterPlayMode();
            // Keep legacy kitchen scenarios independent of the autonomous courier economy.
            Object.FindFirstObjectByType<CourierLine>()?.SetPaused(true);
            Time.captureDeltaTime = 1f / 60;
            player = Object.FindFirstObjectByType<PlayerMotor>(); player.enabled = false;
            inventory = player.GetComponent<BurgerInventory>();
            wallet = Object.FindFirstObjectByType<RestaurantWallet>();
            queue = MainKitchen<CustomerQueue>(); queue.OrderQuantityFactory = () => 3;
            serving = MainKitchen<BurgerServingZone>();
            grill = MainKitchen<ProductionStation>();
            upgrade = MainKitchen<GrillUpgradeZone>();
            crew = Object.FindFirstObjectByType<WorkerHiringZone>(); crew.Worker.enabled = false;
            expansion = Object.FindFirstObjectByType<ShopExpansion>();
            Assert.That(wallet.Coins, Is.EqualTo(82));
            Assert.That(wallet.CompletedSales, Is.EqualTo(4));
            Assert.That(upgrade.Level, Is.EqualTo(2));
            Assert.That(crew.TotalDeliveries, Is.EqualTo(2));
            Assert.That(crew.TotalClears, Is.EqualTo(3));
            Assert.That(expansion.HasExtraTable && expansion.HasBoxing && !expansion.HasDriveThru, Is.True);
            Assert.That(inventory.Count + serving.TotalStock + expansion.Boxing.PackageCount, Is.Zero);
            Assert.That(serving.Cash.GroundValue, Is.Zero);
            for (int frame = 0; frame < 1200 && queue.ReadyCustomer == null; frame++) yield return null;
            customer = queue.ReadyCustomer;
            Assert.That(customer.OrderSize, Is.EqualTo(3));
            Assert.That(customer.Order.Delivered, Is.Zero);
            for (int i = 0; i < 2; i++) { inventory.TryCollectFrom(grill); serving.Stock.TryPlaceFrom(inventory); }
            player.transform.position = serving.ServingPosition + Vector3.up;
            for (int frame = 0; frame < 240 && customer.RemainingQuantity > 1; frame++) yield return null;
            Assert.That(customer.RemainingQuantity, Is.EqualTo(1));
            for (int frame = 0; frame < 20; frame++) yield return null;
            GameplayEvidence.Capture("spec024-dining-shortage-editor.png");
            inventory.TryCollectFrom(grill); serving.Stock.TryPlaceFrom(inventory);
            for (int frame = 0; frame < 120 && wallet.CompletedSales < 5; frame++) yield return null;
            Assert.That(wallet.CompletedSales, Is.EqualTo(5));
            Assert.That(serving.Cash.GroundValue, Is.EqualTo(30));
            Assert.That(customer.PaidAmount, Is.EqualTo(30));
            GameplayEvidence.Capture("spec024-dining-complete-editor.png");
            expansion.Restore(true, false, false, 0, true, true);
            var lane = expansion.DriveThru;
            lane.OrderQuantityFactory = () => 2;
            for (int frame = 0; frame < 600 && !lane.HasStoppedCarAtWindow; frame++) yield return null;
            var carOrder = lane.WaitingOrder;
            Assert.That(carOrder.Quantity, Is.EqualTo(2));
            inventory.TryCollectFrom(grill); inventory.TryBoxOne(); expansion.Boxing.Package.TryPlaceBoxedFrom(inventory);
            player.transform.position = lane.WindowPosition + Vector3.up;
            for (int frame = 0; frame < 240 && carOrder.Remaining > 1; frame++) yield return null;
            Assert.That(carOrder.Remaining, Is.EqualTo(1));
            Assert.That(lane.CompletedOrders, Is.Zero);
            for (int frame = 0; frame < 25; frame++) yield return null;
            GameplayEvidence.Capture("spec024-drive-shortage-editor.png");
            inventory.TryCollectFrom(grill); inventory.TryBoxOne(); expansion.Boxing.Package.TryPlaceBoxedFrom(inventory);
            for (int frame = 0; frame < 120 && !carOrder.IsSettled; frame++) yield return null;
            Assert.That(carOrder.IsSettled, Is.True);
            Assert.That(wallet.CompletedSales, Is.EqualTo(6));
            Assert.That(lane.CompletedOrders, Is.EqualTo(1));
            bool fullCash = false;
            foreach (var pile in serving.Cash.GetComponentsInChildren<CashPickup>())
                if (ShopLayout.Horizontal(pile.transform.position, lane.CashPosition) < 0.5f && pile.Value == 30) fullCash = true;
            Assert.That(fullCash, Is.True);
            Assert.That(wallet.Coins, Is.EqualTo(82));
            GameplayEvidence.Capture("spec024-drive-complete-editor.png");
            LogAssert.NoUnexpectedReceived();
            Time.captureDeltaTime = 0;
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator LeavePlayMode()
        {
            if (Application.isPlaying) { Time.captureDeltaTime = 0; yield return new ExitPlayMode(); }
        }
    }
}
