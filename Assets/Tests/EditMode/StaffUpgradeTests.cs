using System.IO;
using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class StaffUpgradeTests
    {
        GameObject root;
        HrOffice office;
        WorkerHiringZone hiring;
        RestaurantWallet wallet;
        BurgerInventory player;
        StaffUpgradeBoard board;
        StaffUpgradeHud hud;
        RectTransform safe;
        ProductionStation grill;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("StaffUpgradeTest");
            Material wall = RuntimeMaterials.Create(new Color(0.45f, 0.32f, 0.18f));
            Material floor = RuntimeMaterials.Create(new Color(0.76f, 0.62f, 0.42f));
            ShopLayout.CreateFloor(root.transform, floor);
            ShopLayout.CreateWalls(root.transform, wall);
            office = HrOffice.Create(root.transform, wall, floor);

            grill = root.AddComponent<ProductionStation>();
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            grill.Configure(output, null, null);
            var queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter);
            wallet = root.AddComponent<RestaurantWallet>();
            GameObject carrier = new GameObject("Player");
            carrier.transform.SetParent(root.transform);
            player = carrier.AddComponent<BurgerInventory>();
            player.Configure();
            Transform servingPoint = Point("ServingPoint", ShopLayout.ServingCircle);
            Transform pickupPoint = Point("PickupPoint", ShopLayout.Grill + new Vector3(1.05f, 0f, -2.1f));
            Transform anchor = Point("StockAnchor", ShopLayout.CounterTop);
            var stock = root.AddComponent<CounterStock>();
            stock.Configure(anchor, null);
            var drop = root.AddComponent<CounterDropZone>();
            drop.Configure(stock, servingPoint);
            var cashier = root.AddComponent<BurgerServingZone>();
            cashier.Configure(queue, player, wallet, servingPoint, ShopLayout.Exit, stock, (DiningArea)null, drop);
            hiring = root.AddComponent<WorkerHiringZone>();
            hiring.Configure(grill, cashier, wallet, player, pickupPoint, office.HirePoint, ShopLayout.Aisle, drop,
                office.HireLabel);
            board = root.AddComponent<StaffUpgradeBoard>();
            board.Configure(wallet, hiring, player);

            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas.transform.SetParent(root.transform, false);
            var canvasRect = (RectTransform)canvas.transform;
            canvasRect.pivot = Vector2.zero;
            canvasRect.anchorMin = canvasRect.anchorMax = Vector2.zero;
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.sizeDelta = new Vector2(1080f, 1920f);
            var safeObject = new GameObject("SafeArea", typeof(RectTransform));
            safeObject.transform.SetParent(canvas.transform, false);
            safe = (RectTransform)safeObject.transform;
            safe.pivot = Vector2.zero;
            safe.anchorMin = safe.anchorMax = Vector2.zero;
            safe.anchoredPosition = Vector2.zero;
            safe.sizeDelta = new Vector2(1080f, 1920f);
            var pad = new GameObject("VirtualJoystick", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pad.transform.SetParent(safe, false);
            RectTransform padRect = pad.GetComponent<RectTransform>();
            padRect.anchorMin = padRect.anchorMax = Vector2.zero;
            padRect.pivot = new Vector2(0.5f, 0.5f);
            padRect.anchoredPosition = HudChrome.JoystickPosition;
            padRect.sizeDelta = HudChrome.JoystickSize;
            pad.GetComponent<Image>().raycastTarget = true;
            hud = StaffUpgradeHud.Build(safe, board);
            Canvas.ForceUpdateCanvases();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        Transform Point(string name, Vector3 position)
        {
            Transform point = new GameObject(name).transform;
            point.SetParent(root.transform);
            point.position = position;
            return point;
        }

        void HoldHire(float seconds)
        {
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60f); i++) hiring.Advance(1f / 60f);
        }

        RestaurantWorker HireOne()
        {
            int need = Mathf.Max(1, hiring.HireCost);
            while (wallet.Coins < need) wallet.RecordSale(10);
            player.transform.position = ShopLayout.HrHirePoint + Vector3.up;
            HoldHire(1.5f);
            player.transform.position = Vector3.zero;
            hiring.Advance(0.1f);
            Assert.That(hiring.Worker, Is.Not.Null);
            return hiring.Worker;
        }

        void EnterHr()
        {
            player.transform.position = ShopLayout.HrHirePoint + Vector3.up;
            hud.RefreshNow();
        }

        void LeaveHr()
        {
            player.transform.position = Vector3.zero;
            hud.RefreshNow();
        }

        static float Horizontal(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        [Test]
        public void ApproachingHrShowsTheStaffUpgradePopup()
        {
            LeaveHr();
            Assert.That(hud.IsVisible, Is.False);
            player.transform.position = ShopLayout.HrDoor + Vector3.up;
            hud.RefreshNow();
            Assert.That(ShopLayout.ContainsHrUpgradeRange(player.transform.position), Is.True);
            Assert.That(hud.IsVisible, Is.True);
            Assert.That(hud.Popup.TitleLabel.text, Is.EqualTo("Staff upgrades"));
            Assert.That(hud.Popup.FirstLabel.text, Does.Contain("Speed").And.Contain("50"));
            Assert.That(hud.Popup.SecondLabel.text, Does.Contain("Carry").And.Contain("50"));
            Assert.That(hud.Popup.CloseLabel.text, Is.EqualTo("Close"));
            EnterHr();
            Assert.That(hud.IsVisible, Is.True);
        }

        [Test]
        public void ClickCloseHidesTheStaffPopupWithoutSpending()
        {
            wallet.RestoreProgress(50, 0);
            EnterHr();
            Assert.That(hud.IsVisible, Is.True);
            hud.ClickClose();
            hud.RefreshNow();
            Assert.That(hud.IsVisible, Is.False);
            Assert.That(wallet.Coins, Is.EqualTo(50));
            Assert.That(board.SpeedTier, Is.Zero);
            Assert.That(board.CarryTier, Is.Zero);
            hud.RefreshNow();
            Assert.That(hud.IsVisible, Is.False);
            LeaveHr();
            EnterHr();
            Assert.That(hud.IsVisible, Is.True);
        }

        [Test]
        public void ClickSpeedSpendsFiftyAndMakesTheWorkerFaster()
        {
            RestaurantWorker worker = HireOne();
            Assert.That(worker.WalkSpeed, Is.EqualTo(StaffBoost.BaseWalkSpeed));
            Assert.That(worker.Inventory.Capacity, Is.EqualTo(StaffBoost.BaseCarry));
            grill.Advance(12f);
            worker.transform.position = new Vector3(0f, worker.transform.position.y, 0f);
            Vector3 beforePos = worker.transform.position;
            for (int i = 0; i < 30; i++) worker.Advance(1f / 60f);
            float slowStride = Horizontal(beforePos, worker.transform.position);

            wallet.RestoreProgress(50, wallet.CompletedSales);
            EnterHr();
            hud.ClickSpeed();
            hud.RefreshNow();
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(board.SpeedTier, Is.EqualTo(1));
            Assert.That(worker.WalkSpeed, Is.EqualTo(StaffBoost.WalkSpeed(1)).Within(0.001f));
            Assert.That(worker.WalkSpeed, Is.GreaterThan(StaffBoost.BaseWalkSpeed));
            Assert.That(hud.IsVisible, Is.True);
            Assert.That(hud.Popup.FirstLabel.text, Does.Contain("150"));

            worker.transform.position = new Vector3(0f, worker.transform.position.y, 0f);
            Vector3 fastStart = worker.transform.position;
            for (int i = 0; i < 30; i++) worker.Advance(1f / 60f);
            float fastStride = Horizontal(fastStart, worker.transform.position);
            Assert.That(fastStride, Is.GreaterThan(slowStride * 1.05f));
            Assert.That(player.Capacity, Is.EqualTo(PlayerBoost.BaseCarry));
        }

        [Test]
        public void ClickCarryAddsOneToStaffCarry()
        {
            RestaurantWorker worker = HireOne();
            Assert.That(worker.Inventory.Capacity, Is.EqualTo(2));
            wallet.RestoreProgress(50, wallet.CompletedSales);
            EnterHr();
            hud.ClickCarry();
            hud.RefreshNow();
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(board.CarryTier, Is.EqualTo(1));
            Assert.That(worker.Inventory.Capacity, Is.EqualTo(3));
            Assert.That(hud.Popup.SecondLabel.text, Does.Contain("150"));
            Assert.That(player.Capacity, Is.EqualTo(4));
        }

        [Test]
        public void LeavingHrClosesThePopup()
        {
            EnterHr();
            Assert.That(hud.IsVisible, Is.True);
            LeaveHr();
            Assert.That(hud.IsVisible, Is.False);
            player.transform.position = ShopLayout.BoostPoint + Vector3.up;
            hud.RefreshNow();
            Assert.That(hud.IsVisible, Is.False);
        }

        [Test]
        public void PoorButtonsStayGreyAndLaterHiresUseCurrentTiers()
        {
            wallet.RestoreProgress(40, 0);
            EnterHr();
            Assert.That(hud.Popup.FirstButton.interactable, Is.False);
            Assert.That(hud.Popup.FirstButton.GetComponent<Image>().color, Is.EqualTo(StatUpgradePopup.Disabled));
            hud.ClickSpeed();
            Assert.That(wallet.Coins, Is.EqualTo(40));
            Assert.That(board.SpeedTier, Is.Zero);

            wallet.RestoreProgress(50, 0);
            hud.RefreshNow();
            hud.ClickSpeed();
            Assert.That(board.SpeedTier, Is.EqualTo(1));
            RestaurantWorker worker = HireOne();
            Assert.That(worker.WalkSpeed, Is.EqualTo(StaffBoost.WalkSpeed(1)).Within(0.001f));
        }

        [Test]
        public void PopupStaysOffTheJoystickAndOnlyButtonsRaycast()
        {
            EnterHr();
            Canvas.ForceUpdateCanvases();
            Assert.That(HudChrome.OverlapsJoystickKeepout(hud.Popup.Panel, safe), Is.False);
            foreach (Graphic graphic in hud.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.EqualTo(StatUpgradePopup.IsControl(graphic)), graphic.name);
            Assert.That(hud.Popup.CloseButton, Is.Not.Null);
        }

        [Test]
        public void OldSavesKeepStaffTiersAtZeroAndNewSavesRoundTrip()
        {
            HireOne();
            wallet.RestoreProgress(50, wallet.CompletedSales);
            EnterHr();
            hud.ClickSpeed();
            string directory = Path.Combine(Path.GetTempPath(), "BurgerShopStaffUp-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var persistence = root.AddComponent<RestaurantPersistence>();
                var grillUpgrade = root.AddComponent<GrillUpgradeZone>();
                grillUpgrade.Configure(grill, wallet, player, office.HirePoint);
                persistence.Configure(wallet, grillUpgrade, hiring, null, null, board, directory);
                Assert.That(persistence.Flush(), Is.True);
                Assert.That(new LocalSaveStore(directory).Load(out RestaurantSaveData saved), Is.EqualTo(SaveLoadResult.Loaded));
                Assert.That(saved.version, Is.EqualTo(RestaurantSaveData.CurrentVersion));
                Assert.That(saved.staffSpeedTier, Is.EqualTo(1));
                Assert.That(saved.staffCarryTier, Is.Zero);
                Assert.That(saved.playerSpeedTier, Is.Zero);
                Assert.That(saved.playerCarryTier, Is.Zero);

                var v2 = new RestaurantSaveData
                {
                    version = 2, coins = 40, completedSales = 18, grillLevel = 3, workerHired = true,
                    workerDeliveries = 7, hiredWorkerCount = 1, workerClears = 0
                };
                Assert.That(new LocalSaveStore(directory).Save(v2), Is.True);
                board.RestoreTiers(0, 0);
                persistence.Configure(wallet, grillUpgrade, hiring, null, null, board, directory);
                Assert.That(board.SpeedTier, Is.Zero);
                Assert.That(board.CarryTier, Is.Zero);
                Assert.That(hiring.Worker.WalkSpeed, Is.EqualTo(StaffBoost.BaseWalkSpeed));
                Assert.That(hiring.Worker.Inventory.Capacity, Is.EqualTo(2));
                Assert.That(wallet.Coins, Is.EqualTo(40));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
