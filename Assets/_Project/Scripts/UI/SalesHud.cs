using BurgerShop.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class SalesHud : MonoBehaviour
    {
        RestaurantWallet wallet;
        Text label;
        long shownCoins = -1;
        long lastPayment;
        float paymentUntil;

        public void Configure(RestaurantWallet earnings, Text text)
        {
            wallet = earnings;
            label = text;
            Refresh();
        }

        void LateUpdate() => Refresh();

        void Refresh()
        {
            if (wallet == null || label == null) return;
            if (shownCoins >= 0 && shownCoins != wallet.Coins)
            {
                lastPayment = wallet.Coins - shownCoins;
                paymentUntil = Time.time + 1.4f;
            }
            shownCoins = wallet.Coins;
            string feedback = Time.time < paymentUntil ? $"\n+{lastPayment}" : "";
            label.text = $"COINS {wallet.Coins}\nSERVED {wallet.CompletedSales}{feedback}";
        }
    }
}
