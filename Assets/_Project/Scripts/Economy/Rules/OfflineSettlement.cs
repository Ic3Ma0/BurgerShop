using System;

namespace BurgerShop.Economy
{
    // Value snapshots, not another owner of the wallet or persisted receipt.
    public readonly struct OfflineReceipt
    {
        public readonly long Grant;
        public readonly long Ticks;
        public readonly int StaffCount;
        public readonly bool Visible;

        public OfflineReceipt(long grant, long ticks, int staffCount, bool visible)
        {
            Grant = grant;
            Ticks = ticks;
            StaffCount = staffCount;
            Visible = visible;
        }
    }

    public readonly struct OfflineSettlementPlan
    {
        public readonly long Coins;
        public readonly long LastSeenUtcTicks;
        public readonly OfflineReceipt Receipt;

        public OfflineSettlementPlan(long coins, long lastSeenUtcTicks, OfflineReceipt receipt)
        {
            Coins = coins;
            LastSeenUtcTicks = lastSeenUtcTicks;
            Receipt = receipt;
        }
    }

    public static class OfflineSettlement
    {
        // No clock, scene, wallet mutation or file access. The caller commits this plan
        // atomically before publishing any of it to the running restaurant.
        public static OfflineSettlementPlan Calculate(long coins, long lastSeenUtcTicks,
            long nowUtcTicks, OfflineReceipt receipt, int nextUpgradeCost,
            int staffCount, int speedTier, int carryTier)
        {
            long ticks = OfflineEarnings.ElapsedTicks(lastSeenUtcTicks, nowUtcTicks);
            long grant = lastSeenUtcTicks == 0 ? 0 : OfflineEarnings.Grant(
                nextUpgradeCost, staffCount, speedTier, carryTier,
                ticks / (decimal)TimeSpan.TicksPerMinute);
            grant = Math.Min(grant, long.MaxValue - coins);

            // Preserve an unacknowledged receipt when a refresh earns nothing.
            // A later positive settlement replaces it (existing 051 semantics).
            if ((!receipt.Visible && lastSeenUtcTicks > 0 && ticks > 0)
                || (receipt.Visible && grant > 0))
                receipt = new OfflineReceipt(grant, ticks, staffCount, true);

            return new OfflineSettlementPlan(coins + grant, nowUtcTicks, receipt);
        }
    }
}
