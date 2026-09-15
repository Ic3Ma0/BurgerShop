using System.IO;
using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class RestroomPresentationTests
    {
        GameObject root;
        RestroomExpansion room;
        RestaurantWallet wallet;
        BurgerInventory player;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("RestroomPresentation");
            wallet = root.AddComponent<RestaurantWallet>();
            wallet.RestoreProgress(200, 0);
            player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform, false);
            player.Configure();
            room = root.AddComponent<RestroomExpansion>();
            room.Configure(wallet, player, null);
            room.Restore(true, RestroomExpansion.Cost);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void BuiltRoomHasThreeWoodStallsWithToiletsAndAjarDoors()
        {
            Transform area = room.transform.Find("RestroomExpansion");
            Assert.That(area, Is.Not.Null);
            int ajar = 0;
            for (int i = 0; i < RestroomFurniture.StallCount; i++)
            {
                Transform stall = area.Find("Stall_" + i);
                Assert.That(stall, Is.Not.Null, "Stall " + i);
                Assert.That(stall.Find("ToiletBowl"), Is.Not.Null);
                Assert.That(stall.Find("ToiletTank"), Is.Not.Null);
                Color wood = BaseColor(stall.Find("StallPartitionL"));
                Assert.That(wood.r, Is.GreaterThan(wood.b + .15f), "Stall wood should read brown, not the old cyan partitions");
                Assert.That(wood.g, Is.GreaterThan(.25f));
                Transform hinge = stall.Find("CubicleDoorHinge");
                float yaw = hinge.localEulerAngles.y;
                if (yaw > 180f) yaw -= 360f;
                if (Mathf.Abs(yaw) > 15f) ajar++;
                Assert.That(stall.Find("ToiletTank").position.z, Is.LessThan(stall.Find("ToiletBowl").position.z),
                    "Tank sits on the back wall, bowl toward the room");
                if (i < 2)
                {
                    Vector3 towardSeat = Flatten(RestroomExpansion.UsePoint(i) - stall.Find("ToiletBowl").position);
                    Assert.That(Vector3.Dot(towardSeat.normalized, Vector3.forward), Is.GreaterThan(.7f), "Use point " + i);
                }
            }
            Assert.That(ajar, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void SinksLockersTrashAndScrubberMatchTheFacilityLayout()
        {
            Transform area = room.transform.Find("RestroomExpansion");
            Transform sinkA = area.Find("Sink_0");
            Transform sinkB = area.Find("Sink_1");
            Assert.That(sinkA.position.x, Is.GreaterThan(2.5f));
            Assert.That(sinkB.position.x, Is.GreaterThan(2.5f));
            Assert.That(sinkA.Find("SinkTap").position.x, Is.GreaterThan(sinkA.Find("SinkBasin").position.x));
            Transform lockers = area.Find("LockerCabinet");
            Assert.That(lockers.position.x, Is.LessThan(-3.5f));
            Color yellow = BaseColor(lockers.Find("LockerBody"));
            Assert.That(yellow.r, Is.GreaterThan(.8f));
            Assert.That(yellow.g, Is.GreaterThan(.6f));
            Assert.That(yellow.b, Is.LessThan(.4f));
            Transform trash = area.Find("RestroomTrash");
            Assert.That(trash, Is.Not.Null);
            Assert.That(BaseColor(trash.Find("TrashBody")).r, Is.GreaterThan(BaseColor(trash.Find("TrashBody")).g + .3f));
            var label = trash.GetComponentInChildren<TextMesh>();
            Assert.That(label.name, Is.EqualTo("RestroomTrashLabel"));
            Assert.That(label.text, Is.EqualTo("TRASH"));
            Assert.That(area.Find("FloorScrubber"), Is.Not.Null);
            Transform circle = area.Find("CleanerCircle");
            Assert.That(circle, Is.Not.Null);
            Color dash = BaseColor(circle.GetComponentInChildren<Renderer>().transform);
            Assert.That(dash.r, Is.GreaterThan(.9f));
            Assert.That(dash.g, Is.GreaterThan(.9f));
            Assert.That(Vector3.Distance(Flatten(area.Find("FloorScrubber").position), Flatten(RestroomFurniture.CleanerPad)), Is.LessThan(.05f));
        }

        [Test]
        public void CleanBotChipUsesEnglishTimerCopyWithoutReferenceShopChrome()
        {
            Text title = null, timer = null;
            foreach (var text in room.GetComponentsInChildren<Text>(true))
            {
                if (text.name == "CleanBotTitle") title = text;
                if (text.name == "CleanBotTimer") timer = text;
                Assert.That(text.text.ToUpperInvariant(), Does.Not.Contain("SHOP"));
                Assert.That(text.text, Does.Not.Contain("Daily Reward"));
            }
            foreach (var mesh in room.GetComponentsInChildren<TextMesh>(true))
            {
                Assert.That(mesh.text.ToUpperInvariant(), Does.Not.Contain("SHOP"));
                Assert.That(mesh.text, Does.Not.Contain("Daily Reward"));
            }
            Assert.That(title, Is.Not.Null);
            Assert.That(title.text, Is.EqualTo("CLEAN BOT"));
            Assert.That(timer.text, Is.EqualTo("00m 00s"));
            Assert.That(RestroomFurniture.FormatTimer(60), Is.EqualTo("01m 00s"));
            Assert.That(RestroomFurniture.FormatTimer(59), Is.EqualTo("00m 59s"));
            Assert.That(FoodIcons.Get(FoodIcon.Bot), Is.Not.Null);
        }

        [Test]
        public void FurnitureCollidersBlockWhileDoorsToiletsAndTheWhiteCircleDoNot()
        {
            Transform area = room.transform.Find("RestroomExpansion");
            AssertBlocks(area.Find("Stall_0/StallPartitionL"));
            AssertBlocks(area.Find("LockerCabinet/LockerBody"));
            AssertBlocks(area.Find("RestroomTrash/TrashBody"));
            AssertBlocks(area.Find("Sink_0/SinkCabinet"));
            AssertBlocks(area.Find("FloorScrubber/ScrubberBody"));
            Assert.That(SolidOccupancy.BlocksPlayer(area.Find("Stall_0/CubicleDoorHinge/CubicleDoor").GetComponent<Collider>()), Is.False);
            Assert.That(SolidOccupancy.BlocksPlayer(area.Find("Stall_0/ToiletBowl").GetComponent<Collider>()), Is.False);
            foreach (var collider in area.Find("CleanerCircle").GetComponentsInChildren<Collider>())
                Assert.That(SolidOccupancy.BlocksPlayer(collider), Is.False, collider.name);
            Assert.That(area.GetComponentsInChildren<MeshCollider>(true), Is.Empty);
            string xml = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/link.xml"));
            Assert.That(xml, Does.Contain("UnityEngine.BoxCollider"));
            Assert.That(xml, Does.Contain("UnityEngine.CapsuleCollider"));
            Assert.That(xml, Does.Contain("UnityEngine.SphereCollider"));
        }

        [Test]
        public void TwoCubiclesStayExclusiveAndCleanBotClearsDirtOnATimer()
        {
            var a = CustomerAgent.Create(root.transform, 100, Vector3.zero);
            var b = CustomerAgent.Create(root.transform, 102, Vector3.zero);
            var c = CustomerAgent.Create(root.transform, 104, Vector3.zero);
            Assert.That(room.TryVisit(a) && room.TryVisit(b) && room.TryVisit(c), Is.True);
            Assert.That(room.Acquire(a), Is.EqualTo(0));
            Assert.That(room.Acquire(b), Is.EqualTo(1));
            Assert.That(room.Acquire(c), Is.EqualTo(-1));
            room.FinishUse(a, 0);
            Assert.That(room.IsDirty(0), Is.True);
            Assert.That(room.TryHireCleanBot(), Is.True);
            Assert.That(wallet.Coins, Is.EqualTo(150));
            Assert.That(room.CleanBotTimerCopy, Is.EqualTo("01m 00s"));
            Assert.That(room.TryHireCleanBot(), Is.False);
            Assert.That(wallet.Coins, Is.EqualTo(150));
            player.transform.position = Vector3.up;
            for (int i = 0; i < 180; i++) room.AdvanceCleanBot(1f / 60f);
            Assert.That(room.IsDirty(0), Is.False);
            Assert.That(room.CleanBotTimerCopy, Is.EqualTo("00m 57s"));
            room.AdvanceCleanBot(57f);
            Assert.That(room.CleanBotTimerCopy, Is.EqualTo("00m 00s"));
            wallet.RestoreProgress(49, 0);
            Assert.That(room.TryHireCleanBot(), Is.False);
            Assert.That(room.CleanBotTimerCopy, Is.EqualTo("00m 00s"));
        }

        [Test]
        public void StandingInTheWhiteCircleHiresAfterTheSharedEntryDelay()
        {
            player.transform.position = RestroomFurniture.CleanerPad + Vector3.up;
            room.AdvanceCleanBot(FacilityUnlockZone.EntryDelay * .5f);
            Assert.That(room.CleanBotRemaining, Is.EqualTo(0));
            room.AdvanceCleanBot(FacilityUnlockZone.EntryDelay);
            Assert.That(room.CleanBotRemaining, Is.EqualTo(RestroomExpansion.CleanBotDuration));
            Assert.That(wallet.Coins, Is.EqualTo(150));
        }

        static void AssertBlocks(Transform part)
        {
            Assert.That(part, Is.Not.Null);
            Assert.That(SolidOccupancy.BlocksPlayer(part.GetComponent<Collider>()), Is.True, part.name);
        }

        static Color BaseColor(Transform part)
        {
            var material = part.GetComponent<Renderer>().sharedMaterial;
            return material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
        }

        static Vector3 Flatten(Vector3 value) { value.y = 0f; return value; }
    }
}
