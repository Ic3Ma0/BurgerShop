using System;
using UnityEngine;

namespace BurgerShop.Economy
{
    public sealed class RestaurantWallet : MonoBehaviour
    {
        public long Coins { get; private set; }
        public int CompletedSales { get; private set; }
        public event Action<int> SaleRecorded;
        public event Action<int> CoinsSpent;

        public void RestoreProgress(long coins, int completedSales)
        {
            if (coins < 0 || completedSales < 0) throw new ArgumentOutOfRangeException(nameof(coins));
            Coins = coins;
            CompletedSales = completedSales;
        }

        public bool CanCompleteSale() => CompletedSales < int.MaxValue;

        public bool CanCollectCoins(int amount) => amount > 0 && Coins <= long.MaxValue - amount;

        public bool CanRecordSale(int amount) => amount > 0 && CanCompleteSale() && CanCollectCoins(amount);

        public bool RecordCompletedSale()
        {
            if (!CanCompleteSale()) return false;
            CompletedSales++;
            return true;
        }

        public bool CollectCoins(int amount)
        {
            if (!CanCollectCoins(amount)) return false;
            Coins += amount;
            SaleRecorded?.Invoke(amount);
            return true;
        }

        public bool RecordSale(int amount)
        {
            if (!CanRecordSale(amount)) return false;
            Coins += amount;
            CompletedSales++;
            SaleRecorded?.Invoke(amount);
            return true;
        }

        public bool TrySpend(int amount) => TrySpend(amount, null);

        public bool TrySpend(int amount, Action commit)
        {
            if (amount <= 0 || Coins < amount) return false;
            Coins -= amount;
            commit?.Invoke();
            CoinsSpent?.Invoke(amount);
            return true;
        }
    }
}
