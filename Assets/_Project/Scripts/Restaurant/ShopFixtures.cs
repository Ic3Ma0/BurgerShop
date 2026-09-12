using UnityEngine;

namespace BurgerShop.Restaurant
{
    public static class ShopFixtures
    {
        public static Transform CreateCashierCircle(Transform parent, Vector3 position) =>
            CreateDashedCircle(parent, "CashierCircle", position, new Color(0.96f, 0.97f, 1f));

        public static Transform CreateActionCircle(Transform parent, string name, Vector3 position, Color color) =>
            CreateDashedCircle(parent, name, position, color);

        static Transform CreateDashedCircle(Transform parent, string name, Vector3 position, Color color)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            Material dash = BurgerShop.Core.RuntimeMaterials.Create(color, true);
            const int segments = 22;
            const float radius = 0.85f;
            for (int i = 0; i < segments; i++)
            {
                if ((i & 1) == 0) continue;
                float angle = i / (float)segments * Mathf.PI * 2f;
                GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mark.name = "Dash_" + i;
                mark.transform.SetParent(root.transform, false);
                mark.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0.015f, Mathf.Sin(angle) * radius);
                mark.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                mark.transform.localScale = new Vector3(0.22f, 0.02f, 0.07f);
                mark.GetComponent<Renderer>().sharedMaterial = dash;
                SolidOccupancy.Apply(mark.GetComponent<Collider>(), false);
            }
            return root.transform;
        }

        public static CounterStock CreateCounterStock(Transform parent, Vector3 counterTop, bool boxed = false)
        {
            Transform anchor = new GameObject("CounterStockAnchor").transform;
            anchor.SetParent(parent, false);
            anchor.position = counterTop + new Vector3(0.55f, 0.18f, 0f);

            GameObject badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badge.name = boxed ? "PackageStockBadge" : "CounterStockBadge";
            badge.transform.SetParent(parent, false);
            badge.transform.position = counterTop + new Vector3(1.15f, 0.55f, -0.85f);
            badge.transform.localScale = new Vector3(0.7f, 0.55f, 0.04f);
            badge.GetComponent<Renderer>().sharedMaterial = BurgerShop.Core.RuntimeMaterials.Create(new Color(1f, 1f, 1f), true);
            SolidOccupancy.Apply(badge.GetComponent<Collider>(), false);

            TextMesh label = new GameObject(boxed ? "PackageStockCount" : "CounterStockCount").AddComponent<TextMesh>();
            label.transform.SetParent(badge.transform, false);
            label.transform.localPosition = new Vector3(0.12f, 0f, -1.2f);
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.12f;
            label.fontSize = 64;
            label.color = new Color(0.16f, 0.16f, 0.18f);
            label.text = "0";

            Transform icon = boxed
                ? BoxVisualFactory.Create(badge.transform, 0)
                : BurgerVisualFactory.Create(badge.transform, 0);
            icon.name = boxed ? "BadgeBox" : "BadgeBurger";
            icon.localPosition = new Vector3(-0.16f, -0.08f, -1.1f);
            icon.localScale = Vector3.one * 0.35f;

            CounterStock stock = parent.gameObject.AddComponent<CounterStock>();
            stock.Configure(anchor, label);
            return stock;
        }

        public static TextMesh CreateStationLabel(Transform parent, string name, Vector3 position, string text)
        {
            TextMesh label = new GameObject(name).AddComponent<TextMesh>();
            label.transform.SetParent(parent, false);
            label.transform.position = position;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.09f;
            label.fontSize = 36;
            label.color = new Color(0.98f, 0.98f, 1f);
            label.text = text;
            label.gameObject.AddComponent<StationBillboard>();
            return label;
        }
    }

    sealed class StationBillboard : MonoBehaviour
    {
        void LateUpdate()
        {
            if (Camera.main == null) return;
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}
