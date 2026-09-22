using System;

namespace BurgerShop.Persistence
{
    public enum SaveSlotOpResult
    {
        Success,
        InvalidData,
        SlotMissing,
        NotAllowed,
        IncompatibleManifest,
        PermissionDenied,
        CommitFailed
    }

    [Serializable]
    public sealed class SaveSlotInfo
    {
        public int id;
        public string displayName;
        public string fileName;
        public long lastPlayedUtcTicks;
        public int shopRank;
        public long coins;
    }

    [Serializable]
    public sealed class SaveSlotManifest
    {
        public int version = 1;
        public int activeSlotId = 1;
        public SaveSlotInfo[] slots = Array.Empty<SaveSlotInfo>();
    }
}
