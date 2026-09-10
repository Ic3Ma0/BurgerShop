using System;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class ProductionStation : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float productionSeconds = 3f;
        [SerializeField, Min(1)] int capacity = 4;

        Transform outputAnchor;
        Transform progressFill;
        TextMesh statusText;
        float elapsed;

        public int Stock { get; private set; }
        public int Capacity => capacity;
        public float NormalizedProgress => Stock >= capacity ? 1f : Mathf.Clamp01(elapsed / productionSeconds);

        public event Action<int> StockChanged;

        public void Configure(Transform output, Transform fill, TextMesh label, float seconds = 3f, int maxStock = 4)
        {
            outputAnchor = output;
            progressFill = fill;
            statusText = label;
            productionSeconds = Mathf.Max(0.1f, seconds);
            capacity = Mathf.Max(1, maxStock);
            RefreshVisuals();
        }

        void Update()
        {
            Advance(Time.deltaTime);
            FaceLabelTowardsCamera();
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
                AddBurger();
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

        void AddBurger()
        {
            Stock++;
            if (outputAnchor != null)
                BurgerVisualFactory.Create(outputAnchor, Stock - 1);
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
                statusText.text = Stock >= capacity
                    ? $"GRILL  {Stock}/{capacity}  FULL"
                    : $"GRILL  {Stock}/{capacity}  {Mathf.CeilToInt((1f - progress) * productionSeconds)}s";
            }
        }

        void FaceLabelTowardsCamera()
        {
            if (statusText == null || Camera.main == null)
                return;
            statusText.transform.rotation = Camera.main.transform.rotation;
        }
    }

    static class BurgerVisualFactory
    {
        public static Transform Create(Transform parent, int index)
        {
            Transform burger = new GameObject($"Burger_{index + 1}").transform;
            burger.SetParent(parent, false);
            burger.localPosition = new Vector3(0f, index * 0.34f, 0f);

            Material bun = CreateMaterial(new Color(0.95f, 0.61f, 0.20f));
            Material patty = CreateMaterial(new Color(0.25f, 0.09f, 0.04f));
            Material cheese = CreateMaterial(new Color(1f, 0.78f, 0.08f));
            burger.gameObject.AddComponent<BurgerVisual>().OwnMaterials(bun, patty, cheese);

            CreateLayer(burger, "BottomBun", PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0f), new Vector3(0.55f, 0.08f, 0.55f), bun);
            CreateLayer(burger, "Patty", PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(0.52f, 0.055f, 0.52f), patty);
            CreateLayer(burger, "Cheese", PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(0.72f, 0.035f, 0.72f), cheese);
            CreateLayer(burger, "TopBun", PrimitiveType.Sphere, new Vector3(0f, 0.31f, 0f), new Vector3(0.58f, 0.22f, 0.58f), bun);
            return burger;
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

        static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            return material;
        }
    }
}
