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
        Vector3 counterPosition,entranceWorld;
        Vector3[] localSlots;
        int layoutRevision=-1;
        public Vector3[] QueuePositions=>localSlots==null?System.Array.Empty<Vector3>():System.Array.ConvertAll(localSlots,transform.TransformPoint);
        float spawnCountdown;
        readonly Dictionary<CustomerAgent,CustomerWalkPath> personalApproaches=new Dictionary<CustomerAgent,CustomerWalkPath>();
        float approachLength;
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

        // Slots are front first. Shared progress preserves arrival order; personal approach
        // lanes merge at the tail before walking to the exact reserved service slots.
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

            entranceWorld=entrance;
            localSlots=System.Array.ConvertAll(slots,transform.InverseTransformPoint);
            BindRoute(BuildApproach(entrance, slots), slots);
            counterPosition = transform.InverseTransformPoint(counter);
            spawnInterval = Mathf.Max(0.1f, interval);
            spawnCountdown = Mathf.Max(0f, firstArrivalDelay);
        }

        // queueEntry remains a Configure argument for existing callers, but is not a
        // destination: it may be beyond the tail after moving/rotating a counter.
        static List<Vector3> BuildApproach(Vector3 entrance, Vector3[] slots)
        {
            var approach = new List<Vector3>();
            Vector3 indoorFrom = entrance;
            if (entrance == ShopLayout.Entrance)
            {
                approach.Add(Core.RestaurantEntrance.Outside);
                approach.Add(Core.RestaurantEntrance.Corner);
                approach.Add(Core.RestaurantEntrance.Door);
                // Clear the swinging leaves, then turn toward the current queue.
                // Old saves' Entrance can be nine metres inside: visiting it first
                // makes customers overshoot a nearby counter and retrace the door.
                indoorFrom = Core.RestaurantEntrance.Door + Vector3.forward * 1.4f;
            }
            approach.Add(indoorFrom);
            Vector3 tail = slots[slots.Length - 1];
            var layout = Building.FacilityLayout.Current;
            // Keep the live obstacle router's geometry. ConcatWalk/CardinalizeFrom
            // insert elbows based on the original counter, even after it is moved.
            var indoor = layout != null ? layout.Route(indoorFrom, tail) : null;
            if (indoor != null && indoor.Length > 0)
                indoor = RemoveGridDetours(indoorFrom, indoor, layout.Floors());
            else indoor = ShopLayout.Walk(indoorFrom, tail);
            foreach (var point in indoor)
                if (Vector3.Distance(approach[approach.Count-1], point) > .01f) approach.Add(point);
            // Slot distances rely on the final N points being the exact queue slots.
            if (Vector3.Distance(approach[approach.Count-1], tail) > .01f) approach.Add(tail);
            else approach[approach.Count-1] = tail;
            for (int i = slots.Length - 2; i >= 0; i--) approach.Add(slots[i]);
            return approach;
        }

        // Grid cell centres can start behind the actual doorway clearance point.
        // Skip only visible waypoints whose entire shortcut remains on owned floor.
        static Vector3[] RemoveGridDetours(Vector3 from, Vector3[] path, List<Rect> floors)
        {
            var result = new List<Vector3>();
            for (int i = 0; i < path.Length;)
            {
                int next = i;
                for (int j = path.Length - 1; j > i; j--)
                {
                    bool onFloor = true;
                    int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, path[j]) / .2f));
                    for (int k = 0; k <= steps && onFloor; k++)
                    {
                        Vector3 p = Vector3.Lerp(from, path[j], k / (float)steps);
                        onFloor = floors.Exists(f => f.Contains(new Vector2(p.x, p.z)));
                    }
                    if (onFloor && CustomerWalkPath.Clear(from, path[j])) { next = j; break; }
                }
                from = path[next]; result.Add(from); i = next + 1;
            }
            return result.ToArray();
        }

        void BindRoute(List<Vector3> approach, Vector3[] slots)
        {
            route = approach.ToArray();
            for (int i = 0; i < route.Length; i++) route[i] = transform.InverseTransformPoint(route[i]);
            cumulativeDistance = new float[route.Length];
            slotDistance = new float[slots.Length];
            for (int i = 1; i < route.Length; i++)
                cumulativeDistance[i] = cumulativeDistance[i - 1] + Vector3.Distance(route[i - 1], route[i]);
            for (int i = 0; i < slots.Length; i++) slotDistance[i] = cumulativeDistance[route.Length - 1 - i];
            approachLength=slotDistance[slots.Length-1];personalApproaches.Clear();
        }

        void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f || route == null || paused || unfocused)
                return;

            if(Building.FacilityLayout.Current!=null&&layoutRevision!=Building.FacilityLayout.Current.Revision)
            {layoutRevision=Building.FacilityLayout.Current.Revision;RelayoutApproach();}
            // The leader advances first. Clamp each follower to the leader's progress
            // minus a gap, so even a long frame cannot cause overtaking or overlap.
            // Keep the queue still until the served customer has stepped clear.
            // Destroyed/externally removed customers release the gate immediately.
            bool departureClear = departingCustomer == null ||
                Vector3.Distance(departingCustomer.transform.position, transform.TransformPoint(route[route.Length - 1])) >= minimumGap;
            if (departureClear) departingCustomer = null;
            for (int i = 0; departureClear && i < customers.Count; i++)
            {
                CustomerAgent customer = customers[i];
                float target = slotDistance[i];
                float limit = i == 0 ? target : Mathf.Min(target, customers[i - 1].DistanceAlongPath - minimumGap);
                float progress = Mathf.Max(customer.DistanceAlongPath,
                    Mathf.Min(customer.DistanceAlongPath + walkSpeed * CustomerWalkPath.Pace(customer.TicketNumber) * deltaTime, limit));
                Vector3 next=PersonalPosition(customer,progress);
                if(i>0&&Vector3.Distance(next,customers[i-1].transform.position)<.85f){progress=customer.DistanceAlongPath;next=customer.transform.position;}
                customer.MoveOnPath(next, progress, progress >= target - 0.001f, transform.TransformPoint(counterPosition), deltaTime);
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
            bool first = Product==KitchenProduct.Burger && goals!=null && goals.Rank==1
                && !goals.FirstOrderComplete && nextTicket==1 && (wallet?.CompletedSales??0)==0;
            CustomerAgent arriving = CustomerAgent.Create(transform, nextTicket++, transform.TransformPoint(route[0]), first?1:OrderQuantityFactory(), first?CustomerKind.Normal:kind, Product);
            arriving.AssignSlot(Count);
            arriving.Removed += OnCustomerRemoved;
            customers.Add(arriving);
            spawnCountdown = spawnInterval;
        }

        void RelayoutApproach()
        {
            if(localSlots==null)return;
            // Repeated counters share the public entrance after their world pose is committed.
            if(GameObject.Find("RestaurantEntrance")!=null)entranceWorld=ShopLayout.Entrance;
            var slots=QueuePositions;
            var points = BuildApproach(entranceWorld, slots);
            if(points.Count==0)return;
            float[] progress=new float[customers.Count];
            for(int i=0;i<progress.Length;i++)progress[i]=slotDistance[i]>0?customers[i].DistanceAlongPath/slotDistance[i]:1;
            BindRoute(points, slots);
            for(int i=0;i<customers.Count;i++)
            {
                float distance=Mathf.Clamp01(progress[i])*slotDistance[i];
                if(i>0)distance=Mathf.Min(distance,customers[i-1].DistanceAlongPath-minimumGap);
                distance=Mathf.Max(0,distance);
                customers[i].MoveOnPath(PersonalPosition(customers[i],distance),distance,distance>=slotDistance[i]-.001f,transform.TransformPoint(counterPosition),0);
            }
        }
        // Only the waiting front customer can be served. The caller owns departure.
        public bool TryDequeueReadyCustomer(out CustomerAgent customer)
        {
            customer = ReadyCustomer;
            if (customer == null)
                return false;
            return ReleaseServed(customer);
        }

        public bool ReleaseServed(CustomerAgent customer)
        {
            if (customer == null) return false;
            int index = customers.IndexOf(customer);
            if (index < 0)
            {
                departingCustomer = customer;
                return false;
            }
            customer.Removed -= OnCustomerRemoved;
            personalApproaches.Remove(customer);
            customers.RemoveAt(index);
            customer.LeaveQueue();
            departingCustomer = customer;
            ReassignSlots();
            return true;
        }

        void OnCustomerRemoved(CustomerAgent customer)
        {
            if (shuttingDown || this == null) return;
            personalApproaches.Remove(customer);
            if (customers.Remove(customer)) ReassignSlots();
        }

        void ReassignSlots()
        {
            for (int i = 0; i < customers.Count; i++)
                if (customers[i] != null) customers[i].AssignSlot(i);
            spawnCountdown = spawnInterval;
        }

        Vector3 PersonalPosition(CustomerAgent customer,float distance)
        {
            if(distance>=approachLength||approachLength<=0)return PositionAt(distance);
            if(!personalApproaches.TryGetValue(customer,out var path))
            {
                var points=new List<Vector3>();
                for(int i=0;i<=route.Length-localSlots.Length;i++)points.Add(transform.TransformPoint(route[i]));
                path=new CustomerWalkPath(points,customer.TicketNumber);personalApproaches.Add(customer,path);
            }
            return path.At(distance/approachLength*path.Length);
        }

        Vector3 PositionAt(float distance)
        {
            for (int i = 1; i < route.Length; i++)
            {
                if (distance > cumulativeDistance[i]) continue;
                float length = cumulativeDistance[i] - cumulativeDistance[i - 1];
                float t = length > 0f ? (distance - cumulativeDistance[i - 1]) / length : 1f;
                return transform.TransformPoint(Vector3.Lerp(route[i - 1], route[i], t));
            }
            return transform.TransformPoint(route[route.Length - 1]);
        }

        void OnDestroy()
        {
            shuttingDown = true;
            foreach (CustomerAgent customer in customers)
                if (customer != null) customer.Removed -= OnCustomerRemoved;
        }
    }
}
