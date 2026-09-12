using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class ShopExpansionTests
    {
        GameObject root;
        ShopExpansion expansion;
        DiningArea dining;
        BurgerServingZone serving;
        WorkerHiringZone hiring;
        BurgerInventory player;
        RestaurantWallet wallet;
        CounterStock stock;
        CustomerQueue queue;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ExpansionTest");
            player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform);
            player.Configure();
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            var grill = root.AddComponent<ProductionStation>();
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
            serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, player, wallet, point, ShopLayout.Exit, stock, dining, drop);
            hiring = root.AddComponent<WorkerHiringZone>();
            hiring.Configure(grill, serving, wallet, player, root.transform, root.transform, ShopLayout.Aisle, drop);
            expansion = ShopExpansion.Create(root.transform, dining, serving, hiring, player, wallet);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        void Hold(FacilityUnlockZone pad, float seconds)
        {
            player.transform.position = pad.PadPosition + Vector3.up;
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60f); i++) pad.Advance(1f / 60f);
        }

        void Leave(FacilityUnlockZone pad)
        {
            player.transform.position = Vector3.zero;
            pad.Advance(0.1f);
        }

        void FillQueue()
        {
            for (int i = 0; i < 1200; i++) queue.Advance(1f / 60f);
        }

        [Test]
        public void ThreeGreenPadsSitOnEmptyGroundWithListedPrices()
        {
            Assert.That(expansion.TablePad.Cost, Is.EqualTo(150));
            Assert.That(expansion.GrillPad.Cost, Is.EqualTo(200));
            Assert.That(expansion.CounterPad.Cost, Is.EqualTo(250));
            Assert.That(expansion.BoxingPad.Cost, Is.EqualTo(150));
            Assert.That(expansion.DriveThruPad.Cost, Is.EqualTo(250));
            Assert.That(expansion.TablePad.PadPosition, Is.EqualTo(ShopLayout.TableUnlock));
            Assert.That(expansion.GrillPad.PadPosition, Is.EqualTo(ShopLayout.GrillUnlock));
            Assert.That(expansion.CounterPad.PadPosition, Is.EqualTo(ShopLayout.CounterUnlock));
            Assert.That(expansion.BoxingPad.PadPosition, Is.EqualTo(ShopLayout.BoxingUnlock));
            Assert.That(expansion.DriveThruPad.PadPosition, Is.EqualTo(ShopLayout.DriveThruUnlock));
            Assert.That(GameObject.Find("TableUnlockPad"), Is.Not.Null);
            Assert.That(GameObject.Find("GrillUnlockPad"), Is.Not.Null);
            Assert.That(GameObject.Find("CounterUnlockPad"), Is.Not.Null);
            Assert.That(GameObject.Find("BoxingUnlockPad"), Is.Not.Null);
            Assert.That(GameObject.Find("DriveThruUnlockPad"), Is.Not.Null);
            Assert.That(expansion.HasBoxing, Is.False);
            Assert.That(expansion.HasDriveThru, Is.False);
            Assert.That(dining.TableCount, Is.EqualTo(3));
            Assert.That(expansion.HasExtraTable, Is.False);
            Assert.That(expansion.HasExtraGrill, Is.False);
            Assert.That(expansion.HasExtraCounter, Is.False);
            Assert.That(GameObject.Find("UpgradeSpot"), Is.Null);
            Assert.That(expansion.ExtraGrillUpgrade, Is.Null);
        }

        [Test]
        public void PadsDoNotBlockDoorsOrExistingTables()
        {
            Vector3[] awayFromDoors =
            {
                ShopLayout.TableUnlock, ShopLayout.GrillUnlock, ShopLayout.CounterUnlock,
                ShopLayout.ExtraGrill, ShopLayout.ExtraGrillUpgrade, ShopLayout.ExtraCounter,
                ShopLayout.BoxingUnlock, ShopLayout.BoxingCircle, ShopLayout.PackageCounter,
                ShopLayout.DriveThruUnlock, ShopLayout.DriveThruCircle, ShopLayout.DriveThruWindow
            };
            for (int i = 0; i < awayFromDoors.Length; i++)
            {
                Assert.That(Horizontal(awayFromDoors[i], ShopLayout.Entrance), Is.GreaterThan(4f),
                    awayFromDoors[i].ToString());
                Assert.That(Horizontal(awayFromDoors[i], ShopLayout.HrDoor), Is.GreaterThan(5f),
                    awayFromDoors[i].ToString());
                Assert.That(Horizontal(awayFromDoors[i], ShopLayout.BoostDoor), Is.GreaterThan(3.5f),
                    awayFromDoors[i].ToString());
            }

            Vector3[] notOnStarterTables =
            {
                ShopLayout.GrillUnlock, ShopLayout.CounterUnlock, ShopLayout.ExtraGrill, ShopLayout.ExtraCounter,
                ShopLayout.BoxingUnlock, ShopLayout.BoxingCircle, ShopLayout.PackageCounter,
                ShopLayout.DriveThruUnlock, ShopLayout.DriveThruCircle, ShopLayout.DriveThruWindow
            };
            for (int i = 0; i < notOnStarterTables.Length; i++)
            {
                for (int t = 0; t < ShopLayout.Tables.Length; t++)
                    Assert.That(Horizontal(notOnStarterTables[i], ShopLayout.Tables[t]), Is.GreaterThan(2.8f),
                        notOnStarterTables[i] + " vs table " + t);
            }

            Assert.That(Horizontal(ShopLayout.TableUnlock, ShopLayout.ExtraTable), Is.LessThan(0.05f));
            Assert.That(Horizontal(ShopLayout.GrillUnlock, ShopLayout.ExtraGrill), Is.LessThan(0.05f));
            Assert.That(Horizontal(ShopLayout.CounterUnlock, ShopLayout.ExtraCounter), Is.LessThan(0.05f));
            Assert.That(Horizontal(ShopLayout.BoxingUnlock, ShopLayout.BoxingTable), Is.LessThan(0.05f));
            Assert.That(Horizontal(ShopLayout.DriveThruUnlock, ShopLayout.DriveThruCircle),
                Is.GreaterThan(ShopLayout.AisleMin));
        }

        [Test]
        public void TablePadUnlocksAFourthFacingTableAndHidesTheCircle()
        {
            wallet.RestoreProgress(150, 0);
            Hold(expansion.TablePad, 3f);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(expansion.HasExtraTable, Is.True);
            Assert.That(dining.TableCount, Is.EqualTo(4));
            Assert.That(dining.SeatCount, Is.EqualTo(8));
            Assert.That(expansion.ExtraTable.Center, Is.EqualTo(ShopLayout.ExtraTable));
            AssertChairFaces(expansion.ExtraTable, "ChairA");
            AssertChairFaces(expansion.ExtraTable, "ChairB");
            Assert.That(expansion.TablePad.IsPurchased, Is.True);
            Assert.That(expansion.TablePad.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void PartialInvestmentSurvivesLeavingAndShortReentryDoesNotSpend()
        {
            wallet.RestoreProgress(149, 0);
            Hold(expansion.TablePad, 5f);
            Assert.That(expansion.HasExtraTable, Is.False);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(expansion.TablePad.Remaining, Is.EqualTo(1));
            Leave(expansion.TablePad);
            wallet.CollectCoins(10);
            Hold(expansion.TablePad, 0.2f);
            Assert.That(wallet.Coins, Is.EqualTo(10));
            Leave(expansion.TablePad);
            Hold(expansion.TablePad, 0.3f);
            Assert.That(expansion.HasExtraTable, Is.True);
            Assert.That(wallet.Coins, Is.EqualTo(9));
        }

        [Test]
        public void ExtraGrillStartsAtLv1ThenChangesLookAndSpeedOnEachUpgrade()
        {
            wallet.RestoreProgress(440, 0);
            Hold(expansion.GrillPad, 3f);
            Assert.That(wallet.Coins, Is.EqualTo(240));
            Assert.That(expansion.HasExtraGrill, Is.True);
            Assert.That(expansion.ExtraGrillLevel, Is.EqualTo(1));
            Assert.That(expansion.ExtraGrill.Station.ProductionSeconds, Is.EqualTo(3f));
            Assert.That(expansion.ExtraGrill.Station.Capacity, Is.EqualTo(4));
            Assert.That(expansion.ExtraGrill.ActiveLookName, Is.EqualTo("Look_Lv1"));
            Assert.That(expansion.ExtraGrill.ActiveLookScale, Is.EqualTo(ExpandableGrill.LookScales[0]));
            int lv1Parts = expansion.ExtraGrill.ActivePartCount;
            Assert.That(lv1Parts, Is.GreaterThanOrEqualTo(4));
            Assert.That(expansion.ExtraGrill.ActiveLook.Find("Chimney"), Is.Null);
            Assert.That(expansion.ExtraGrill.ActiveLook.Find("GrillDeck_2"), Is.Null);

            GrillUpgradeZone upgrade = expansion.ExtraGrillUpgrade;
            AssertExtraGrillOwnsItsUpgradeSpot(root.GetComponent<ProductionStation>());
            Assert.That(upgrade.NextCost, Is.EqualTo(80));
            player.transform.position = upgrade.UpgradePosition + Vector3.up;
            for (int i = 0; i < 90; i++) upgrade.Advance(1f / 60f);
            Assert.That(upgrade.Level, Is.EqualTo(2));
            Assert.That(wallet.Coins, Is.EqualTo(160));
            Assert.That(expansion.ExtraGrill.Station.ProductionSeconds, Is.EqualTo(1.5f));
            Assert.That(expansion.ExtraGrill.Station.Capacity, Is.EqualTo(6));
            Assert.That(expansion.ExtraGrill.ActiveLookName, Is.EqualTo("Look_Lv2"));
            Assert.That(expansion.ExtraGrill.ActiveLookScale, Is.EqualTo(ExpandableGrill.LookScales[1]));
            Assert.That(expansion.ExtraGrill.ActivePartCount, Is.Not.EqualTo(lv1Parts));
            Assert.That(expansion.ExtraGrill.ActiveLook.Find("Chimney"), Is.Not.Null);
            Assert.That(expansion.ExtraGrill.ActiveLook.Find("GrillDeck_2"), Is.Not.Null);
            Assert.That(upgrade.NextCost, Is.EqualTo(160));

            player.transform.position = Vector3.zero;
            upgrade.Advance(0.1f);
            player.transform.position = upgrade.UpgradePosition + Vector3.up;
            for (int i = 0; i < 90; i++) upgrade.Advance(1f / 60f);
            Assert.That(upgrade.Level, Is.EqualTo(3));
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(expansion.ExtraGrill.Station.ProductionSeconds, Is.EqualTo(0.8f));
            Assert.That(expansion.ExtraGrill.Station.Capacity, Is.EqualTo(8));
            Assert.That(expansion.ExtraGrill.ActiveLookName, Is.EqualTo("Look_Lv3"));
            Assert.That(expansion.ExtraGrill.ActiveLookScale, Is.EqualTo(ExpandableGrill.LookScales[2]));
            Assert.That(expansion.ExtraGrill.ActiveLook.Find("ChimneyL"), Is.Not.Null);
            Assert.That(expansion.ExtraGrill.ActiveLook.Find("ChimneyR"), Is.Not.Null);
            Assert.That(expansion.ExtraGrill.ActiveLook.Find("Beacon"), Is.Not.Null);
            Assert.That(expansion.ExtraGrill.ActiveLook.Find("GrillDeck_3"), Is.Not.Null);
            Assert.That(upgrade.IsMaxLevel, Is.True);
            long coins = wallet.Coins;
            for (int i = 0; i < 120; i++) upgrade.Advance(1f / 60f);
            Assert.That(wallet.Coins, Is.EqualTo(coins));
            Assert.That(upgrade.Level, Is.EqualTo(3));
        }

        [Test]
        public void ExtraCounterHoldsBurgersAndSharesCashierCooldown()
        {
            wallet.RestoreProgress(250, 0);
            Hold(expansion.CounterPad, 3f);
            Assert.That(expansion.HasExtraCounter, Is.True);
            Assert.That(expansion.ExtraStock, Is.Not.Null);
            FillQueue();
            var starterGrill = root.GetComponent<ProductionStation>();
            starterGrill.Advance(12f);
            player.TryCollectFrom(starterGrill);
            player.transform.position = expansion.ExtraDrop.DropPosition + Vector3.up;
            expansion.ExtraDrop.Advance(0.01f);
            Assert.That(expansion.ExtraDrop.TryDepositFrom(player), Is.True);
            Assert.That(expansion.ExtraStock.Count, Is.EqualTo(1));
            Assert.That(stock.Count, Is.Zero);
            CustomerAgent first = queue.ReadyCustomer;
            player.transform.position = ShopLayout.ExtraServingCircle + Vector3.up;
            Assert.That(serving.TryServeFrom(player), Is.True);
            serving.Advance(BurgerServingZone.HandoffDuration);
            Assert.That(expansion.ExtraStock.Count, Is.Zero);
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            starterGrill.Advance(12f);
            player.TryCollectFrom(starterGrill);
            player.transform.position = serving.ServingPosition + Vector3.up;
            serving.DropZone.TryDepositFrom(player);
            first.AdvanceDeparture(100f);
            for (int i = 0; i < 1200 && queue.ReadyCustomer == null; i++) queue.Advance(1f / 60f);
            Assert.That(queue.ReadyCustomer, Is.Not.Null);
            Assert.That(serving.TryServeFrom(player), Is.False, "shared cooldown must block the other white circle");
        }

        [Test]
        public void RestoreSpawnsBoughtFacilitiesWithoutChargingAgain()
        {
            wallet.RestoreProgress(600, 4);
            var upgrade = root.AddComponent<GrillUpgradeZone>();
            upgrade.Configure(root.GetComponent<ProductionStation>(), wallet, player, root.transform);
            string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "BurgerShopExpand-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                Hold(expansion.TablePad, 3f);
                Leave(expansion.GrillPad);
                Hold(expansion.GrillPad, 3f);
                var persistence = root.AddComponent<RestaurantPersistence>();
                persistence.Configure(wallet, upgrade, hiring, null, expansion, directory);
                Assert.That(persistence.Flush(), Is.True);
                Assert.That(new LocalSaveStore(directory).Load(out RestaurantSaveData data), Is.EqualTo(SaveLoadResult.Loaded));
                Assert.That(data.version, Is.EqualTo(RestaurantSaveData.CurrentVersion));
                Assert.That(data.boughtExtraTable, Is.True);
                Assert.That(data.boughtExtraGrill, Is.True);
                Assert.That(data.extraGrillLevel, Is.EqualTo(1));
                Assert.That(data.coins, Is.EqualTo(250));
                Object.DestroyImmediate(expansion.gameObject);
                dining = DiningArea.Create(root.transform, ShopLayout.Tables);
                expansion = ShopExpansion.Create(root.transform, dining, serving, hiring, player, wallet);
                expansion.Restore(data.ResolvedBoughtExtraTable, data.ResolvedBoughtExtraGrill,
                    data.ResolvedBoughtExtraCounter, data.ResolvedExtraGrillLevel);
                Assert.That(dining.TableCount, Is.EqualTo(4));
                Assert.That(expansion.HasExtraGrill, Is.True);
                Assert.That(expansion.ExtraGrillLevel, Is.EqualTo(1));
                Assert.That(expansion.ExtraGrill.ActiveLookName, Is.EqualTo("Look_Lv1"));
                AssertExtraGrillOwnsItsUpgradeSpot(root.GetComponent<ProductionStation>());
                Assert.That(wallet.Coins, Is.EqualTo(250));
            }
            finally
            {
                if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, true);
            }
        }

        [Test]
        public void CapsuleCanAskToInstallATableWhenThePlayerCanAffordIt()
        {
            wallet.RestoreProgress(150, 0);
            var tracker = root.AddComponent<SessionGoalTracker>();
            tracker.Configure(player, root.GetComponent<ProductionStation>(), stock, queue, wallet, serving,
                dining, null, null, null, expansion);
            tracker.Advance(2f);
            Assert.That(tracker.Title, Does.StartWith("Install a table"));
            Hold(expansion.TablePad, 3f);
            tracker.Advance(0.01f);
            Assert.That(tracker.Title, Does.StartWith("Install a table"));
            Assert.That(tracker.IsCelebrating, Is.True);
        }

        [Test]
        public void ExtraGrillUpgradeSpotBindsToTheNewMachineNotTheStarter()
        {
            ProductionStation starter = root.GetComponent<ProductionStation>();
            Assert.That(GameObject.Find("UpgradeSpot"), Is.Null);
            wallet.RestoreProgress(200, 0);
            Hold(expansion.GrillPad, 3f);
            AssertExtraGrillOwnsItsUpgradeSpot(starter);
            Assert.That(expansion.ExtraGrillUpgrade.Level, Is.EqualTo(1));
            Assert.That(expansion.ExtraGrillUpgrade.NextCost, Is.EqualTo(80));
        }

        [Test]
        public void RestoreLv3KeepsTheUpgradeSpotAndLook()
        {
            ProductionStation starter = root.GetComponent<ProductionStation>();
            expansion.Restore(false, true, false, 3);
            Assert.That(expansion.ExtraGrillLevel, Is.EqualTo(3));
            Assert.That(expansion.ExtraGrill.ActiveLookName, Is.EqualTo("Look_Lv3"));
            Assert.That(expansion.ExtraGrill.ActiveLookScale, Is.EqualTo(ExpandableGrill.LookScales[2]));
            Assert.That(expansion.ExtraGrill.Station.ProductionSeconds, Is.EqualTo(0.8f));
            AssertExtraGrillOwnsItsUpgradeSpot(starter);
            Assert.That(expansion.ExtraGrillUpgrade.IsMaxLevel, Is.True);
            Assert.That(expansion.GrillPad.IsPurchased, Is.True);
            Assert.That(wallet.Coins, Is.Zero);
        }

        [Test]
        public void AC01And02Invest120Then100BuildsOneGrillAndKeeps20()
        {
            wallet.RestoreProgress(120, 0);
            Hold(expansion.GrillPad, 3f);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(expansion.GrillPad.Remaining, Is.EqualTo(80));
            Assert.That(expansion.HasExtraGrill, Is.False);
            Leave(expansion.GrillPad);
            expansion.GrillPad.Advance(2f);
            Assert.That(expansion.GrillPad.Remaining, Is.EqualTo(80));
            wallet.CollectCoins(100);
            Hold(expansion.GrillPad, 3f);
            Assert.That(wallet.Coins, Is.EqualTo(20));
            Assert.That(expansion.HasExtraGrill, Is.True);
            Hold(expansion.GrillPad, 5f);
            Assert.That(wallet.Coins, Is.EqualTo(20));
            Assert.That(root.GetComponentsInChildren<ExpandableGrill>().Length, Is.EqualTo(1));
        }

        [Test]
        public void AC03ExactTailAndAC04ZeroWalletResumeWithoutReentering()
        {
            expansion.GrillPad.RestoreInvestment(120);
            wallet.RestoreProgress(25, 0);
            Hold(expansion.GrillPad, 3f);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(expansion.GrillPad.Remaining, Is.EqualTo(55));
            Hold(expansion.GrillPad, 10f);
            wallet.CollectCoins(100);
            expansion.GrillPad.Advance(0.01f);
            Assert.That(wallet.Coins, Is.EqualTo(90), "no banked empty-wallet time burst");
            Hold(expansion.GrillPad, 1f);
            Assert.That(wallet.Coins, Is.EqualTo(45));
            Assert.That(expansion.HasExtraGrill, Is.True);
        }

        [Test]
        public void ContributionsStartAfterPointThreeAndRespectPointOneCadence()
        {
            wallet.RestoreProgress(100, 0);
            Hold(expansion.GrillPad, 0.2f);
            Assert.That(wallet.Coins, Is.EqualTo(100));
            Hold(expansion.GrillPad, 0.1f);
            Assert.That(wallet.Coins, Is.EqualTo(90));
            Hold(expansion.GrillPad, 0.1f);
            Assert.That(wallet.Coins, Is.EqualTo(80));
            Leave(expansion.GrillPad);
            expansion.GrillPad.Advance(2f);
            Assert.That(wallet.Coins, Is.EqualTo(80));
            Hold(expansion.GrillPad, 0.2f);
            Assert.That(wallet.Coins, Is.EqualTo(80));
        }

        [Test]
        public void WalletObserversSeeInvestmentAndConstructionAlreadyCommitted()
        {
            wallet.RestoreProgress(200, 0);
            wallet.CoinsSpent += _ =>
            {
                Assert.That(wallet.Coins + expansion.GrillPad.Invested, Is.EqualTo(200));
                if (expansion.GrillPad.Remaining == 0)
                    Assert.That(expansion.HasExtraGrill, Is.True);
            };
            Hold(expansion.GrillPad, 3f);
            Assert.That(expansion.HasExtraGrill, Is.True);
        }

        [Test]
        public void OverlappingPadsChooseNearestAndKeepCurrentTargetOnTie()
        {
            // Deliberately intersect the two zones. Update order must not charge both.
            expansion.GrillPad.transform.position = new Vector3(3, 0, 0);
            expansion.TablePad.transform.position = new Vector3(4, 0, 0);
            wallet.RestoreProgress(1000, 0);
            player.transform.position = new Vector3(3.75f, 1, 0);
            for (int i = 0; i < 24; i++) { expansion.GrillPad.Advance(1f / 60); expansion.TablePad.Advance(1f / 60); }
            Assert.That(expansion.TablePad.Invested, Is.EqualTo(20));
            Assert.That(expansion.GrillPad.Invested, Is.Zero);
            player.transform.position = new Vector3(3.5f, 1, 0);
            for (int i = 0; i < 12; i++) { expansion.GrillPad.Advance(1f / 60); expansion.TablePad.Advance(1f / 60); }
            Assert.That(expansion.TablePad.Invested, Is.EqualTo(40));
            Assert.That(expansion.GrillPad.Invested, Is.Zero);
            player.transform.position = new Vector3(3.2f, 1, 0);
            for (int i = 0; i < 18; i++) { expansion.TablePad.Advance(1f / 60); expansion.GrillPad.Advance(1f / 60); }
            Assert.That(expansion.GrillPad.Invested, Is.EqualTo(10));
            Assert.That(expansion.TablePad.Invested, Is.EqualTo(40));
        }

        [Test]
        public void AC05PauseAndFocusStopInvestmentAndRestartEntryDelay()
        {
            wallet.RestoreProgress(100, 0);
            Hold(expansion.GrillPad, 0.3f);
            Lifecycle(expansion.GrillPad, "OnApplicationPause", true);
            expansion.GrillPad.Advance(10f);
            Assert.That(wallet.Coins, Is.EqualTo(90));
            Lifecycle(expansion.GrillPad, "OnApplicationPause", false);
            Hold(expansion.GrillPad, 0.2f);
            Assert.That(wallet.Coins, Is.EqualTo(90));
            Hold(expansion.GrillPad, 0.1f);
            Assert.That(wallet.Coins, Is.EqualTo(80));
            Lifecycle(expansion.GrillPad, "OnApplicationFocus", false);
            expansion.GrillPad.Advance(10f);
            Assert.That(wallet.Coins, Is.EqualTo(80));
            Lifecycle(expansion.GrillPad, "OnApplicationFocus", true);
            Hold(expansion.GrillPad, 0.2f);
            Assert.That(wallet.Coins, Is.EqualTo(80));
        }

        [Test]
        public void AC05TwoInvestmentsAndWalletRoundTripTogetherAndCompletionRequestsSave()
        {
            string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BurgerShopInvestment-" + System.Guid.NewGuid());
            try
            {
                var upgrade = root.AddComponent<GrillUpgradeZone>();
                upgrade.Configure(root.GetComponent<ProductionStation>(), wallet, player, root.transform);
                var persistence = root.AddComponent<RestaurantPersistence>();
                persistence.Configure(wallet, upgrade, hiring, null, expansion, directory);
                wallet.RestoreProgress(200, 0);
                Hold(expansion.GrillPad, 0.4f);
                Leave(expansion.GrillPad);
                Hold(expansion.TablePad, 0.5f);
                Assert.That(persistence.Flush(), Is.True);
                var store = new LocalSaveStore(directory);
                Assert.That(store.Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
                Assert.That(data.coins, Is.EqualTo(150));
                Assert.That(data.grillInvestment, Is.EqualTo(20));
                Assert.That(data.tableInvestment, Is.EqualTo(30));
                Object.DestroyImmediate(persistence);
                Object.DestroyImmediate(expansion.gameObject);
                expansion = ShopExpansion.Create(root.transform, dining, serving, hiring, player, wallet);
                persistence = root.AddComponent<RestaurantPersistence>();
                persistence.Configure(wallet, upgrade, hiring, null, expansion, directory);
                Assert.That(wallet.Coins, Is.EqualTo(150));
                Assert.That(expansion.GrillPad.Remaining, Is.EqualTo(180));
                Assert.That(expansion.TablePad.Remaining, Is.EqualTo(120));
                Hold(expansion.TablePad, 2f);
                persistence.Advance(0.01f); // Earlier than the regular two-second autosave.
                Assert.That(store.Load(out data), Is.EqualTo(SaveLoadResult.Loaded));
                Assert.That(data.coins, Is.EqualTo(30));
                Assert.That(data.boughtExtraTable, Is.True);
                Assert.That(data.tableInvestment, Is.EqualTo(150));
            }
            finally { if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, true); }
        }

        [Test]
        public void FullyInvestedUnmarkedSaveBuildsWithoutChargingAndPurchasedOldFacilityStaysOwned()
        {
            wallet.RestoreProgress(75, 0);
            expansion.Restore(true, false, false, 0);
            expansion.RestoreInvestments(0, 200, 0, 0, 0);
            expansion.RestoreInvestments(0, 200, 0, 0, 0);
            Assert.That(expansion.HasExtraTable, Is.True);
            Assert.That(expansion.TablePad.Remaining, Is.Zero);
            Assert.That(expansion.HasExtraGrill, Is.True);
            Assert.That(expansion.ExtraGrillLevel, Is.EqualTo(1));
            Assert.That(wallet.Coins, Is.EqualTo(75));
            Assert.That(root.GetComponentsInChildren<ExpandableGrill>().Length, Is.EqualTo(1));
        }

        [Test]
        public void AC07AllFiveLabelsRemainVisibleAbovePlayerAndShowRemainingWhileInside()
        {
            FacilityUnlockZone[] pads = { expansion.TablePad, expansion.GrillPad, expansion.CounterPad, expansion.BoxingPad, expansion.DriveThruPad };
            foreach (var pad in pads)
            {
                wallet.RestoreProgress(25, 0);
                Hold(pad, 1f);
                var label = expansion.transform.Find(pad.name + "Label").GetComponent<TextMesh>();
                Lifecycle(pad, "LateUpdate");
                Assert.That(label.gameObject.activeSelf, Is.True);
                Assert.That(label.text, Is.EqualTo(pad.Title + "\nRemaining " + (pad.Cost - 25)));
                Assert.That(label.transform.position.y, Is.GreaterThan(player.transform.position.y + 1.5f));
                Assert.That(pad.transform.Find("FacilityIcon_" + pad.Title).position.y, Is.GreaterThan(label.transform.position.y + 0.7f));
                Leave(pad);
            }
            Assert.That(expansion.NextInstallHint, Does.Contain("Remaining 125"));
        }

        [Test]
        public void EmployeeAtPadCannotTriggerSpendingWithoutPlayer()
        {
            wallet.RestoreProgress(1000, 0);
            hiring.RestoreWorkers(1, 0, 0);
            var worker = root.GetComponentInChildren<RestaurantWorker>();
            worker.transform.position = expansion.GrillPad.PadPosition;
            player.transform.position = Vector3.zero;
            expansion.GrillPad.Advance(10f);
            Assert.That(wallet.Coins, Is.EqualTo(1000));
            Assert.That(expansion.GrillPad.Invested, Is.Zero);
        }

        static void Lifecycle(FacilityUnlockZone zone, string method, params object[] args) =>
            typeof(FacilityUnlockZone).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(zone, args);

        void AssertExtraGrillOwnsItsUpgradeSpot(ProductionStation starter)
        {
            Transform spot = expansion.ExtraGrill.transform.Find("UpgradeSpot");
            Assert.That(spot, Is.Not.Null);
            Assert.That(spot.gameObject.activeInHierarchy, Is.True);
            Assert.That(spot.position, Is.EqualTo(ShopLayout.ExtraGrillUpgrade));
            Assert.That(expansion.ExtraGrillUpgrade, Is.Not.Null);
            Assert.That(expansion.ExtraGrillUpgrade.Station, Is.SameAs(expansion.ExtraGrill.Station));
            Assert.That(expansion.ExtraGrillUpgrade.Station, Is.Not.SameAs(starter));
            Assert.That(expansion.ExtraGrillUpgrade.UpgradePosition, Is.EqualTo(ShopLayout.ExtraGrillUpgrade));
            Assert.That(Horizontal(spot.position, ShopLayout.ExtraGrill), Is.LessThan(3f));
            Assert.That(Horizontal(spot.position, ShopLayout.Grill), Is.GreaterThan(4f));
        }

        static float Horizontal(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        static void AssertChairFaces(DiningTable table, string name)
        {
            Transform chair = table.transform.Find(name);
            Assert.That(chair, Is.Not.Null);
            Vector3 toward = Flatten(table.Center - chair.position);
            Vector3 forward = Flatten(chair.forward);
            Assert.That(Vector3.Dot(forward.normalized, toward.normalized), Is.GreaterThan(0.9f));
            Transform back = chair.Find("Back");
            Assert.That(Vector3.Distance(Flatten(back.position), Flatten(table.Center)),
                Is.GreaterThan(Vector3.Distance(Flatten(chair.position), Flatten(table.Center))));
        }

        static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
