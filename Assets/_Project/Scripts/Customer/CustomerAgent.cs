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
        Vector3 burgerStart;
        Vector3[] exitRoute;
        int exitWaypoint;
        float departureTime;
        const float HandoffSeconds = 0.4f;
        const float DepartureSpeed = 2.4f;

        public int TicketNumber { get; private set; }
        public int QueueIndex { get; private set; }
        public int OrderSize => 1;
        public float DistanceAlongPath { get; private set; }
        public bool HasReachedSlot { get; private set; }
        public bool HasOrdered { get; private set; }
        public bool IsDeparting { get; private set; }
        public bool DepartureComplete { get; private set; }
        public int PaidAmount { get; private set; }
        public event Action<CustomerAgent> Removed;

        internal static CustomerAgent Create(Transform parent, int ticket, Vector3 entrance)
        {
            GameObject root = new GameObject($"Customer_{ticket}");
            root.transform.SetParent(parent, false);
            root.transform.position = entrance;
            CustomerAgent agent = root.AddComponent<CustomerAgent>();
            agent.TicketNumber = ticket;
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
            label.text = "x1";
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

        internal void BeginDeparture(Transform burger, Vector3[] waypoints, int payment)
        {
            IsDeparting = true;
            PaidAmount = payment;
            exitRoute = (Vector3[])waypoints.Clone();
            carriedBurger = burger;
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
            receipt.text = $"+{payment}";
            orderBubble.gameObject.SetActive(true);
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
            if (carriedBurger != null)
            {
                float t = Mathf.Clamp01(departureTime / HandoffSeconds);
                carriedBurger.localPosition = Vector3.Lerp(burgerStart, new Vector3(0f, 0.85f, 0.6f), t)
                    + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.6f);
                carriedBurger.localRotation = Quaternion.Slerp(carriedBurger.localRotation, Quaternion.identity, t);
            }
            if (departureTime > 1.4f) orderBubble.gameObject.SetActive(false);

            float remaining = Mathf.Max(0f, departureTime - HandoffSeconds) - Mathf.Max(0f, previousTime - HandoffSeconds);
            float travel = remaining * DepartureSpeed;
            while (travel > 0f && exitWaypoint < exitRoute.Length)
            {
                Vector3 offset = exitRoute[exitWaypoint] - transform.position;
                float distance = offset.magnitude;
                if (distance > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(offset);
                if (travel < distance)
                {
                    transform.position += offset.normalized * travel;
                    break;
                }
                transform.position = exitRoute[exitWaypoint++];
                travel -= distance;
            }
            if (exitWaypoint == exitRoute.Length)
            {
                DepartureComplete = true;
                gameObject.SetActive(false);
                BurgerVisual.Release(gameObject);
            }
        }

        void LateUpdate()
        {
            if (orderBubble != null && Camera.main != null)
                orderBubble.rotation = Camera.main.transform.rotation;
        }

        void OnDestroy()
        {
            Removed?.Invoke(this);
            if (ownedMaterials != null)
                foreach (Material material in ownedMaterials)
                    if (material != null) BurgerVisual.Release(material);
        }

        static Material MaterialFor(Color color, bool unlit = false)
        {
            Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")
                ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }

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
