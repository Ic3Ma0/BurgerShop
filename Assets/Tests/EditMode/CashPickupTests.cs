using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class CashPickupTests
    {
        GameObject root;
        CustomerQueue queue;
        BurgerInventory inventory;
        ProductionStation grill;
        BurgerServingZone serving;
        CounterStock stock;
        CounterDropZone drop;
        DiningTable table;
        RestaurantWallet wallet;
        CashFloor cash;
        GrillUpgradeZone upgrade;
        WorkerHiringZone hiring;
        SaleFeedback sfx;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("CashPickupTest");
            queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(new Vector3(-8f, 0f, -4f), new Vector3(-2f, 0f, -4f),
                new[] { new Vector3(-2f, 0f, 1.7f), new Vector3(-2f, 0f, -0.1f), new Vector3(-2f, 0f, -1.9f) }, new Vector3(-2f, 0f, 3.3f));
            GameObject player = new GameObject("Player");
            player.transform.SetParent(root.transform);
            inventory = player.AddComponent<BurgerInventory>();
            inventory.Configure();
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            grill = root.AddComponent<ProductionStation>();
            grill.Configure(output, null, null);
            wallet = root.AddComponent<RestaurantWallet>();
            Transform point = new GameObject("ServingPoint").transform;
            point.SetParent(root.transform);
            point.position = new Vector3(0.9f, 0f, 3.3f);
            Transform anchor = new GameObject("StockAnchor").transform;
            anchor.SetParent(root.transform);
            stock = root.AddComponent<CounterStock>();
            stock.Configure(anchor, null);
            drop = root.AddComponent<CounterDropZone>();
            drop.Configure(stock, point);
            table = DiningTable.Create(root.transform, new Vector3(-5.6f, 0f, 0.2f));
            cash = root.AddComponent<CashFloor>();
            serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, inventory, wallet, point,
                new[] { new Vector3(-8.2f, 0f, -4.4f), new Vector3(-10.5f, 0f, -6.2f) }, stock, table, drop, 10, cash);
            cash.Configure(wallet, inventory.transform, CashFloor.CounterDropPosition(serving.ServingPosition));
            Transform upgradePoint = new GameObject("Upgrade").transform;
            upgradePoint.SetParent(root.transform);
            upgradePoint.position = new Vector3(2f, 0f, -2.8f);
            upgrade = root.AddComponent<GrillUpgradeZone>();
            upgrade.Configure(grill, wallet, inventory, upgradePoint, null, null);
            Transform hirePoint = new GameObject("Hire").transform;
            hirePoint.SetParent(root.transform);
            hirePoint.position = new Vector3(0f, 0f, -5.5f);
            hiring = root.AddComponent<WorkerHiringZone>();
            hiring.Configure(grill, serving, wallet, inventory, point, hirePoint, new Vector3(0.9f, 0f, 0.4f), drop);
            sfx = root.AddComponent<SaleFeedback>();
            sfx.Configure(wallet);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        void FillQueue()
        {
            for (int i = 0; i < 1200; i++) queue.Advance(1f / 60f);
        }

        void Load(int count = 1)
        {
            grill.Advance(12f);
            for (int i = 0; i < count; i++) inventory.TryCollectFrom(grill);
        }

        void AtCounter() => inventory.transform.position = serving.ServingPosition + Vector3.up;

        void ServeOne()
        {
            FillQueue();
            Load();
            AtCounter();
            serving.Advance(0.01f);
            serving.Advance(BurgerServingZone.HandoffDuration);
        }

        void CollectCounterCash()
        {
            inventory.transform.position = cash.NearestPilePosition;
            serving.Advance(1f);
        }

        CustomerAgent FinishMeal()
        {
            FillQueue();
            Load();
            AtCounter();
            CustomerAgent customer = queue.ReadyCustomer;
            serving.Advance(0.01f);
            serving.Advance(BurgerServingZone.HandoffDuration);
            customer.AdvanceDeparture(100f);
            return customer;
        }

        [Test]
        public void ServingDropsTenOnTheGroundAndDoesNotAddCoins()
        {
            ServeOne();
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(cash.PileCount, Is.EqualTo(1));
            Assert.That(cash.GroundValue, Is.EqualTo(10));
            Vector3 offset = cash.NearestPilePosition - serving.ServingPosition;
            offset.y = 0f;
            Assert.That(offset.magnitude, Is.GreaterThan(1.1f));
            Assert.That(cash.NearestPilePosition.y, Is.EqualTo(.04f).Within(.001f));
        }

        [Test]
        public void WalkingOntoCounterCashAddsTenAfterTheFlight()
        {
            ServeOne();
            inventory.transform.position = Vector3.zero;
            serving.Advance(1f);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(cash.GroundValue, Is.EqualTo(10));
            Transform bill = cash.transform.Find("Cash_0");
            Vector3 start = bill.position;
            inventory.transform.position = cash.NearestPilePosition;
            serving.Advance(0.05f);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(cash.GroundValue, Is.EqualTo(10));
            Assert.That(Vector3.Distance(bill.position, start), Is.GreaterThan(0.04f));
            serving.Advance(CashPickup.FlyDuration);
            Assert.That(wallet.Coins, Is.EqualTo(10));
            Assert.That(cash.GroundValue, Is.Zero);
            Assert.That(cash.PileCount, Is.Zero);
        }

        [Test]
        public void FinishedMealDropsTableCashWithoutPayingUntilWalkedOver()
        {
            FinishMeal();
            Assert.That(table.TrashCount, Is.EqualTo(DiningTable.TrashPerGuest));
            Assert.That(table.IsDirty, Is.True);
            Assert.That(cash.GroundValue, Is.EqualTo(20));
            Assert.That(cash.PileCount, Is.EqualTo(2));
            Assert.That(wallet.Coins, Is.Zero);
            Vector3 tableCash = CashFloor.TableDropPosition(table);
            Assert.That(Vector3.Distance(Flatten(cash.NearestPilePosition), Flatten(tableCash)), Is.GreaterThan(1f));
            inventory.transform.position = tableCash;
            serving.Advance(1f);
            Assert.That(wallet.Coins, Is.EqualTo(10));
            Assert.That(cash.GroundValue, Is.EqualTo(10));
            Assert.That(table.TrashCount, Is.EqualTo(DiningTable.TrashPerGuest));
        }

        [Test]
        public void LeavingTableCashUntouchedKeepsCoinsUnchanged()
        {
            FinishMeal();
            inventory.transform.position = serving.ServingPosition;
            serving.Advance(1f);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(cash.GroundValue, Is.EqualTo(20));
        }

        [Test]
        public void UpgradeAndHireSpendOnlyCollectedCoins()
        {
            ServeOne();
            inventory.transform.position = upgrade.UpgradePosition + Vector3.up;
            for (int i = 0; i < 90; i++) upgrade.Advance(1f / 60f);
            Assert.That(upgrade.Level, Is.EqualTo(1));
            Assert.That(wallet.Coins, Is.Zero);
            CollectCounterCash();
            Assert.That(wallet.Coins, Is.EqualTo(10));
            wallet.CollectCoins(20);
            inventory.transform.position = upgrade.UpgradePosition + Vector3.up;
            for (int i = 0; i < 90; i++) upgrade.Advance(1f / 60f);
            Assert.That(upgrade.Level, Is.EqualTo(2));
            Assert.That(wallet.Coins, Is.Zero);
            wallet.CollectCoins(50);
            inventory.transform.position = hiring.HiringPosition + Vector3.up;
            for (int i = 0; i < 90; i++) hiring.Advance(1f / 60f);
            Assert.That(hiring.IsHired, Is.True);
            Assert.That(wallet.Coins, Is.Zero);
        }

        [Test]
        public void WorkerServeDropsCashButDoesNotVacuumIt()
        {
            wallet.CollectCoins(50);
            inventory.transform.position = hiring.HiringPosition + Vector3.up;
            for (int i = 0; i < 90; i++) hiring.Advance(1f / 60f);
            RestaurantWorker worker = hiring.Worker;
            Assert.That(worker, Is.Not.Null);
            FillQueue();
            grill.Advance(6f);
            worker.Inventory.TryCollectFrom(grill);
            worker.transform.position = serving.ServingPosition + Vector3.up;
            Assert.That(drop.TryDepositFrom(worker.Inventory), Is.True);
            inventory.transform.position = Vector3.zero;
            Assert.That(serving.TryServeFrom(worker.Inventory), Is.True);
            serving.Advance(BurgerServingZone.HandoffDuration);
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(cash.GroundValue, Is.EqualTo(10));
            worker.transform.position = cash.NearestPilePosition;
            serving.Advance(1f);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(cash.GroundValue, Is.EqualTo(10));
        }

        [Test]
        public void MultiplePilesFlyConcurrentlyAndCreditExactlyOnce()
        {
            cash.DropAtCounter();
            cash.DropAtCounter();
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(cash.PileCount, Is.EqualTo(2));
            Assert.That(cash.GroundValue, Is.EqualTo(20));
            inventory.transform.position = cash.NearestPilePosition;
            serving.Advance(0.08f);
            Assert.That(cash.GetComponentsInChildren<CashPickup>().Length,Is.Zero,"Both piles have launched");
            Assert.That(wallet.Coins,Is.Zero,"Credit occurs on arrival");
            // BS-SPEC-057: packets are staggered and each flight now lasts 0.30 seconds.
            serving.Advance(CashPickup.FlyDuration+CashFloor.PickupInterval);
            Assert.That(wallet.Coins, Is.EqualTo(20));
            Assert.That(cash.GroundValue, Is.Zero);
            Assert.That(cash.PileCount, Is.Zero);
            serving.Advance(.5f);Assert.That(wallet.Coins,Is.EqualTo(20));
        }

        [Test]
        public void CollectingCashPlaysCoinSoundOnceAndServingDoesNot()
        {
            ServeOne();
            Assert.That(sfx.CoinPlayCount, Is.Zero);
            Assert.That(sfx.SpendPlayCount, Is.Zero);
            AudioSource source = sfx.GetComponent<AudioSource>();
            Assert.That(source.clip, Is.Not.Null);
            Assert.That(source.clip.samples, Is.GreaterThan(0));
            Assert.That(source.clip.length, Is.GreaterThan(0.079f).And.LessThan(0.151f));
            CollectCounterCash();
            Assert.That(wallet.Coins, Is.EqualTo(10));
            Assert.That(sfx.CoinPlayCount, Is.EqualTo(1));
            Assert.That(sfx.SpendPlayCount, Is.Zero);
        }

        [Test]
        public void SpendingPlaysALowerCoinSoundWithoutASecondGainChime()
        {
            wallet.CollectCoins(30);
            Assert.That(sfx.CoinPlayCount, Is.EqualTo(1));
            inventory.transform.position = upgrade.UpgradePosition + Vector3.up;
            for (int i = 0; i < 90; i++) upgrade.Advance(1f / 60f);
            Assert.That(upgrade.Level, Is.EqualTo(2));
            Assert.That(sfx.SpendPlayCount, Is.EqualTo(1));
            Assert.That(sfx.CoinPlayCount, Is.EqualTo(1));
        }

        [Test] public void CashGrowsVerticallyAndCollectsExactly()
        {
            for(int i=0;i<20;i++)cash.DropAtCounter();
            Assert.That(cash.NearestPilePosition.y,Is.GreaterThan(2));
            Assert.That(cash.GroundValue,Is.EqualTo(200));
            inventory.transform.position=cash.NearestPilePosition;
            long before=wallet.Coins;cash.Advance(3);
            Assert.That(cash.GroundValue,Is.Zero);Assert.That(wallet.Coins-before,Is.EqualTo(200));
        }
        [Test]
        public void BillsAreLargeBrightAndLabeledTen()
        {
            cash.DropAtCounter();
            Transform bill = cash.transform.Find("Cash_0/Visual/Bill_1");
            Assert.That(bill, Is.Not.Null);
            Assert.That(bill.localScale.x, Is.EqualTo(CashPickup.BillWidth));
            Assert.That(bill.localScale.y, Is.EqualTo(CashPickup.BillThickness));
            Assert.That(bill.localScale.z, Is.EqualTo(CashPickup.BillDepth));
            Assert.That(bill.localScale.x, Is.GreaterThanOrEqualTo(0.18f * 2f));
            Assert.That(bill.localScale.z, Is.GreaterThanOrEqualTo(0.11f * 2f));
            Assert.That(bill.localScale.y, Is.GreaterThan(0.012f));
            Color color = bill.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor");
            Assert.That(Vector4.Distance(color,BurgerShop.Core.BanknoteLook.Green),Is.LessThan(.001f));
            Assert.That(color.g, Is.GreaterThan(color.r + 0.2f));
            Color stripe = cash.transform.Find("Cash_0/Visual/Bill_0").GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor");
            Assert.That(Vector4.Distance(stripe,BurgerShop.Core.BanknoteLook.Green),Is.LessThan(.001f));
            Assert.That(cash.transform.Find("Cash_0/Visual/BillFace").GetComponent<Renderer>().sharedMaterial.mainTexture,Is.SameAs(BurgerShop.Core.BanknoteLook.Texture));
            TextMesh label = cash.transform.Find("Cash_0/Visual/Amount").GetComponent<TextMesh>();
            Assert.That(label.text, Is.EqualTo("10"));
            Assert.That(label.characterSize, Is.LessThan(0.2f));
            Assert.That(CashPickup.FaceTilt, Is.Zero);
        }

        [Test]
        public void IdleBillStacksStayFlatAndStill()
        {
            cash.DropAtCounter();
            inventory.transform.position = Vector3.zero;
            Transform visual = cash.transform.Find("Cash_0/Visual");
            float y0 = visual.localPosition.y;
            float yaw0 = visual.localEulerAngles.y;
            serving.Advance(0.35f);
            float y1 = visual.localPosition.y;
            serving.Advance(0.35f);
            Assert.That(Mathf.Abs(y1 - y0) + Mathf.Abs(visual.localPosition.y - y1), Is.LessThan(.001f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(yaw0, visual.localEulerAngles.y)), Is.LessThan(.001f));
            Assert.That(wallet.Coins, Is.Zero);
        }

        [Test]
        public void StandingInTheServingCircleDoesNotCollectCounterCash()
        {
            ServeOne();
            AtCounter();
            serving.Advance(1f);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(cash.GroundValue, Is.EqualTo(10));
            float gap = Flatten(cash.NearestPilePosition - serving.ServingPosition).magnitude;
            Assert.That(CashFloor.PickupRadius, Is.EqualTo(0.85f));
            Assert.That(CashFloor.PickupRadius, Is.LessThan(gap - 0.2f));
        }

        static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
