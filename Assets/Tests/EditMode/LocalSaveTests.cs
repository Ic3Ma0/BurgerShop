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
            Assert.That(File.Exists(store.FilePath + ".tmp"), Is.False);
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
            string newer = File.ReadAllText(store.FilePath).Replace("\"version\": 1", "\"version\": 2");
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
