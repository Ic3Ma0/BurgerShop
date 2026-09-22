using System;
using System.IO;
using System.Linq;
using BurgerShop.Building;
using BurgerShop.Persistence;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec077SaveContractTests
    {
        string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "BurgerShopSpec077-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Assert.That(directory, Does.Not.Contain(Application.persistentDataPath));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingOrCorruptManifestDiscoversOccupiedSlots(bool corruptManifest)
        {
            Assert.That(new LocalSaveStore(directory, SaveSlotStore.LegacyFileName).Save(Snapshot(100)), Is.True);
            Assert.That(new LocalSaveStore(directory, SaveSlotStore.NumberedFileName(2)).Save(Snapshot(200)), Is.True);
            Assert.That(new LocalSaveStore(directory, SaveSlotStore.NumberedFileName(4)).Save(Snapshot(400)), Is.True);
            if (corruptManifest)
                File.WriteAllText(Path.Combine(directory, SaveSlotStore.ManifestFileName), "{");

            var slots = new SaveSlotStore(directory);
            Assert.That(slots.Slots.Select(s => s.id), Is.EquivalentTo(new[] { 1, 2, 4 }));
            Assert.That(slots.TryCreateSlot(out int created), Is.EqualTo(SaveSlotOpResult.Success));
            Assert.That(created, Is.EqualTo(3));
        }

        [Test]
        public void BackupOnlyNumberedSlotIsDiscovered()
        {
            var store = new LocalSaveStore(directory, SaveSlotStore.NumberedFileName(2));
            Assert.That(store.Save(Snapshot(222)), Is.True);
            File.Copy(store.FilePath, store.BackupPath, true);
            File.Delete(store.FilePath);
            var slots = new SaveSlotStore(directory);
            Assert.That(slots.Find(2), Is.Not.Null);
            Assert.That(slots.Find(2).coins, Is.EqualTo(222));
        }

        [Test]
        public void FutureManifestIsNotRewritten()
        {
            Assert.That(new LocalSaveStore(directory, SaveSlotStore.LegacyFileName).Save(Snapshot(100)), Is.True);
            var future = new SaveSlotManifest
            {
                version = SaveSlotStore.ManifestVersion + 10,
                activeSlotId = 1,
                slots = new[]
                {
                    new SaveSlotInfo
                    {
                        id = 1, fileName = SaveSlotStore.LegacyFileName,
                        displayName = "Future format", shopRank = 1, coins = 100
                    }
                }
            };
            string original = JsonUtility.ToJson(future, true);
            string path = Path.Combine(directory, SaveSlotStore.ManifestFileName);
            File.WriteAllText(path, original);
            var slots = new SaveSlotStore(directory);
            Assert.That(slots.IsIncompatible, Is.True);
            Assert.That(File.ReadAllText(path), Is.EqualTo(original));
            Assert.That(slots.CanStartNewGame, Is.False);
            Assert.That(slots.TryCreateSlot(out _), Is.EqualTo(SaveSlotOpResult.IncompatibleManifest));
        }

        [Test]
        public void ManifestCommitFailureDoesNotActivateOrAllocate()
        {
            Assert.That(new LocalSaveStore(directory).Save(Snapshot(100)), Is.True);
            var slots = new SaveSlotStore(directory);
            int previousActive = slots.ActiveSlotId;
            int previousCount = slots.OccupiedCount;
            File.Delete(slots.ManifestPath);
            Directory.CreateDirectory(slots.ManifestPath);
            Assert.That(slots.TryCreateSlot(out _), Is.EqualTo(SaveSlotOpResult.CommitFailed).Or.EqualTo(SaveSlotOpResult.PermissionDenied));
            Assert.That(slots.ActiveSlotId, Is.EqualTo(previousActive));
            Assert.That(slots.OccupiedCount, Is.EqualTo(previousCount));
        }

        [TestCase(64)]
        [TestCase(256)]
        public void SuccessfulSaveIsReadableWithoutBackupRollback(int recordCount)
        {
            var store = new LocalSaveStore(directory);
            Assert.That(store.Save(Snapshot(100)), Is.True);
            var large = Snapshot(200);
            large.layout = Enumerable.Range(1, recordCount).Select(Placement).ToArray();
            Assert.That(large.IsValid, Is.True);
            bool accepted = store.Save(large);
            var result = new LocalSaveStore(directory).Load(out var restored);
            Assert.That(result, Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(restored.coins, Is.EqualTo(accepted ? 200 : 100));
            if (accepted) Assert.That(restored.layout.Length, Is.EqualTo(recordCount));
        }

        [Test]
        public void OversizeSnapshotIsRejectedBeforeReplacingTheGoodFile()
        {
            var store = new LocalSaveStore(directory);
            Assert.That(store.Save(Snapshot(100)), Is.True);
            var huge = Snapshot(200);
            huge.layout = Enumerable.Range(1, 4000).Select(Placement).ToArray();
            Assert.That(huge.IsValid, Is.True);
            Assert.That(store.Save(huge), Is.False);
            Assert.That(new LocalSaveStore(directory).Load(out var restored), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(restored.coins, Is.EqualTo(100));
        }

        [Test]
        public void DeleteLeavesIndexAloneWhenFilesCannotBeRemoved()
        {
            Assert.That(new LocalSaveStore(directory, SaveSlotStore.LegacyFileName).Save(Snapshot(1)), Is.True);
            Assert.That(new LocalSaveStore(directory, SaveSlotStore.NumberedFileName(2)).Save(Snapshot(2)), Is.True);
            var slots = new SaveSlotStore(directory);
            Assert.That(slots.OccupiedCount, Is.EqualTo(2));
            int idle = slots.ActiveSlotId == 1 ? 2 : 1;
            string blocker = Path.Combine(directory, slots.FileNameFor(idle));
            using (var held = new FileStream(blocker, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var result = slots.TryDelete(idle);
                if (result == SaveSlotOpResult.Success)
                    Assert.That(slots.Find(idle), Is.Null);
                else
                {
                    Assert.That(result, Is.EqualTo(SaveSlotOpResult.CommitFailed));
                    Assert.That(slots.Find(idle), Is.Not.Null);
                }
            }
        }

        static FacilityPlacementRecord Placement(int i) => new FacilityPlacementRecord
        {
            id = "custom:" + i.ToString("x32"),
            kind = (int)FacilityKind.TrashBin,
            purchased = true,
            x = 0, z = 0, yaw = 0, level = 1
        };

        static RestaurantSaveData Snapshot(long coins) => new RestaurantSaveData
        {
            version = RestaurantSaveData.CurrentVersion,
            coins = coins,
            grillLevel = 1,
            shopRank = 1,
            colaLevel = 1
        };
    }
}
