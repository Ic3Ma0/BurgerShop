using System.Collections.Generic;
using BurgerShop.Core;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class CounterStock : MonoBehaviour
    {
        public Vector3 CounterPosition => stockAnchor!=null?stockAnchor.position-new Vector3(.55f,.18f,0):transform.position;
        public int ServiceLevel { get; set; } = 1;
        public const float LayerHeight = 0.22f;
        Transform stockAnchor;
        TextMesh countLabel;
        readonly List<Transform> burgers = new List<Transform>();
        readonly NumberPunch punch = new NumberPunch();
        Transform punchTarget;
        Vector3 punchRest = Vector3.one;
        int count;
        int lastCount = int.MinValue;
        KitchenProduct product = KitchenProduct.Burger;

        public int Count => count;
        public KitchenProduct Product => product;
        public bool IsFull => false;
        public bool IsEmpty => count == 0;
        public bool IsPunching => punch.IsActive;
        public float PunchScale => punch.Scale;

        public void Configure(Transform anchor, TextMesh label, KitchenProduct kind = KitchenProduct.Burger)
        {
            stockAnchor = anchor;
            countLabel = label;
            product = kind;
            punchTarget = countLabel != null && countLabel.transform.parent != null
                ? countLabel.transform.parent
                : countLabel != null ? countLabel.transform : null;
            punchRest = punchTarget != null ? punchTarget.localScale : Vector3.one;
            lastCount = int.MinValue;
            RefreshLabel();
        }

        void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            punch.Advance(deltaTime);
            if (punchTarget != null)
                punchTarget.localScale = punchRest * punch.Scale;
        }

        public bool TryPlaceFrom(BurgerInventory carrier)
        {
            if (product == KitchenProduct.Cola)
                return TryPlaceColaFrom(carrier);
            if (carrier == null || !carrier.isActiveAndEnabled || !carrier.TryTakeBurger(out Transform burger))
                return false;
            return Accept(burger, false);
        }

        public bool TryPlaceColaFrom(BurgerInventory carrier)
        {
            if (carrier == null || !carrier.isActiveAndEnabled || !carrier.TryTakeCola(out Transform cup))
                return false;
            return Accept(cup, false);
        }

        public bool TryPlaceBoxedFrom(BurgerInventory carrier)
        {
            if (carrier == null || !carrier.isActiveAndEnabled || !carrier.TryTakeBoxed(out Transform box))
                return false;
            return Accept(box, true);
        }

        bool Accept(Transform item, bool boxed)
        {
            AttachVisual(item, boxed);
            count++;
            RefreshLabel();
            return true;
        }

        public bool TryTakeBurger(out Transform burger) => TryTakeItem(out burger, false);

        public bool TryTakeBoxed(out Transform box) => TryTakeItem(out box, true);

        bool TryTakeItem(out Transform item, bool boxed)
        {
            item = null;
            if (count == 0) return false;
            count--;
            if (burgers.Count > 0)
            {
                item = burgers[burgers.Count - 1];
                burgers.RemoveAt(burgers.Count - 1);
                if (item != null) item.SetParent(null, true);
            }
            else
                item = SpawnDetached(count, boxed);
            RefreshLabel();
            return true;
        }

        Transform CreateVisual(Transform parent, int index) =>
            product == KitchenProduct.Cola
                ? ColaVisualFactory.Create(parent, index)
                : BurgerVisualFactory.Create(parent, index);

        void AttachVisual(Transform burger, bool boxed = false)
        {
            if (burger == null)
                burger = boxed
                    ? BoxVisualFactory.Create(VisualParent, burgers.Count)
                    : CreateVisual(VisualParent, burgers.Count);
            else
                burger.SetParent(VisualParent, false);
            Place(burger, burgers.Count);
            burgers.Add(burger);
        }

        Transform SpawnDetached(int slotIndex, bool boxed)
        {
            Transform item = boxed
                ? BoxVisualFactory.Create(VisualParent, slotIndex)
                : CreateVisual(VisualParent, slotIndex);
            Place(item, slotIndex);
            item.SetParent(null, true);
            return item;
        }

        Transform VisualParent => stockAnchor != null ? stockAnchor : transform;

        public static Vector3 SlotLocal(int index) =>
            new Vector3(0f, Mathf.Max(0, index) * LayerHeight, 0f);

        static void Place(Transform item, int index)
        {
            if (item == null) return;
            item.localPosition = SlotLocal(index);
            item.localRotation = Quaternion.identity;
            item.localScale = Vector3.one * 0.85f;
        }

        void LateUpdate()
        {
            if (countLabel == null || Camera.main == null) return;
            countLabel.transform.rotation = Camera.main.transform.rotation;
        }

        void RefreshLabel()
        {
            if (lastCount != int.MinValue && count > lastCount)
                punch.Play();
            lastCount = count;
            if (countLabel == null) return;
            string text = count.ToString();
            countLabel.text = text;
            countLabel.characterSize = text.Length >= 3 ? 0.07f : text.Length >= 2 ? 0.09f : 0.12f;
            Transform badge = countLabel.transform.parent;
            if (badge != null) badge.gameObject.SetActive(count > 0);
            else countLabel.gameObject.SetActive(count > 0);
        }
    }
}
