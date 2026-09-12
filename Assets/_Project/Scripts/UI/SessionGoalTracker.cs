using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.UI
{
    public sealed class SessionGoalTracker : MonoBehaviour
    {
        public const int MaxStars = 15;
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
        bool pickedUp;
        bool stocked;
        bool sawTable;
        bool sawGrill;
        bool sawCounter;
        bool sawBoxing;
        bool sawDriveThru;
        bool sawFourSeat;
        bool sawSquare;
        bool boxedBurger;
        bool stockedPackage;
        int lastBoxed = -1;
        int lastPackage = -1;

        public string Title { get; private set; } = "Install a table";
        public int Progress { get; private set; } = 1;
        public int Required { get; private set; } = 1;
        public bool IsCelebrating { get; private set; }
        public int Stars { get; private set; } = 1;

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
            lastInventory = inventory != null ? inventory.Count : 0;
            lastCounter = StockCount();
            lastSales = wallet != null ? wallet.CompletedSales : 0;
            sawTable = expansion != null && expansion.HasExtraTable;
            sawGrill = expansion != null && expansion.HasExtraGrill;
            sawCounter = expansion != null && expansion.HasExtraCounter;
            sawBoxing = expansion != null && expansion.HasBoxing;
            sawDriveThru = expansion != null && expansion.HasDriveThru;
            sawFourSeat = expansion != null && expansion.HasFourSeatTable;
            sawSquare = expansion != null && expansion.HasSquareTable;
            lastBoxed = inventory != null ? inventory.BoxedCount : 0;
            lastPackage = PackageCount();
            celebrateLeft = 0f;
            IsCelebrating = false;
            RefreshStars();
            Evaluate();
        }

        void LateUpdate() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (deltaTime < 0f) return;
            DetectMilestones();
            RefreshStars();
            if (IsCelebrating)
            {
                celebrateLeft -= deltaTime;
                if (celebrateLeft > 0f) return;
                IsCelebrating = false;
            }
            Evaluate();
        }

        void DetectMilestones()
        {
            int carried = inventory != null ? inventory.Count : 0;
            int stock = StockCount();
            int sales = wallet != null ? wallet.CompletedSales : 0;
            if (!pickedUp && carried > lastInventory && lastInventory >= 0)
                Complete("Pick up a burger");
            if (!stocked && stock > lastCounter && lastCounter >= 0)
                Complete("Move to burger counter");
            int packagesNow = PackageCount();
            if (sales > lastSales)
                Complete(expansion != null && expansion.HasDriveThru && lastPackage >= 0 && packagesNow < lastPackage
                    ? "Sell a combo" : "Serve a customer");
            if (expansion != null)
            {
                if (expansion.HasExtraTable && !sawTable) Complete("Install a table");
                if (expansion.HasFourSeatTable && !sawFourSeat) Complete("Install a 4-seat table");
                if (expansion.HasSquareTable && !sawSquare) Complete("Install a square table");
                if (expansion.HasExtraGrill && !sawGrill) Complete("Install a grill");
                if (expansion.HasExtraCounter && !sawCounter) Complete("Install a counter");
                if (expansion.HasBoxing && !sawBoxing) Complete("Install a boxing table");
                if (expansion.HasDriveThru && !sawDriveThru) Complete("Install a drive-thru");
                sawTable = expansion.HasExtraTable;
                sawFourSeat = expansion.HasFourSeatTable;
                sawSquare = expansion.HasSquareTable;
                sawGrill = expansion.HasExtraGrill;
                sawCounter = expansion.HasExtraCounter;
                sawBoxing = expansion.HasBoxing;
                sawDriveThru = expansion.HasDriveThru;
            }
            int boxed = inventory != null ? inventory.BoxedCount : 0;
            int packages = PackageCount();
            if (!boxedBurger && boxed > lastBoxed && lastBoxed >= 0)
                Complete("Box the burger");
            if (!stockedPackage && packages > lastPackage && lastPackage >= 0)
                Complete("Stock the package counter");
            if (carried > 0) pickedUp = true;
            if (stock > 0) stocked = true;
            if (boxed > 0) boxedBurger = true;
            if (packages > 0) stockedPackage = true;
            lastInventory = carried;
            lastCounter = stock;
            lastBoxed = boxed;
            lastPackage = packages;
            lastSales = sales;
        }

        int PackageCount() => expansion != null && expansion.Boxing != null ? expansion.Boxing.PackageCount : 0;

        int StockCount()
        {
            if (serving != null) return serving.TotalStock;
            return counter != null ? counter.Count : 0;
        }

        void Complete(string title)
        {
            Title = title;
            Progress = Required = 1;
            IsCelebrating = true;
            celebrateLeft = .6f;
            if (title == "Pick up a burger") pickedUp = true;
            if (title == "Move to burger counter") stocked = true;
            if (title == "Box the burger") boxedBurger = true;
            if (title == "Stock the package counter") stockedPackage = true;
        }

        void Evaluate()
        {
            int carried = inventory != null ? inventory.Count : 0;
            int stock = StockCount();
            bool ready = queue != null && queue.ReadyCustomer != null;

            if (trash != null && trash.Count > 0)
            {
                Show("Take trash to the bin", 0, 1);
                return;
            }
            if (dining != null && dining.HasTrashOnTables)
            {
                Show("Clear the table", 0, 1);
                return;
            }
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

        void RefreshStars()
        {
            int sales = wallet != null ? wallet.CompletedSales : 0;
            int extra = (pickedUp ? 1 : 0) + (stocked ? 1 : 0) + sales;
            Stars = Mathf.Clamp(1 + extra, 1, MaxStars);
        }
    }
}
