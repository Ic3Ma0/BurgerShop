using System;
using System.Collections.Generic;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.Player
{
    public enum CarriedItemKind { Burger, Boxed, EmptyBag, Bagged, Cola }
    public sealed class BurgerInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] int capacity = 4;
        [SerializeField] Transform carryAnchor;
        readonly List<Transform> burgers = new List<Transform>();
        readonly List<CarriedItemKind> boxed = new List<CarriedItemKind>();

        int incomingBoxes;
        public int IncomingBoxes => incomingBoxes;
        public int Count => burgers.Count + incomingBoxes;
        public int LooseCount
        {
            get
            {
                int loose = 0;
                for (int i = 0; i < boxed.Count; i++)
                    if (boxed[i] == CarriedItemKind.Burger) loose++;
                return loose;
            }
        }
        public int ColaCount => CountKind(CarriedItemKind.Cola);
        public int BoxedCount => CountKind(CarriedItemKind.Boxed) + incomingBoxes;
        public int EmptyBagCount => CountKind(CarriedItemKind.EmptyBag);
        public int BaggedCount => CountKind(CarriedItemKind.Bagged);
        int CountKind(CarriedItemKind kind) { int n=0;foreach(var k in boxed)if(k==kind)n++;return n; }
        public int PermanentCapacity => capacity;
        public float CapacityMultiplier { get; set; } = 1f;
        public int Capacity => Mathf.CeilToInt(capacity * CapacityMultiplier);
        public bool IsFull => Count >= Capacity;
        public event Action<int> CountChanged;

        void Awake() => Configure(capacity);

        public void Configure(int maxCapacity = PlayerBoost.BaseCarry)
        {
            capacity = Mathf.Max(1, maxCapacity);
            if (carryAnchor == null)
            {
                carryAnchor = new GameObject("CarryStack").transform;
                carryAnchor.SetParent(transform, false);
                carryAnchor.localPosition = new Vector3(0f, 0.1f, 0.8f);
            }
        }

        public void ApplyBoostLevel(int level)
        {
            if (level < 0 || level > PlayerBoost.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(level));
            Configure(PlayerBoost.CarryCapacity(level));
        }

        public bool TryCollectFrom(ProductionStation station)
        {
            if (IsFull || station == null || !station.TryTakeBurger(out Transform burger))
                return false;

            bool drink = station.Product == KitchenProduct.Cola;
            if (burger == null)
                burger = drink ? ColaVisualFactory.Create(carryAnchor, Count) : BurgerVisualFactory.Create(carryAnchor, Count);
            else
                burger.SetParent(carryAnchor, false);

            burger.localPosition = new Vector3(0f, Count * 0.34f, 0f);
            burger.localRotation = Quaternion.identity;
            burger.localScale = Vector3.one;
            burgers.Add(burger);
            boxed.Add(drink ? CarriedItemKind.Cola : CarriedItemKind.Burger);
            CountChanged?.Invoke(Count);
            return true;
        }

        internal bool TryReserveIncomingBox()
        {
            if (IsFull) return false;
            incomingBoxes++;
            return true;
        }

        internal void ReceiveReservedBox(Transform box)
        {
            if (incomingBoxes <= 0) throw new InvalidOperationException("No incoming box was reserved.");
            incomingBoxes--;
            burgers.Add(box);
            boxed.Add(CarriedItemKind.Boxed);
            Restack();
            CountChanged?.Invoke(Count);
        }

        public bool TryBoxOne()
        {
            for (int i = burgers.Count - 1; i >= 0; i--)
            {
                if (boxed[i] != CarriedItemKind.Burger) continue;
                Transform loose = burgers[i];
                if (loose != null)
                    BurgerVisual.Release(loose.gameObject);
                Transform box = BoxVisualFactory.Create(carryAnchor, i);
                box.localPosition = new Vector3(0f, i * 0.34f, 0f);
                box.localRotation = Quaternion.identity;
                box.localScale = Vector3.one;
                burgers[i] = box;
                boxed[i] = CarriedItemKind.Boxed;
                CountChanged?.Invoke(Count);
                return true;
            }
            return false;
        }

        public bool TryTakeBurger()
        {
            if (!TryTakeBurger(out Transform burger)) return false;
            if (burger != null)
            {
                burger.gameObject.SetActive(false);
                BurgerVisual.Release(burger.gameObject);
            }
            return true;
        }

        // Transfer ownership of the existing visual to the customer without duplicating it.
        public bool TryTakeBurger(out Transform burger) => TryTake(CarriedItemKind.Burger, out burger);

        public bool TryTakeCola(out Transform cup) => TryTake(CarriedItemKind.Cola, out cup);
        public bool TryTakeCola()
        {
            if (!TryTakeCola(out var cup)) return false;
            if (cup != null) BurgerVisual.Release(cup.gameObject);
            return true;
        }

        public bool TryTakeBoxed(out Transform box) => TryTake(CarriedItemKind.Boxed, out box);

        public bool TryTake(CarriedItemKind wantBoxed, out Transform item)
        {
            item = null;
            for (int i = burgers.Count - 1; i >= 0; i--)
            {
                if (boxed[i] != wantBoxed) continue;
                item = burgers[i];
                burgers.RemoveAt(i);
                boxed.RemoveAt(i);
                if (item != null)
                    item.SetParent(null, true);
                Restack();
                CountChanged?.Invoke(Count);
                return true;
            }
            return false;
        }

        public bool TryReceive(CarriedItemKind kind, Transform visual)
        {
            if(IsFull || visual==null)return false;
            burgers.Add(visual);boxed.Add(kind);Restack();CountChanged?.Invoke(Count);return true;
        }

        void Restack()
        {
            if (carryAnchor == null) return;
            for (int i = 0; i < burgers.Count; i++)
            {
                if (burgers[i] == null) continue;
                burgers[i].SetParent(carryAnchor, false);
                burgers[i].localPosition = new Vector3(0f, i * 0.34f, 0f);
                burgers[i].localRotation = Quaternion.identity;
                burgers[i].localScale = Vector3.one;
            }
        }
    }
}
