using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public enum WorkerState { ToGrill, Collecting, ToCounter, Serving }

    [ExecuteAlways]
    public sealed class RestaurantWorker : MonoBehaviour
    {
        ProductionStation grill;
        BurgerServingZone serving;
        Transform pickupPoint;
        Vector3 aisleCorner;
        Vector3[] route;
        int waypoint;
        float pickupCooldown;
        Material[] ownedMaterials;
        TextMesh label;
        const float WalkSpeed = 3.8f;

        public BurgerInventory Inventory { get; private set; }
        public WorkerState State { get; private set; }
        public int CompletedDeliveries { get; private set; }
        public string Activity => !isActiveAndEnabled || !DependenciesReady ? "Paused"
            : State == WorkerState.ToGrill ? "Walking to grill"
            : State == WorkerState.Collecting ? "Waiting for burgers"
            : State == WorkerState.ToCounter ? "Carrying to counter" : "Waiting to serve";
        bool DependenciesReady => grill != null && grill.isActiveAndEnabled && serving != null
            && serving.isActiveAndEnabled && Inventory != null && Inventory.isActiveAndEnabled && pickupPoint != null;

        public void Configure(ProductionStation source, BurgerServingZone cashier, Transform pickup,
            Vector3 corner, BurgerInventory inventory)
        {
            grill = source;
            serving = cashier;
            pickupPoint = pickup;
            aisleCorner = corner;
            Inventory = inventory;
            StartTrip(WorkerState.ToGrill);
        }

        void Update()
        {
            if (Application.isPlaying) Advance(Time.deltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f || !isActiveAndEnabled || !DependenciesReady) return;
            if (State == WorkerState.ToGrill || State == WorkerState.ToCounter)
            {
                if (MoveAlongRoute(deltaTime))
                {
                    State = State == WorkerState.ToGrill ? WorkerState.Collecting : WorkerState.Serving;
                    pickupCooldown = 0f;
                }
                return;
            }
            if (State == WorkerState.Collecting)
            {
                pickupCooldown = Mathf.Max(0f, pickupCooldown - deltaTime);
                // Recheck the actual pickup point before transferring any stock.
                Vector3 offset = transform.position - pickupPoint.position;
                offset.y = 0f;
                if (offset.sqrMagnitude > 1f) { StartTrip(WorkerState.ToGrill); return; }
                if (!Inventory.IsFull && pickupCooldown <= 0f && Inventory.TryCollectFrom(grill))
                    pickupCooldown = 0.25f;
                // Do not wait for a full load while a burger could be delivered now.
                if (Inventory.IsFull || (Inventory.Count > 0 && grill.Stock == 0)) StartTrip(WorkerState.ToCounter);
                return;
            }
            if (Inventory.Count == 0) { StartTrip(WorkerState.ToGrill); return; }
            Vector3 servingOffset = transform.position - serving.ServingPosition;
            servingOffset.y = 0f;
            if (servingOffset.sqrMagnitude > 0.7f * 0.7f) { StartTrip(WorkerState.ToCounter); return; }
            if (serving.TryServeFrom(Inventory))
            {
                CompletedDeliveries++;
                if (Inventory.Count == 0) StartTrip(WorkerState.ToGrill);
            }
        }

        void StartTrip(WorkerState state)
        {
            State = state;
            Vector3 destination = state == WorkerState.ToGrill
                ? (pickupPoint != null ? pickupPoint.position : transform.position)
                : (serving != null ? serving.ServingPosition : transform.position);
            route = new[] { AtHeight(aisleCorner), AtHeight(destination) };
            waypoint = 0;
        }

        Vector3 AtHeight(Vector3 point) => new Vector3(point.x, transform.position.y, point.z);

        bool MoveAlongRoute(float deltaTime)
        {
            float remaining = WalkSpeed * deltaTime;
            while (waypoint < route.Length)
            {
                Vector3 offset = route[waypoint] - transform.position;
                float distance = offset.magnitude;
                if (distance > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(offset), 1f - Mathf.Exp(-12f * deltaTime));
                if (remaining < distance)
                {
                    transform.position += offset.normalized * remaining;
                    return false;
                }
                transform.position = route[waypoint++];
                remaining -= distance;
            }
            return true;
        }

        void LateUpdate()
        {
            if (label == null || Inventory == null) return;
            label.text = $"STAFF {Inventory.Count}/{Inventory.Capacity}";
            if (Camera.main != null) label.transform.rotation = Camera.main.transform.rotation;
        }

        void OnDestroy()
        {
            if (ownedMaterials != null)
                foreach (Material material in ownedMaterials)
                    if (material != null) BurgerVisual.Release(material);
        }

        internal static RestaurantWorker Create(Transform parent, Vector3 position, ProductionStation grill,
            BurgerServingZone cashier, Transform pickup, Vector3 aisle)
        {
            GameObject root = new GameObject("RestaurantWorker");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(position.x, 1.05f, position.z);
            RestaurantWorker worker = root.AddComponent<RestaurantWorker>();
            Material uniform = MaterialFor(new Color(0.13f, 0.58f, 0.64f));
            Material white = MaterialFor(new Color(0.98f, 0.96f, 0.89f));
            Material skin = MaterialFor(new Color(0.93f, 0.71f, 0.49f));
            worker.ownedMaterials = new[] { uniform, white, skin };
            Part(root.transform, "Uniform", PrimitiveType.Capsule, new Vector3(0f, -0.3f, 0f), new Vector3(0.65f, 0.7f, 0.65f), uniform);
            Part(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.65f, 0f), Vector3.one * 0.5f, skin);
            Part(root.transform, "Hat", PrimitiveType.Cylinder, new Vector3(0f, 0.94f, 0f), new Vector3(0.62f, 0.1f, 0.62f), white);
            Part(root.transform, "Apron", PrimitiveType.Cube, new Vector3(0f, -0.13f, 0.32f), new Vector3(0.48f, 0.55f, 0.08f), white);
            Part(root.transform, "Nose", PrimitiveType.Sphere, new Vector3(0f, 0.64f, 0.25f), Vector3.one * 0.12f, skin);
            worker.label = new GameObject("WorkerLabel").AddComponent<TextMesh>();
            worker.label.transform.SetParent(root.transform, false);
            worker.label.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            worker.label.anchor = TextAnchor.MiddleCenter;
            worker.label.fontSize = 36;
            worker.label.characterSize = 0.07f;
            worker.label.color = Color.white;
            BurgerInventory inventory = root.AddComponent<BurgerInventory>();
            inventory.Configure(2);
            worker.Configure(grill, cashier, pickup, aisle, inventory);
            return worker;
        }

        static Material MaterialFor(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }

        static void Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
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
