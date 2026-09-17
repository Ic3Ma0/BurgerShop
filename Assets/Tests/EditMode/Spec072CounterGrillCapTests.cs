using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec072CounterGrillCapTests
    {
        GameObject root;

        [SetUp]
        public void SetUp() => root = new GameObject("Spec072");

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void CounterStacksTwentyBurgersInOneRisingColumn()
        {
            Transform anchor = new GameObject("StockAnchor").transform;
            anchor.SetParent(root.transform, false);
            CounterStock stock = root.AddComponent<CounterStock>();
            stock.Configure(anchor, null);

            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform, false);
            ProductionStation station = root.AddComponent<ProductionStation>();
            station.Configure(output, null, null, 0.1f, 24);

            GameObject player = new GameObject("Player");
            player.transform.SetParent(root.transform, false);
            BurgerInventory inventory = player.AddComponent<BurgerInventory>();
            inventory.Configure();

            const int piled = 20;
            while (stock.Count < piled)
            {
                if (station.Stock == 0)
                    station.Advance(10f);
                Assert.That(inventory.TryCollectFrom(station), Is.True);
                Assert.That(stock.TryPlaceFrom(inventory), Is.True);
            }

            Assert.That(stock.Count, Is.EqualTo(piled));
            Assert.That(stock.IsFull, Is.False);
            Assert.That(anchor.childCount, Is.EqualTo(piled));
            for (int i = 0; i < piled; i++)
            {
                Vector3 local = anchor.GetChild(i).localPosition;
                Vector3 slot = CounterStock.SlotLocal(i);
                Assert.That(local.x, Is.EqualTo(slot.x).Within(0.001f));
                Assert.That(local.y, Is.EqualTo(slot.y).Within(0.001f));
                Assert.That(local.z, Is.EqualTo(slot.z).Within(0.001f));
                if (i == 0) continue;
                Vector3 previous = anchor.GetChild(i - 1).localPosition;
                Assert.That(local.y, Is.GreaterThan(previous.y));
                Assert.That(local.x, Is.EqualTo(previous.x).Within(0.001f));
                Assert.That(local.z, Is.EqualTo(previous.z).Within(0.001f));
            }
        }

        [Test]
        public void BurgerReadyCapsAreFiveEightTwelveAndColaKeepsFourSixEight()
        {
            Assert.That(ProductionStation.BurgerLevelCaps, Is.EqualTo(new[] { 5, 8, 12 }));
            Assert.That(ProductionStation.CapacityForLevel(1), Is.EqualTo(5));
            Assert.That(ProductionStation.CapacityForLevel(2), Is.EqualTo(8));
            Assert.That(ProductionStation.CapacityForLevel(3), Is.EqualTo(12));
            Assert.That(ProductionStation.ColaLevelCaps, Is.EqualTo(new[] { 4, 6, 8 }));
            Assert.That(ProductionStation.CapacityForLevel(1, KitchenProduct.Cola), Is.EqualTo(4));
            Assert.That(ProductionStation.CapacityForLevel(2, KitchenProduct.Cola), Is.EqualTo(6));
            Assert.That(ProductionStation.CapacityForLevel(3, KitchenProduct.Cola), Is.EqualTo(8));
        }

        [Test]
        public void BurgerGrillStopsAtLevelCapAndUpgradeRaisesReadyPile()
        {
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform, false);
            ProductionStation station = root.AddComponent<ProductionStation>();
            station.Configure(output, null, null, 1f, ProductionStation.CapacityForLevel(1));

            station.Advance(20f);
            Assert.That(station.Stock, Is.EqualTo(5));
            Assert.That(output.childCount, Is.EqualTo(5));
            station.Advance(20f);
            Assert.That(station.Stock, Is.EqualTo(5));

            station.SetCapacity(ProductionStation.CapacityForLevel(2));
            station.Advance(20f);
            Assert.That(station.Stock, Is.EqualTo(8));
            station.Advance(20f);
            Assert.That(station.Stock, Is.EqualTo(8));

            station.SetCapacity(ProductionStation.CapacityForLevel(3));
            station.Advance(20f);
            Assert.That(station.Stock, Is.EqualTo(12));
            station.Advance(20f);
            Assert.That(station.Stock, Is.EqualTo(12));
            Assert.That(output.childCount, Is.EqualTo(12));
        }

        [Test]
        public void TakingFromAFullLevelOneGrillAllowsOneMoreCook()
        {
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform, false);
            ProductionStation station = root.AddComponent<ProductionStation>();
            station.Configure(output, null, null, 1f, ProductionStation.CapacityForLevel(1));
            station.Advance(20f);
            Assert.That(station.Stock, Is.EqualTo(5));

            Assert.That(station.TryTakeBurger(), Is.True);
            Assert.That(station.Stock, Is.EqualTo(4));
            station.Advance(0.99f);
            Assert.That(station.Stock, Is.EqualTo(4));
            station.Advance(0.02f);
            Assert.That(station.Stock, Is.EqualTo(5));
            station.Advance(20f);
            Assert.That(station.Stock, Is.EqualTo(5));
        }

        [Test]
        public void ExtraGrillKeepsAnIndependentReadyCap()
        {
            BurgerInventory player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform, false);
            player.Configure();
            RestaurantWallet wallet = root.AddComponent<RestaurantWallet>();
            ExpandableGrill starter = ExpandableGrill.CreateStarter(root.transform, player, wallet);
            ExpandableGrill extra = ExpandableGrill.Create(root.transform, ShopLayout.ExtraGrill, player, wallet);

            starter.Station.Advance(100f);
            extra.Station.Advance(100f);
            Assert.That(starter.Station.Stock, Is.EqualTo(5));
            Assert.That(extra.Station.Stock, Is.EqualTo(5));

            extra.Upgrade.RestoreLevel(3);
            extra.Station.Advance(100f);
            Assert.That(starter.Station.Stock, Is.EqualTo(5));
            Assert.That(starter.Station.Capacity, Is.EqualTo(5));
            Assert.That(extra.Station.Stock, Is.EqualTo(12));
            Assert.That(extra.Station.Capacity, Is.EqualTo(12));
        }
    }
}
