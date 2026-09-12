using System;
using System.IO;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class LocalSaveTests
    {
        string directory;
        LocalSaveStore store;
        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "BurgerShopSave-" + Guid.NewGuid().ToString("N"));
            store = new LocalSaveStore(directory);
        }
        [TearDown] public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            if (File.Exists(directory)) File.Delete(directory);
        }
        static RestaurantSaveData Progress(long coins = 25) => new RestaurantSaveData
        { version = 1, coins = coins, completedSales = 18, grillLevel = 3, workerHired = true, workerDeliveries = 7 };

        static RestaurantSaveData ProgressV2(long coins = 25, int hired = 2, int clears = 3) => new RestaurantSaveData
        {
            version = 2, coins = coins, completedSales = 18, grillLevel = 3, workerHired = hired > 0,
            workerDeliveries = 7, hiredWorkerCount = hired, workerClears = clears
        };

        [Test] public void FirstRunDoesNotCreateOrLoadPhantomProgress()
        {
            Assert.That(store.Load(out var data), Is.EqualTo(SaveLoadResult.NewGame));
            Assert.That(data, Is.Null);
            Assert.That(Directory.Exists(directory), Is.False);
        }
        [Test] public void RoundTripRestoresEveryPermanentFieldAndLargeCoinBalance()
        {
            Assert.That(store.Save(Progress(long.MaxValue)), Is.True);
            Assert.That(new LocalSaveStore(directory).Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.coins, Is.EqualTo(long.MaxValue));
            Assert.That(data.completedSales, Is.EqualTo(18));
            Assert.That(data.grillLevel, Is.EqualTo(3));
            Assert.That(data.workerHired, Is.True);
            Assert.That(data.workerDeliveries, Is.EqualTo(7));
            Assert.That(data.ResolvedHiredCount, Is.EqualTo(1));
            Assert.That(File.Exists(store.FilePath + ".tmp"), Is.False);
        }
        [Test] public void Version2RoundTripRestoresHiredCountAndClears()
        {
            Assert.That(store.Save(ProgressV2(long.MaxValue, 3, 11)), Is.True);
            Assert.That(new LocalSaveStore(directory).Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.version, Is.EqualTo(2));
            Assert.That(data.hiredWorkerCount, Is.EqualTo(3));
            Assert.That(data.workerClears, Is.EqualTo(11));
            Assert.That(data.ResolvedHiredCount, Is.EqualTo(3));
            Assert.That(data.workerHired, Is.True);
        }
        [Test] public void Version1HiredFlagResolvesToOneWorkerWithoutNewFields()
        {
            Assert.That(store.Save(Progress(40)), Is.True);
            Assert.That(store.Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.version, Is.EqualTo(1));
            Assert.That(data.workerHired, Is.True);
            Assert.That(data.hiredWorkerCount, Is.EqualTo(0));
            Assert.That(data.ResolvedHiredCount, Is.EqualTo(1));
        }
        static RestaurantSaveData ProgressV3(long coins = 25, int boost = 2, bool table = false, bool grill = false,
            bool counter = false, int extraGrill = 0) => new RestaurantSaveData
        {
            version = 3, coins = coins, completedSales = 18, grillLevel = 3, workerHired = true,
            workerDeliveries = 7, hiredWorkerCount = 1, workerClears = 3, boostLevel = boost,
            boughtExtraTable = table, boughtExtraGrill = grill, boughtExtraCounter = counter,
            extraGrillLevel = extraGrill
        };

        [Test] public void Version3RoundTripRestoresBoostLevelAndKeepsCoins()
        {
            Assert.That(store.Save(ProgressV3(640, 4)), Is.True);
            Assert.That(new LocalSaveStore(directory).Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.version, Is.EqualTo(3));
            Assert.That(data.boostLevel, Is.EqualTo(4));
            Assert.That(data.ResolvedBoostLevel, Is.EqualTo(4));
            Assert.That(data.ResolvedPlayerSpeedTier, Is.EqualTo(4));
            Assert.That(data.ResolvedPlayerCarryTier, Is.EqualTo(4));
            Assert.That(data.coins, Is.EqualTo(640));
            Assert.That(data.ResolvedBoughtExtraTable, Is.False);
            Assert.That(data.ResolvedExtraGrillLevel, Is.Zero);
        }

        [Test] public void Version3RoundTripRestoresPaidFacilitiesAndKeepsCoins()
        {
            Assert.That(store.Save(ProgressV3(600, 0, true, true, true, 2)), Is.True);
            Assert.That(new LocalSaveStore(directory).Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.version, Is.EqualTo(3));
            Assert.That(data.coins, Is.EqualTo(600));
            Assert.That(data.ResolvedBoughtExtraTable, Is.True);
            Assert.That(data.ResolvedBoughtExtraGrill, Is.True);
            Assert.That(data.ResolvedBoughtExtraCounter, Is.True);
            Assert.That(data.ResolvedExtraGrillLevel, Is.EqualTo(2));
            Assert.That(data.boostLevel, Is.Zero);
        }

        [Test] public void Version2LoadLeavesExtrasUnboughtAndKeepsCoins()
        {
            Assert.That(store.Save(ProgressV2(600, 2, 1)), Is.True);
            Assert.That(store.Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.version, Is.EqualTo(2));
            Assert.That(data.coins, Is.EqualTo(600));
            Assert.That(data.ResolvedBoughtExtraTable, Is.False);
            Assert.That(data.ResolvedBoughtExtraGrill, Is.False);
            Assert.That(data.ResolvedBoughtExtraCounter, Is.False);
            Assert.That(data.ResolvedExtraGrillLevel, Is.Zero);
        }

        [Test] public void Version1And2ResolveBoostToZeroAndKeepCoins()
        {
            Assert.That(store.Save(Progress(88)), Is.True);
            Assert.That(store.Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.version, Is.EqualTo(1));
            Assert.That(data.ResolvedBoostLevel, Is.EqualTo(0));
            Assert.That(data.ResolvedPlayerSpeedTier, Is.EqualTo(0));
            Assert.That(data.ResolvedPlayerCarryTier, Is.EqualTo(0));
            Assert.That(data.coins, Is.EqualTo(88));
            Assert.That(store.Save(ProgressV2(99, 2, 1)), Is.True);
            Assert.That(new LocalSaveStore(directory).Load(out data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.version, Is.EqualTo(2));
            Assert.That(data.ResolvedBoostLevel, Is.EqualTo(0));
            Assert.That(data.coins, Is.EqualTo(99));
        }

        [Test] public void Version3RejectsBoostLevelOutsideZeroToFive()
        {
            var data = ProgressV3();
            data.boostLevel = 6;
            Assert.That(store.Save(data), Is.False);
            data = ProgressV3();
            data.boostLevel = -1;
            Assert.That(store.Save(data), Is.False);
            data = ProgressV3(boost: 0);
            data.boughtExtraGrill = true;
            data.extraGrillLevel = 0;
            Assert.That(data.IsValid, Is.False);
        }

        static RestaurantSaveData ProgressV4(long coins = 25, int speed = 2, int carry = 1) => new RestaurantSaveData
        {
            version = 4, coins = coins, completedSales = 18, grillLevel = 3, workerHired = true,
            workerDeliveries = 7, hiredWorkerCount = 1, workerClears = 3, boostLevel = 1,
            staffSpeedTier = speed, staffCarryTier = carry
        };

        [Test] public void Version4RoundTripRestoresStaffTiers()
        {
            Assert.That(store.Save(ProgressV4(80, 3, 2)), Is.True);
            Assert.That(new LocalSaveStore(directory).Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.version, Is.EqualTo(4));
            Assert.That(data.staffSpeedTier, Is.EqualTo(3));
            Assert.That(data.staffCarryTier, Is.EqualTo(2));
            Assert.That(data.ResolvedStaffSpeedTier, Is.EqualTo(3));
            Assert.That(data.ResolvedStaffCarryTier, Is.EqualTo(2));
            Assert.That(data.ResolvedBoostLevel, Is.EqualTo(1));
            Assert.That(data.ResolvedPlayerSpeedTier, Is.EqualTo(1));
            Assert.That(data.ResolvedPlayerCarryTier, Is.EqualTo(1));
            Assert.That(data.ResolvedStaffSpeedTier, Is.EqualTo(3));
        }

        [Test] public void Version1Through3ResolveStaffTiersToZero()
        {
            Assert.That(store.Save(Progress(40)), Is.True);
            Assert.That(store.Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.ResolvedStaffSpeedTier, Is.Zero);
            Assert.That(data.ResolvedStaffCarryTier, Is.Zero);
            Assert.That(store.Save(ProgressV2(40, 2, 1)), Is.True);
            Assert.That(new LocalSaveStore(directory).Load(out data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.version, Is.EqualTo(2));
            Assert.That(data.ResolvedStaffSpeedTier, Is.Zero);
            Assert.That(data.ResolvedStaffCarryTier, Is.Zero);
            Assert.That(store.Save(ProgressV3(40, 2)), Is.True);
            Assert.That(new LocalSaveStore(directory).Load(out data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.version, Is.EqualTo(3));
            Assert.That(data.ResolvedStaffSpeedTier, Is.Zero);
            Assert.That(data.ResolvedStaffCarryTier, Is.Zero);
            Assert.That(data.ResolvedBoostLevel, Is.EqualTo(2));
            Assert.That(data.ResolvedPlayerSpeedTier, Is.EqualTo(2));
            Assert.That(data.ResolvedPlayerCarryTier, Is.EqualTo(2));
        }

        [Test] public void Version4RejectsStaffTiersOutsideZeroToFive()
        {
            var data = ProgressV4();
            data.staffSpeedTier = 6;
            Assert.That(store.Save(data), Is.False);
            data = ProgressV4();
            data.staffCarryTier = -1;
            Assert.That(store.Save(data), Is.False);
        }

        static RestaurantSaveData ProgressV5(long coins = 25, int playerSpeed = 2, int playerCarry = 1,
            int staffSpeed = 0, int staffCarry = 0) => new RestaurantSaveData
        {
            version = 5, coins = coins, completedSales = 18, grillLevel = 3, workerHired = true,
            workerDeliveries = 7, hiredWorkerCount = 1, workerClears = 3, boostLevel = 0,
            staffSpeedTier = staffSpeed, staffCarryTier = staffCarry,
            playerSpeedTier = playerSpeed, playerCarryTier = playerCarry
        };

        [Test] public void Version5RoundTripRestoresIndependentPlayerTiers()
        {
            Assert.That(store.Save(ProgressV5(90, 4, 1, 3, 2)), Is.True);
            Assert.That(new LocalSaveStore(directory).Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.version, Is.EqualTo(5));
            Assert.That(data.playerSpeedTier, Is.EqualTo(4));
            Assert.That(data.playerCarryTier, Is.EqualTo(1));
            Assert.That(data.ResolvedPlayerSpeedTier, Is.EqualTo(4));
            Assert.That(data.ResolvedPlayerCarryTier, Is.EqualTo(1));
            Assert.That(data.staffSpeedTier, Is.EqualTo(3));
            Assert.That(data.staffCarryTier, Is.EqualTo(2));
        }

        [Test] public void Version5RejectsPlayerTiersOutsideZeroToFive()
        {
            var data = ProgressV5();
            data.playerSpeedTier = 6;
            Assert.That(store.Save(data), Is.False);
            data = ProgressV5();
            data.playerCarryTier = -1;
            Assert.That(store.Save(data), Is.False);
        }

        static RestaurantSaveData ProgressV6(long coins = 25, bool boxing = false, bool lane = false) =>
            new RestaurantSaveData
            {
                version = 6, coins = coins, completedSales = 18, grillLevel = 3, workerHired = true,
                workerDeliveries = 7, hiredWorkerCount = 1, workerClears = 3, boostLevel = 0,
                staffSpeedTier = 1, staffCarryTier = 0, playerSpeedTier = 2, playerCarryTier = 1,
                boughtBoxingStation = boxing, boughtDriveThru = lane
            };

        [Test] public void Version6RoundTripRestoresBoxingAndDriveThruFlags()
        {
            Assert.That(store.Save(ProgressV6(80, true, true)), Is.True);
            Assert.That(new LocalSaveStore(directory).Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.version, Is.EqualTo(6));
            Assert.That(data.boughtBoxingStation, Is.True);
            Assert.That(data.boughtDriveThru, Is.True);
            Assert.That(data.ResolvedBoughtBoxingStation, Is.True);
            Assert.That(data.ResolvedBoughtDriveThru, Is.True);
            Assert.That(data.playerSpeedTier, Is.EqualTo(2));
            Assert.That(data.coins, Is.EqualTo(80));
        }

        [Test] public void Version5AndOlderResolveBoxingAndDriveThruUnbought()
        {
            Assert.That(store.Save(ProgressV5(90, 4, 1)), Is.True);
            Assert.That(store.Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.version, Is.EqualTo(5));
            Assert.That(data.ResolvedBoughtBoxingStation, Is.False);
            Assert.That(data.ResolvedBoughtDriveThru, Is.False);
            Assert.That(store.Save(ProgressV3(88, 2)), Is.True);
            Assert.That(new LocalSaveStore(directory).Load(out data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.ResolvedBoughtBoxingStation, Is.False);
            Assert.That(data.ResolvedBoughtDriveThru, Is.False);
        }

        [Test] public void V6FixtureWithOriginalChecksumLoadsMixedPurchasesWithoutLosingRights()
        {
            // Frozen checksum produced from the pre-023 schema, independent of the current Checksum method.
            var old = ProgressV6(80, true, false);
            old.boughtExtraTable = true; old.boughtExtraGrill = true; old.extraGrillLevel = 2;
            Directory.CreateDirectory(directory);
            File.WriteAllText(store.FilePath, "{\"data\":" + JsonUtility.ToJson(old) + ",\"checksum\":\"mCEYqNRMQFrbW31D0JfUEDdbAN5FCkLh/qx+eWmdLfI=\"}");
            Assert.That(store.Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.coins, Is.EqualTo(80));
            Assert.That(data.completedSales, Is.EqualTo(18));
            Assert.That(data.workerDeliveries, Is.EqualTo(7));
            Assert.That(data.ResolvedHiredCount, Is.EqualTo(1));
            Assert.That(data.ResolvedExtraGrillLevel, Is.EqualTo(2));
            Assert.That(data.ResolvedStaffSpeedTier, Is.EqualTo(1));
            Assert.That(data.ResolvedPlayerSpeedTier, Is.EqualTo(2));
            Assert.That(data.ResolvedPlayerCarryTier, Is.EqualTo(1));
            Assert.That(data.ResolvedBoughtExtraTable, Is.True);
            Assert.That(data.ResolvedBoughtExtraCounter, Is.False);
            Assert.That(data.ResolvedBoughtBoxingStation, Is.True);
            Assert.That(data.ResolvedBoughtDriveThru, Is.False);
            Assert.That(data.grillInvestment, Is.Zero);
        }

        [TestCase(-1)] [TestCase(201)]
        public void V7InvalidInvestmentDoesNotGrantCoinsOrOverwriteSave(int amount)
        {
            var data = ProgressV6(); data.version = 7; data.grillInvestment = amount;
            Assert.That(data.IsValid, Is.False);
            Assert.That(store.Save(data), Is.False);
            Assert.That(File.Exists(store.FilePath), Is.False);
        }

        [Test] public void V7FiveInvestmentsAreChecksummedAndCorruptionRecoversPreviousWholeSnapshot()
        {
            var data = ProgressV6(500); data.version = 7;
            data.tableInvestment = 10; data.grillInvestment = 20; data.counterInvestment = 30;
            data.boxingInvestment = 40; data.driveThruInvestment = 50;
            Assert.That(store.Save(data), Is.True);
            data.coins = 475; data.grillInvestment = 45;
            Assert.That(store.Save(data), Is.True);
            string bytes = File.ReadAllText(store.FilePath).Replace("\"grillInvestment\": 45", "\"grillInvestment\": 200");
            File.WriteAllText(store.FilePath, bytes);
            Assert.That(store.Load(out data), Is.EqualTo(SaveLoadResult.RecoveredBackup));
            Assert.That(data.coins, Is.EqualTo(500));
            Assert.That(data.tableInvestment, Is.EqualTo(10));
            Assert.That(data.grillInvestment, Is.EqualTo(20));
            Assert.That(data.counterInvestment, Is.EqualTo(30));
            Assert.That(data.boxingInvestment, Is.EqualTo(40));
            Assert.That(data.driveThruInvestment, Is.EqualTo(50));
        }

        [Test] public void Version8RoundTripRestoresTableSetsAndInvestments()
        {
            var data = ProgressV6(90);
            data.version = 8;
            data.boughtExtraTable = true;
            data.tableInvestment = 150;
            data.table0Set = 3;
            data.table0Investment = 80;
            data.table1Investment = 40;
            data.extraTableSet = 2;
            data.extraTableInvestment = 80;
            Assert.That(store.Save(data), Is.True);
            Assert.That(new LocalSaveStore(directory).Load(out var loaded), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(loaded.version, Is.EqualTo(8));
            Assert.That(loaded.table0Set, Is.EqualTo(3));
            Assert.That(loaded.table0Investment, Is.EqualTo(80));
            Assert.That(loaded.table1Investment, Is.EqualTo(40));
            Assert.That(loaded.ResolvedExtraTableSet, Is.EqualTo(2));
            Assert.That(loaded.coins, Is.EqualTo(90));
        }

        [Test] public void Version7AndOlderResolveTableSetsToStarter()
        {
            var v7 = ProgressV6(88);
            v7.version = 7;
            Assert.That(store.Save(v7), Is.True);
            Assert.That(store.Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.version, Is.EqualTo(7));
            Assert.That(data.ResolvedTable0Set, Is.Zero);
            Assert.That(data.ResolvedTable0Investment, Is.Zero);
            Assert.That(data.ResolvedExtraTableSet, Is.Zero);
            Assert.That(data.coins, Is.EqualTo(88));
        }

        [Test] public void Version8RejectsChosenSetWithoutFullInvestmentOrUnboughtExtra()
        {
            var data = ProgressV6();
            data.version = 8;
            data.table0Set = 1;
            data.table0Investment = 40;
            Assert.That(data.IsValid, Is.False);
            Assert.That(store.Save(data), Is.False);
            data = ProgressV6();
            data.version = 8;
            data.boughtExtraTable = false;
            data.extraTableSet = 3;
            data.extraTableInvestment = 80;
            Assert.That(data.IsValid, Is.False);
        }

        [Test] public void Version2RejectsHiredCountOutsideZeroToThree()
        {
            var data = ProgressV2();
            data.hiredWorkerCount = 4;
            Assert.That(store.Save(data), Is.False);
            data = ProgressV2();
            data.hiredWorkerCount = 0;
            data.workerHired = true;
            Assert.That(store.Save(data), Is.False);
            data = ProgressV2(hired: 0, clears: 0);
            data.workerDeliveries = 0;
            data.workerHired = false;
            Assert.That(data.IsValid, Is.True);
        }
        [Test] public void RepeatedSavesKeepPreviousValidSnapshotAsBackup()
        {
            store.Save(Progress(10)); store.Save(Progress(20)); store.Save(Progress(30));
            File.WriteAllText(store.FilePath, "interrupted file");
            Assert.That(store.Load(out var data), Is.EqualTo(SaveLoadResult.RecoveredBackup));
            Assert.That(data.coins, Is.EqualTo(20));
            Assert.That(store.Save(data), Is.True);
            File.WriteAllText(store.FilePath, "broken again");
            Assert.That(store.Load(out data), Is.EqualTo(SaveLoadResult.RecoveredBackup));
            Assert.That(data.coins, Is.EqualTo(20), "Repair must not replace the valid backup with corrupt primary bytes.");
        }
        [Test] public void MissingPrimaryRecoversBackupAndIgnoresUncommittedTemporaryFile()
        {
            store.Save(Progress(10)); store.Save(Progress(20));
            File.Delete(store.FilePath);
            File.WriteAllText(store.FilePath + ".tmp", "partial write");
            Assert.That(store.Load(out var data), Is.EqualTo(SaveLoadResult.RecoveredBackup));
            Assert.That(data.coins, Is.EqualTo(10));
        }
        [TestCase("{}")] [TestCase("not json")] [TestCase("{\"data\":{\"version\":1}}")]
        public void UnreadableFilesArePreservedAndCannotBeOverwritten(string bytes)
        {
            Directory.CreateDirectory(directory); File.WriteAllText(store.FilePath, bytes);
            Assert.That(store.Load(out _), Is.EqualTo(SaveLoadResult.Unreadable));
            Assert.That(store.Save(Progress()), Is.False);
            Assert.That(File.ReadAllText(store.FilePath), Is.EqualTo(bytes));
        }
        [Test] public void ChangedPayloadWithoutMatchingChecksumFallsBackToBackup()
        {
            store.Save(Progress(10)); store.Save(Progress(20));
            File.WriteAllText(store.FilePath, File.ReadAllText(store.FilePath).Replace("\"coins\": 20", "\"coins\": 999"));
            Assert.That(store.Load(out var data), Is.EqualTo(SaveLoadResult.RecoveredBackup));
            Assert.That(data.coins, Is.EqualTo(10));
        }
        [Test] public void SaveFromNewerVersionIsNotDowngradedToOldBackup()
        {
            store.Save(Progress(10)); store.Save(Progress(20));
            string newer = File.ReadAllText(store.FilePath).Replace("\"version\": 1", "\"version\": " + (RestaurantSaveData.CurrentVersion + 1));
            File.WriteAllText(store.FilePath, newer);
            Assert.That(store.Load(out _), Is.EqualTo(SaveLoadResult.NewerVersion));
            Assert.That(store.CanWrite, Is.False);
            Assert.That(store.Save(Progress(1)), Is.False);
            Assert.That(File.ReadAllText(store.FilePath), Is.EqualTo(newer));
        }
        [Test] public void InvalidValuesAreRejectedBeforeAnyFileIsWritten()
        {
            var data = Progress(); data.coins = -1; Assert.That(store.Save(data), Is.False);
            data = Progress(); data.grillLevel = 4; Assert.That(store.Save(data), Is.False);
            data = Progress(); data.workerDeliveries = 19; Assert.That(store.Save(data), Is.False);
            data = Progress(); data.workerHired = false; Assert.That(store.Save(data), Is.False);
            data = Progress(); data.completedSales = -1; Assert.That(store.Save(data), Is.False);
            Assert.That(Directory.Exists(directory), Is.False);
        }
        [Test] public void DiskFailureReturnsFailureAndCanRetryAfterRecovery()
        {
            File.WriteAllText(directory, "blocks directory creation");
            Assert.That(store.Save(Progress()), Is.False);
            Assert.That(store.LastError, Is.Not.Empty);
            File.Delete(directory);
            Assert.That(store.Save(Progress()), Is.True);
            Assert.That(store.LastError, Is.Null);
        }
        [Test] public void WalletRestoreIsSilentAndRejectsNegativeProgress()
        {
            var root = new GameObject("WalletRestore");
            try
            {
                var wallet = root.AddComponent<RestaurantWallet>();
                int notifications = 0;
                wallet.CoinsSpent += _ => notifications++;
                wallet.SaleRecorded += _ => notifications++;
                wallet.RestoreProgress(123, 40);
                Assert.That(wallet.Coins, Is.EqualTo(123));
                Assert.That(wallet.CompletedSales, Is.EqualTo(40));
                Assert.That(notifications, Is.Zero);
                Assert.Throws<ArgumentOutOfRangeException>(() => wallet.RestoreProgress(-1, 40));
                Assert.That(wallet.Coins, Is.EqualTo(123));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
