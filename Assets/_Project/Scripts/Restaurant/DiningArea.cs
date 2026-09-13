using System;
using System.Collections.Generic;
using BurgerShop.Customer;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class DiningArea : MonoBehaviour
    {
        public static Vector3[] ShopPositions => ShopLayout.Tables;

        DiningTable[] tables;
        TrashInventory boundCollector;
        CashFloor boundCash;

        public IReadOnlyList<DiningTable> Tables => tables ?? Array.Empty<DiningTable>();
        public int TableCount => tables?.Length ?? 0;
        public int SeatCount
        {
            get
            {
                int count = 0;
                if (tables == null) return 0;
                for (int i = 0; i < tables.Length; i++)
                    if (tables[i] != null) count += tables[i].SeatCount;
                return count;
            }
        }
        public int OccupiedSeats
        {
            get
            {
                int count = 0;
                if (tables == null) return 0;
                for (int i = 0; i < tables.Length; i++)
                    if (tables[i] != null) count += tables[i].OccupiedSeats;
                return count;
            }
        }
        public Vector3 WaitPosition
        {
            get
            {
                if (tables == null || tables.Length == 0) return Vector3.zero;
                Vector3 wait = tables[0] != null ? tables[0].WaitPosition : Vector3.zero;
                for (int i = 1; i < tables.Length; i++)
                {
                    if (tables[i] == null) continue;
                    Vector3 candidate = tables[i].WaitPosition;
                    if (candidate.x < wait.x) wait = candidate;
                }
                return wait;
            }
        }

        public int TrashOnTables
        {
            get
            {
                int count = 0;
                if (tables == null) return 0;
                for (int i = 0; i < tables.Length; i++)
                    if (tables[i] != null) count += tables[i].TrashCount;
                return count;
            }
        }

        public bool HasTrashOnTables => TrashOnTables > 0;

        public DiningTable FindDirtyTable()
        {
            if (tables == null) return null;
            for (int i = 0; i < tables.Length; i++)
                if (tables[i] != null && tables[i].IsDirty) return tables[i];
            return null;
        }

        public void Configure(DiningTable[] diningTables)
        {
            tables = diningTables ?? Array.Empty<DiningTable>();
        }

        public void BindCollector(TrashInventory bag)
        {
            boundCollector = bag;
            if (tables == null) return;
            for (int i = 0; i < tables.Length; i++)
                tables[i]?.BindCollector(bag);
        }

        public void BindCash(CashFloor cash)
        {
            boundCash = cash;
            if (tables == null) return;
            for (int i = 0; i < tables.Length; i++)
                tables[i]?.BindCash(cash);
        }

        public DiningTable AddTable(Vector3 position, DiningTableKind kind = DiningTableKind.Pair)
        {
            DiningTable table = DiningTable.Create(transform, position, kind);
            RegisterTable(table);
            return table;
        }

        public void RegisterTable(DiningTable table)
        {
            if (table == null || (tables != null && System.Array.IndexOf(tables, table) >= 0)) return;
            int count = tables != null ? tables.Length : 0;
            var next = new DiningTable[count + 1];
            if (tables != null)
                for (int i = 0; i < count; i++) next[i] = tables[i];
            next[count] = table;
            tables = next;
            if (boundCollector != null) table.BindCollector(boundCollector);
            if (boundCash != null) table.BindCash(boundCash);
        }

        public bool TryAssignSeat(CustomerAgent guest, out DiningTable table, out Vector3 sitPosition, out int seatIndex)
        {
            table = null;
            sitPosition = WaitPosition;
            seatIndex = -1;
            if (guest == null || tables == null) return false;
            for (int i = 0; i < tables.Length; i++)
            {
                DiningTable candidate = tables[i];
                if (candidate == null || candidate.IsDirty) continue;
                if (!candidate.TryAssignSeat(guest, out sitPosition, out seatIndex)) continue;
                table = candidate;
                return true;
            }
            table = WatchTable();
            if (table != null) sitPosition = table.WaitPosition;
            return false;
        }

        DiningTable WatchTable()
        {
            if (tables == null) return null;
            for (int i = 0; i < tables.Length; i++)
            {
                DiningTable candidate = tables[i];
                if (candidate != null && candidate.IsDirty && candidate.OccupiedSeats < candidate.SeatCount)
                    return candidate;
            }
            return null;
        }

        public static DiningArea Wrap(DiningTable table)
        {
            if (table == null) return null;
            DiningArea existing = table.GetComponentInParent<DiningArea>();
            if (existing != null) return existing;
            DiningArea area = table.gameObject.AddComponent<DiningArea>();
            area.Configure(new[] { table });
            return area;
        }

        public static DiningArea Create(Transform parent, Vector3[] positions = null)
        {
            positions = positions ?? ShopPositions;
            GameObject root = new GameObject("DiningArea");
            root.transform.SetParent(parent, false);
            DiningArea area = root.AddComponent<DiningArea>();
            var created = new DiningTable[positions.Length];
            for (int i = 0; i < positions.Length; i++)
                created[i] = DiningTable.Create(root.transform, positions[i]);
            area.Configure(created);
            return area;
        }
    }
}
