using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class GrillPresentationTests
    {
        GameObject root;
        BurgerInventory player;
        RestaurantWallet wallet;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("GrillPresentation");
            player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform);
            player.Configure();
            wallet = root.AddComponent<RestaurantWallet>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void StarterAndExtraUseTheSameLookKitAtEachLevel()
        {
            ExpandableGrill starter = ExpandableGrill.CreateStarter(root.transform, player, wallet);
            ExpandableGrill extra = ExpandableGrill.Create(root.transform, ShopLayout.ExtraGrill, player, wallet);
            for (int level = 1; level <= 3; level++)
            {
                starter.Upgrade.RestoreLevel(level);
                extra.Upgrade.RestoreLevel(level);
                Assert.That(starter.ActiveLookName, Is.EqualTo("Look_Lv" + level));
                Assert.That(extra.ActiveLookName, Is.EqualTo(starter.ActiveLookName));
                Assert.That(starter.ActiveLookScale, Is.EqualTo(ExpandableGrill.LookScales[level - 1]));
                Assert.That(extra.ActiveLookScale, Is.EqualTo(starter.ActiveLookScale));
                Assert.That(starter.ActivePartCount, Is.EqualTo(extra.ActivePartCount));
                Assert.That(starter.Station.Capacity, Is.EqualTo(ProductionStation.CapacityForLevel(level)));
                Assert.That(extra.Station.Capacity, Is.EqualTo(starter.Station.Capacity));
            }
        }

        [Test]
        public void Lv3BurgersSitAboveTheBodyAndOnTheTray()
        {
            ExpandableGrill grill = ExpandableGrill.CreateStarter(root.transform, player, wallet);
            grill.Upgrade.RestoreLevel(3);
            grill.Station.Advance(100f);
            Assert.That(grill.Station.Stock, Is.EqualTo(ProductionStation.CapacityForLevel(3)));
            Transform body = grill.ActiveLook.Find("Body");
            Transform tray = grill.ActiveLook.Find("OutputTray");
            Transform burger = grill.OutputAnchor.GetChild(0);
            Assert.That(body, Is.Not.Null);
            Assert.That(tray, Is.Not.Null);
            Assert.That(burger, Is.Not.Null);
            float bodyTop = body.GetComponent<Renderer>().bounds.max.y;
            float trayTop = tray.GetComponent<Renderer>().bounds.max.y;
            Assert.That(burger.position.y, Is.GreaterThan(bodyTop));
            Assert.That(burger.position.y, Is.GreaterThan(trayTop - 0.05f));
            Assert.That(Vector3.Distance(Flatten(burger.position), Flatten(tray.position)), Is.LessThan(0.35f));
        }

        [Test]
        public void EachMachineKeepsItsOwnCapAndOmitsFullFromWorldCopy()
        {
            ExpandableGrill starter = ExpandableGrill.CreateStarter(root.transform, player, wallet);
            ExpandableGrill extra = ExpandableGrill.Create(root.transform, ShopLayout.ExtraGrill, player, wallet);
            starter.Station.Advance(100f);
            extra.Upgrade.RestoreLevel(3);
            extra.Station.Advance(100f);
            Assert.That(starter.Station.Stock, Is.EqualTo(ProductionStation.CapacityForLevel(1)));
            Assert.That(extra.Station.Stock, Is.EqualTo(ProductionStation.CapacityForLevel(3)));
            Assert.That(starter.Station.Capacity, Is.EqualTo(ProductionStation.CapacityForLevel(1)));
            Assert.That(extra.Station.Capacity, Is.EqualTo(ProductionStation.CapacityForLevel(3)));
            Assert.That(starter.StatusCopy, Is.EqualTo("GRILL 5/5"));
            Assert.That(extra.StatusCopy, Is.EqualTo("GRILL 12/12  MAX"));
            Assert.That(CountCopy(starter.transform, "FULL"), Is.Zero);
            Assert.That(CountCopy(extra.transform, "FULL"), Is.Zero);
            Assert.That(CountCopy(starter.transform, "GRILL 5/5"), Is.EqualTo(1));
            Assert.That(CountCopy(extra.transform, "GRILL 12/12"), Is.EqualTo(1));
        }

        static int CountCopy(Transform root, string token)
        {
            int count = 0;
            TextMesh[] labels = root.GetComponentsInChildren<TextMesh>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null && labels[i].text != null && labels[i].text.Contains(token))
                    count++;
            }
            return count;
        }

        static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
