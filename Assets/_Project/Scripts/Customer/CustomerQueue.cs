using System;
using System.Collections.Generic;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.Customer
{
    public sealed class CustomerQueue : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float spawnInterval = 4f;
        [SerializeField, Min(0.1f)] float walkSpeed = 1.92f;
        [SerializeField, Min(0.1f)] float minimumGap = 1.2f;
        readonly List<CustomerAgent> customers = new List<CustomerAgent>();
        Vector3[] route;
        float[] cumulativeDistance;
        float[] slotDistance;
        Vector3 counterPosition;
        float spawnCountdown;
        int nextTicket = 1;
        bool shuttingDown;
        bool paused, unfocused;
        readonly SpecialCustomerPolicy specials = new SpecialCustomerPolicy();
        public Func<CustomerKind> CustomerKindFactory { get; set; }
        void OnApplicationPause(bool value) => paused = value;
        void OnApplicationFocus(bool value) => unfocused = !value;
        CustomerAgent departingCustomer;

        public Func<int> OrderQuantityFactory { get; set; } = OrderQuantities.Dining;
        public KitchenProduct Product { get; set; } = KitchenProduct.Burger;

        public int Count => customers.Count;
        public int Capacity => slotDistance?.Length ?? 0;
        public bool IsFull => Count >= Capacity;
        public IReadOnlyList<CustomerAgent> Customers => customers;
        public CustomerAgent FrontCustomer => Count > 0 ? customers[0] : null;
        public CustomerAgent ReadyCustomer => FrontCustomer != null && FrontCustomer.HasReachedSlot && FrontCustomer.HasOrdered && FrontCustomer.CanAcceptOrder ? FrontCustomer : null;
        public float MinimumGap => minimumGap;

        // Slots are supplied front first. Everyone follows one path from the entrance
        // through the tail toward the counter, preserving arrival order around bends.
        public void Configure(Vector3 entrance, Vector3 queueEntry, Vector3[] slots, Vector3 counter,
            float interval = 4f, float firstArrivalDelay = 1.5f)
        {
            if (slots == null || slots.Length == 0)
                throw new ArgumentException("At least one queue slot is required.", nameof(slots));
            if (Count != 0)
                throw new InvalidOperationException("An occupied queue cannot be reconfigured.");
            for (int i = 1; i < slots.Length; i++)
                if (Vector3.Distance(slots[i - 1], slots[i]) < minimumGap)
                    throw new ArgumentException("Queue slots must be at least MinimumGap apart.", nameof(slots));

            route = new Vector3[slots.Length + 2];
            cumulativeDistance = new float[route.Length];
            slotDistance = new float[slots.Length];
            route[0] = entrance;
            route[1] = queueEntry;
            for (int i = 0; i < slots.Length; i++) route[i + 2] = slots[slots.Length - 1 - i];
            for (int i = 1; i < route.Length; i++)
                cumulativeDistance[i] = cumulativeDistance[i - 1] + Vector3.Distance(route[i - 1], route[i]);
            for (int i = 0; i < slots.Length; i++) slotDistance[i] = cumulativeDistance[route.Length - 1 - i];
            counterPosition = counter;
            spawnInterval = Mathf.Max(0.1f, interval);
            spawnCountdown = Mathf.Max(0f, firstArrivalDelay);
        }

        void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f || route == null || paused || unfocused)
                return;

            // The leader advances first. Clamp each follower to the leader's progress
            // minus a gap, so even a long frame cannot cause overtaking or overlap.
            // Keep the queue still until the served customer has stepped clear.
            // Destroyed/externally removed customers release the gate immediately.
            bool departureClear = departingCustomer == null ||
                Vector3.Distance(departingCustomer.transform.position, route[route.Length - 1]) >= minimumGap;
            if (departureClear) departingCustomer = null;
            for (int i = 0; departureClear && i < customers.Count; i++)
            {
                CustomerAgent customer = customers[i];
                float target = slotDistance[i];
                float limit = i == 0 ? target : Mathf.Min(target, customers[i - 1].DistanceAlongPath - minimumGap);
                float progress = Mathf.Max(customer.DistanceAlongPath,
                    Mathf.Min(customer.DistanceAlongPath + walkSpeed * deltaTime, limit));
                customer.MoveOnPath(PositionAt(progress), progress, progress >= target - 0.001f, counterPosition, deltaTime);
            }

            FrontCustomer?.AdvanceCalling(deltaTime, FindFirstObjectByType<BurgerShop.Player.PlayerMotor>()?.transform);

            if (IsFull)
            {
                spawnCountdown = spawnInterval;
                return;
            }
            spawnCountdown -= deltaTime;
            if (spawnCountdown > 0f)
                return;
            // Reserve the entrance too: an aggressive interval must not stack new spawns.
            if (Count > 0 && customers[Count - 1].DistanceAlongPath < minimumGap)
                return;

            var goals = FindFirstObjectByType<BurgerShop.UI.SessionGoalTracker>();
            var wallet = FindFirstObjectByType<BurgerShop.Economy.RestaurantWallet>();
            bool occupied = customers.Exists(c => c.Kind != CustomerKind.Normal);
            CustomerKind kind = CustomerKindFactory != null ? CustomerKindFactory() : specials.Next(
                Product == KitchenProduct.Burger && goals != null && goals.Rank >= 2 && wallet != null && wallet.CompletedSales >= 5, occupied, UnityEngine.Random.value);
            CustomerAgent arriving = CustomerAgent.Create(transform, nextTicket++, route[0], OrderQuantityFactory(), kind, Product);
            arriving.AssignSlot(Count);
            arriving.Removed += OnCustomerRemoved;
            customers.Add(arriving);
            spawnCountdown = spawnInterval;
        }

        // Only the waiting front customer can be served. The caller owns departure.
        public bool TryDequeueReadyCustomer(out CustomerAgent customer)
        {
            customer = ReadyCustomer;
            if (customer == null)
                return false;
            customer.Removed -= OnCustomerRemoved;
            customers.RemoveAt(0);
            customer.LeaveQueue();
            departingCustomer = customer;
            ReassignSlots();
            return true;
        }

        void OnCustomerRemoved(CustomerAgent customer)
        {
            if (shuttingDown || this == null) return;
            if (customers.Remove(customer)) ReassignSlots();
        }

        void ReassignSlots()
        {
            for (int i = 0; i < customers.Count; i++)
                if (customers[i] != null) customers[i].AssignSlot(i);
            spawnCountdown = spawnInterval;
        }

        Vector3 PositionAt(float distance)
        {
            for (int i = 1; i < route.Length; i++)
            {
                if (distance > cumulativeDistance[i]) continue;
                float length = cumulativeDistance[i] - cumulativeDistance[i - 1];
                float t = length > 0f ? (distance - cumulativeDistance[i - 1]) / length : 1f;
                return Vector3.Lerp(route[i - 1], route[i], t);
            }
            return route[route.Length - 1];
        }

        void OnDestroy()
        {
            shuttingDown = true;
            foreach (CustomerAgent customer in customers)
                if (customer != null) customer.Removed -= OnCustomerRemoved;
        }
    }
}
