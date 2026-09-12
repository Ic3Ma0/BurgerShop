using UnityEngine;

namespace BurgerShop.Restaurant
{
    public static class TrashVisual
    {
        public static Transform Create(Transform parent, int index)
        {
            Material paper = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.96f, 0.96f, 0.93f));
            Material crease = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.80f, 0.80f, 0.77f));
            Material fold = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.70f, 0.70f, 0.68f));
            GameObject root = new GameObject("Trash_" + index);
            root.transform.SetParent(parent, false);
            Lump(root.transform, "Core", Vector3.zero, Vector3.one * 0.50f, Vector3.zero, paper);
            Lump(root.transform, "LumpA", new Vector3(0.12f, 0.10f, -0.06f), new Vector3(0.36f, 0.30f, 0.34f), new Vector3(18f, 35f, -22f), crease);
            Lump(root.transform, "LumpB", new Vector3(-0.11f, 0.08f, 0.09f), new Vector3(0.32f, 0.28f, 0.36f), new Vector3(-24f, 70f, 16f), paper);
            Lump(root.transform, "LumpC", new Vector3(0.06f, -0.10f, 0.11f), new Vector3(0.30f, 0.26f, 0.28f), new Vector3(40f, -18f, 28f), fold);
            Lump(root.transform, "LumpD", new Vector3(-0.08f, -0.07f, -0.12f), new Vector3(0.28f, 0.24f, 0.30f), new Vector3(-32f, 12f, -40f), crease);
            Lump(root.transform, "LumpE", new Vector3(0.10f, 0.02f, 0.12f), new Vector3(0.26f, 0.22f, 0.24f), new Vector3(12f, -55f, 8f), paper);
            Lump(root.transform, "LumpF", new Vector3(-0.02f, 0.12f, -0.02f), new Vector3(0.24f, 0.20f, 0.26f), new Vector3(-8f, 25f, 33f), fold);
            return root.transform;
        }

        static void Lump(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Vector3 euler, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = Quaternion.Euler(euler);
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            collider.enabled = false;
            BurgerVisual.Release(collider);
        }
    }
}
