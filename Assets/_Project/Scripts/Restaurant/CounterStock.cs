using System.Collections.Generic;
using BurgerShop.Core;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class CounterStock : MonoBehaviour
    {
        public const int MaxVisibleBurgers = 8;
        Transform stockAnchor;
        TextMesh countLabel;
        readonly List<Transform> burgers = new List<Transform>();
        readonly NumberPunch punch = new NumberPunch();
        Transform punchTarget;
        Vector3 punchRest = Vector3.one;
        int count;
        int lastCount = int.MinValue;

        public int Count => count;
        public bool IsFull => false;
        public bool IsEmpty => count == 0;
        public bool IsPunching => punch.IsActive;
        public float PunchScale => punch.Scale;

        public void Configure(Transform anchor, TextMesh label)
        {
            stockAnchor = anchor;
            countLabel = label;
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
            if (carrier == null || !carrier.isActiveAndEnabled || !carrier.TryTakeBurger(out Transform burger))
                return false;
            return Accept(burger, false);
        }

        public bool TryPlaceBoxedFrom(BurgerInventory carrier)
        {
            if (carrier == null || !carrier.isActiveAndEnabled || !carrier.TryTakeBoxed(out Transform box))
                return false;
            return Accept(box, true);
        }

        bool Accept(Transform item, bool boxed)
        {
            if (burgers.Count < MaxVisibleBurgers)
                AttachVisual(item, boxed);
            else if (item != null)
                BurgerVisual.Release(item.gameObject);
            count++;
            RefreshLabel();
            return true;
        }

        public bool TryTakeBurger(out Transform burger)
        {
            burger = null;
            if (count == 0) return false;
            count--;
            if (count >= MaxVisibleBurgers)
                burger = BurgerVisualFactory.Create(null, 0);
            else if (burgers.Count > 0)
            {
                burger = burgers[burgers.Count - 1];
                burgers.RemoveAt(burgers.Count - 1);
                if (burger != null) burger.SetParent(null, true);
            }
            else
                burger = BurgerVisualFactory.Create(null, 0);
            RefreshLabel();
            return true;
        }

        public bool TryTakeBoxed(out Transform box)
        {
            box = null;
            if (count == 0) return false;
            count--;
            if (count >= MaxVisibleBurgers)
                box = BoxVisualFactory.Create(null, 0);
            else if (burgers.Count > 0)
            {
                box = burgers[burgers.Count - 1];
                burgers.RemoveAt(burgers.Count - 1);
                if (box != null) box.SetParent(null, true);
            }
            else
                box = BoxVisualFactory.Create(null, 0);
            RefreshLabel();
            return true;
        }

        void AttachVisual(Transform burger, bool boxed = false)
        {
            if (burger == null)
                burger = boxed
                    ? BoxVisualFactory.Create(stockAnchor, burgers.Count)
                    : BurgerVisualFactory.Create(stockAnchor, burgers.Count);
            else
                burger.SetParent(stockAnchor, false);
            burger.localPosition = new Vector3(0f, burgers.Count * 0.22f, 0f);
            burger.localRotation = Quaternion.identity;
            burger.localScale = Vector3.one * 0.85f;
            burgers.Add(burger);
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
