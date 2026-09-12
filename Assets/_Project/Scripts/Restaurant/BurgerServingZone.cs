using System;
using System.Collections.Generic;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class BurgerServingZone : MonoBehaviour
    {
        CustomerQueue queue;
        BurgerInventory inventory;
        RestaurantWallet wallet;
        Transform servingPoint;
        Vector3[] exitRoute;
        CounterStock stock;
        DiningArea dining;
        CounterDropZone drop;
        CashFloor cash;
        readonly List<CounterStock> stocks = new List<CounterStock>(2);
        readonly List<CounterDropZone> drops = new List<CounterDropZone>(2);
        readonly List<Transform> servePoints = new List<Transform>(2);
        float cooldown;
        CustomerAgent ownedCustomer;
        int ownedCounter = -1;
        Transform flyingBurger;
        Vector3 flightOrigin;
        float flightAge;
        BurgerInventory flightServer;
        bool paused;
        public const float HandoffDuration = 0.45f;
        public const float BetweenItems = 0.15f;
        public bool IsHandoffActive => ownedCustomer != null && ownedCustomer.Order.InFlight;
        public int OwningCounter => ownedCustomer != null ? ownedCounter : -1;
        public CustomerAgent ActiveCustomer => ownedCustomer;
        public bool HasReadyCustomer => queue != null && queue.ReadyCustomer != null;
        public int DeliveredUnits { get; private set; }
        public int CompletedOrders { get; private set; }
        public int ServiceableStock => OwningCounter >= 0 ? StockAt(OwningCounter).Count : TotalStock;
        public int ActiveOrderStockDeficit => ownedCustomer != null
            ? Mathf.Max(0, ownedCustomer.RemainingQuantity - (IsHandoffActive ? 1 : 0) - ServiceableStock) : 0;
        [SerializeField, Min(0.1f)] float radius = 0.85f;
        [SerializeField, Min(1)] int price = 10;

        public int Price => price;
        public CounterStock Stock => stock;
        public int TotalStock
        {
            get
            {
                int total = 0;
                for (int i = 0; i < stocks.Count; i++)
                    if (stocks[i] != null && stocks[i].isActiveAndEnabled) total += stocks[i].Count;
                return total;
            }
        }
        public DiningTable Table => dining != null && dining.TableCount > 0 ? dining.Tables[0] : null;
        public DiningArea Dining => dining;
        public CounterDropZone DropZone => drop;
        public CashFloor Cash => cash;
        public Vector3 ServingPosition => servingPoint != null ? servingPoint.position : transform.position;
        public bool IsInRange => IsActorInRange(inventory != null ? inventory.transform : null);
        public bool ReadyToSell => isActiveAndEnabled && !paused && !IsHandoffActive && cooldown <= 0f && queue != null && queue.isActiveAndEnabled
            && wallet != null && wallet.isActiveAndEnabled && wallet.CanCompleteSale()
            && ServiceableStock > 0 && queue.ReadyCustomer != null;

        public bool IsActorInRange(Transform actor)
        {
            return IndexInRange(actor) >= 0;
        }

        int IndexInRange(Transform actor)
        {
            if (actor == null) return -1;
            if (servePoints.Count == 0)
                return InRangeOf(actor, servingPoint) ? 0 : -1;
            for (int i = 0; i < servePoints.Count; i++)
                if (InRangeOf(actor, servePoints[i])) return i;
            return -1;
        }

        bool InRangeOf(Transform actor, Transform point)
        {
            if (actor == null || point == null) return false;
            Vector3 offset = actor.position - point.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }

        public void Configure(CustomerQueue customers, BurgerInventory carrier, RestaurantWallet earnings,
            Transform point, Vector3[] departureRoute, CounterStock counter, DiningTable table,
            CounterDropZone dropZone = null, int burgerPrice = 10, CashFloor cashFloor = null)
        {
            Configure(customers, carrier, earnings, point, departureRoute, counter, DiningArea.Wrap(table),
                dropZone, burgerPrice, cashFloor);
        }

        public void Configure(CustomerQueue customers, BurgerInventory carrier, RestaurantWallet earnings,
            Transform point, Vector3[] departureRoute, CounterStock counter, DiningArea hall,
            CounterDropZone dropZone = null, int burgerPrice = 10, CashFloor cashFloor = null)
        {
            if (departureRoute == null || departureRoute.Length == 0)
                throw new ArgumentException("A departure route is required.", nameof(departureRoute));
            queue = customers;
            inventory = carrier;
            wallet = earnings;
            servingPoint = point;
            exitRoute = (Vector3[])departureRoute.Clone();
            stock = counter;
            dining = hall;
            drop = dropZone;
            price = Mathf.Max(1, burgerPrice);
            cooldown = 0f;
            stocks.Clear();
            drops.Clear();
            servePoints.Clear();
            if (counter != null) stocks.Add(counter);
            if (dropZone != null) drops.Add(dropZone);
            if (point != null) servePoints.Add(point);
            BindCash(cashFloor);
        }

        public void RegisterCounter(CounterStock extraStock, CounterDropZone extraDrop, Transform extraCircle)
        {
            if (extraStock != null && !stocks.Contains(extraStock)) stocks.Add(extraStock);
            if (extraDrop != null && !drops.Contains(extraDrop)) drops.Add(extraDrop);
            if (extraCircle != null && !servePoints.Contains(extraCircle)) servePoints.Add(extraCircle);
        }

        public bool TryDepositFrom(BurgerInventory carrier)
        {
            bool placed = false;
            for (int i = 0; i < drops.Count; i++)
                if (drops[i] != null && drops[i].TryDepositFrom(carrier)) placed = true;
            return placed;
        }

        public Vector3 NearestDropPosition(Vector3 from)
        {
            if (OwningCounter >= 0 && OwningCounter < drops.Count && drops[OwningCounter] != null)
                return drops[OwningCounter].DropPosition;
            Vector3 best = drop != null ? drop.DropPosition : ServingPosition;
            float bestSqr = HorizontalSqr(from, best);
            for (int i = 0; i < drops.Count; i++)
            {
                if (drops[i] == null) continue;
                float sqr = HorizontalSqr(from, drops[i].DropPosition);
                if (sqr >= bestSqr) continue;
                bestSqr = sqr;
                best = drops[i].DropPosition;
            }
            return best;
        }

        public Vector3 NearestServePosition(Vector3 from, bool requireStock)
        {
            if (OwningCounter >= 0 && OwningCounter < servePoints.Count) return servePoints[OwningCounter].position;
            Vector3 best = ServingPosition;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < servePoints.Count; i++)
            {
                if (servePoints[i] == null) continue;
                if (requireStock && (i >= stocks.Count || stocks[i] == null || stocks[i].Count <= 0)) continue;
                float sqr = HorizontalSqr(from, servePoints[i].position);
                if (sqr >= bestSqr) continue;
                bestSqr = sqr;
                best = servePoints[i].position;
            }
            if (bestSqr < float.MaxValue) return best;
            return ServingPosition;
        }

        static float HorizontalSqr(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }

        public void BindCash(CashFloor cashFloor)
        {
            cash = cashFloor;
            dining?.BindCash(cashFloor);
        }

        void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f || paused || !isActiveAndEnabled) return;
            cooldown = Mathf.Max(0f, cooldown - deltaTime);
            for (int i = 0; i < drops.Count; i++)
            {
                if (drops[i] == null) continue;
                drops[i].Advance(deltaTime);
                drops[i].TryDepositFrom(inventory);
            }
            AdvanceHandoff(deltaTime);
            TryServeFrom(inventory);
            cash?.Advance(deltaTime);
        }

        // Player and staff use the same counter stock and cooldown. Calling this
        // does not advance time, so extra carriers cannot accelerate the cashier.
        public bool TryServeFrom(BurgerInventory carrier)
        {
            if (!ReadyToSell || carrier == null || !carrier.isActiveAndEnabled || exitRoute == null) return false;
            int index = IndexInRange(carrier.transform);
            if (index < 0 || (OwningCounter >= 0 && index != OwningCounter)) return false;
            CounterStock pile = StockAt(index);
            if (pile == null || !pile.isActiveAndEnabled || pile.Count == 0) return false;
            CustomerAgent customer = queue.ReadyCustomer;
            if (!customer.Order.TryReserve()) return false;
            if (!pile.TryTakeBurger(out flyingBurger)) { customer.Order.CancelReservation(); return false; }
            ownedCustomer = customer;
            ownedCounter = index;
            flightServer = carrier;
            flightAge = 0f;
            flightOrigin = flyingBurger != null ? flyingBurger.position : pile.transform.position;
            if (flyingBurger != null) flyingBurger.SetParent(customer.transform, true);
            return true;
        }

        void AdvanceHandoff(float seconds)
        {
            if (!IsHandoffActive) return;
            flightAge += seconds;
            float t = Mathf.Clamp01(flightAge / HandoffDuration);
            if (flyingBurger != null)
            {
                Vector3 destination = ownedCustomer.transform.TransformPoint(new Vector3(0, 0.85f, 0.6f));
                flyingBurger.position = Vector3.Lerp(flightOrigin, destination, t * t * (3f - 2f * t))
                    + Vector3.up * (0.55f * Mathf.Sin(t * Mathf.PI));
            }
            if (t < 1f) return;
            CustomerAgent customer = ownedCustomer;
            customer.ReceiveItem(flyingBurger);
            DeliveredUnits++;
            cooldown = Mathf.Max(.05f, BetweenItems - .05f*(StockAt(ownedCounter).ServiceLevel-1));
            if (customer.Order.IsComplete && customer.Order.TrySettle())
            {
                int amount = customer.OrderSize * price;
                queue.TryDequeueReadyCustomer(out _);
                customer.BeginDeparture(flyingBurger, exitRoute, amount, dining, true);
                wallet.RecordCompletedSale();
                CompletedOrders++;
                UI.FeedbackDirector.Current?.World(customer.transform.position,"",.45f,flightServer != null ? flightServer.transform : null);
                Transform used = ownedCounter < servePoints.Count ? servePoints[ownedCounter] : servingPoint;
                if (used != null) cash?.DropAt(CashFloor.CounterDropPosition(used.position), amount);
                else cash?.DropAtCounter(amount);
                flightServer?.GetComponent<RestaurantWorker>()?.RecordCompletedOrder();
                ownedCustomer = null;
                ownedCounter = -1;
            }
            flyingBurger = null;
            flightServer = null;
        }

        void OnApplicationPause(bool value) => paused = value;

        CounterStock StockAt(int index)
        {
            if (index >= 0 && index < stocks.Count && stocks[index] != null) return stocks[index];
            return stock;
        }
    }
}
