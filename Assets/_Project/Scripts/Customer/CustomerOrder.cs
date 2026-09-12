using System;
using UnityEngine;

namespace BurgerShop.Customer
{
    // One reservation and one settlement per order, shared by every potential server.
    public sealed class CustomerOrder
    {
        public int Quantity { get; }
        public int Delivered { get; private set; }
        public int Remaining => Quantity - Delivered;
        public bool InFlight { get; private set; }
        public bool IsComplete => Remaining == 0;
        public bool IsSettled { get; private set; }
        public CustomerOrder(int quantity, int maximum = 4)
        {
            if (quantity < 1 || quantity > maximum) throw new ArgumentOutOfRangeException(nameof(quantity));
            Quantity = quantity;
        }
        public bool TryReserve()
        {
            if (InFlight || IsComplete) return false;
            InFlight = true;
            return true;
        }
        public void CancelReservation() => InFlight = false;
        public bool Receive()
        {
            if (!InFlight || IsComplete) return false;
            InFlight = false;
            Delivered++;
            return true;
        }
        public bool TrySettle()
        {
            if (!IsComplete || IsSettled) return false;
            IsSettled = true;
            return true;
        }
    }

    public static class OrderQuantities
    {
        public static readonly System.Collections.Generic.IReadOnlyList<int> DiningWeights = Array.AsReadOnly(new[] { 50, 30, 15, 5 });
        public static readonly System.Collections.Generic.IReadOnlyList<int> DriveWeights = Array.AsReadOnly(new[] { 60, 40 });
        public static int Dining() => DiningForRoll(UnityEngine.Random.Range(0, 100));
        public static int Drive() => DriveForRoll(UnityEngine.Random.Range(0, 100));
        public static int DiningForRoll(int roll) => Pick(roll, DiningWeights);
        public static int DriveForRoll(int roll) => Pick(roll, DriveWeights);
        static int Pick(int roll, System.Collections.Generic.IReadOnlyList<int> weights)
        {
            if (roll < 0 || roll >= 100) throw new ArgumentOutOfRangeException(nameof(roll));
            int sum = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                sum += weights[i];
                if (roll < sum) return i + 1;
            }
            throw new InvalidOperationException("Order weights must total 100.");
        }
    }
}
