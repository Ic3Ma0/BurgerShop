using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class TrashBin : MonoBehaviour
    {
        public static Vector3 ShopPosition => ShopLayout.TrashBin;
        public static readonly Vector3 MouthLocal = new Vector3(0f, 0.48f, 0f);

        TrashInventory inventory;
        [SerializeField, Min(0.1f)] float radius = 1f;
        [SerializeField, Min(0.05f)] float dropInterval = 0.25f;
        float cooldown;

        public Vector3 DropPosition => transform.position;
        public float Radius => radius;

        public bool IsInRange
        {
            get
            {
                if (inventory == null) return false;
                Vector3 offset = inventory.transform.position - DropPosition;
                offset.y = 0f;
                return offset.sqrMagnitude <= radius * radius;
            }
        }

        public void Configure(TrashInventory carrier, float dropRadius = 1f, float interval = 0.25f)
        {
            inventory = carrier;
            radius = Mathf.Max(0.1f, dropRadius);
            dropInterval = Mathf.Max(0.05f, interval);
            cooldown = 0f;
        }

        void Update()
        {
            if (Application.isPlaying) Advance(Time.deltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            int held = inventory != null ? inventory.Count : 0;
            inventory?.AdvanceDumps(deltaTime);
            bool completed = inventory != null && inventory.Count < held;
            if (inventory == null || !inventory.isActiveAndEnabled) return;
            if (!IsInRange)
            {
                cooldown = 0f;
                return;
            }
            if (completed)
            {
                cooldown = dropInterval;
                return;
            }
            cooldown = Mathf.Max(0f, cooldown - deltaTime);
            if (cooldown <= 0f && TryDumpFrom(inventory, out TrashMotion started))
            {
                cooldown = dropInterval;
                started.Advance(deltaTime);
                inventory.AdvanceDumps(0f);
            }
        }

        public bool TryDumpFrom(TrashInventory carrier)
        {
            return TryDumpFrom(carrier, out _);
        }

        public bool TryDumpFrom(TrashInventory carrier, out TrashMotion motion)
        {
            motion = null;
            if (!isActiveAndEnabled || cooldown > 0f || carrier == null || !carrier.isActiveAndEnabled
                || carrier.Count == 0 || !IsActorInRange(carrier.transform)) return false;
            return carrier.TryBeginDump(this, out motion);
        }

        public bool IsActorInRange(Transform actor)
        {
            if (actor == null) return false;
            Vector3 offset = actor.position - DropPosition;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }

        public static TrashBin Create(Transform parent, Vector3 position)
        {
            GameObject root = new GameObject("TrashBin");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            Material body = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.18f, 0.19f, 0.21f));
            Material lid = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.10f, 0.11f, 0.12f));
            Material rim = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.28f, 0.30f, 0.32f));
            Part(root.transform, "Body", PrimitiveType.Cylinder, new Vector3(0f, 0.42f, 0f), new Vector3(0.58f, 0.42f, 0.58f), body);
            Part(root.transform, "Rim", PrimitiveType.Cylinder, new Vector3(0f, 0.86f, 0f), new Vector3(0.64f, 0.04f, 0.64f), rim);
            Part(root.transform, "Lid", PrimitiveType.Cylinder, new Vector3(0f, 0.94f, 0f), new Vector3(0.66f, 0.06f, 0.66f), lid);
            Part(root.transform, "Handle", PrimitiveType.Cube, new Vector3(0f, 1.06f, 0f), new Vector3(0.18f, 0.08f, 0.08f), lid);
            return root.AddComponent<TrashBin>();
        }

        static void Part(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            SolidOccupancy.Apply(part.GetComponent<Collider>(), true);
        }
    }
}
