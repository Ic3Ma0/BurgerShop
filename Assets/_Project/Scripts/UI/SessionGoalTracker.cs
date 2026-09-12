using System;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.UI
{
    public sealed class SessionGoalTracker : MonoBehaviour
    {
        BurgerInventory inventory;
        ProductionStation grill;
        CounterStock counter;
        CustomerQueue queue;
        RestaurantWallet wallet;
        BurgerServingZone serving;
        DiningArea dining;
        TrashInventory trash;
        WorkerHiringZone hiring;
        BoostUpgradeZone boost;
        ShopExpansion expansion;
        float celebrateLeft;
        int lastInventory = -1;
        int lastCounter = -1;
        int lastSales = -1;
        int lastBoxed = -1;
        int lastPackage = -1;
        [NonSerialized] int rank = ShopRanks.Min;
        [NonSerialized] int goalIndex;
        [NonSerialized] int goalProgress;
        [NonSerialized] ShopGoal[] rankGoals = Array.Empty<ShopGoal>();

        public event Action ProgressChanged;

        public string Title { get; private set; } = "Pick up a burger";
        public int Progress { get; private set; }
        public int Required { get; private set; } = 1;
        public bool IsCelebrating { get; private set; }
        public bool IsRankingUp { get; private set; }
        public int Rank => rank;
        public int GoalIndex => goalIndex;
        public int GoalProgress => goalProgress;
        public int Stars { get; private set; }
        public int StarCap => ShopRanks.StarCap(Rank);
        public bool IsMaxRank => ShopRanks.IsMax(Rank);
        public string StarLabel => IsMaxRank ? "MAX" : $"Lv.{Rank}  {Stars}/{StarCap}";

        public void Configure(BurgerInventory carrier, ProductionStation station, CounterStock stock,
            CustomerQueue customers, RestaurantWallet earnings, BurgerServingZone cashier,
            DiningArea hall = null, TrashInventory trashBag = null, WorkerHiringZone staff = null,
            BoostUpgradeZone playerBoost = null, ShopExpansion shop = null)
        {
            inventory = carrier;
            grill = station;
            counter = stock;
            queue = customers;
            wallet = earnings;
            serving = cashier;
            dining = hall;
            trash = trashBag;
            hiring = staff;
            boost = playerBoost;
            expansion = shop;
            SnapshotCounts();
            celebrateLeft = 0f;
            IsCelebrating = false;
            IsRankingUp = false;
            if (rank < ShopRanks.Min) rank = ShopRanks.Min;
            ReloadGoals();
            expansion?.ApplyRank(rank);
            PaintCurrent();
        }

        public void Restore(int nextRank, int nextGoalIndex, int nextGoalProgress)
        {
            rank = Mathf.Clamp(nextRank, ShopRanks.Min, ShopRanks.Max);
            ReloadGoals();
            goalIndex = rankGoals.Length == 0 ? 0 : Mathf.Clamp(nextGoalIndex, 0, Mathf.Max(0, rankGoals.Length - 1));
            goalProgress = Mathf.Max(0, nextGoalProgress);
            Stars = IsMaxRank ? StarCap : goalIndex;
            SnapshotCounts();
            celebrateLeft = 0f;
            IsCelebrating = false;
            IsRankingUp = false;
            expansion?.ApplyRank(Rank);
            PaintCurrent();
        }

        void LateUpdate() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (deltaTime < 0f) return;
            if (IsCelebrating || IsRankingUp)
            {
                SnapshotCounts(true);
                celebrateLeft -= deltaTime;
                if (celebrateLeft > 0f) return;
                IsCelebrating = false;
                if (IsRankingUp)
                {
                    IsRankingUp = false;
                    PaintCurrent();
                    return;
                }
                AdvanceAfterGoal();
                return;
            }
            DetectMilestones();
            if (!IsCelebrating && !IsRankingUp)
                PaintCurrent();
        }

        void DetectMilestones()
        {
            if (IsMaxRank || goalIndex < 0 || goalIndex >= rankGoals.Length)
            {
                SnapshotCounts(true);
                return;
            }

            ShopGoal goal = rankGoals[goalIndex];
            int carried = inventory != null ? inventory.Count : 0;
            int boxed = inventory != null ? inventory.BoxedCount : 0;
            int sales = wallet != null ? wallet.CompletedSales : 0;
            if (lastSales < 0) lastSales = sales;
            if (lastInventory < 0) lastInventory = carried;
            if (lastBoxed < 0) lastBoxed = boxed;

            int gained = 0;
            if (goal.Kind == ShopGoalKind.PickupBurger && carried > lastInventory)
                gained = 1;
            else if (goal.Kind == ShopGoalKind.ServeCustomers && sales > lastSales)
                gained = sales - lastSales;
            else if (goal.Kind == ShopGoalKind.BoxBurger && boxed > lastBoxed)
                gained = 1;
            else if (goal.Kind == ShopGoalKind.InstallTable && expansion != null && expansion.HasExtraTable)
                gained = goal.Required;
            else if (goal.Kind == ShopGoalKind.InstallBoxing && expansion != null && expansion.HasBoxing)
                gained = goal.Required;
            else if (goal.Kind == ShopGoalKind.InstallGrill && expansion != null && expansion.HasExtraGrill)
                gained = goal.Required;
            else if (goal.Kind == ShopGoalKind.InstallCounter && expansion != null && expansion.HasExtraCounter)
                gained = goal.Required;
            else if (goal.Kind == ShopGoalKind.InstallDriveThru && expansion != null && expansion.HasDriveThru)
                gained = goal.Required;

            SnapshotCounts(true);
            if (gained <= 0) return;
            goalProgress = Mathf.Min(goal.Required, goalProgress + gained);
            Progress = goalProgress;
            Required = goal.Required;
            Title = goal.Title;
            if (goalProgress >= goal.Required)
                CompleteCurrent();
            else
                ProgressChanged?.Invoke();
        }

        void CompleteCurrent()
        {
            ShopGoal goal = rankGoals[goalIndex];
            Title = goal.Title;
            Progress = Required = goal.Required;
            Stars = Mathf.Min(goalIndex + 1, StarCap);
            IsCelebrating = true;
            celebrateLeft = 0.6f;
            ProgressChanged?.Invoke();
        }

        void AdvanceAfterGoal()
        {
            goalIndex++;
            goalProgress = 0;
            if (goalIndex >= rankGoals.Length)
            {
                if (rank < ShopRanks.Max)
                {
                    rank++;
                    goalIndex = 0;
                    ReloadGoals();
                    Stars = 0;
                    expansion?.ApplyRank(rank);
                    IsRankingUp = true;
                    celebrateLeft = 0.9f;
                    Title = rank >= ShopRanks.Max ? "Shop MAX" : "Shop Rank " + rank;
                    Progress = Required = 1;
                    FeedbackDirector.Current?.Success(
                        inventory != null ? inventory.transform.position : Vector3.zero,
                        "Rank Up!",
                        inventory != null ? inventory.transform : null);
                    ProgressChanged?.Invoke();
                    return;
                }
                goalIndex = 0;
                ReloadGoals();
            }
            Stars = goalIndex;
            PaintCurrent();
            ProgressChanged?.Invoke();
        }

        void PaintCurrent()
        {
            if (TryShowChore())
                return;
            if (!IsMaxRank && goalIndex >= 0 && goalIndex < rankGoals.Length)
            {
                ShopGoal goal = rankGoals[goalIndex];
                Title = goal.Title;
                Required = goal.Required;
                Progress = Mathf.Clamp(goalProgress, 0, Required);
                return;
            }
            ShowLoop();
        }

        bool TryShowChore()
        {
            if (trash != null && trash.Count > 0)
            {
                Show("Take trash to the bin", 0, 1);
                return true;
            }
            if (dining != null && dining.HasTrashOnTables)
            {
                Show("Clear the table", 0, 1);
                return true;
            }
            return false;
        }

        void ShowLoop()
        {
            int carried = inventory != null ? inventory.Count : 0;
            int stock = StockCount();
            bool ready = queue != null && queue.ReadyCustomer != null;
            if (hiring != null && !hiring.IsFull && wallet != null && wallet.Coins >= hiring.HireCost)
            {
                if (!hiring.HasVisitedOffice)
                    Show("Open the HR office", 0, 1);
                else
                    Show($"Hire a worker · {hiring.HireCost}", 0, 1);
                return;
            }
            if (expansion != null)
            {
                string install = expansion.NextInstallHint;
                if (!string.IsNullOrEmpty(install))
                {
                    Show(install, 0, 1);
                    return;
                }
            }
            if (boost != null && !boost.IsMaxLevel && wallet != null && wallet.Coins >= boost.NextCost)
            {
                if (!boost.HasVisitedRoom)
                    Show("Open the boost room", 0, 1);
                else
                    Show("Upgrade carry", 0, 1);
                return;
            }
            if (expansion != null && expansion.HasBoxing)
            {
                if (carried > 0 && inventory != null && inventory.LooseCount > 0)
                {
                    Show("Box the burger", 0, 1);
                    return;
                }
                if (inventory != null && inventory.BoxedCount > 0)
                {
                    Show("Stock the package counter", 0, 1);
                    return;
                }
                if (expansion.Boxing.OutputCount > 0)
                {
                    Show("Collect boxed burgers", 0, 1);
                    return;
                }
                if (expansion.Boxing.InputCount + expansion.Boxing.ProcessingCount > 0)
                {
                    Show("Pack the burgers", 0, 1);
                    return;
                }
            }
            if (expansion != null && expansion.HasDriveThru && PackageCount() > 0
                && expansion.DriveThru != null && expansion.DriveThru.HasWaitingCar)
            {
                Show("Sell a combo", 0, 1);
                return;
            }
            if (carried > 0 && (counter == null || !counter.IsFull))
            {
                Show("Move to burger counter", 0, 1);
                return;
            }
            if (stock > 0 && ready)
            {
                Show("Serve a customer", 0, 1);
                return;
            }
            if (stock > 0)
            {
                Show("Wait at the counter", 0, 1);
                return;
            }
            Show("Pick up a burger", 0, 1);
        }

        void Show(string title, int progress, int required)
        {
            Title = title;
            Progress = progress;
            Required = Mathf.Max(1, required);
        }

        void ReloadGoals() => rankGoals = ShopRanks.Goals(rank);

        int PackageCount() => expansion != null && expansion.Boxing != null ? expansion.Boxing.PackageCount : 0;

        int StockCount()
        {
            if (serving != null) return serving.TotalStock;
            return counter != null ? counter.Count : 0;
        }

        void SnapshotCounts(bool keepLast = false)
        {
            int carried = inventory != null ? inventory.Count : 0;
            int stock = StockCount();
            int sales = wallet != null ? wallet.CompletedSales : 0;
            int boxed = inventory != null ? inventory.BoxedCount : 0;
            int packages = PackageCount();
            if (!keepLast || lastInventory < 0) lastInventory = carried;
            else lastInventory = carried;
            lastCounter = stock;
            lastSales = sales;
            lastBoxed = boxed;
            lastPackage = packages;
        }
    }
}
