using System;
using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.Rendering;

namespace BurgerShop.Customer
{
    [ExecuteAlways]
    public sealed class CustomerAgent : MonoBehaviour
    {
        Transform orderBubble;
        Material[] ownedMaterials;
        Transform carriedBurger;
        readonly System.Collections.Generic.List<Transform> received = new System.Collections.Generic.List<Transform>();
        Vector3 burgerStart;
        Vector3[] exitRoute;
        int exitWaypoint;
        float departureTime;
        DiningArea hall;
        DiningTable table;
        Vector3 seatPosition;
        int seatIndex = -1;
        float eatTime;
        Phase phase;
        const float HandoffSeconds = 0.4f;
        const float DepartureSpeed = 2.4f;

        enum Phase { None, Handoff, ToSeat, Eating, Exiting }

        public int TicketNumber { get; private set; }
        public int QueueIndex { get; private set; }
        public CustomerOrder Order { get; private set; } = new CustomerOrder(1);
        public int OrderSize => Order.Quantity;
        public int RemainingQuantity => Order.Remaining;
        public float DistanceAlongPath { get; private set; }
        public bool HasReachedSlot { get; private set; }
        public bool HasOrdered { get; private set; }
        public bool IsDeparting { get; private set; }
        public bool IsDining => phase == Phase.ToSeat || phase == Phase.Eating;
        public bool IsEating => phase == Phase.Eating;
        public bool DepartureComplete { get; private set; }
        public int PaidAmount { get; private set; }
        public event Action<CustomerAgent> Removed;

        internal static CustomerAgent Create(Transform parent, int ticket, Vector3 entrance, int quantity = 1)
        {
            GameObject root = new GameObject($"Customer_{ticket}");
            root.transform.SetParent(parent, false);
            root.transform.position = entrance;
            CustomerAgent agent = root.AddComponent<CustomerAgent>();
            agent.TicketNumber = ticket;
            agent.Order = new CustomerOrder(quantity);
            Color[] shirts = { new Color(0.23f, 0.52f, 0.91f), new Color(0.70f, 0.35f, 0.69f), new Color(0.28f, 0.70f, 0.69f) };
            Material shirt = MaterialFor(shirts[(ticket - 1) % shirts.Length]);
            Material skin = MaterialFor(new Color(0.93f, 0.71f, 0.49f));
            Material hair = MaterialFor(new Color(0.19f, 0.14f, 0.12f));
            Material bubble = MaterialFor(new Color(1f, 0.98f, 0.90f), true);
            agent.ownedMaterials = new[] { shirt, skin, hair, bubble };

            Part(root.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.70f, 0f), new Vector3(0.65f, 0.65f, 0.65f), shirt);
            Part(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.48f, 0f), Vector3.one * 0.55f, skin);
            Part(root.transform, "Hair", PrimitiveType.Cube, new Vector3(0f, 1.69f, -0.03f), new Vector3(0.55f, 0.14f, 0.48f), hair);
            Part(root.transform, "Nose", PrimitiveType.Sphere, new Vector3(0f, 1.47f, 0.28f), new Vector3(0.14f, 0.12f, 0.15f), skin);

            agent.orderBubble = new GameObject("OrderBubble").transform;
            agent.orderBubble.SetParent(root.transform, false);
            agent.orderBubble.localPosition = new Vector3(0f, 2.25f, 0f);
            Part(agent.orderBubble, "Background", PrimitiveType.Cube, Vector3.zero, new Vector3(1.15f, 0.55f, 0.035f), bubble);
            Transform icon = BurgerVisualFactory.Create(agent.orderBubble, 0);
            icon.name = "BurgerIcon";
            icon.localPosition = new Vector3(-0.28f, -0.12f, -0.18f);
            icon.localScale = Vector3.one * 0.65f;
            TextMesh label = new GameObject("OrderQuantity").AddComponent<TextMesh>();
            label.transform.SetParent(agent.orderBubble, false);
            label.transform.localPosition = new Vector3(0.2f, 0f, -0.06f);
            label.anchor = TextAnchor.MiddleCenter;
            label.characterSize = 0.12f;
            label.fontSize = 48;
            label.color = new Color(0.22f, 0.15f, 0.08f);
            label.text = "x" + quantity;
            foreach (Renderer renderer in agent.orderBubble.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            agent.orderBubble.gameObject.SetActive(false);
            return agent;
        }

