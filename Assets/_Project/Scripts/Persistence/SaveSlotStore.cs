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
        bool incompatible;

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
        public bool IsIncompatible => incompatible;
        public SaveSlotOpResult LastOpResult { get; private set; } = SaveSlotOpResult.Success;
        public string LastError { get; private set; }

        public static string NumberedFileName(int id) => "restaurant-save-" + id + ".json";

        public string FileNameFor(int id)
        {
            var slot = Find(id);
            if (slot != null && !string.IsNullOrEmpty(slot.fileName)) return slot.fileName;
            return id <= 1 ? LegacyFileName : NumberedFileName(id);
        }

        public string FilePathFor(int id) => Path.Combine(directory, FileNameFor(id));

        public SaveSlotInfo Find(int id) => Find(manifest, id);

        public bool CanStartNewGame => !incompatible && OccupiedCount < MaxSlots && FirstEmptyId() > 0;

        public bool CanDelete(int id) => !incompatible && id != ActiveSlotId && OccupiedCount > 1 && Find(id) != null;

        public SaveSlotOpResult RecordActive(int shopRank, long coins, long lastPlayedUtcTicks, string displayName = null)
        {
            if (incompatible) return SetResult(SaveSlotOpResult.IncompatibleManifest, "manifest is newer than this reader");
            var next = Clone(manifest);
            var slot = EnsureSlot(next, next.activeSlotId <= 0 ? 1 : next.activeSlotId);
            slot.shopRank = shopRank < 1 ? 1 : shopRank;
            slot.coins = coins < 0 ? 0 : coins;
            slot.lastPlayedUtcTicks = lastPlayedUtcTicks;
            if (!string.IsNullOrEmpty(displayName)) slot.displayName = displayName;
            else if (string.IsNullOrEmpty(slot.displayName)) slot.displayName = Label(slot.id);
            var result = Write(next);
            if (result != SaveSlotOpResult.Success) return result;
            manifest = next;
            return SetResult(SaveSlotOpResult.Success, null);
        }

        public SaveSlotOpResult TryCreateSlot(out int id)
        {
            id = 0;
            if (incompatible) return SetResult(SaveSlotOpResult.IncompatibleManifest, "manifest is newer than this reader");
            if (!CanStartNewGame) return SetResult(SaveSlotOpResult.NotAllowed, "no empty slot");
            id = FirstEmptyId();
            if (id <= 0)
            {
                id = 0;
                return SetResult(SaveSlotOpResult.NotAllowed, "no empty slot");
            }
            var next = Clone(manifest);
            next.activeSlotId = id;
            EnsureSlot(next, id);
            var result = Write(next);
            if (result != SaveSlotOpResult.Success)
            {
                id = 0;
                return result;
            }
            manifest = next;
            return SetResult(SaveSlotOpResult.Success, null);
        }

        public SaveSlotOpResult TryActivate(int id)
        {
            if (incompatible) return SetResult(SaveSlotOpResult.IncompatibleManifest, "manifest is newer than this reader");
            if (Find(id) == null) return SetResult(SaveSlotOpResult.SlotMissing, "slot " + id + " is not in the index");
            var next = Clone(manifest);
            next.activeSlotId = id;
            var result = Write(next);
            if (result != SaveSlotOpResult.Success) return result;
            manifest = next;
            return SetResult(SaveSlotOpResult.Success, null);
        }

        public SaveSlotOpResult TryDelete(int id)
        {
            if (incompatible) return SetResult(SaveSlotOpResult.IncompatibleManifest, "manifest is newer than this reader");
            if (Find(id) == null) return SetResult(SaveSlotOpResult.SlotMissing, "slot " + id + " is not in the index");
            if (!CanDelete(id)) return SetResult(SaveSlotOpResult.NotAllowed, "cannot delete the active or last slot");
            foreach (string path in FilesForSlot(id))
                TryDeletePath(path);
            if (SlotFilesRemain(id))
                return SetResult(SaveSlotOpResult.CommitFailed, "slot files still exist");
            var next = Clone(manifest);
            var kept = new List<SaveSlotInfo>(OccupiedCount);
            if (next.slots != null)
                foreach (var slot in next.slots)
                    if (slot != null && slot.id != id) kept.Add(slot);
            next.slots = kept.ToArray();
            var result = Write(next);
            if (result != SaveSlotOpResult.Success) return result;
            manifest = next;
            return SetResult(SaveSlotOpResult.Success, null);
        }

        SaveSlotManifest LoadOrMigrate()
        {
            if (File.Exists(ManifestPath))
            {
                if (TryReadManifest(out var loaded, out bool future))
                {
                    if (future)
                    {
                        incompatible = true;
                        LastOpResult = SaveSlotOpResult.IncompatibleManifest;
                        LastError = "manifest is newer than this reader";
                        return loaded ?? EmptyManifest();
                    }
                    var merged = MergeDiscovered(loaded);
                    if (IndexChanged(loaded, merged))
                        Write(merged);
                    return merged;
                }
            }

            var recovered = RecoverFromFiles();
            if (recovered.slots != null && recovered.slots.Length > 0)
            {
                Write(recovered);
                return recovered;
            }

            return EmptyManifest();
        }

        SaveSlotManifest MergeDiscovered(SaveSlotManifest loaded)
        {
            var recovered = RecoverFromFiles();
            var map = new Dictionary<int, SaveSlotInfo>();
            if (loaded?.slots != null)
                foreach (var slot in loaded.slots)
                    if (slot != null && slot.id >= 1 && slot.id <= MaxSlots)
                        map[slot.id] = CloneSlot(slot);
            if (recovered.slots != null)
                foreach (var slot in recovered.slots)
                    if (slot != null && slot.id >= 1 && slot.id <= MaxSlots && !map.ContainsKey(slot.id))
                        map[slot.id] = CloneSlot(slot);
            var merged = new SaveSlotManifest
            {
                version = ManifestVersion,
                activeSlotId = loaded != null && loaded.activeSlotId >= 1 && loaded.activeSlotId <= MaxSlots
                    ? loaded.activeSlotId
                    : 1,
                slots = ToSortedSlots(map)
            };
            if (Find(merged, merged.activeSlotId) == null && merged.slots.Length > 0)
                merged.activeSlotId = NewestId(merged.slots);
            return merged;
        }

        SaveSlotManifest RecoverFromFiles()
        {
            var map = new Dictionary<int, SaveSlotInfo>();
            for (int id = 1; id <= MaxSlots; id++)
            {
                if (!TryResolveOccupiedFile(id, out string fileName)) continue;
                map[id] = Summarize(id, fileName);
            }
            var slots = ToSortedSlots(map);
            return new SaveSlotManifest
            {
                version = ManifestVersion,
                activeSlotId = slots.Length == 0 ? 1 : NewestId(slots),
                slots = slots
            };
        }

        bool TryResolveOccupiedFile(int id, out string fileName)
        {
            string[] names = id <= 1
                ? new[] { LegacyFileName, NumberedFileName(1) }
                : new[] { NumberedFileName(id) };
            foreach (string name in names)
            {
                string path = Path.Combine(directory, name);
                if (File.Exists(path) || File.Exists(path + ".bak"))
                {
                    fileName = name;
                    return true;
                }
            }
            fileName = id <= 1 ? LegacyFileName : NumberedFileName(id);
            return false;
        }

        bool SlotOccupiedOnDisk(int id) => TryResolveOccupiedFile(id, out _);

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

        SaveSlotInfo EnsureSlot(SaveSlotManifest target, int id)
        {
            var existing = Find(target, id);
            if (existing != null)
            {
                if (string.IsNullOrEmpty(existing.fileName)) existing.fileName = id <= 1 ? LegacyFileName : NumberedFileName(id);
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
            var list = new List<SaveSlotInfo>(target.slots ?? Array.Empty<SaveSlotInfo>()) { created };
            list.Sort((a, b) => a.id.CompareTo(b.id));
            target.slots = list.ToArray();
            return created;
        }

        int FirstEmptyId()
        {
            for (int id = 1; id <= MaxSlots; id++)
                if (Find(id) == null && !SlotOccupiedOnDisk(id)) return id;
            return 0;
        }

        bool TryReadManifest(out SaveSlotManifest loaded, out bool future)
        {
            loaded = null;
            future = false;
            try
            {
                loaded = JsonUtility.FromJson<SaveSlotManifest>(File.ReadAllText(ManifestPath));
                if (loaded == null) return false;
                if (loaded.version > ManifestVersion)
                {
                    future = true;
                    if (loaded.slots == null) loaded.slots = Array.Empty<SaveSlotInfo>();
                    return true;
                }
                if (loaded.slots == null) loaded.slots = Array.Empty<SaveSlotInfo>();
                if (loaded.activeSlotId < 1 || loaded.activeSlotId > MaxSlots) loaded.activeSlotId = 1;
                loaded.version = ManifestVersion;
                return true;
            }
            catch (Exception)
            {
                loaded = null;
                future = false;
                return false;
            }
        }

        SaveSlotOpResult Write(SaveSlotManifest data)
        {
            if (incompatible) return SetResult(SaveSlotOpResult.IncompatibleManifest, "manifest is newer than this reader");
            if (data == null) return SetResult(SaveSlotOpResult.InvalidData, "manifest is null");
            data.version = ManifestVersion;
            string path = ManifestPath;
            string temporary = path + ".tmp";
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                return SetResult(SaveSlotOpResult.Success, null);
            }
            catch (UnauthorizedAccessException error)
            {
                return SetResult(SaveSlotOpResult.PermissionDenied, error.Message);
            }
            catch (System.Security.SecurityException error)
            {
                return SetResult(SaveSlotOpResult.PermissionDenied, error.Message);
            }
            catch (IOException error)
            {
                return SetResult(SaveSlotOpResult.CommitFailed, error.Message);
            }
            catch (NotSupportedException error)
            {
                return SetResult(SaveSlotOpResult.CommitFailed, error.Message);
            }
            catch (Exception error)
            {
                return SetResult(SaveSlotOpResult.CommitFailed, error.Message);
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (Exception) { }
            }
        }

        IEnumerable<string> FilesForSlot(int id)
        {
            var names = new HashSet<string>();
            var listed = Find(id);
            if (listed != null && !string.IsNullOrEmpty(listed.fileName)) names.Add(listed.fileName);
            if (id <= 1)
            {
                names.Add(LegacyFileName);
                names.Add(NumberedFileName(1));
            }
            else names.Add(NumberedFileName(id));
            foreach (string name in names)
            {
                string path = Path.Combine(directory, name);
                yield return path;
                yield return path + ".bak";
                yield return path + ".tmp";
            }
        }

        bool SlotFilesRemain(int id)
        {
            foreach (string path in FilesForSlot(id))
                if (File.Exists(path) || Directory.Exists(path)) return true;
            return false;
        }

        static void TryDeletePath(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception) { }
        }

        SaveSlotOpResult SetResult(SaveSlotOpResult result, string error)
        {
            LastOpResult = result;
            LastError = error;
            return result;
        }

        static SaveSlotManifest EmptyManifest() => new SaveSlotManifest
        {
            version = ManifestVersion,
            activeSlotId = 1,
            slots = Array.Empty<SaveSlotInfo>()
        };

        static SaveSlotInfo Find(SaveSlotManifest data, int id)
        {
            if (data?.slots == null) return null;
            for (int i = 0; i < data.slots.Length; i++)
                if (data.slots[i] != null && data.slots[i].id == id) return data.slots[i];
            return null;
        }

        static SaveSlotInfo[] ToSortedSlots(Dictionary<int, SaveSlotInfo> map)
        {
            var list = new List<SaveSlotInfo>(map.Count);
            foreach (var pair in map) list.Add(pair.Value);
            list.Sort((a, b) => a.id.CompareTo(b.id));
            return list.ToArray();
        }

        static int NewestId(SaveSlotInfo[] slots)
        {
            int active = slots[0].id;
            long newest = slots[0].lastPlayedUtcTicks;
            for (int i = 1; i < slots.Length; i++)
                if (slots[i].lastPlayedUtcTicks >= newest)
                {
                    newest = slots[i].lastPlayedUtcTicks;
                    active = slots[i].id;
                }
            return active;
        }

        static bool IndexChanged(SaveSlotManifest before, SaveSlotManifest after)
        {
            if (before == null || after == null) return true;
            if (before.activeSlotId != after.activeSlotId) return true;
            int beforeCount = before.slots == null ? 0 : before.slots.Length;
            int afterCount = after.slots == null ? 0 : after.slots.Length;
            if (beforeCount != afterCount) return true;
            for (int i = 0; i < afterCount; i++)
            {
                if (before.slots[i] == null || after.slots[i] == null) return true;
                if (before.slots[i].id != after.slots[i].id) return true;
            }
            return false;
        }

        static SaveSlotManifest Clone(SaveSlotManifest source)
        {
            if (source == null) return EmptyManifest();
            var copy = JsonUtility.FromJson<SaveSlotManifest>(JsonUtility.ToJson(source));
            if (copy == null) return EmptyManifest();
            if (copy.slots == null) copy.slots = Array.Empty<SaveSlotInfo>();
            return copy;
        }

        static SaveSlotInfo CloneSlot(SaveSlotInfo source)
        {
            if (source == null) return null;
            return JsonUtility.FromJson<SaveSlotInfo>(JsonUtility.ToJson(source));
        }

        public static string Label(int id) => "Shop " + id;
    }
}
