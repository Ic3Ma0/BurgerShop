using System.Collections.Generic;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class TrashBin : MonoBehaviour
    {
        public static Vector3 ShopPosition => ShopLayout.TrashBin;
        public const float VisualScale = 1.7f;
        // The enlarged bin and player capsule stop their centres about 1.04 m apart.
        public const float DefaultDropRadius = 1.25f;
        public const float DumpInterval = 0.11f;
        public const float MinDumpInterval = 0.10f;
        public static readonly Vector3 MouthLocal = new Vector3(0f, 0.86f, 0f);

        TrashInventory inventory;
        BurgerInventory food;
        readonly List<TrashMotion> discarded = new List<TrashMotion>();
        [SerializeField, Min(0.1f)] float radius = DefaultDropRadius;
        [SerializeField, Min(0.05f)] float dropInterval = DumpInterval;
        float cooldown;

        public Vector3 DropPosition => transform.position;
        public float Radius => radius;
        public float DropInterval => dropInterval;

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

        public void Configure(TrashInventory carrier, float dropRadius = DefaultDropRadius, float interval = DumpInterval)
        {
            inventory = carrier;
            food = carrier != null ? carrier.GetComponent<BurgerInventory>() : null;
            radius = Mathf.Max(0.1f, dropRadius);
            dropInterval = Mathf.Max(MinDumpInterval, interval);
            cooldown = 0f;
        }

        void Update()
        {
            if (Application.isPlaying) Advance(Time.deltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f || !isActiveAndEnabled) return;
            for (int i = discarded.Count - 1; i >= 0; i--)
            {
                var flight = discarded[i];
                if (flight != null) flight.Advance(deltaTime);
                if (flight == null || flight.IsFinished) discarded.RemoveAt(i);
            }
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
            if (cooldown <= 0f && inventory.Count == 0 && food != null && food.isActiveAndEnabled
                && (food.LooseCount > 0 || food.ColaCount > 0))
            {
                var kind = food.ColaCount > 0 ? CarriedItemKind.Cola : CarriedItemKind.Burger;
                if (food.TryTake(kind, out Transform item))
                {
                    cooldown = dropInterval;
                    if (item != null)
                    {
                        var flight = item.gameObject.AddComponent<TrashMotion>();
                        flight.Launch(transform, MouthLocal, Vector3.up * .6f, Vector3.zero,
                            () => BurgerVisual.Release(item.gameObject), TrashInventory.DumpDuration);
                        discarded.Add(flight);
                    }
                    UI.FeedbackDirector.Current?.RequestSound(UI.FeedbackSound.Dump);
                }
                return;
            }
            if (cooldown <= 0f && TryDumpFrom(inventory, out TrashMotion started))
            {
                cooldown = dropInterval;
                started.Advance(deltaTime);
                inventory.AdvanceDumps(0f);
            }
        }

        void OnDestroy()
        {
            foreach (var flight in discarded)
                if (flight != null) BurgerVisual.Release(flight.gameObject);
            discarded.Clear();
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
            root.transform.localScale = Vector3.one * VisualScale;
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
