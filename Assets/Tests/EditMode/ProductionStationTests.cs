using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class ProductionStationTests
    {
        GameObject root;
        ProductionStation station;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("StationTest");
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            station = root.AddComponent<ProductionStation>();
            station.Configure(output, null, null, 2f, 2);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ProducesAtConfiguredIntervalUntilCapacity()
        {
            station.Advance(1f);
            Assert.That(station.Stock, Is.Zero);
            Assert.That(station.NormalizedProgress, Is.EqualTo(0.5f).Within(0.001f));

            station.Advance(5f);
            Assert.That(station.Stock, Is.EqualTo(2));
            Assert.That(station.Capacity, Is.EqualTo(2));
        }

        [Test]
        public void TakingBurgerReopensProduction()
        {
            station.Advance(4f);
            Assert.That(station.TryTakeBurger(), Is.True);
            Assert.That(station.Stock, Is.EqualTo(1));

            station.Advance(2f);
            Assert.That(station.Stock, Is.EqualTo(2));
        }

        [Test]
        public void RaisingCapacityResumesProductionWithoutLosingStock()
        {
            station.Advance(10f);
            Assert.That(station.Stock, Is.EqualTo(2));
            station.SetCapacity(6);
            Assert.That(station.Capacity, Is.EqualTo(6));
            Assert.That(station.Stock, Is.EqualTo(2));
            station.Advance(8f);
            Assert.That(station.Stock, Is.EqualTo(6));
        }

        [Test]
        public void EmptyStationCannotProvideBurger()
        {
            Assert.That(station.TryTakeBurger(), Is.False);
            Assert.That(station.Stock, Is.Zero);
        }
    }
}
