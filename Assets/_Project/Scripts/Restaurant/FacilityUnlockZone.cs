using System;
using BurgerShop.Economy;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class FacilityUnlockZone : MonoBehaviour, IInvestZone
    {
        public const float EntryDelay = 0.3f;
        public const float ContributionInterval = 0.1f;
        public const int ContributionSize = 10;
        RestaurantWallet wallet;
        BurgerInventory player;
        Transform pad;
        TextMesh marker;
        Action onUnlocked;
        float untilContribution = EntryDelay;
        bool selected;
        bool storeOnly;
        public void SetStoreOnly(){storeOnly=true;RankVisible=false;enabled=false;ResetEntry();if(pad!=null)pad.gameObject.SetActive(false);if(marker!=null)marker.gameObject.SetActive(false);}
        bool paused;
        bool unfocused;
        const float Radius = 1f;

        public int Cost { get; private set; }
        public int Invested { get; private set; }
        public int Remaining => Cost - Invested;
        public string Title { get; private set; } = "BUY";
        public bool IsPurchased { get; private set; }
        public bool PurchasedThisVisit { get; private set; }
        public float Progress => Cost > 0 ? (float)Invested / Cost : 0f;
        public long MissingCoins => wallet != null ? Math.Max(0, Remaining - wallet.Coins) : Remaining;
        public Vector3 PadPosition => pad != null ? pad.position : transform.position;
        public RestaurantWallet Wallet => wallet;
        public BurgerInventory Player => player;
        public bool IsSelected => selected;
        public bool RankVisible { get; private set; } = true;
        public bool IsAvailable => !storeOnly && isActiveAndEnabled && RankVisible && !IsPurchased && wallet != null && wallet.isActiveAndEnabled
            && player != null && player.isActiveAndEnabled && !paused && !unfocused;
        public bool IsInRange => player != null && !IsPurchased && DistanceSquared <= Radius * Radius;
        public float DistanceSquared
        {
            get { Vector3 offset = player.transform.position - PadPosition; offset.y = 0f; return offset.sqrMagnitude; }
        }

        void OnEnable() => InvestZoneRegistry.Register(this);
        void OnDisable() { InvestZoneRegistry.Unregister(this); ResetEntry(); }
        void OnApplicationPause(bool value) { paused = value; ResetEntry(); }
        void OnApplicationFocus(bool value) { unfocused = !value; ResetEntry(); }
        void ResetEntry() { selected = false; untilContribution = EntryDelay; }

        public void Configure(RestaurantWallet earnings, BurgerInventory carrier, Transform point, int price,
            string title, Action unlocked, TextMesh label = null)
        {
            if (isActiveAndEnabled) InvestZoneRegistry.Register(this);
            wallet = earnings;
            player = carrier;
            pad = point;
            Cost = Mathf.Max(1, price);
            Title = string.IsNullOrEmpty(title) ? "BUY" : title;
            onUnlocked = unlocked;
            marker = label;
            ResetEntry();
            PurchasedThisVisit = false;
            RefreshMarker();
        }

        public void RestorePurchased()
        {
            Invested = Cost;
            IsPurchased = true;
            ResetEntry();
            PurchasedThisVisit = false;
            if (pad != null) pad.gameObject.SetActive(false);
            RefreshMarker();
        }

        public void RestoreInvestment(int amount)
        {
            if (amount < 0 || amount > Cost) throw new ArgumentOutOfRangeException(nameof(amount));
            if (IsPurchased) return;
            Invested = amount;
            ResetEntry();
            if (Remaining == 0) Complete(false);
            RefreshMarker();
        }

        public void SetRankVisible(bool visible)
        {
            RankVisible = !storeOnly && (visible || Invested > 0);
            if (IsPurchased)
            {
                if (pad != null) pad.gameObject.SetActive(false);
                if (marker != null) marker.gameObject.SetActive(false);
                return;
            }
            if (pad != null) pad.gameObject.SetActive(RankVisible);
            if (marker != null) marker.gameObject.SetActive(RankVisible);
        }

        IInvestZone Nearest() => InvestZoneRegistry.Nearest(this);

        void Update() => Advance(Time.deltaTime);
        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f || IsPurchased) return;
            if (!IsAvailable || !IsInRange || Nearest() != this)
            { ResetEntry(); PurchasedThisVisit = false; return; }
            selected = true;
            untilContribution -= deltaTime;
            while (untilContribution <= 0.00001f && !IsPurchased)
            {
                int amount = (int)Math.Min(Math.Min(ContributionSize, Remaining), wallet.Coins);
                if (amount <= 0) { untilContribution = 0f; break; }
                // Publish the wallet event only after investment and ownership have both committed.
                if (!wallet.TrySpend(amount, () =>
                {
                    Invested += amount;
                    if (Remaining == 0) Complete(true);
                    RefreshMarker();
                })) break;
                if (Application.isPlaying)
                    InvestmentCoin.Launch(player.transform.position + Vector3.up, PadPosition + Vector3.up * 0.2f, transform.parent);
                untilContribution += ContributionInterval;
            }
        }

        void Complete(bool awardStars)
        {
            if (IsPurchased) return;
            IsPurchased = true;
            PurchasedThisVisit = true;
            if (awardStars)
                GetComponentInParent<UI.SessionGoalTracker>()?.AddUpgradeStars();
            onUnlocked?.Invoke();
            UI.FeedbackDirector.Current?.Success(PadPosition,"Built!",player != null ? player.transform : null);
            if (pad != null) pad.gameObject.SetActive(false);
            RefreshMarker();
        }

        void LateUpdate()
        {
            if (marker == null) return;
            marker.gameObject.SetActive(!storeOnly && !IsPurchased && RankVisible);
            if (Camera.main != null) marker.transform.rotation = Camera.main.transform.rotation;
        }

        void RefreshMarker()
        {
            if (marker == null) return;
            marker.text = IsPurchased ? "BUILT!" : $"{Title}\nRemaining {Remaining}\n{ShopRanks.StarRewardCopy}";
            marker.gameObject.SetActive(!storeOnly && !IsPurchased && RankVisible);
        }
    }
}
