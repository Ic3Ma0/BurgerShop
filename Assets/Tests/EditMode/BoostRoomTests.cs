using System.IO;
using BurgerShop.Core;
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
    public sealed class BoostRoomTests
    {
        GameObject root;
        BoostRoom room;
        BoostUpgradeZone boost;
        RestaurantWallet wallet;
        BurgerInventory player;
        PlayerMotor motor;
        BurgerInventory staffBag;
        PlayerUpgradeHud hud;
        RectTransform safe;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("BoostRoomTest");
            Material wall = RuntimeMaterials.Create(new Color(0.45f, 0.32f, 0.18f));
            Material floor = RuntimeMaterials.Create(new Color(0.76f, 0.62f, 0.42f));
            ShopLayout.CreateFloor(root.transform, floor);
            ShopLayout.CreateWalls(root.transform, wall);
            HrOffice.Create(root.transform, wall, floor);
            room = BoostRoom.Create(root.transform, wall, floor);

            wallet = root.AddComponent<RestaurantWallet>();
            GameObject carrier = new GameObject("Player");
            carrier.transform.SetParent(root.transform);
            carrier.AddComponent<CharacterController>();
            motor = carrier.AddComponent<PlayerMotor>();
            player = carrier.AddComponent<BurgerInventory>();
            player.Configure();
            GameObject staff = new GameObject("Staff");
            staff.transform.SetParent(root.transform);
            staffBag = staff.AddComponent<BurgerInventory>();
            staffBag.Configure(2);
            boost = root.AddComponent<BoostUpgradeZone>();
            boost.Configure(wallet, player, motor, room.BoostPoint, room.BoostLabel);

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
            hud = PlayerUpgradeHud.Build(safe, boost);
            Canvas.ForceUpdateCanvases();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        void Enter()
        {
            player.transform.position = ShopLayout.BoostPoint + Vector3.up;
            boost.Advance(0.01f);
            hud.RefreshNow();
        }

        void Leave()
        {
            player.transform.position = Vector3.zero;
            boost.Advance(0.01f);
            hud.RefreshNow();
        }

        static float Horizontal(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        [Test]
        public void ShopHasBoostRoomWithOpenDoorAndEnglishLabels()
        {
            Assert.That(room.transform.Find("BoostWall-Z"), Is.Not.Null);
            Assert.That(room.transform.Find("BoostWall+X"), Is.Not.Null);
            Assert.That(room.transform.Find("BoostWall-X"), Is.Not.Null);
            Assert.That(room.transform.Find("TrainingPad"), Is.Not.Null);
            Assert.That(room.transform.Find("TrainingBar"), Is.Not.Null);
            Assert.That(GameObject.Find("BoostUpgradeSpot"), Is.Null);
            Assert.That(Vector3.Distance(room.BoostPoint.position, ShopLayout.BoostPoint), Is.LessThan(0.001f));
            Assert.That(room.BoostLabel.text, Does.Contain("Player upgrades"));
            Assert.That(GameObject.Find("BoostHallSign").GetComponent<TextMesh>().text, Is.EqualTo("Boost"));
            Assert.That(GameObject.Find("TrainingSign").GetComponent<TextMesh>().text, Is.EqualTo("Training"));
            Assert.That(room.BoostLabel.text, Does.Not.Contain("pizza").IgnoreCase);
            Assert.That(room.BoostLabel.text, Does.Not.Contain("Kingshot"));
            Assert.That(room.BoostLabel.text, Does.Not.Contain("Doughnut"));
        }

        [Test]
        public void DoorwayIsOpenAndRoomSitsOutsideWithoutBlockingHall()
        {
            Physics.SyncTransforms();
            Assert.That(Physics.CheckBox(ShopLayout.BoostDoor + Vector3.up * 0.6f, new Vector3(0.9f, 0.4f, 0.2f)), Is.False);
            Assert.That(ShopLayout.BoostRoomCenter.z, Is.LessThan(-ShopLayout.WallHalf));
            Assert.That(ShopLayout.BoostStation.z, Is.LessThan(-ShopLayout.WallHalf));
            Assert.That(ShopLayout.ContainsBoostRoom(ShopLayout.BoostPoint), Is.True);
            Assert.That(ShopLayout.ContainsBoostUpgradeRange(ShopLayout.BoostDoor + Vector3.up), Is.True);
            Assert.That(ShopLayout.ContainsBoostRoom(ShopLayout.HiringSpot), Is.False);
            Assert.That(ShopLayout.ContainsBoostRoom(ShopLayout.TableUnlock), Is.False);
            Assert.That(ShopLayout.ContainsBoostRoom(ShopLayout.ExtraTable), Is.False);
            Assert.That(ShopLayout.ContainsBoostRoom(ShopLayout.GrillUnlock), Is.False);
            Assert.That(ShopLayout.ContainsBoostRoom(ShopLayout.CounterUnlock), Is.False);
            Assert.That(Horizontal(ShopLayout.BoostStation, ShopLayout.HrDoor), Is.GreaterThan(8f));
            Assert.That(Horizontal(ShopLayout.BoostStation, ShopLayout.Counter), Is.GreaterThan(8f));
            Assert.That(Horizontal(ShopLayout.BoostStation, ShopLayout.Tables[0]), Is.GreaterThan(8f));
            Assert.That(Horizontal(ShopLayout.BoostStation, ShopLayout.Grill), Is.GreaterThan(8f));
            Assert.That(Horizontal(ShopLayout.BoostDoor, ShopLayout.TableUnlock), Is.GreaterThan(5f));
            Assert.That(Horizontal(ShopLayout.BoostDoor, ShopLayout.HrDoor), Is.GreaterThan(8f));
        }

        [Test]
        public void EnteringBoostRoomShowsThePlayerUpgradePopup()
        {
            Leave();
            Assert.That(hud.IsVisible, Is.False);
            player.transform.position = ShopLayout.BoostDoor + Vector3.up;
            hud.RefreshNow();
            Assert.That(ShopLayout.ContainsBoostUpgradeRange(player.transform.position), Is.True);
            Assert.That(hud.IsVisible, Is.True);
            Assert.That(hud.Popup.TitleLabel.text, Is.EqualTo("Player upgrades"));
            Assert.That(hud.Popup.FirstLabel.text, Does.Contain("Speed").And.Contain("50"));
            Assert.That(hud.Popup.SecondLabel.text, Does.Contain("Carry").And.Contain("50"));
            Assert.That(hud.Popup.CloseLabel.text, Is.EqualTo("X"));
            Enter();
            Assert.That(hud.IsVisible, Is.True);
        }

        [Test]
        public void ClickSpeedAndCarrySpendIndependentlyAndLeaveCloses()
        {
            int[] prices = { 50, 150, 300, 450, 600 };
            float[] speeds = { 6.325f, 7.15f, 7.975f, 8.8f, 9.625f };
            wallet.RestoreProgress(3100, 0);
            Assert.That(player.Capacity, Is.EqualTo(4));
            Assert.That(motor.MoveSpeed, Is.EqualTo(5.5f).Within(0.001f));
            Assert.That(staffBag.Capacity, Is.EqualTo(2));

            Enter();
            for (int i = 0; i < 90; i++) boost.Advance(1f / 60f);
            Assert.That(boost.SpeedTier, Is.Zero, "Standing no longer buys a combined tier.");
            Assert.That(wallet.Coins, Is.EqualTo(3100));

            hud.ClickSpeed();
            hud.RefreshNow();
            Assert.That(wallet.Coins, Is.EqualTo(3050));
            Assert.That(boost.SpeedTier, Is.EqualTo(1));
            Assert.That(boost.CarryTier, Is.Zero);
            Assert.That(motor.MoveSpeed, Is.EqualTo(speeds[0]).Within(0.001f));
            Assert.That(player.Capacity, Is.EqualTo(4));
            Assert.That(hud.IsVisible, Is.True);
            Assert.That(hud.Popup.FirstLabel.text, Does.Contain("150"));

            hud.ClickCarry();
            hud.RefreshNow();
            Assert.That(wallet.Coins, Is.EqualTo(3000));
            Assert.That(boost.CarryTier, Is.EqualTo(1));
            Assert.That(player.Capacity, Is.EqualTo(5));
            Assert.That(staffBag.Capacity, Is.EqualTo(2));
            Assert.That(hud.Popup.SecondLabel.text, Does.Contain("150"));

            for (int tier = 2; tier <= 5; tier++)
            {
                hud.ClickSpeed();
                hud.ClickCarry();
                hud.RefreshNow();
                Assert.That(boost.SpeedTier, Is.EqualTo(tier));
                Assert.That(boost.CarryTier, Is.EqualTo(tier));
                Assert.That(motor.MoveSpeed, Is.EqualTo(speeds[tier - 1]).Within(0.001f));
                Assert.That(player.Capacity, Is.EqualTo(4 + tier));
                Assert.That(wallet.Coins, Is.EqualTo(3100 - 2 * Sum(prices, tier)));
                Assert.That(staffBag.Capacity, Is.EqualTo(2));
            }

            Assert.That(boost.IsMaxLevel, Is.True);
            Assert.That(hud.Popup.FirstLabel.text, Does.Contain("MAX"));
            Assert.That(hud.Popup.SecondLabel.text, Does.Contain("MAX"));
            hud.ClickSpeed();
            hud.ClickCarry();
            Assert.That(wallet.Coins, Is.Zero);
            Leave();
            Assert.That(hud.IsVisible, Is.False);
        }

        [Test]
        public void ClickCloseHidesThePopupWithoutSpending()
        {
            wallet.RestoreProgress(50, 0);
            Enter();
            Assert.That(hud.IsVisible, Is.True);
            hud.ClickClose();
            hud.RefreshNow();
            Assert.That(hud.IsVisible, Is.False);
            Assert.That(wallet.Coins, Is.EqualTo(50));
            Assert.That(boost.SpeedTier, Is.Zero);
            Assert.That(boost.CarryTier, Is.Zero);
            Assert.That(player.Capacity, Is.EqualTo(4));
            Assert.That(motor.MoveSpeed, Is.EqualTo(5.5f).Within(0.001f));
            hud.RefreshNow();
            Assert.That(hud.IsVisible, Is.False, "X keeps the sheet closed while still in the room.");
            Leave();
            Enter();
            Assert.That(hud.IsVisible, Is.True);
        }

        [Test]
        public void InsufficientFundsStayGreyAndHallDoesNotOpenThePopup()
        {
            wallet.RestoreProgress(49, 0);
            Enter();
            Assert.That(hud.Popup.FirstButton.interactable, Is.False);
            Assert.That(hud.Popup.FirstButton.GetComponent<Image>().color, Is.EqualTo(StatUpgradePopup.Disabled));
            hud.ClickSpeed();
            hud.ClickCarry();
            Assert.That(wallet.Coins, Is.EqualTo(49));
            Assert.That(boost.SpeedTier, Is.Zero);
            Assert.That(player.Capacity, Is.EqualTo(4));
            Leave();
            Assert.That(hud.IsVisible, Is.False);
            player.transform.position = ShopLayout.HiringSpot + Vector3.up;
            wallet.RestoreProgress(50, 0);
            hud.RefreshNow();
            Assert.That(hud.IsVisible, Is.False);
            hud.ClickSpeed();
            Assert.That(boost.SpeedTier, Is.Zero);
            Assert.That(wallet.Coins, Is.EqualTo(50));
        }

        [Test]
        public void PopupStaysOffTheJoystickAndOnlyControlsRaycast()
        {
            Enter();
            Canvas.ForceUpdateCanvases();
            Assert.That(HudChrome.OverlapsJoystickKeepout(hud.Popup.Panel, safe), Is.False);
            foreach (Graphic graphic in hud.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.EqualTo(StatUpgradePopup.IsControl(graphic)), graphic.name);
        }

        [Test]
        public void OldSavesMigrateBoostLevelToBothPlayerTiers()
        {
            var v1 = new RestaurantSaveData
            {
                version = 1, coins = 88, completedSales = 18, grillLevel = 3,
                workerHired = true, workerDeliveries = 7
            };
            Assert.That(v1.ResolvedPlayerSpeedTier, Is.EqualTo(0));
            Assert.That(v1.ResolvedPlayerCarryTier, Is.EqualTo(0));
            boost.RestoreTiers(v1.ResolvedPlayerSpeedTier, v1.ResolvedPlayerCarryTier);
            wallet.RestoreProgress(v1.coins, v1.completedSales);
            Assert.That(player.Capacity, Is.EqualTo(4));
            Assert.That(motor.MoveSpeed, Is.EqualTo(5.5f).Within(0.001f));
            Assert.That(wallet.Coins, Is.EqualTo(88));

            var v3 = new RestaurantSaveData
            {
                version = 3, coins = 120, completedSales = 18, grillLevel = 2,
                workerHired = true, workerDeliveries = 4, hiredWorkerCount = 1, workerClears = 1,
                boostLevel = 3
            };
            Assert.That(v3.ResolvedPlayerSpeedTier, Is.EqualTo(3));
            Assert.That(v3.ResolvedPlayerCarryTier, Is.EqualTo(3));
            boost.RestoreTiers(v3.ResolvedPlayerSpeedTier, v3.ResolvedPlayerCarryTier);
            Assert.That(boost.SpeedTier, Is.EqualTo(3));
            Assert.That(boost.CarryTier, Is.EqualTo(3));
            Assert.That(player.Capacity, Is.EqualTo(7));
            Assert.That(motor.MoveSpeed, Is.EqualTo(7.975f).Within(0.001f));
            Assert.That(staffBag.Capacity, Is.EqualTo(2));
        }

        [Test]
        public void RestoreTiersAndNewSavesRoundTripIndependently()
        {
            wallet.RestoreProgress(40, 2);
            boost.RestoreTiers(4, 2);
            Assert.That(player.Capacity, Is.EqualTo(6));
            Assert.That(motor.MoveSpeed, Is.EqualTo(8.8f).Within(0.001f));
            Assert.That(wallet.Coins, Is.EqualTo(40));
            Assert.That(staffBag.Capacity, Is.EqualTo(2));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => boost.RestoreTiers(6, 0));

            string directory = Path.Combine(Path.GetTempPath(), "BurgerShopPlayerUp-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var persistence = root.AddComponent<RestaurantPersistence>();
                var grill = root.AddComponent<ProductionStation>();
                Transform output = new GameObject("Output").transform;
                output.SetParent(root.transform);
                grill.Configure(output, null, null);
                var grillUpgrade = root.AddComponent<GrillUpgradeZone>();
                grillUpgrade.Configure(grill, wallet, player, room.BoostPoint);
                var hiring = root.AddComponent<WorkerHiringZone>();
                hiring.Configure(grill, null, wallet, player, room.BoostPoint, room.BoostPoint, ShopLayout.Aisle, null);
                persistence.Configure(wallet, grillUpgrade, hiring, boost, null, null, directory);
                Assert.That(persistence.Flush(), Is.True);
                Assert.That(new LocalSaveStore(directory).Load(out RestaurantSaveData saved), Is.EqualTo(SaveLoadResult.Loaded));
                Assert.That(saved.version, Is.EqualTo(RestaurantSaveData.CurrentVersion));
                Assert.That(saved.playerSpeedTier, Is.EqualTo(4));
                Assert.That(saved.playerCarryTier, Is.EqualTo(2));
                Assert.That(saved.staffSpeedTier, Is.Zero);
                Assert.That(saved.staffCarryTier, Is.Zero);

                var v4 = new RestaurantSaveData
                {
                    version = 4, coins = 77, completedSales = 18, grillLevel = 3, workerHired = false,
                    workerDeliveries = 0, hiredWorkerCount = 0, workerClears = 0, boostLevel = 2,
                    staffSpeedTier = 1, staffCarryTier = 0
                };
                Assert.That(new LocalSaveStore(directory).Save(v4), Is.True);
                persistence.Configure(wallet, grillUpgrade, hiring, boost, null, null, directory);
                Assert.That(boost.SpeedTier, Is.EqualTo(2));
                Assert.That(boost.CarryTier, Is.EqualTo(2));
                Assert.That(wallet.Coins, Is.EqualTo(77));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        static int Sum(int[] values, int count)
        {
            int total = 0;
            for (int i = 0; i < count; i++) total += values[i];
            return total;
        }
    }
}
