using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class DiningTrashTests
    {
        GameObject root;
        CustomerQueue queue;
        BurgerInventory inventory;
        TrashInventory trash;
        ProductionStation grill;
        BurgerServingZone serving;
        CounterStock stock;
        CounterDropZone drop;
        DiningTable table;
        DiningArea dining;
        TrashBin bin;
        RestaurantWallet wallet;
        Transform servingPoint;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("DiningTrashTest");
            queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(new Vector3(-8f, 0f, -4f), new Vector3(-2f, 0f, -4f),
                new[] { new Vector3(-2f, 0f, 1.7f), new Vector3(-2f, 0f, -0.1f), new Vector3(-2f, 0f, -1.9f) }, new Vector3(-2f, 0f, 3.3f));
            GameObject player = new GameObject("Player");
            player.transform.SetParent(root.transform);
            inventory = player.AddComponent<BurgerInventory>();
            inventory.Configure();
            trash = player.AddComponent<TrashInventory>();
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            grill = root.AddComponent<ProductionStation>();
            grill.Configure(output, null, null);
            wallet = root.AddComponent<RestaurantWallet>();
            servingPoint = new GameObject("ServingPoint").transform;
            servingPoint.SetParent(root.transform);
            servingPoint.position = new Vector3(0.9f, 0f, 3.3f);
            Transform anchor = new GameObject("StockAnchor").transform;
            anchor.SetParent(root.transform);
            stock = root.AddComponent<CounterStock>();
            stock.Configure(anchor, null);
            drop = root.AddComponent<CounterDropZone>();
            drop.Configure(stock, servingPoint);
            table = DiningTable.Create(root.transform, new Vector3(-5.6f, 0f, 0.2f));
            table.BindCollector(trash);
            dining = DiningArea.Wrap(table);
            bin = TrashBin.Create(root.transform, TrashBin.ShopPosition);
            bin.Configure(trash);
            serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, inventory, wallet, servingPoint,
                new[] { new Vector3(-8.2f, 0f, -4.4f), new Vector3(-10.5f, 0f, -6.2f) }, stock, dining, drop);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        void FillQueue()
        {
            for (int i = 0; i < 1200; i++) queue.Advance(1f / 60f);
        }

        void Load(int count = 1)
        {
            grill.Advance(12f);
            for (int i = 0; i < count; i++) inventory.TryCollectFrom(grill);
        }

        void AtCounter() => inventory.transform.position = serving.ServingPosition + Vector3.up;

        CustomerAgent ServeAndFinishMeal()
        {
            FillQueue();
            Load();
            AtCounter();
            CustomerAgent customer = queue.ReadyCustomer;
            serving.Advance(0.01f);
            serving.Advance(BurgerServingZone.HandoffDuration);
            customer.AdvanceDeparture(100f);
            return customer;
        }

        void PickupAllOnTable()
        {
            inventory.transform.position = table.Center;
            for (int i = 0; i < 8 && table.TrashCount > 0; i++)
                table.Advance(1f);
        }

        void DumpAll()
        {
            inventory.transform.position = bin.DropPosition;
            for (int i = 0; i < 12 && trash.Count > 0; i++)
                bin.Advance(1f);
        }

        [Test]
        public void FinishedMealLeavesExactlyTwoLargePaperWadsAndLocksTheWholeTable()
        {
            ServeAndFinishMeal();
            Assert.That(table.TrashCount, Is.EqualTo(DiningTable.TrashPerGuest));
            Assert.That(table.OutstandingTrash, Is.EqualTo(DiningTable.TrashPerGuest));
            Assert.That(table.IsDirty, Is.True);
            Assert.That(table.IsSeatBlocked(0), Is.True);
            Assert.That(table.IsSeatBlocked(1), Is.True);
            Assert.That(table.OccupiedSeats, Is.Zero);
            Assert.That(table.HasAvailableSeat, Is.False);
            AssertPaperWad(table.transform.Find("Trash_0"));
            AssertPaperWad(table.transform.Find("Trash_1"));
        }

        [Test]
        public void NearbyPickupFliesThenCountsAfterTheAnimation()
        {
            table.LeaveMealTrash(0);
            Transform wad = table.transform.Find("Trash_1");
            Assert.That(wad, Is.Not.Null);
            Vector3 start = wad.position;
            inventory.transform.position = Vector3.zero;
            table.Advance(1f);
            Assert.That(table.TrashCount, Is.EqualTo(2));
            Assert.That(trash.Count, Is.Zero);
            inventory.transform.position = table.Center;
            table.Advance(0.05f);
            Assert.That(table.TrashCount, Is.EqualTo(2));
            Assert.That(trash.Count, Is.Zero);
            Assert.That(wad != null && wad.gameObject.activeInHierarchy, Is.True);
            Assert.That(Vector3.Distance(wad.position, start), Is.GreaterThan(0.04f));
            table.Advance(TrashMotion.Duration);
            Assert.That(table.TrashCount, Is.EqualTo(1));
            Assert.That(trash.Count, Is.EqualTo(1));
            table.Advance(1f);
            Assert.That(table.TrashCount, Is.Zero);
            Assert.That(trash.Count, Is.EqualTo(2));
            Assert.That(inventory.Count, Is.Zero);
            Assert.That(inventory.Capacity, Is.EqualTo(4));
        }

        [Test]
        public void DumpFliesIntoTheBinThenCountsAfterTheAnimation()
        {
            table.LeaveMealTrash(0);
            PickupAllOnTable();
            Assert.That(trash.Count, Is.EqualTo(2));
            Transform stack = inventory.transform.Find("TrashStack");
            Assert.That(stack, Is.Not.Null);
            Assert.That(stack.childCount, Is.EqualTo(2));
            Transform wad = stack.GetChild(1);
            Vector3 start = wad.position;
            inventory.transform.position = bin.DropPosition;
            bin.Advance(0.05f);
            Assert.That(trash.Count, Is.EqualTo(2));
            Assert.That(wad != null && wad.gameObject.activeInHierarchy, Is.True);
            Assert.That(Vector3.Distance(wad.position, start), Is.GreaterThan(0.04f));
            bin.Advance(TrashMotion.Duration);
            Assert.That(trash.Count, Is.EqualTo(1));
            bin.Advance(1f);
            Assert.That(trash.Count, Is.Zero);
            Assert.That(table.OutstandingTrash, Is.Zero);
        }

        [Test]
        public void EnlargedBinCanReceiveTrashOutsideItsPhysicalFootprint()
        {
            table.LeaveMealTrash(0);
            PickupAllOnTable();
            inventory.transform.position=bin.DropPosition+Vector3.right*1.1f;
            Assert.That(bin.IsInRange,Is.True,"the player stops outside the enlarged bin, not at its centre");
            for(int i=0;i<120;i++)bin.Advance(1f/60);
            Assert.That(trash.Count,Is.Zero);
            Assert.That(table.OutstandingTrash,Is.Zero);
            inventory.transform.position=bin.DropPosition+Vector3.right*2;
            Assert.That(bin.IsInRange,Is.False);
        }

        [Test]
        public void DumpIsFasterThanTablePickupAndTheBinIsLarger()
        {
            Assert.That(TrashInventory.DumpDuration, Is.LessThan(TrashMotion.Duration));
            Assert.That(bin.DropInterval, Is.LessThan(TrashMotion.Duration));
            Assert.That(bin.DropInterval, Is.GreaterThanOrEqualTo(0.10f));
            Assert.That(bin.DropInterval, Is.LessThanOrEqualTo(0.12f));
            Renderer body = bin.transform.Find("Body").GetComponent<Renderer>();
            Assert.That(body.bounds.size.y, Is.GreaterThan(1.2f));
            Assert.That(bin.transform.localScale.x, Is.EqualTo(TrashBin.VisualScale).Within(0.01f));
        }

        [Test]
        public void DumpSoundRequestDoesNotThrowAndDumpStillCompletes()
        {
            table.LeaveMealTrash(0);
            PickupAllOnTable();
            int previous = PlayerPrefs.GetInt(FeedbackDirector.SoundPreference, 1);
            var hudRoot = new GameObject("DumpHud", typeof(RectTransform), typeof(Canvas));
            hudRoot.transform.SetParent(root.transform, false);
            try
            {
                PlayerPrefs.SetInt(FeedbackDirector.SoundPreference, 1);
                var hud = FeedbackDirector.Build(hudRoot.transform, inventory);
                Assert.DoesNotThrow(() => hud.RequestSound(FeedbackSound.Dump));
                inventory.transform.position = bin.DropPosition;
                Assert.DoesNotThrow(() => bin.Advance(0.05f));
                bin.Advance(TrashMotion.Duration);
                Assert.That(trash.Count, Is.EqualTo(1));
            }
            finally
            {
                PlayerPrefs.SetInt(FeedbackDirector.SoundPreference, previous);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void DumpClipIsAShortProceduralRustle()
        {
            var clip = CoinSfx.CreateDump();
            try
            {
                Assert.That(clip.length, Is.GreaterThan(0.05f).And.LessThan(0.25f));
                var samples = new float[clip.samples];
                Assert.That(clip.GetData(samples, 0), Is.True);
                float peak = 0f;
                for (int i = 0; i < samples.Length; i++)
                    peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
                Assert.That(peak, Is.GreaterThan(0.02f).And.LessThan(0.9f));
            }
            finally { Object.DestroyImmediate(clip); }
        }

        [Test]
        public void SeatReopensAfterTableTrashIsPickedUp()
        {
            table.LeaveMealTrash(0);
            Assert.That(table.HasAvailableSeat, Is.False);
            PickupAllOnTable();
            Assert.That(table.TrashCount, Is.Zero);
            Assert.That(table.IsDirty, Is.False);
            Assert.That(table.IsSeatBlocked(0), Is.False);
            Assert.That(table.HasAvailableSeat, Is.True);
            Assert.That(table.OutstandingTrash, Is.EqualTo(DiningTable.TrashPerGuest));
            DumpAll();
            Assert.That(trash.Count, Is.Zero);
            Assert.That(table.OutstandingTrash, Is.Zero);
            FillQueue();
            Load();
            AtCounter();
            CustomerAgent guest = queue.ReadyCustomer;
            serving.Advance(0.01f);
            serving.Advance(BurgerServingZone.HandoffDuration);
            WaitUntilEating(guest);
            Assert.That(guest.IsEating, Is.True);
        }

        [Test]
        public void EachGuestLeavesTwoTrashAndPlayerCanHoldMoreThanFour()
        {
            DiningArea hall = DiningArea.Create(root.transform, new[]
            {
                new Vector3(4f, 0f, 0f),
                new Vector3(4f, 0f, -3f),
                new Vector3(7f, 0f, 0f)
            });
            hall.BindCollector(trash);
            foreach (DiningTable extra in hall.Tables)
            {
                extra.LeaveMealTrash(0);
                inventory.transform.position = extra.Center;
                extra.Advance(1f);
                extra.Advance(1f);
            }
            Assert.That(hall.TrashOnTables, Is.Zero);
            Assert.That(trash.Count, Is.EqualTo(6));
            Assert.That(trash.Count, Is.GreaterThan(inventory.Capacity));
        }

        [Test]
        public void NextCustomerWaitsBesideADirtyTableFacingItUntilTrashIsCleared()
        {
            ServeAndFinishMeal();
            Assert.That(table.IsDirty, Is.True);
            Assert.That(table.HasAvailableSeat, Is.False);
            FillQueue();
            Load();
            AtCounter();
            CustomerAgent second = queue.ReadyCustomer;
            serving.Advance(1f);
            serving.Advance(BurgerServingZone.HandoffDuration);
            for (int i = 0; i < 240; i++) second.AdvanceDeparture(1f / 60f);
            Assert.That(second.IsEating, Is.False);
            Assert.That(second.IsDining, Is.True);
            Assert.That(second.gameObject.activeInHierarchy, Is.True);
            Assert.That(Vector3.Distance(Flatten(second.transform.position), Flatten(table.WaitPosition)), Is.LessThan(0.15f));
            Assert.That(FacesTable(second, table), Is.True);
            Assert.That(table.OccupiedSeats, Is.Zero);

            PickupAllOnTable();
            Assert.That(table.HasAvailableSeat, Is.True);
            WaitUntilEating(second);
            Assert.That(second.IsEating, Is.True);
            Assert.That(table.OccupiedSeats, Is.EqualTo(1));
        }

        [Test]
        public void DirtyTableDoesNotBlockACleanNeighbor()
        {
            DiningArea hall = DiningArea.Create(root.transform, new[]
            {
                new Vector3(2f, 0f, 2f),
                new Vector3(2f, 0f, -2f)
            });
            hall.Tables[0].LeaveMealTrash(0);
            Assert.That(hall.Tables[0].HasAvailableSeat, Is.False);
            Assert.That(hall.Tables[1].HasAvailableSeat, Is.True);
            serving.Configure(queue, inventory, wallet, servingPoint,
                new[] { new Vector3(-8.2f, 0f, -4.4f) }, stock, hall, drop);
            FillQueue();
            Load();
            AtCounter();
            CustomerAgent guest = queue.ReadyCustomer;
            serving.Advance(0.01f);
            serving.Advance(BurgerServingZone.HandoffDuration);
            WaitUntilEating(guest);
            Assert.That(guest.IsEating, Is.True);
            Assert.That(hall.Tables[0].OccupiedSeats, Is.Zero);
            Assert.That(hall.Tables[1].OccupiedSeats, Is.EqualTo(1));
        }

        void WaitUntilEating(CustomerAgent guest)
        {
            guest.AdvanceDeparture(0.4f);
            for (int i = 0; i < 400 && !guest.IsEating; i++)
                guest.AdvanceDeparture(1f / 60f);
        }

        static void AssertPaperWad(Transform wad)
        {
            Assert.That(wad, Is.Not.Null);
            Assert.That(wad.childCount, Is.GreaterThanOrEqualTo(5));
            Bounds bounds = default;
            bool first = true;
            foreach (Renderer renderer in wad.GetComponentsInChildren<Renderer>())
            {
                if (first)
                {
                    bounds = renderer.bounds;
                    first = false;
                }
                else bounds.Encapsulate(renderer.bounds);
                Color color = renderer.sharedMaterial.color;
                Assert.That(color.r, Is.GreaterThan(0.65f));
                Assert.That(color.g, Is.GreaterThan(0.65f));
                Assert.That(color.b, Is.GreaterThan(0.65f));
            }
            Assert.That(first, Is.False);
            Assert.That(bounds.size.x, Is.GreaterThan(0.45f));
            Assert.That(bounds.size.y, Is.GreaterThan(0.40f));
            Assert.That(bounds.size.z, Is.GreaterThan(0.45f));
        }

        static bool FacesTable(CustomerAgent guest, DiningTable dining)
        {
            Vector3 toward = Flatten(dining.Center - guest.transform.position);
            Vector3 forward = Flatten(guest.transform.forward);
            if (toward.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f) return false;
            return Vector3.Dot(forward.normalized, toward.normalized) > 0.9f;
        }

        static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
