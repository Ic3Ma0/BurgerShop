using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BurgerShop.Persistence
{
    public sealed class SaveSlotStore
    {
        public const int MaxSlots = 8;
        public const int ManifestVersion = 1;
        public const string ManifestFileName = "restaurant-slots.json";
        public const string LegacyFileName = "restaurant-save.json";

        readonly string directory;
        SaveSlotManifest manifest;

        public SaveSlotStore(string directory)
        {
            this.directory = directory;
            manifest = LoadOrMigrate();
        }

        public int ActiveSlotId => manifest.activeSlotId <= 0 ? 1 : manifest.activeSlotId;
        public string ManifestPath => Path.Combine(directory, ManifestFileName);
        public SaveSlotInfo[] Slots => manifest.slots ?? Array.Empty<SaveSlotInfo>();
        public int OccupiedCount => Slots.Length;
        public string ActiveFileName => FileNameFor(ActiveSlotId);
        public bool ManifestExists => File.Exists(ManifestPath);

        public static string NumberedFileName(int id) => "restaurant-save-" + id + ".json";

        public string FileNameFor(int id)
        {
            var slot = Find(id);
            if (slot != null && !string.IsNullOrEmpty(slot.fileName)) return slot.fileName;
            return id <= 1 ? LegacyFileName : NumberedFileName(id);
        }

        public string FilePathFor(int id) => Path.Combine(directory, FileNameFor(id));

        public SaveSlotInfo Find(int id)
        {
            if (manifest.slots == null) return null;
            for (int i = 0; i < manifest.slots.Length; i++)
                if (manifest.slots[i] != null && manifest.slots[i].id == id) return manifest.slots[i];
            return null;
        }

        public bool CanStartNewGame => OccupiedCount < MaxSlots;

        public bool CanDelete(int id) => id != ActiveSlotId && OccupiedCount > 1 && Find(id) != null;

        public void RecordActive(int shopRank, long coins, long lastPlayedUtcTicks, string displayName = null)
        {
            var slot = Ensure(ActiveSlotId);
            slot.shopRank = shopRank < 1 ? 1 : shopRank;
            slot.coins = coins < 0 ? 0 : coins;
            slot.lastPlayedUtcTicks = lastPlayedUtcTicks;
            if (!string.IsNullOrEmpty(displayName)) slot.displayName = displayName;
            else if (string.IsNullOrEmpty(slot.displayName)) slot.displayName = Label(slot.id);
            Write();
        }

        public bool TryCreateSlot(out int id)
        {
            id = 0;
            if (!CanStartNewGame) return false;
            id = FirstEmptyId();
            if (id <= 0) return false;
            manifest.activeSlotId = id;
            Ensure(id);
            Write();
            return true;
        }

        public bool TryActivate(int id)
        {
            if (Find(id) == null) return false;
            manifest.activeSlotId = id;
            Write();
            return true;
        }

        public bool TryDelete(int id)
        {
            if (!CanDelete(id)) return false;
            TryDeleteFile(FilePathFor(id));
            TryDeleteFile(FilePathFor(id) + ".bak");
            TryDeleteFile(FilePathFor(id) + ".tmp");
            var kept = new List<SaveSlotInfo>(OccupiedCount);
            foreach (var slot in manifest.slots)
                if (slot != null && slot.id != id) kept.Add(slot);
            manifest.slots = kept.ToArray();
            Write();
            return true;
        }

        SaveSlotManifest LoadOrMigrate()
        {
            if (File.Exists(ManifestPath))
            {
                var loaded = ReadManifest();
                if (loaded != null && loaded.slots != null && loaded.slots.Length > 0)
                    return loaded;
            }

            string legacy = Path.Combine(directory, LegacyFileName);
            if (File.Exists(legacy) || File.Exists(legacy + ".bak"))
            {
                var migrated = MigrateLegacy();
                Write(migrated);
                return migrated;
            }

            var recovered = RecoverFromFiles();
            if (recovered.slots != null && recovered.slots.Length > 0)
            {
                Write(recovered);
                return recovered;
            }

            return new SaveSlotManifest
            {
                version = ManifestVersion,
                activeSlotId = 1,
                slots = Array.Empty<SaveSlotInfo>()
            };
        }

        SaveSlotManifest MigrateLegacy()
        {
            var store = new LocalSaveStore(directory, LegacyFileName);
            store.Load(out var data);
            return new SaveSlotManifest
            {
                version = ManifestVersion,
                activeSlotId = 1,
                slots = new[]
                {
                    new SaveSlotInfo
                    {
                        id = 1,
                        displayName = Label(1),
                        fileName = LegacyFileName,
                        lastPlayedUtcTicks = data != null && data.version >= 16 ? data.lastSeenUtcTicks : 0,
                        shopRank = data != null ? data.ResolvedShopRank : 1,
                        coins = data != null ? data.coins : 0
                    }
                }
            };
        }

        SaveSlotManifest RecoverFromFiles()
        {
            var found = new List<SaveSlotInfo>();
            if (File.Exists(Path.Combine(directory, LegacyFileName)))
                found.Add(Summarize(1, LegacyFileName));
            string numberedOne = Path.Combine(directory, NumberedFileName(1));
            if (found.Count == 0 && File.Exists(numberedOne))
                found.Add(Summarize(1, NumberedFileName(1)));
            for (int id = 2; id <= MaxSlots; id++)
            {
                string name = NumberedFileName(id);
                if (File.Exists(Path.Combine(directory, name))) found.Add(Summarize(id, name));
            }
            int active = found.Count == 0 ? 1 : found[0].id;
            long newest = -1;
            foreach (var slot in found)
                if (slot.lastPlayedUtcTicks >= newest) { newest = slot.lastPlayedUtcTicks; active = slot.id; }
            return new SaveSlotManifest
            {
                version = ManifestVersion,
                activeSlotId = active,
                slots = found.ToArray()
            };
        }

        SaveSlotInfo Summarize(int id, string fileName)
        {
            var store = new LocalSaveStore(directory, fileName);
            store.Load(out var data);
            return new SaveSlotInfo
            {
                id = id,
                displayName = Label(id),
                fileName = fileName,
                lastPlayedUtcTicks = data != null && data.version >= 16 ? data.lastSeenUtcTicks : 0,
                shopRank = data != null ? data.ResolvedShopRank : 1,
                coins = data != null ? data.coins : 0
            };
        }

        SaveSlotInfo Ensure(int id)
        {
            var existing = Find(id);
            if (existing != null)
            {
                if (string.IsNullOrEmpty(existing.fileName)) existing.fileName = FileNameFor(id);
                if (string.IsNullOrEmpty(existing.displayName)) existing.displayName = Label(id);
                if (existing.shopRank < 1) existing.shopRank = 1;
                return existing;
            }
            var created = new SaveSlotInfo
            {
                id = id,
                displayName = Label(id),
                fileName = id <= 1 ? LegacyFileName : NumberedFileName(id),
                shopRank = 1,
                coins = 0
            };
            var list = new List<SaveSlotInfo>(manifest.slots ?? Array.Empty<SaveSlotInfo>()) { created };
            list.Sort((a, b) => a.id.CompareTo(b.id));
            manifest.slots = list.ToArray();
            return created;
        }

        int FirstEmptyId()
        {
            for (int id = 1; id <= MaxSlots; id++)
                if (Find(id) == null) return id;
            return 0;
        }

        SaveSlotManifest ReadManifest()
        {
            try
            {
                var loaded = JsonUtility.FromJson<SaveSlotManifest>(File.ReadAllText(ManifestPath));
                if (loaded == null) return null;
                if (loaded.version > ManifestVersion) return null;
                if (loaded.slots == null) loaded.slots = Array.Empty<SaveSlotInfo>();
                if (loaded.activeSlotId < 1 || loaded.activeSlotId > MaxSlots) loaded.activeSlotId = 1;
                return loaded;
            }
            catch (Exception) { return null; }
        }

        void Write() => Write(manifest);

        void Write(SaveSlotManifest data)
        {
            if (data == null) return;
            data.version = ManifestVersion;
            string path = ManifestPath;
            string temporary = path + ".tmp";
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            catch (Exception) { }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (Exception) { }
            }
        }

        static void TryDeleteFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception) { }
        }

        public static string Label(int id) => "Shop " + id;
    }
}
