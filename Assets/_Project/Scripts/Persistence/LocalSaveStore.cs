using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace BurgerShop.Persistence
{
    public enum SaveLoadResult { NewGame, Loaded, RecoveredBackup, Unreadable, NewerVersion, Unavailable }

    public sealed class LocalSaveStore
    {
        // 16 KiB rejected pretty-printed v17 snapshots that still pass IsValid (tens of layout rows).
        // 256 KiB covers hundreds of unique layout records plus the rest of the v17 envelope, and
        // still stops a runaway write from replacing a good file.
        public const int MaxFileBytes = 256 * 1024;

        [Serializable]
        sealed class Envelope
        {
            public RestaurantSaveData data;
            public string checksum;
        }

        readonly string path;
        bool preserveBackup;
        public string FilePath => path;
        public string BackupPath => path + ".bak";
        public bool CanWrite { get; private set; } = true;
        public string LastError { get; private set; }

        public LocalSaveStore(string directory) : this(directory, "restaurant-save.json") { }

        public LocalSaveStore(string directory, string fileName)
            => path = Path.Combine(directory, string.IsNullOrEmpty(fileName) ? "restaurant-save.json" : fileName);

        public SaveLoadResult Load(out RestaurantSaveData data)
        {
            data = null;
            LastError = null;
            CanWrite = true;
            preserveBackup = false;
            try
            {
                SaveLoadResult result = Read(path, out data);
                if (result == SaveLoadResult.Loaded) return result;
                if (result == SaveLoadResult.NewerVersion) { CanWrite = false; return result; }
                SaveLoadResult backup = Read(BackupPath, out data);
                if (backup == SaveLoadResult.Loaded)
                {
                    preserveBackup = true;
                    return SaveLoadResult.RecoveredBackup;
                }
                if (backup == SaveLoadResult.NewerVersion) { CanWrite = false; return backup; }
                if (result == SaveLoadResult.NewGame && backup == SaveLoadResult.NewGame) return SaveLoadResult.NewGame;
                CanWrite = false; // Keep unreadable files for recovery instead of replacing the user's progress.
                return SaveLoadResult.Unreadable;
            }
            catch (Exception error) when (IsFileError(error))
            {
                LastError = error.Message;
                CanWrite = false;
                return SaveLoadResult.Unavailable;
            }
        }

        static SaveLoadResult Read(string source, out RestaurantSaveData data)
        {
            data = null;
            if (!File.Exists(source)) return SaveLoadResult.NewGame;
            if (new FileInfo(source).Length > MaxFileBytes) return SaveLoadResult.Unreadable;
            Envelope envelope;
            try { envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(source)); }
            catch (ArgumentException) { return SaveLoadResult.Unreadable; }
            if (envelope?.data == null) return SaveLoadResult.Unreadable;
            if (envelope.data.version > RestaurantSaveData.CurrentVersion) return SaveLoadResult.NewerVersion;
            if (!envelope.data.IsValid || envelope.checksum != envelope.data.Checksum()) return SaveLoadResult.Unreadable;
            data = envelope.data;
            return SaveLoadResult.Loaded;
        }

        public bool Save(RestaurantSaveData data)
        {
            if (!CanWrite || data == null || !data.IsValid) return false;
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Envelope { data = data, checksum = data.Checksum() }, true));
            if (bytes.Length > MaxFileBytes)
            {
                LastError = "snapshot exceeds " + MaxFileBytes + " bytes";
                return false;
            }
            string temporary = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (FileStream stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, preserveBackup ? null : BackupPath);
                else File.Move(temporary, path);
                preserveBackup = false;
                LastError = null;
                return true;
            }
            catch (Exception error) when (IsFileError(error))
            {
                LastError = error.Message;
                return false;
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (Exception error) when (IsFileError(error)) { }
            }
        }

        static bool IsFileError(Exception error) => error is IOException || error is UnauthorizedAccessException
            || error is System.Security.SecurityException || error is NotSupportedException;
    }
}
