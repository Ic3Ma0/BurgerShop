using System.IO;
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
    public sealed class TableUpgradeTests
    {
        GameObject root;
        DiningArea dining;
        ShopExpansion expansion;
        BurgerServingZone serving;
        WorkerHiringZone hiring;
        BurgerInventory player;
        RestaurantWallet wallet;
        CounterStock stock;
        CustomerQueue queue;
        CashFloor cash;
        ProductionStation grill;
        TableUpgradeBoard board;
        TableChangeHud hud;
        RectTransform safe;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("TableUpgradeTest");
            player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform);
            player.Configure();
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            grill = root.AddComponent<ProductionStation>();
            grill.Configure(output, null, null);
            wallet = root.AddComponent<RestaurantWallet>();
            queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter);
            Transform point = new GameObject("Circle").transform;
            point.SetParent(root.transform);
            point.position = ShopLayout.ServingCircle;
            Transform anchor = new GameObject("Anchor").transform;
            anchor.SetParent(root.transform);
            stock = root.AddComponent<CounterStock>();
            stock.Configure(anchor, null);
            CounterDropZone drop = root.AddComponent<CounterDropZone>();
            drop.Configure(stock, point);
            dining = DiningArea.Create(root.transform, ShopLayout.Tables);
            cash = root.AddComponent<CashFloor>();
            serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, player, wallet, point, ShopLayout.Exit, stock, dining, drop, 10, cash);
            cash.Configure(wallet, player.transform, ShopLayout.CounterCash);
            dining.BindCash(cash);
            hiring = root.AddComponent<WorkerHiringZone>();
            hiring.Configure(grill, serving, wallet, player, root.transform, root.transform, ShopLayout.Aisle, drop);
            expansion = ShopExpansion.Create(root.transform, dining, serving, hiring, player, wallet, cash);
            expansion.Restore(false, false, false, 0, false, false, false, false, true);
            board = root.AddComponent<TableUpgradeBoard>();
            board.Configure(dining, expansion, wallet, player);

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
            hud = TableChangeHud.Build(safe, board);
            Canvas.ForceUpdateCanvases();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            ShopLayout.ResetWingLock();
        }

        void Hold(TableUpgradeZone zone, float seconds)
        {
            player.transform.position = zone.PadPosition + Vector3.up;
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60f); i++) zone.Advance(1f / 60f);
        }

        void HoldFacility(FacilityUnlockZone pad, float seconds)
        {
            player.transform.position = pad.PadPosition + Vector3.up;
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60f); i++) pad.Advance(1f / 60f);
        }

        void Leave(TableUpgradeZone zone)
        {
            player.transform.position = Vector3.zero;
            zone.Advance(0.1f);
            hud.RefreshNow();
        }

        void FillQueue()
        {
            // BS-SPEC-046 moved arrival to the exterior sidewalk. Wait for arrival
            // before testing table pay/eating duration, while keeping a bounded timeout.
            for (int i = 0; i < 3600 && queue.ReadyCustomer == null; i++) queue.Advance(1f / 60f);
            Assert.That(queue.ReadyCustomer, Is.Not.Null, "Customer must reach the cashier from the sidewalk");
        }

        CustomerAgent FinishMealOn(DiningTable table)
        {
            FillQueue();
            grill.Advance(12f);
            player.TryCollectFrom(grill);
            player.transform.position = serving.ServingPosition + Vector3.up;
            Assert.That(serving.DropZone.TryDepositFrom(player), Is.True);
            CustomerAgent customer = queue.ReadyCustomer;
            Assert.That(serving.TryServeFrom(player), Is.True);
            serving.Advance(BurgerServingZone.HandoffDuration);
            customer.AdvanceDeparture(0.4f);
            for (int i = 0; i < 1200 && !customer.IsEating; i++)
                customer.AdvanceDeparture(1f / 60f);
            Assert.That(customer.IsEating, Is.True, "Customer must sit after the indoor aisle walk");
            customer.AdvanceDeparture(table.EatSeconds + 0.2f);
            return customer;
        }

        [Test]
        public void CatalogPaysMoreThanStarterAndMatchesCashFloor()
        {
            Assert.That(TableSetCatalog.StarterPay, Is.EqualTo(CashFloor.DiningDrop));
            Assert.That(TableSetCatalog.Get(TableSetId.Bistro).Cost, Is.EqualTo(50));
            Assert.That(TableSetCatalog.Get(TableSetId.Diner).Cost, Is.EqualTo(80));
            Assert.That(TableSetCatalog.Get(TableSetId.Patio).Cost, Is.EqualTo(120));
            Assert.That(TableSetCatalog.Get(TableSetId.Bistro).MealPay, Is.EqualTo(12));
            Assert.That(TableSetCatalog.Get(TableSetId.Diner).MealPay, Is.EqualTo(12));
            Assert.That(TableSetCatalog.Get(TableSetId.Patio).MealPay, Is.EqualTo(16));
            Assert.That(TableSetCatalog.Get(TableSetId.Diner).EatSeconds, Is.EqualTo(2.4f));
            Assert.That(TableSetCatalog.Get(TableSetId.Patio).EatSeconds, Is.EqualTo(3.6f));
            Assert.That(TableSetCatalog.Get(TableSetId.Bistro).EatSeconds, Is.EqualTo(3f));
            for (int i = 0; i < TableSetCatalog.ChoiceCount; i++)
                Assert.That(TableSetCatalog.Get(TableSetCatalog.Choices[i]).MealPay,
                    Is.GreaterThan(TableSetCatalog.StarterPay));
        }

        [Test]
        public void BuiltStarterTablesGetPurplePadsAndUnbuiltExtraDoesNot()
        {
            Assert.That(board.ZoneCount, Is.EqualTo(2));
            Assert.That(GameObject.Find("Chair3UnlockPad"), Is.Null);
            Assert.That(expansion.HasExtraTable, Is.False);
            for (int i = 0; i < 2; i++)
            {
                TableUpgradeZone zone = board.ZoneAt(i);
                Assert.That(zone, Is.Not.Null);
                Assert.That(zone.PadPosition, Is.EqualTo(ShopLayout.TableUpgradePad(ShopLayout.Tables[i])));
                Assert.That(zone.Cost, Is.EqualTo(80));
                Color color = zone.GetComponent<Renderer>().sharedMaterial.color;
                Assert.That(color.b, Is.GreaterThan(color.g));
            }
            Assert.That(board.ZoneAt(2), Is.Null);
        }

        [Test]
        public void BuyingExtraTableAddsAFourthUpgradePad()
        {
            wallet.RestoreProgress(150, 0);
            HoldFacility(expansion.TablePad, 3f);
            Assert.That(expansion.HasExtraTable, Is.True);
            Assert.That(board.ZoneCount, Is.EqualTo(3));
            Assert.That(board.ZoneAt(3).Table, Is.SameAs(expansion.ExtraTable));
            Assert.That(board.ZoneAt(3).PadPosition, Is.EqualTo(ShopLayout.TableUpgradePad(ShopLayout.ExtraTable)));
        }

        [Test]
        public void BuyingFourSeatAndSquareAddsUpgradePadsOnlyAfterPurchase()
        {
            Assert.That(board.ZoneCount, Is.EqualTo(2));
            Assert.That(GameObject.Find("Chair4UnlockPad"), Is.Null);
            Assert.That(GameObject.Find("Chair5UnlockPad"), Is.Null);
            wallet.RestoreProgress(200, 0);
            HoldFacility(expansion.FourSeatPad, 3f);
            Assert.That(board.ZoneCount, Is.EqualTo(3));
            Assert.That(board.ZoneAt(4).Table, Is.SameAs(expansion.FourSeatTable));
            Assert.That(board.ZoneAt(4).PadPosition, Is.EqualTo(ShopLayout.TableUpgradePad(ShopLayout.FourSeatTable)));
            Assert.That(board.ZoneAt(5), Is.Null);
            wallet.RestoreProgress(150, 0);
            HoldFacility(expansion.SquarePad, 3f);
            Assert.That(board.ZoneCount, Is.EqualTo(4));
            Assert.That(board.ZoneAt(5).Table, Is.SameAs(expansion.SquareTable));
            Assert.That(board.ZoneAt(5).PadPosition, Is.EqualTo(ShopLayout.TableUpgradePad(ShopLayout.SquareTable)));
            string[] chairs = { "ChairA", "ChairB", "ChairC", "ChairD" };
            for (int i = 0; i < chairs.Length; i++)
            {
                Transform chair = board.ZoneAt(4).Table.transform.Find(chairs[i]);
                Vector3 toward = board.ZoneAt(4).Table.Center - chair.position;
                toward.y = 0f;
                Vector3 forward = chair.forward;
                forward.y = 0f;
                Assert.That(Vector3.Dot(forward.normalized, toward.normalized), Is.GreaterThan(0.9f), chairs[i]);
            }
        }

        [Test]
        public void InvestingEightyOpensTableChangePopupWithoutRaisingPayYet()
        {
            wallet.RestoreProgress(80, 0);
            TableUpgradeZone zone = board.ZoneAt(0);
            Hold(zone, 2f);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(zone.PendingChoice, Is.True);
            Assert.That(dining.Tables[0].MealPay, Is.EqualTo(10));
            hud.RefreshNow();
            Assert.That(hud.IsVisible, Is.True);
            string copy = hud.Popup.CombinedCopy().ToLowerInvariant();
            Assert.That(hud.Popup.TitleLabel.text, Is.EqualTo("TABLE CHANGE"));
            Assert.That(hud.Popup.LevelLabel.text, Is.EqualTo("Level 2"));
            Assert.That(copy, Does.Contain("bistro"));
            Assert.That(copy, Does.Contain("diner"));
            Assert.That(copy, Does.Contain("patio"));
            Assert.That(copy, Does.Contain("value"));
            Assert.That(copy, Does.Contain("turnover"));
            Assert.That(copy, Does.Contain("premium"));
            Assert.That(copy, Does.Contain("12 / meal"));
            Assert.That(copy, Does.Contain("16 / meal"));
            Assert.That(copy, Does.Contain("2.4s"));
            Assert.That(copy, Does.Contain("3.6s"));
            Assert.That(hud.Popup.SelectButtons[0].interactable, Is.True);
            Assert.That(hud.Popup.SelectButtons[1].interactable, Is.True);
            Assert.That(hud.Popup.SelectButtons[2].interactable, Is.False, "Patio still needs the 40 premium difference");
            Assert.That(hud.Popup.SelectLabels[2].text.ToLowerInvariant(), Does.Contain("need"));
            Assert.That(copy, Does.Contain("select"));
            Assert.That(copy, Does.Not.Contain("diamond"));
            Assert.That(copy, Does.Not.Contain("ad"));
            Assert.That(copy, Does.Not.Contain("free"));
        }

        [Test]
        public void SelectingPatioRecolorsChairsFacingTheTableAndPaysSixteen()
        {
            wallet.RestoreProgress(120, 0);
            Hold(board.ZoneAt(0), 2f);
            hud.RefreshNow();
            hud.ClickSelect(2);
            DiningTable table = dining.Tables[0];
            Assert.That(table.SetId, Is.EqualTo(TableSetId.Patio));
            Assert.That(table.MealPay, Is.EqualTo(16));
            Assert.That(board.ZoneAt(0).PendingChoice, Is.False);
            Assert.That(board.ZoneAt(0).gameObject.activeSelf, Is.False);
            hud.RefreshNow();
            Assert.That(hud.IsVisible, Is.False);
            Transform chair = table.transform.Find("ChairA");
            Vector3 toward = table.Center - chair.position;
            toward.y = 0f;
            Vector3 forward = chair.forward;
            forward.y = 0f;
            Assert.That(Vector3.Dot(forward.normalized, toward.normalized), Is.GreaterThan(0.9f));
            CustomerAgent guest = FinishMealOn(table);
            Assert.That(guest.IsEating, Is.False);
            Assert.That(cash.GroundValue, Is.EqualTo(26));
        }

        [Test]
        public void DinerShortensEatingAndStillPaysMoreThanStarter()
        {
            dining.Tables[0].ApplySet(TableSetId.Diner);
            FillQueue();
            grill.Advance(12f);
            player.TryCollectFrom(grill);
            player.transform.position = serving.ServingPosition + Vector3.up;
            Assert.That(serving.DropZone.TryDepositFrom(player), Is.True);
            CustomerAgent customer = queue.ReadyCustomer;
            Assert.That(serving.TryServeFrom(player), Is.True);
            serving.Advance(BurgerServingZone.HandoffDuration);
            customer.AdvanceDeparture(0.4f);
            for (int i = 0; i < 1200 && !customer.IsEating; i++)
                customer.AdvanceDeparture(1f / 60f);
            Assert.That(customer.IsEating, Is.True, "Customer must sit after the indoor aisle walk");
            customer.AdvanceDeparture(2.35f);
            Assert.That(customer.IsEating, Is.True);
            customer.AdvanceDeparture(0.2f);
            Assert.That(customer.IsEating, Is.False);
            Assert.That(cash.GroundValue, Is.EqualTo(22));
        }

        [Test]
        public void PatioStillCostsThePremiumDifferenceAfterThePadDeposit()
        {
            wallet.RestoreProgress(80, 0);
            Hold(board.ZoneAt(0), 2f);
            hud.RefreshNow();
            hud.ClickSelect(2);
            Assert.That(dining.Tables[0].SetId, Is.EqualTo(TableSetId.Starter));
            Assert.That(board.ZoneAt(0).PendingChoice, Is.True);
            Assert.That(wallet.Coins, Is.Zero);
            wallet.CollectCoins(40);
            hud.RefreshNow();
            hud.ClickSelect(2);
            Assert.That(dining.Tables[0].SetId, Is.EqualTo(TableSetId.Patio));
            Assert.That(wallet.Coins, Is.Zero);
        }

        [Test]
        public void PartialInvestStopsAndResumeOnlyPaysTheRemainder()
        {
            wallet.RestoreProgress(40, 0);
            TableUpgradeZone zone = board.ZoneAt(1);
            Hold(zone, 2f);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(zone.Remaining, Is.EqualTo(40));
            Assert.That(zone.PendingChoice, Is.False);
            hud.RefreshNow();
            Assert.That(hud.IsVisible, Is.False);
            Leave(zone);
            wallet.CollectCoins(40);
            Hold(zone, 2f);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(zone.PendingChoice, Is.True);
        }

        [Test]
        public void CloseWithoutChoosingReopensAfterLeavingAndDoesNotChargeAgain()
        {
            wallet.RestoreProgress(80, 0);
            TableUpgradeZone zone = board.ZoneAt(0);
            Hold(zone, 2f);
            hud.RefreshNow();
            hud.ClickClose();
            Assert.That(hud.IsVisible, Is.False);
            Assert.That(zone.PendingChoice, Is.True);
            Leave(zone);
            Hold(zone, 1f);
            hud.RefreshNow();
            Assert.That(hud.IsVisible, Is.True);
            Assert.That(wallet.Coins, Is.Zero);
        }

        [Test]
        public void PopupStaysOffTheJoystickAndOnlySelectAndCloseRaycast()
        {
            wallet.RestoreProgress(80, 0);
            Hold(board.ZoneAt(0), 2f);
            hud.RefreshNow();
            Canvas.ForceUpdateCanvases();
            Assert.That(HudChrome.OverlapsJoystickKeepout(hud.Popup.Panel, safe), Is.False);
            foreach (Graphic graphic in hud.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.EqualTo(TableChangePopup.IsControl(graphic)), graphic.name);
        }

        [Test]
        public void StaffStandingOnThePadDoesNotInvest()
        {
            wallet.RestoreProgress(80, 0);
            TableUpgradeZone zone = board.ZoneAt(0);
            player.transform.position = Vector3.zero;
            zone.Advance(2f);
            Assert.That(wallet.Coins, Is.EqualTo(80));
            Assert.That(zone.Invested, Is.Zero);
        }

        [Test]
        public void TablePadAndFacilityPadDoNotChargeTogether()
        {
            TableUpgradeZone tablePad = board.ZoneAt(0);
            tablePad.transform.position = new Vector3(3f, 0.02f, 0f);
            expansion.GrillPad.transform.position = new Vector3(4f, 0.02f, 0f);
            wallet.RestoreProgress(1000, 0);
            player.transform.position = new Vector3(3.75f, 1f, 0f);
            for (int i = 0; i < 24; i++)
            {
                tablePad.Advance(1f / 60f);
                expansion.GrillPad.Advance(1f / 60f);
            }
            Assert.That(tablePad.Invested + expansion.GrillPad.Invested, Is.EqualTo(20));
            Assert.That(tablePad.Invested == 0 || expansion.GrillPad.Invested == 0, Is.True);
        }

        [Test]
        public void SaveRestoresChosenSetAndPendingChoiceWithoutCharging()
        {
            string directory = Path.Combine(Path.GetTempPath(), "BurgerShopTableSet-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                wallet.RestoreProgress(200, 0);
                Hold(board.ZoneAt(0), 2f);
                hud.RefreshNow();
                hud.ClickSelect(2);
                Hold(board.ZoneAt(1), 2f);
                var upgrade = root.AddComponent<GrillUpgradeZone>();
                upgrade.Configure(grill, wallet, player, root.transform);
                var persistence = root.AddComponent<RestaurantPersistence>();
                persistence.Configure(wallet, upgrade, hiring, null, expansion, null, null, directory, tables: board);
                Assert.That(persistence.Flush(), Is.True);
                Assert.That(new LocalSaveStore(directory).Load(out RestaurantSaveData data), Is.EqualTo(SaveLoadResult.Loaded));
                Assert.That(data.version, Is.EqualTo(RestaurantSaveData.CurrentVersion));
                Assert.That(data.table0Set, Is.EqualTo((int)TableSetId.Patio));
                Assert.That(data.table1Investment, Is.EqualTo(80));
                Assert.That(data.table1Set, Is.Zero);
                Assert.That(data.coins, Is.EqualTo(0));

                Object.DestroyImmediate(persistence);
                dining.Tables[0].ApplySet(TableSetId.Starter);
                board.ZoneAt(1).Restore(0, 0);
                persistence = root.AddComponent<RestaurantPersistence>();
                persistence.Configure(wallet, upgrade, hiring, null, expansion, null, null, directory, tables: board);
                Assert.That(dining.Tables[0].SetId, Is.EqualTo(TableSetId.Patio));
                Assert.That(dining.Tables[0].MealPay, Is.EqualTo(16));
                Assert.That(board.ZoneAt(1).PendingChoice, Is.True);
                Assert.That(wallet.Coins, Is.Zero);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void Version7SaveKeepsCoinsAndLeavesTablesOnStarter()
        {
            string directory = Path.Combine(Path.GetTempPath(), "BurgerShopTableV7-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var upgrade = root.AddComponent<GrillUpgradeZone>();
                upgrade.Configure(grill, wallet, player, root.transform);
                var v7 = new RestaurantSaveData
                {
                    version = 7, coins = 640, completedSales = 18, grillLevel = 1, workerHired = false,
                    workerDeliveries = 0, hiredWorkerCount = 0, workerClears = 0, boughtExtraTable = true,
                    tableInvestment = 150
                };
                Assert.That(new LocalSaveStore(directory).Save(v7), Is.True);
                var persistence = root.AddComponent<RestaurantPersistence>();
                persistence.Configure(wallet, upgrade, hiring, null, expansion, null, null, directory, tables: board);
                Assert.That(wallet.Coins, Is.EqualTo(640));
                Assert.That(expansion.HasExtraTable, Is.True);
                Assert.That(dining.Tables[0].SetId, Is.EqualTo(TableSetId.Starter));
                Assert.That(board.ZoneAt(0).Invested, Is.Zero);
                Assert.That(board.ZoneAt(3), Is.Not.Null);
                Assert.That(board.ZoneAt(3).SetId, Is.EqualTo(TableSetId.Starter));
                Assert.That(expansion.HasFourSeatTable, Is.False);
                Assert.That(expansion.HasSquareTable, Is.False);
                Assert.That(board.ZoneAt(4), Is.Null);
                Assert.That(board.ZoneAt(5), Is.Null);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void SaveRestoresFourSeatPatioWithoutChargingAgain()
        {
            string directory = Path.Combine(Path.GetTempPath(), "BurgerShopFourSeat-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                wallet.RestoreProgress(320, 0);
                HoldFacility(expansion.FourSeatPad, 3f);
                Hold(board.ZoneAt(4), 2f);
                hud.RefreshNow();
                hud.ClickSelect(2);
                var upgrade = root.AddComponent<GrillUpgradeZone>();
                upgrade.Configure(grill, wallet, player, root.transform);
                var persistence = root.AddComponent<RestaurantPersistence>();
                persistence.Configure(wallet, upgrade, hiring, null, expansion, null, null, directory, tables: board);
                Assert.That(persistence.Flush(), Is.True);
                Assert.That(new LocalSaveStore(directory).Load(out RestaurantSaveData data), Is.EqualTo(SaveLoadResult.Loaded));
                Assert.That(data.version, Is.EqualTo(RestaurantSaveData.CurrentVersion));
                Assert.That(data.boughtFourSeatTable, Is.True);
                Assert.That(data.fourSeatSet, Is.EqualTo((int)TableSetId.Patio));
                Assert.That(data.coins, Is.EqualTo(0));

                Object.DestroyImmediate(persistence);
                expansion.FourSeatTable.ApplySet(TableSetId.Starter);
                persistence = root.AddComponent<RestaurantPersistence>();
                persistence.Configure(wallet, upgrade, hiring, null, expansion, null, null, directory, tables: board);
                Assert.That(expansion.HasFourSeatTable, Is.True);
                Assert.That(expansion.FourSeatTable.SetId, Is.EqualTo(TableSetId.Patio));
                Assert.That(expansion.FourSeatTable.MealPay, Is.EqualTo(16));
                Assert.That(board.ZoneAt(4).HasChosenSet, Is.True);
                Assert.That(wallet.Coins, Is.Zero);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
