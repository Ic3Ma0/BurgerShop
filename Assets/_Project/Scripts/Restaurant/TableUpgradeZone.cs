using System;
using BurgerShop.Economy;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class TableUpgradeZone : MonoBehaviour, IInvestZone
    {
        public const float Radius = 1f;
        RestaurantWallet wallet;
        BurgerInventory player;
        DiningTable table;
        Transform pad;
        TextMesh marker;
        Action changed;
        float untilContribution = FacilityUnlockZone.EntryDelay;
        bool selected;
        bool paused;
        bool unfocused;

        public DiningTable Table => table;
        public int SlotIndex { get; private set; }
        public int Cost => TableSetCatalog.UpgradeCost;
        public int Invested { get; private set; }
        public int Remaining => Cost - Invested;
        public bool IsPurchased { get; private set; }
        public bool PendingChoice { get; private set; }
        public bool HasChosenSet => table != null && TableSetCatalog.IsChoice(table.SetId);
        public TableSetId SetId => table != null ? table.SetId : TableSetId.Starter;
        public Vector3 PadPosition => pad != null ? pad.position : transform.position;
        public RestaurantWallet Wallet => wallet;
        public BurgerInventory Player => player;
        public bool IsSelected => selected;
        public bool IsAvailable => isActiveAndEnabled && !IsPurchased && !HasChosenSet && wallet != null
            && wallet.isActiveAndEnabled && player != null && player.isActiveAndEnabled && !paused && !unfocused;
        public bool IsInRange => player != null && !IsPurchased && !HasChosenSet
            && DistanceSquared <= Radius * Radius;
        public bool IsChoiceRange => player != null && PendingChoice && DistanceSquared <= Radius * Radius;
        public float DistanceSquared
        {
            get
            {
                if (player == null) return float.MaxValue;
                Vector3 offset = player.transform.position - PadPosition;
                offset.y = 0f;
                return offset.sqrMagnitude;
            }
        }

        void OnEnable() => InvestZoneRegistry.Register(this);
        void OnDisable() { InvestZoneRegistry.Unregister(this); ResetEntry(); }
        void OnApplicationPause(bool value) { paused = value; ResetEntry(); }
        void OnApplicationFocus(bool value) { unfocused = !value; ResetEntry(); }
        void ResetEntry() { selected = false; untilContribution = FacilityUnlockZone.EntryDelay; }

        public void Configure(DiningTable dining, int slot, RestaurantWallet earnings, BurgerInventory carrier,
            Transform point, TextMesh label, Action onChanged)
        {
            table = dining;
            SlotIndex = slot;
            wallet = earnings;
            player = carrier;
            pad = point;
            marker = label;
            changed = onChanged;
            if (isActiveAndEnabled) InvestZoneRegistry.Register(this);
            ResetEntry();
            RefreshMarker();
        }

        public void Restore(int setId, int investment)
        {
            if (!TableSetCatalog.IsConsistent(setId, investment))
                throw new ArgumentOutOfRangeException(nameof(setId));
            Invested = investment;
            ResetEntry();
            if (setId != 0)
            {
                table?.ApplySet((TableSetId)setId);
                IsPurchased = true;
                PendingChoice = false;
                HidePad();
            }
            else if (investment >= Cost)
            {
                IsPurchased = true;
                PendingChoice = true;
                ShowPad();
            }
            else
            {
                IsPurchased = false;
                PendingChoice = false;
                ShowPad();
            }
            RefreshMarker();
        }

        public bool TryChoose(TableSetId id)
        {
            if (!PendingChoice || !TableSetCatalog.IsChoice(id) || table == null) return false;
            table.ApplySet(id);
            GetComponentInParent<UI.SessionGoalTracker>()?.AddUpgradeStars();
            PendingChoice = false;
            HidePad();
            RefreshMarker();
            UI.VisualMeshPulse.Play(table.transform);
            changed?.Invoke();
            return true;
        }

        void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f || HasChosenSet) return;
            if (PendingChoice)
            {
                ResetEntry();
                return;
            }
            if (!IsAvailable || !IsInRange || InvestZoneRegistry.Nearest(this) != this)
            {
                ResetEntry();
                return;
            }
            selected = true;
            untilContribution -= deltaTime;
            while (untilContribution <= 0.00001f && !IsPurchased)
            {
                int amount = (int)Math.Min(Math.Min(FacilityUnlockZone.ContributionSize, Remaining), wallet.Coins);
                if (amount <= 0) { untilContribution = 0f; break; }
                if (!wallet.TrySpend(amount, () =>
                {
                    Invested += amount;
                    if (Remaining == 0) Complete();
                    RefreshMarker();
                })) break;
                if (Application.isPlaying)
                    InvestmentCoin.Launch(player.transform.position + Vector3.up, PadPosition + Vector3.up * 0.2f,
                        transform.parent);
                untilContribution += FacilityUnlockZone.ContributionInterval;
            }
        }

        void Complete()
        {
            if (IsPurchased) return;
            IsPurchased = true;
            PendingChoice = true;
            UI.FeedbackDirector.Current?.Success(PadPosition, "Level Up!", player != null ? player.transform : null);
            RefreshMarker();
            changed?.Invoke();
        }

        void LateUpdate()
        {
            if (marker == null) return;
            marker.gameObject.SetActive(!HasChosenSet);
            if (Camera.main != null) marker.transform.rotation = Camera.main.transform.rotation;
        }

        void RefreshMarker()
        {
            if (marker == null) return;
            marker.text = HasChosenSet ? "" : PendingChoice ? "PICK SET" : $"TABLE\nRemaining {Remaining}";
            marker.gameObject.SetActive(!HasChosenSet);
        }

        public void SetRankVisible(bool visible)
        {
            bool show=visible && !HasChosenSet;
            if(pad!=null)pad.gameObject.SetActive(show);
            if(marker!=null)marker.gameObject.SetActive(show);
        }

        void HidePad()
        {
            if (pad != null) pad.gameObject.SetActive(false);
            if (marker != null) marker.gameObject.SetActive(false);
        }

        void ShowPad()
        {
            if (pad != null) pad.gameObject.SetActive(true);
        }
    }
}
