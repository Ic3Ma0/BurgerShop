using System;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public enum KitchenProduct { Burger, Cola }

    public sealed class ProductionStation : MonoBehaviour
    {
        public static readonly int[] LevelCaps = { 4, 6, 8 };

        [SerializeField, Min(0.1f)] float productionSeconds = 3f;
        [SerializeField, Min(1)] int capacity = 4;

        Transform outputAnchor;
        Transform progressFill;
        TextMesh statusText;
        KitchenProduct product = KitchenProduct.Burger;
        float elapsed;
        bool maxedTier;

        public int Stock { get; private set; }
        public KitchenProduct Product => product;
        public int Capacity => capacity;
        public Transform OutputAnchor => outputAnchor;
        public string StatusCopy => statusText != null ? statusText.text : "";
        public float ProductionSeconds => productionSeconds;
        public float NormalizedProgress => Stock >= capacity ? 1f : Mathf.Clamp01(elapsed / productionSeconds);

        public event Action<int> StockChanged;

        public static int CapacityForLevel(int level) =>
            LevelCaps[Mathf.Clamp(level, 1, LevelCaps.Length) - 1];

        public void Configure(Transform output, Transform fill, TextMesh label, float seconds = 3f, int maxStock = 4,
            KitchenProduct kind = KitchenProduct.Burger)
        {
            outputAnchor = output;
            progressFill = fill;
            statusText = label;
            product = kind;
            productionSeconds = Mathf.Max(0.1f, seconds);
            capacity = Mathf.Max(1, maxStock);
            RefreshVisuals();
        }

        public void AttachOutput(Transform output) => outputAnchor = output;

        public void AttachFill(Transform fill) => progressFill = fill;

        public void SetCapacity(int maxStock)
        {
            if (maxStock < 1)
                throw new ArgumentOutOfRangeException(nameof(maxStock));
            capacity = maxStock;
            RefreshVisuals();
        }

        public void SetMaxedTier(bool maxed)
        {
            maxedTier = maxed;
            RefreshVisuals();
        }

        void Update()
        {
            Advance(Time.deltaTime);
            FaceLabelTowardsCamera();
        }

        public void SetProductionSeconds(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            float progress = Stock >= capacity ? 0f : NormalizedProgress;
            productionSeconds = Mathf.Max(0.1f, seconds);
            // Preserve the fraction already cooked, without spawning or losing stock.
            elapsed = progress * productionSeconds;
            RefreshVisuals();
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f || Stock >= capacity)
            {
                RefreshVisuals();
                return;
            }

            elapsed += deltaTime;
            while (elapsed >= productionSeconds && Stock < capacity)
            {
                elapsed -= productionSeconds;
                AddItem();
            }

            if (Stock >= capacity)
                elapsed = 0f;

            RefreshVisuals();
        }

        public bool TryTakeBurger()
        {
            if (!TryTakeBurger(out Transform burger))
                return false;

            if (burger != null)
                BurgerVisual.Release(burger.gameObject);
            return true;
        }

        // Ownership of the detached visual passes to the caller (for carrying or serving).
        public bool TryTakeBurger(out Transform burger)
        {
            burger = null;
            if (Stock == 0)
                return false;

            Stock--;
            if (outputAnchor != null && outputAnchor.childCount > 0)
            {
                burger = outputAnchor.GetChild(outputAnchor.childCount - 1);
                // Detach now: Destroy is deferred in Play Mode and could select the same
                // child twice when several burgers leave during one frame.
                burger.SetParent(null, true);
            }

            StockChanged?.Invoke(Stock);
            RefreshVisuals();
            return true;
        }

        void AddItem()
        {
            Stock++;
            var owner=GetComponentInParent<ExpandableGrill>();
            if(owner!=null && (owner.GetComponentInParent<Building.FacilityInstance>() is var facility && facility!=null ? facility.Kind==Building.FacilityKind.BurgerMachine && facility.Id!="grill-main" : ShopLayout.Horizontal(owner.transform.position,ShopLayout.ExtraGrill)<.2f))
                GetComponentInParent<UI.SessionGoalTracker>()?.RecordMilestone(ShopGoalKind.ExtraProduction);
            if (outputAnchor != null)
            {
                if (product == KitchenProduct.Cola)
                    ColaVisualFactory.Create(outputAnchor, Stock - 1);
                else
                    BurgerVisualFactory.Create(outputAnchor, Stock - 1);
            }
            StockChanged?.Invoke(Stock);
        }

        void RefreshVisuals()
        {
            float progress = NormalizedProgress;
            if (progressFill != null)
            {
                Vector3 scale = progressFill.localScale;
                scale.x = progress;
                progressFill.localScale = scale;
                progressFill.localPosition = new Vector3((progress - 1f) * 0.75f, progressFill.localPosition.y, progressFill.localPosition.z);
            }

            if (statusText != null)
            {
                string noun = product == KitchenProduct.Cola ? "COLA" : "GRILL";
                statusText.text = maxedTier
                    ? $"{noun} {Stock}/{capacity}  MAX"
                    : $"{noun} {Stock}/{capacity}";
            }
        }

        void FaceLabelTowardsCamera()
        {
            if (statusText == null || Camera.main == null)
                return;
            statusText.transform.rotation = Camera.main.transform.rotation;
        }
    }

    static class BoxVisualFactory
    {
        public static Transform Create(Transform parent, int index)
        {
            Transform box = new GameObject($"Box_{index + 1}").transform;
            box.SetParent(parent, false);
            box.localPosition = new Vector3(0f, index * 0.34f, 0f);

            Material board = CreateMaterial(new Color(.04f,.40f,.66f));
            Material stripe = CreateMaterial(new Color(.88f,.98f,1f));
            Material lid = CreateMaterial(new Color(.10f,.58f,.80f));
            box.gameObject.AddComponent<BurgerVisual>().OwnMaterials(board, stripe, lid);

            CreateLayer(box, "Body", PrimitiveType.Cube, new Vector3(0f, 0.14f, 0f), new Vector3(0.62f, 0.22f, 0.62f), board);
            CreateLayer(box, "Lid", PrimitiveType.Cube, new Vector3(0f, 0.28f, 0f), new Vector3(0.66f, 0.06f, 0.66f), lid);
            CreateLayer(box, "Band", PrimitiveType.Cube, new Vector3(0f, 0.20f, 0f), new Vector3(0.68f, 0.05f, 0.18f), stripe);
            CreateLayer(box,"WhiteEmblem",PrimitiveType.Cube,new Vector3(0,.32f,0),new Vector3(.34f,.012f,.34f),stripe);
            CreateLayer(box,"BlueEmblem",PrimitiveType.Cube,new Vector3(0,.33f,0),new Vector3(.24f,.012f,.24f),board);
            return box;
        }

        static void CreateLayer(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material)
        {
            GameObject layer = GameObject.CreatePrimitive(primitive);
            layer.name = name;
            layer.transform.SetParent(parent, false);
            layer.transform.localPosition = position;
            layer.transform.localScale = scale;
            Renderer renderer = layer.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
            Collider collider = layer.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(collider);
                else
                    UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        static Material CreateMaterial(Color color) => BurgerShop.Core.RuntimeMaterials.Create(color);
    }

    static class ColaVisualFactory
    {
        public static Transform Create(Transform parent, int index)
        {
            Transform cup = new GameObject($"Cola_{index + 1}").transform;
            cup.SetParent(parent, false);
            cup.localPosition = new Vector3(0f, index * 0.34f, 0f);

            Material body = CreateMaterial(new Color(0.72f, 0.10f, 0.14f));
            Material stripe = CreateMaterial(new Color(0.96f, 0.96f, 0.98f));
            Material straw = CreateMaterial(new Color(0.90f, 0.90f, 0.92f));
            Material lid = CreateMaterial(new Color(0.18f, 0.18f, 0.20f));
            cup.gameObject.AddComponent<BurgerVisual>().OwnMaterials(body, stripe, straw, lid);

            CreateLayer(cup, "Cup", PrimitiveType.Cylinder, new Vector3(0f, 0.16f, 0f), new Vector3(0.38f, 0.16f, 0.38f), body);
            CreateLayer(cup, "Stripe", PrimitiveType.Cylinder, new Vector3(0f, 0.16f, 0f), new Vector3(0.40f, 0.035f, 0.40f), stripe);
            CreateLayer(cup, "Lid", PrimitiveType.Cylinder, new Vector3(0f, 0.30f, 0f), new Vector3(0.36f, 0.03f, 0.36f), lid);
            CreateLayer(cup, "Straw", PrimitiveType.Cylinder, new Vector3(0.08f, 0.42f, 0f), new Vector3(0.05f, 0.12f, 0.05f), straw);
            return cup;
        }

        static void CreateLayer(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material)
        {
            GameObject layer = GameObject.CreatePrimitive(primitive);
            layer.name = name;
            layer.transform.SetParent(parent, false);
            layer.transform.localPosition = position;
            layer.transform.localScale = scale;
            Renderer renderer = layer.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
            Collider collider = layer.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(collider);
                else
                    UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        static Material CreateMaterial(Color color) => BurgerShop.Core.RuntimeMaterials.Create(color);
    }
}
