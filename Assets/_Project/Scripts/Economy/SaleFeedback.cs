using BurgerShop.Restaurant;
using BurgerShop.UI;
using UnityEngine;

namespace BurgerShop.Economy
{
    public sealed class SaleFeedback : MonoBehaviour
    {
        RestaurantWallet wallet;
        AudioSource source;
        AudioClip coin;
        AudioClip spend;

        public int CoinPlayCount { get; private set; }
        public int SpendPlayCount { get; private set; }

        public void Configure(RestaurantWallet earnings)
        {
            if (wallet != null)
            {
                wallet.SaleRecorded -= PlayCoin;
                wallet.CoinsSpent -= PlaySpend;
            }
            wallet = earnings;
            source = GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0.28f;
            if (coin == null) coin = CoinSfx.CreateCoin();
            if (spend == null) spend = CoinSfx.CreateSpend();
            source.clip = coin;
            if (wallet != null)
            {
                wallet.SaleRecorded += PlayCoin;
                wallet.CoinsSpent += PlaySpend;
            }
        }

        void PlayCoin(int amount)
        {
            if (!isActiveAndEnabled || source == null || coin == null) return;
            if (FeedbackDirector.Current != null)
            { if(FeedbackDirector.Current.RequestSound(FeedbackSound.Cash))CoinPlayCount++; return; }
            CoinPlayCount++;
        }

        void PlaySpend(int amount)
        {
            if (!isActiveAndEnabled || source == null || spend == null) return;
            if (FeedbackDirector.Current != null)
            { if(FeedbackDirector.Current.RequestSound(FeedbackSound.Spend))SpendPlayCount++; return; }
            SpendPlayCount++;
        }

        void OnDestroy()
        {
            if (wallet != null)
            {
                wallet.SaleRecorded -= PlayCoin;
                wallet.CoinsSpent -= PlaySpend;
            }
            if (coin != null) BurgerVisual.Release(coin);
            if (spend != null) BurgerVisual.Release(spend);
            coin = null;
            spend = null;
        }
    }
}
