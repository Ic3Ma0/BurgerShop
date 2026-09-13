using System.Collections.Generic;
using BurgerShop.Economy;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class CashFloor : MonoBehaviour
    {
        public const int CounterDrop = 10;
        public const int DiningDrop = 10;
        public const float PickupRadius = 0.85f;
        public const float PickupInterval = 0.04f;
        public static readonly Vector3 CounterOffsetFromServing = new Vector3(-1.5f, 0.04f, 0.4f);
        public static readonly Vector3 TableOffsetFromCenter = new Vector3(1.15f, 0.04f, 0.35f);

        public const float MaxStackHeight=4f;
        readonly Dictionary<CashPickup,Vector3> origins=new Dictionary<CashPickup,Vector3>();
        readonly List<CashPickup> piles = new List<CashPickup>();
        RestaurantWallet wallet;
        Transform collector;
        Vector3 counterOrigin;
        float cooldown;

        public int PileCount => piles.Count;
        public int GroundValue
        {
            get
            {
                int total = 0;
                for (int i = 0; i < piles.Count; i++)
                    if (piles[i] != null) total += piles[i].Value;
                return total;
            }
        }
        public Vector3 CounterOrigin => counterOrigin;
        public Vector3 NearestPilePosition
        {
            get
            {
                CashPickup pile = NearestAvailable();
                return pile != null ? pile.transform.position : counterOrigin;
            }
        }

        public void Configure(RestaurantWallet earnings, Transform player, Vector3 dropOrigin)
        {
            wallet = earnings;
            collector = player;
            counterOrigin = dropOrigin;
            cooldown = 0f;
        }

        public static Vector3 CounterDropPosition(Vector3 servingPosition)
        {
            return servingPosition + CounterOffsetFromServing;
        }

        public static Vector3 TableDropPosition(DiningTable table)
        {
            return table != null ? table.Center + TableOffsetFromCenter : Vector3.zero;
        }

        public CashPickup DropAtCounter(int amount = CounterDrop)
        {
            return DropAt(counterOrigin, amount);
        }

        public CashPickup DropAtTable(DiningTable table, int amount = DiningDrop)
        {
            return table == null ? null : DropAt(TableDropPosition(table), amount);
        }

        public CashPickup DropAt(Vector3 origin, int amount)
        {
            if (amount <= 0) return null;
            amount = GetComponent<UI.SessionGoalTracker>()?.AddIncomeBonus(amount) ?? amount;
            int index = CountNear(origin);
            Vector3 slot = origin;
            CashPickup pile = CashPickup.Create(transform, slot, amount, index);
            piles.Add(pile);origins[pile]=origin;Restack();
            return pile;
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            while(deltaTime>0.00001f)
            {
                float dt=Mathf.Min(.02f,deltaTime);deltaTime-=dt;
                TickFlights(dt);
                if(collector==null||!collector.gameObject.activeInHierarchy)continue;
                cooldown=Mathf.Max(0,cooldown-dt);
                if(cooldown<=0&&TryBeginPickup(out _))cooldown=PickupInterval;
            }
        }

        public bool TryBeginPickup(out TrashMotion motion)
        {
            motion = null;
            CashPickup pile = NearestInRange();
            if (pile == null || pile.IsCollecting || wallet == null || !wallet.CanCollectCoins(pile.Value))
                return false;
            long reserved=0;
            foreach(var pending in piles)if(pending!=null&&pending.IsCollecting)reserved+=pending.Value;
            if(wallet.Coins>long.MaxValue-pile.Value-reserved)return false;
            CashPickup captured = pile;
            Vector3 pickupOrigin=pile.transform.position;
            captured.LaunchTo(collector, () => FinishCollect(captured,pickupOrigin));
            Restack();
            motion = captured.Motion;
            return motion != null;
        }

        void FinishCollect(CashPickup pile,Vector3 pickupOrigin)
        {
            piles.Remove(pile);origins.Remove(pile);
            int value = pile != null ? pile.Value : 0;
            Vector3 position=pickupOrigin;
            if (pile != null) BurgerVisual.Release(pile.gameObject);
            if (value > 0 && wallet != null && wallet.CollectCoins(value))
                UI.FeedbackDirector.Current?.Cash(position,value);
        }

        CashPickup NearestInRange()
        {
            CashPickup nearest = NearestAvailable();
            if (nearest == null || collector == null) return null;
            Vector3 offset = collector.position - nearest.transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= PickupRadius * PickupRadius ? nearest : null;
        }

        CashPickup NearestAvailable()
        {
            CashPickup nearest = null;
            float best = float.MaxValue;
            if (collector == null) return null;
            for (int i = 0; i < piles.Count; i++)
            {
                CashPickup pile = piles[i];
                if (pile == null || pile.IsCollecting) continue;
                Vector3 offset = collector.position - pile.transform.position;
                offset.y = 0f;
                float sqr = offset.sqrMagnitude;
                if (sqr > best || sqr==best&&nearest!=null&&pile.transform.position.y<nearest.transform.position.y) continue;
                best = sqr;
                nearest = pile;
            }
            return nearest;
        }

        void Restack()
        {
            // Each pickup retains its exact monetary value; only its display height changes.
            var totals=new Dictionary<Vector3,float>();var heights=new Dictionary<Vector3,float>();
            foreach(var pile in piles)if(pile!=null&&!pile.IsCollecting&&origins.TryGetValue(pile,out var origin))
                totals[origin]=(totals.TryGetValue(origin,out var sum)?sum:0)+pile.StackHeight;
            foreach(var pile in piles)
            {
                if(pile==null||pile.IsCollecting||!origins.TryGetValue(pile,out var origin))continue;
                float compression=Mathf.Min(1,MaxStackHeight/totals[origin]);
                float height=heights.TryGetValue(origin,out var current)?current:0;
                pile.transform.position=origin+Vector3.up*height;
                pile.Visual.localScale=new Vector3(1,pile.StackHeight/CashPickup.BaseStackHeight*compression,1);
                heights[origin]=height+pile.StackHeight*compression;
            }
        }
        int CountNear(Vector3 origin)
        {
            int count = 0;
            for (int i = 0; i < piles.Count; i++)
            {
                if (piles[i] == null) continue;
                Vector3 offset = piles[i].transform.position - origin;
                offset.y = 0f;
                if (offset.sqrMagnitude <= 1.4f * 1.4f) count++;
            }
            return count;
        }

        static Vector3 GridOffset(int index)
        {
            int col = index % 2;
            int row = index / 2;
            return new Vector3(-col * 0.50f, 0f, row * 0.36f);
        }

        int FlyingCount()
        {
            int count = 0;
            for (int i = 0; i < piles.Count; i++)
                if (piles[i] != null && piles[i].IsCollecting) count++;
            return count;
        }

        void TickFlights(float deltaTime)
        {
            for (int i = piles.Count - 1; i >= 0; i--)
                piles[i]?.Advance(deltaTime);
        }
    }
}
