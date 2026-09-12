using BurgerShop.Core;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class ShopLayoutTests
    {
        GameObject root;

        [SetUp]
        public void SetUp() => root = new GameObject("ShopLayoutTest");

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void FloorAndWallsAreOneAndAHalfTimesThePreviousShop()
        {
            Assert.That(ShopLayout.Scale, Is.EqualTo(1.5f));
            Assert.That(ShopLayout.PreviousFloorSize, Is.EqualTo(20f));
            Assert.That(ShopLayout.FloorSize, Is.EqualTo(30f));
            Assert.That(ShopLayout.FloorSize, Is.EqualTo(ShopLayout.PreviousFloorSize * 1.5f));
            Assert.That(ShopLayout.WallHalf, Is.EqualTo(15f));

            Material mat = RuntimeMaterials.Create(new Color(0.5f, 0.4f, 0.3f));
            GameObject floor = ShopLayout.CreateFloor(root.transform, mat);
            ShopLayout.CreateWalls(root.transform, mat);

            Assert.That(floor.transform.localScale.x, Is.EqualTo(30f));
            Assert.That(floor.transform.localScale.z, Is.EqualTo(30f));
            Assert.That(floor.transform.localScale.x, Is.GreaterThanOrEqualTo(20f * 1.5f));
            Transform wallZ = root.transform.Find("Wall+Z");
            Transform wallWest = root.transform.Find("Wall-X");
            Transform wallEastNorth = root.transform.Find("Wall+X_N");
            Transform wallEastSouth = root.transform.Find("Wall+X_S");
            Transform wallSouthWest = root.transform.Find("Wall-Z_W");
            Transform wallSouthEast = root.transform.Find("Wall-Z_E");
            Assert.That(wallZ, Is.Not.Null);
            Assert.That(wallZ.position.z, Is.EqualTo(15f));
            Assert.That(wallWest.position.x, Is.EqualTo(-15f));
            Assert.That(root.transform.Find("Wall+X"), Is.Null);
            Assert.That(root.transform.Find("Wall-Z"), Is.Null);
            Assert.That(wallEastNorth, Is.Not.Null);
            Assert.That(wallEastSouth, Is.Not.Null);
            Assert.That(wallSouthWest, Is.Not.Null);
            Assert.That(wallSouthEast, Is.Not.Null);
            Assert.That(wallEastNorth.position.x, Is.EqualTo(15f));
            Assert.That(wallEastSouth.position.x, Is.EqualTo(15f));
            Assert.That(wallSouthWest.position.z, Is.EqualTo(-15f));
            Assert.That(wallSouthEast.position.z, Is.EqualTo(-15f));
            Assert.That(Mathf.Abs(wallZ.position.z) * 2f, Is.GreaterThanOrEqualTo(20f * 1.5f));
        }

        [Test]
        public void ZonesSitOnTheIntegerGrid()
        {
            Assert.That(ShopLayout.Grill, Is.EqualTo(new Vector3(5f, 0f, 9f)));
            Assert.That(ShopLayout.UpgradeSpot, Is.EqualTo(new Vector3(5f, 0.02f, 11.4f)));
            Assert.That(ShopLayout.ExtraGrill, Is.EqualTo(new Vector3(12f, 0f, 9f)));
            Assert.That(ShopLayout.GrillUnlock, Is.EqualTo(new Vector3(12f, 0.02f, 9f)));
            Assert.That(ShopLayout.ExtraGrillUpgrade, Is.EqualTo(new Vector3(12f, 0.02f, 11.4f)));
            Assert.That(ShopLayout.Counter, Is.EqualTo(new Vector3(-3f, 0f, 4f)));
            Assert.That(ShopLayout.ServingCircle, Is.EqualTo(new Vector3(0.6f, 0.02f, 4f)));
            Assert.That(ShopLayout.ExtraCounter, Is.EqualTo(new Vector3(-3f, 0f, 8f)));
            Assert.That(ShopLayout.CounterUnlock, Is.EqualTo(new Vector3(-3f, 0.02f, 8f)));
            Assert.That(ShopLayout.ExtraServingCircle, Is.EqualTo(new Vector3(0.6f, 0.02f, 8f)));
            Assert.That(ShopLayout.Tables[0], Is.EqualTo(new Vector3(-8f, 0f, 7f)));
            Assert.That(ShopLayout.Tables[1], Is.EqualTo(new Vector3(-8f, 0f, 3f)));
            Assert.That(ShopLayout.Tables[2], Is.EqualTo(new Vector3(-12f, 0f, 7f)));
            Assert.That(ShopLayout.ExtraTable, Is.EqualTo(new Vector3(-12f, 0f, 3f)));
            Assert.That(ShopLayout.TableUnlock, Is.EqualTo(new Vector3(-12f, 0.02f, 3f)));
            Assert.That(ShopLayout.TrashBin, Is.EqualTo(new Vector3(-13f, 0f, 0f)));
            Assert.That(DiningArea.ShopPositions[0], Is.EqualTo(ShopLayout.Tables[0]));
            Assert.That(TrashBin.ShopPosition, Is.EqualTo(ShopLayout.TrashBin));
            Assert.That(ShopLayout.BoxingUnlock, Is.EqualTo(new Vector3(-9f, 0.02f, -9f)));
            Assert.That(ShopLayout.BoxingCircle, Is.EqualTo(new Vector3(-8f, 0.02f, -7.2f)));
            Assert.That(ShopLayout.PackageCounter, Is.EqualTo(ShopLayout.DriveThruWindow));
            Assert.That(ShopLayout.DriveThruWindow, Is.EqualTo(new Vector3(-9f, 0f, -14.35f)));
            Assert.That(ShopLayout.DriveThruCircle, Is.EqualTo(new Vector3(-9f, 0.02f, -13.05f)));
            Assert.That(ShopLayout.DriveThruUnlock, Is.EqualTo(new Vector3(-6f, 0.02f, -12f)));
            Assert.That(ShopLayout.DriveThruQueue[0].x, Is.EqualTo(ShopLayout.DriveThruWindow.x));
            Assert.That(ShopLayout.BoostDoorX, Is.EqualTo(11f));
            Assert.That(ShopLayout.HrDoorZ, Is.EqualTo(ShopLayout.Aisle.z));
            Assert.That(ShopLayout.HiringSpot, Is.EqualTo(new Vector3(2f, 0.02f, 0f)));
            Assert.That(ShopLayout.Cola, Is.EqualTo(new Vector3(9f, 0f, 3f)));
            Assert.That(ShopLayout.ColaUpgrade, Is.EqualTo(new Vector3(9f, 0.02f, 5.4f)));
            Assert.That(Vector3.Distance(ShopLayout.ColaPickup, new Vector3(10.15f, 0.015f, 0.8f)), Is.LessThan(.0001f));
            Assert.That(ShopLayout.ColaCounter, Is.EqualTo(new Vector3(3f, 0f, -3f)));
            Assert.That(ShopLayout.ColaServingCircle, Is.EqualTo(new Vector3(6.6f, 0.02f, -3f)));
            Assert.That(ShopLayout.ColaQueueEntry, Is.EqualTo(new Vector3(3f, 0f, -13f)));
            Assert.That(ShopLayout.ColaQueueSlots[0], Is.EqualTo(new Vector3(3f, 0f, -5.5f)));
            Assert.That(ShopLayout.ColaQueueSlots[1], Is.EqualTo(new Vector3(3f, 0f, -8.5f)));
            Assert.That(ShopLayout.ColaQueueSlots[2], Is.EqualTo(new Vector3(3f, 0f, -11.5f)));
        }

        [Test]
        public void UnlockPadsSitOnTheFacilityTheyBuy()
        {
            AssertPadOn(ShopLayout.TableUnlock, ShopLayout.ExtraTable);
            AssertPadOn(ShopLayout.GrillUnlock, ShopLayout.ExtraGrill);
            AssertPadOn(ShopLayout.CounterUnlock, ShopLayout.ExtraCounter);
            AssertPadOn(ShopLayout.BoxingUnlock, ShopLayout.BoxingTable);
            Assert.That(ShopLayout.Horizontal(ShopLayout.DriveThruUnlock, ShopLayout.DriveThruCircle),
                Is.GreaterThan(ShopLayout.AisleMin));
            Assert.That(ShopLayout.Horizontal(ShopLayout.DriveThruUnlock, ShopLayout.DriveThruWindow),
                Is.GreaterThan(ShopLayout.AisleMin));
        }

        [Test]
        public void KeyFacilitiesKeepAisleClearance()
        {
            const float min = ShopLayout.AisleMin;
            Vector3[] tables =
            {
                ShopLayout.Tables[0], ShopLayout.Tables[1], ShopLayout.Tables[2], ShopLayout.ExtraTable
            };

            AssertFar("grills", ShopLayout.Grill, ShopLayout.ExtraGrill, min);
            AssertFar("grill-counter", ShopLayout.Grill, ShopLayout.Counter, min);
            AssertFar("grill-extra-counter", ShopLayout.Grill, ShopLayout.ExtraCounter, min);
            AssertFar("extra-grill-counter", ShopLayout.ExtraGrill, ShopLayout.Counter, min);
            AssertFar("counters", ShopLayout.Counter, ShopLayout.ExtraCounter, min);
            AssertFar("white-circles", ShopLayout.ServingCircle, ShopLayout.ExtraServingCircle, min);
            AssertFar("grill-upgrades", ShopLayout.UpgradeSpot, ShopLayout.ExtraGrillUpgrade, min);
            AssertFar("cola-vs-starter-grill", ShopLayout.Cola, ShopLayout.Grill, min);
            AssertFar("cola-vs-extra-grill", ShopLayout.Cola, ShopLayout.ExtraGrill, min);
            AssertFar("cola-vs-burger-counter", ShopLayout.Cola, ShopLayout.Counter, min);
            AssertFar("cola-counter-vs-burger-counter", ShopLayout.ColaCounter, ShopLayout.Counter, min);
            AssertFar("cola-white-vs-burger-white", ShopLayout.ColaServingCircle, ShopLayout.ServingCircle, min);
            AssertFar("cola-vs-entrance", ShopLayout.Cola, ShopLayout.Entrance, min);
            AssertFar("cola-counter-vs-entrance", ShopLayout.ColaCounter, ShopLayout.Entrance, min);
            AssertFar("cola-upgrade-on-own-machine", ShopLayout.ColaUpgrade, ShopLayout.Cola, 2f);
            AssertFar("starter-upgrade-on-own-grill", ShopLayout.UpgradeSpot, ShopLayout.Grill, 2f);
            Assert.That(ShopLayout.Horizontal(ShopLayout.UpgradeSpot, ShopLayout.Grill), Is.LessThan(3f));
            Assert.That(ShopLayout.Horizontal(ShopLayout.ExtraGrillUpgrade, ShopLayout.ExtraGrill), Is.LessThan(3f));
            AssertFar("extra-upgrade-not-on-starter", ShopLayout.ExtraGrillUpgrade, ShopLayout.Grill, 4f);
            AssertFar("pickup-vs-white", ShopLayout.GrillPickup, ShopLayout.ServingCircle, min);
            AssertFar("extra-pickup-vs-extra-white", ShopLayout.ExtraGrillPickup, ShopLayout.ExtraServingCircle, min);

            for (int i = 0; i < tables.Length; i++)
            {
                for (int j = i + 1; j < tables.Length; j++)
                    AssertFar("tables " + i + "-" + j, tables[i], tables[j], min);
                AssertFar("table-counter " + i, tables[i], ShopLayout.Counter, min);
                AssertFar("table-extra-counter " + i, tables[i], ShopLayout.ExtraCounter, min);
                AssertFar("table-grill " + i, tables[i], ShopLayout.Grill, min);
                AssertFar("table-trash " + i, tables[i], ShopLayout.TrashBin, min);
                AssertFar("table-box " + i, tables[i], ShopLayout.BoxingTable, min);
            }

            AssertFar("trash-counter", ShopLayout.TrashBin, ShopLayout.Counter, min);
            AssertFar("box-pack", ShopLayout.BoxingTable, ShopLayout.PackageCounter, 2f);
            Assert.That(ShopLayout.PackageCounter, Is.EqualTo(ShopLayout.DriveThruWindow), "Package stock and car service share the blue counter.");
            AssertFar("box-circle-window-circle", ShopLayout.BoxingCircle, ShopLayout.DriveThruCircle, min);
            AssertFar("lane-buy-vs-window-green", ShopLayout.DriveThruUnlock, ShopLayout.DriveThruCircle, min);
            AssertFar("boost-door-vs-window", ShopLayout.BoostDoor, ShopLayout.DriveThruWindow, 8f);
            AssertFar("boost-door-vs-box", ShopLayout.BoostDoor, ShopLayout.BoxingTable, 5f);
            AssertFar("boost-door-vs-hr", ShopLayout.BoostDoor, ShopLayout.HrDoor, 8f);
            AssertFar("hr-vs-grill", ShopLayout.HrDoor, ShopLayout.Grill, 5f);
            AssertFar("hr-vs-extra-grill", ShopLayout.HrDoor, ShopLayout.ExtraGrill, 5f);
            AssertFar("entrance-vs-box", ShopLayout.Entrance, ShopLayout.BoxingUnlock, 4f);
            AssertFar("queue-vs-box", ShopLayout.QueueSlots[2], ShopLayout.BoxingTable, min);

            Assert.That(ShopLayout.Grill.x, Is.GreaterThan(0f));
            Assert.That(ShopLayout.Grill.z, Is.GreaterThan(0f));
            Assert.That(ShopLayout.ExtraGrill.x, Is.GreaterThan(ShopLayout.Grill.x));
            Assert.That(ShopLayout.UpgradeSpot.z, Is.GreaterThan(ShopLayout.Grill.z));
            Assert.That(ShopLayout.ExtraGrillUpgrade.z, Is.GreaterThan(ShopLayout.ExtraGrill.z));
            Assert.That(ShopLayout.GrillPickup.z, Is.LessThan(ShopLayout.Grill.z));
            Assert.That(ShopLayout.ServingCircle.x, Is.GreaterThan(ShopLayout.Counter.x));
            Assert.That(ShopLayout.ExtraServingCircle.x, Is.GreaterThan(ShopLayout.ExtraCounter.x));
            Assert.That(ShopLayout.ColaServingCircle.x, Is.GreaterThan(ShopLayout.ColaCounter.x));
            Assert.That(ShopLayout.ColaUpgrade.z, Is.GreaterThan(ShopLayout.Cola.z));
            Assert.That(ShopLayout.ColaPickup.z, Is.LessThan(ShopLayout.Cola.z));
            Assert.That(ShopLayout.Cola.x, Is.GreaterThan(0f));
            Assert.That(ShopLayout.ColaQueueSlots[0].z, Is.LessThan(ShopLayout.ColaCounter.z));
            Assert.That(ShopLayout.Tables[0].x, Is.LessThan(0f));
            Assert.That(ShopLayout.DriveThruWindow.z, Is.EqualTo(-ShopLayout.WallHalf).Within(1f));
            Assert.That(ShopLayout.DriveThruQueue[0].z, Is.LessThan(-ShopLayout.WallHalf));
            Assert.That(ShopLayout.BoostDoorX, Is.GreaterThan(ShopLayout.DriveThruSpawn.x));
            Assert.That(ShopLayout.BoostDoorX, Is.GreaterThan(0f));
            Assert.That(ShopLayout.ContainsHall(ShopLayout.HiringSpot), Is.True);
            Assert.That(ShopLayout.ContainsHrOffice(ShopLayout.HiringSpot), Is.False);
            Assert.That(ShopLayout.ContainsBoostRoom(ShopLayout.HiringSpot), Is.False);
        }

        static void AssertPadOn(Vector3 pad, Vector3 facility)
        {
            Assert.That(ShopLayout.Horizontal(pad, facility), Is.LessThan(0.05f), pad + " vs " + facility);
        }

        static void AssertFar(string label, Vector3 a, Vector3 b, float min)
        {
            Assert.That(ShopLayout.Horizontal(a, b), Is.GreaterThanOrEqualTo(min), label + " " + a + " vs " + b);
        }
    }
}
