using System.Collections.Generic;
using BurgerShop.Economy;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public interface IInvestZone
    {
        bool IsAvailable { get; }
        bool IsInRange { get; }
        float DistanceSquared { get; }
        bool IsSelected { get; }
        RestaurantWallet Wallet { get; }
        BurgerInventory Player { get; }
    }

    public static class InvestZoneRegistry
    {
        static readonly List<IInvestZone> zones = new List<IInvestZone>();

        static bool Alive(IInvestZone zone) => zone != null && !(zone is Object obj && obj == null);

        public static void Register(IInvestZone zone)
        {
            if (Alive(zone) && !zones.Contains(zone)) zones.Add(zone);
        }

        public static void Unregister(IInvestZone zone) => zones.Remove(zone);

        public static IInvestZone Nearest(IInvestZone self)
        {
            if (!Alive(self)) return null;
            IInvestZone best = null;
            float distance = float.MaxValue;
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                IInvestZone zone = zones[i];
                if (!Alive(zone))
                {
                    zones.RemoveAt(i);
                    continue;
                }
                if (zone.Wallet != self.Wallet || zone.Player != self.Player) continue;
                if (!zone.IsAvailable || !zone.IsInRange) continue;
                float next = zone.DistanceSquared;
                if (next < distance - 0.0001f || (Mathf.Abs(next - distance) <= 0.0001f && zone.IsSelected))
                {
                    best = zone;
                    distance = next;
                }
            }
            return best;
        }
    }
}
