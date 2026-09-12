using System;
using System.Collections.Generic;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.Player
{
    public sealed class TrashInventory : MonoBehaviour
    {
        public const float CarrySpacing = 0.28f;
        readonly List<HeldTrash> items = new List<HeldTrash>();
        readonly List<TrashMotion> dumps = new List<TrashMotion>();
        readonly List<GameObject> spent = new List<GameObject>();
        Transform carryAnchor;

        public int Count => items.Count;
        public event Action<int> CountChanged;

        public void Configure()
        {
            if (carryAnchor != null) return;
            carryAnchor = new GameObject("TrashStack").transform;
            carryAnchor.SetParent(transform, false);
            carryAnchor.localPosition = new Vector3(-0.35f, 0.1f, 0.75f);
        }

        public void AdvanceDumps(float deltaTime)
        {
            if (deltaTime > 0f)
            {
                for (int i = dumps.Count - 1; i >= 0; i--)
                    dumps[i]?.Advance(deltaTime);
            }
            for (int i = 0; i < spent.Count; i++)
                if (spent[i] != null) BurgerVisual.Release(spent[i]);
            spent.Clear();
        }

        public bool TryCollect(DiningTable table, int seatIndex, Transform visual)
        {
            if (table == null || seatIndex < 0) return false;
            Configure();
            if (visual == null)
                visual = TrashVisual.Create(carryAnchor, Count);
            visual.SetParent(carryAnchor, false);
            visual.localPosition = new Vector3(0f, Count * CarrySpacing, 0f);
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;
            items.Add(new HeldTrash(table, seatIndex, visual));
            CountChanged?.Invoke(Count);
            return true;
        }

        public bool TryBeginDump(TrashBin bin, out TrashMotion motion)
        {
            motion = null;
            if (bin == null) return false;
            HeldTrash item = NextHeld();
            if (item == null || item.Visual == null) return false;
            Configure();
            item.InFlight = true;
            Restack();
            motion = item.Visual.gameObject.AddComponent<TrashMotion>();
            TrashMotion launched = motion;
            launched.Launch(bin.transform, TrashBin.MouthLocal, Vector3.up * 0.32f, Vector3.one * 0.08f,
                () => FinishDump(item, launched));
            dumps.Add(launched);
            return true;
        }

        public bool TryDump(out DiningTable table, out int seatIndex)
        {
            table = null;
            seatIndex = -1;
            HeldTrash item = NextHeld();
            if (item == null) return false;
            items.Remove(item);
            table = item.Table;
            seatIndex = item.SeatIndex;
            if (item.Visual != null)
            {
                item.Visual.gameObject.SetActive(false);
                BurgerVisual.Release(item.Visual.gameObject);
            }
            table?.NotifyTrashDisposed(seatIndex);
            Restack();
            CountChanged?.Invoke(Count);
            return true;
        }

        HeldTrash NextHeld()
        {
            for (int i = items.Count - 1; i >= 0; i--)
                if (!items[i].InFlight) return items[i];
            return null;
        }

        void FinishDump(HeldTrash item, TrashMotion motion)
        {
            dumps.Remove(motion);
            items.Remove(item);
            item.Table?.NotifyTrashDisposed(item.SeatIndex);
            if (item.Visual != null)
            {
                item.Visual.gameObject.SetActive(false);
                spent.Add(item.Visual.gameObject);
            }
            Restack();
            CountChanged?.Invoke(Count);
        }

        void Restack()
        {
            int slot = 0;
            for (int i = 0; i < items.Count; i++)
            {
                Transform visual = items[i].Visual;
                if (visual == null || items[i].InFlight) continue;
                visual.SetParent(carryAnchor, false);
                visual.localPosition = new Vector3(0f, slot * CarrySpacing, 0f);
                visual.localRotation = Quaternion.identity;
                slot++;
            }
        }

        sealed class HeldTrash
        {
            public readonly DiningTable Table;
            public readonly int SeatIndex;
            public readonly Transform Visual;
            public bool InFlight;

            public HeldTrash(DiningTable table, int seatIndex, Transform visual)
            {
                Table = table;
                SeatIndex = seatIndex;
                Visual = visual;
            }
        }
    }
}
