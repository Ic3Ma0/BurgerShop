using UnityEngine;

namespace BurgerShop.Restaurant
{
    // Visual only: the economic transaction commits before this animation starts.
    public sealed class InvestmentCoin : MonoBehaviour
    {
        Vector3 from;
        Vector3 to;
        float age;
        public static void Launch(Vector3 origin, Vector3 destination, Transform parent)
        {
            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "InvestmentCoin";
            coin.transform.SetParent(parent, true);
            coin.transform.position = origin;
            coin.transform.localScale = new Vector3(0.22f, 0.04f, 0.22f);
            var collider = coin.GetComponent<Collider>();
            collider.enabled = false;
            BurgerVisual.Release(collider);
            Material gold = Core.RuntimeMaterials.Create(new Color(1f, 0.78f, 0.08f));
            coin.GetComponent<Renderer>().sharedMaterial = gold;
            coin.AddComponent<BurgerVisual>().OwnMaterials(gold);
            var motion = coin.AddComponent<InvestmentCoin>();
            motion.from = origin;
            motion.to = destination;
        }
        void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / 0.3f);
            transform.position = Vector3.Lerp(from, to, t) + Vector3.up * (0.5f * Mathf.Sin(t * Mathf.PI));
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