        internal void AssignSlot(int index)
        {
            QueueIndex = index;
            HasReachedSlot = false;
        }

        internal void MoveOnPath(Vector3 position, float distance, bool arrived, Vector3 counter, float deltaTime)
        {
            Vector3 direction = arrived ? counter - position : position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.00001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 1f - Mathf.Exp(-12f * deltaTime));
            transform.position = position;
            DistanceAlongPath = distance;
            HasReachedSlot = arrived;
            if (arrived && !HasOrdered)
            {
                HasOrdered = true;
                orderBubble.gameObject.SetActive(true);
            }
        }

        internal void LeaveQueue()
        {
            QueueIndex = -1;
            HasReachedSlot = false;
            orderBubble.gameObject.SetActive(false);
        }

        internal void ReceiveItem(Transform burger)
        {
            if (!Order.Receive()) return;
            if (burger != null)
            {
                burger.SetParent(transform, true);
                burger.localPosition = new Vector3(0, 0.85f + received.Count * 0.15f, 0.6f);
                burger.localRotation = Quaternion.identity;
                received.Add(burger);
            }
            orderBubble.GetComponentInChildren<TextMesh>(true).text = "x" + Order.Remaining;
        }

        internal void BeginDeparture(Transform burger, Vector3[] waypoints, int payment, DiningArea dining = null,
            bool alreadyReceived = false)
        {
            IsDeparting = true;
            departureTime = alreadyReceived ? HandoffSeconds : 0f;
            if (burger != null && !received.Contains(burger)) received.Add(burger);
            PaidAmount = payment;
            exitRoute = waypoints != null ? (Vector3[])waypoints.Clone() : Array.Empty<Vector3>();
            carriedBurger = burger;
            hall = dining;
            table = null;
            seatIndex = -1;
            eatTime = 0f;
            if (burger != null)
            {
                burger.SetParent(transform, true);
                burgerStart = burger.localPosition;
            }
            orderBubble.Find("Background").gameObject.SetActive(false);
            orderBubble.Find("BurgerIcon").gameObject.SetActive(false);
            TextMesh receipt = orderBubble.GetComponentInChildren<TextMesh>(true);
            receipt.transform.localPosition = Vector3.zero;
            receipt.color = new Color(1f, 0.78f, 0.12f);
            receipt.text = "";
            orderBubble.gameObject.SetActive(true);
            phase = Phase.Handoff;
            if (hall != null)
                hall.TryAssignSeat(this, out table, out seatPosition, out seatIndex);
        }

        void Update()
        {
            if (Application.isPlaying) AdvanceDeparture(Time.deltaTime);
        }

        public void AdvanceDeparture(float deltaTime)
        {
            if (!IsDeparting || DepartureComplete || deltaTime <= 0f) return;
            float previousTime = departureTime;
            departureTime += deltaTime;
            if (carriedBurger != null && phase != Phase.Eating && phase != Phase.Exiting)
            {
                float t = Mathf.Clamp01(departureTime / HandoffSeconds);
                carriedBurger.localPosition = Vector3.Lerp(burgerStart, new Vector3(0f, 0.85f, 0.6f), t)
                    + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.6f);
                carriedBurger.localRotation = Quaternion.Slerp(carriedBurger.localRotation, Quaternion.identity, t);
            }
            if (departureTime > 1.4f) orderBubble.gameObject.SetActive(false);

            if (phase == Phase.Handoff)
            {
                if (departureTime < HandoffSeconds) return;
                phase = hall != null || table != null ? Phase.ToSeat : Phase.Exiting;
            }

            float remaining = DepartureSpeed * (phase == Phase.Exiting && previousTime < HandoffSeconds
                ? Mathf.Max(0f, departureTime - HandoffSeconds) - Mathf.Max(0f, previousTime - HandoffSeconds)
                : deltaTime);

            if (phase == Phase.ToSeat)
            {
                if (seatIndex < 0)
                {
                    if (hall != null)
                        hall.TryAssignSeat(this, out table, out seatPosition, out seatIndex);
                    else if (table != null && !table.IsDirty)
                        table.TryAssignSeat(this, out seatPosition, out seatIndex);
                }
                Vector3 wait = table != null ? table.WaitPosition : (hall != null ? hall.WaitPosition : transform.position);
                Vector3 target = seatIndex >= 0 ? seatPosition : wait;
                if (!StepToward(target, ref remaining)) return;
                if (seatIndex < 0)
                {
                    FaceTable();
                    return;
                }
                FaceTable();
                PlaceBurgerOnTable();
                phase = Phase.Eating;
                eatTime = 0f;
            }

            if (phase == Phase.Eating)
            {
                eatTime += deltaTime;
                float need = table != null ? table.EatSeconds : 3f;
                if (eatTime < need) return;
                int finishedSeat = seatIndex;
                DiningTable finishedTable = table;
                if (finishedTable != null && finishedSeat >= 0)
                {
                    finishedTable.LeaveMealTrash(finishedSeat);
                    finishedTable.LeaveMealCash();
                }
                finishedTable?.Release(this);
                seatIndex = -1;
                ReleaseMeal();
                phase = Phase.Exiting;
            }

            while (remaining > 0f && exitWaypoint < exitRoute.Length)
            {
                Vector3 offset = exitRoute[exitWaypoint] - transform.position;
                float distance = offset.magnitude;
                if (distance > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(offset);
                if (remaining < distance)
                {
                    transform.position += offset.normalized * remaining;
                    break;
                }
                transform.position = exitRoute[exitWaypoint++];
                remaining -= distance;
            }
            if (exitWaypoint == exitRoute.Length)
            {
                DepartureComplete = true;
                gameObject.SetActive(false);
                BurgerVisual.Release(gameObject);
            }
        }

        bool StepToward(Vector3 target, ref float travel)
        {
            Vector3 offset = target - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance <= 0.04f)
            {
                transform.position = new Vector3(target.x, transform.position.y, target.z);
                return true;
            }
            if (distance > 0.0001f)
                transform.rotation = Quaternion.LookRotation(offset);
            if (travel < distance)
            {
                transform.position += offset.normalized * travel;
                travel = 0f;
                return false;
            }
            transform.position = new Vector3(target.x, transform.position.y, target.z);
            travel -= distance;
            return true;
        }

        void FaceTable()
        {
            if (table == null) return;
            Vector3 look = table.Center - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look);
        }

        void PlaceBurgerOnTable()
        {
            if (carriedBurger == null || table == null) return;
            carriedBurger.SetParent(transform, true);
            Vector3 toward = table.Center - transform.position;
            toward.y = 0f;
            Vector3 place = table.Center + Vector3.up * 0.86f;
            if (toward.sqrMagnitude > 0.0001f) place += toward.normalized * -0.22f;
            carriedBurger.position = place;
            carriedBurger.rotation = Quaternion.identity;
            int index = 0;
            foreach (Transform food in received)
            {
                if (food == null || food == carriedBurger) continue;
                food.SetParent(transform, true);
                food.position = place + new Vector3(0.24f * (++index), 0, 0);
                food.rotation = Quaternion.identity;
            }
        }

        void ReleaseMeal()
        {
            foreach (Transform food in received)
                if (food != null) BurgerVisual.Release(food.gameObject);
            received.Clear();
            carriedBurger = null;
        }

        void LateUpdate()
        {
            if (orderBubble != null && Camera.main != null)
                orderBubble.rotation = Camera.main.transform.rotation;
        }

        void OnDestroy()
        {
            // Meal visuals remain owned children; Unity tears them down with the customer.
            table?.Release(this);
            Removed?.Invoke(this);
            if (ownedMaterials != null)
                foreach (Material material in ownedMaterials)
                    if (material != null) BurgerVisual.Release(material);
        }

        static Material MaterialFor(Color color, bool unlit = false) => BurgerShop.Core.RuntimeMaterials.Create(color, unlit);

        static void Part(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            collider.enabled = false;
            BurgerVisual.Release(collider);
        }
    }
}
