using System;
using System.Collections.Generic;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.Player
{
    public sealed class BurgerInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] int capacity = 4;
        [SerializeField] Transform carryAnchor;
        readonly List<Transform> burgers = new List<Transform>();

        public int Count => burgers.Count;
        public int Capacity => capacity;
        public bool IsFull => Count >= capacity;
        public event Action<int> CountChanged;

        void Awake() => Configure(capacity);

        public void Configure(int maxCapacity = 4)
        {
            capacity = Mathf.Max(1, Count, maxCapacity);
            if (carryAnchor == null)
            {
                carryAnchor = new GameObject("CarryStack").transform;
                carryAnchor.SetParent(transform, false);
                carryAnchor.localPosition = new Vector3(0f, 0.1f, 0.8f);
            }
        }

        public bool TryCollectFrom(ProductionStation station)
        {
            if (IsFull || station == null || !station.TryTakeBurger(out Transform burger))
                return false;

            if (burger == null)
                burger = BurgerVisualFactory.Create(carryAnchor, Count);
            else
                burger.SetParent(carryAnchor, false);

            burger.localPosition = new Vector3(0f, Count * 0.34f, 0f);
            burger.localRotation = Quaternion.identity;
            burger.localScale = Vector3.one;
            burgers.Add(burger);
            CountChanged?.Invoke(Count);
            return true;
        }

        // Delivery can consume carried stock through this interface in Goal 05.
        public bool TryTakeBurger()
        {
            if (Count == 0)
                return false;

            Transform burger = burgers[Count - 1];
            burgers.RemoveAt(Count - 1);
            if (burger != null)
            {
                burger.SetParent(null, true);
                burger.gameObject.SetActive(false);
                BurgerVisual.Release(burger.gameObject);
            }
            CountChanged?.Invoke(Count);
            return true;
        }
    }
}
