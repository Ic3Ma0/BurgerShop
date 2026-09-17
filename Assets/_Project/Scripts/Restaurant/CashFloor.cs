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
        public const float PickupInterval = CashCollectionFeel.Interval;
        public static readonly Vector3 CounterOffsetFromServing = new Vector3(-1.5f, 0.04f, 0.4f);
        public static readonly Vector3 TableOffsetFromCenter = new Vector3(1.15f, 0.04f, 0.35f);

        public const float MaxStackHeight=4f;
        readonly Dictionary<CashPickup,Vector3> origins=new Dictionary<CashPickup,Vector3>();
        readonly List<CashPickup> piles = new List<CashPickup>();
        readonly Dictionary<Vector3,float> stackCompression=new Dictionary<Vector3,float>();
        readonly Dictionary<Vector3,float> totals=new Dictionary<Vector3,float>();
        readonly Dictionary<Vector3,float> heights=new Dictionary<Vector3,float>();
        RestaurantWallet wallet;
        Transform collector;
        Vector3 counterOrigin;
        float cooldown;
        bool streaming;
        Vector3 streamOrigin;
        int packetValue;

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

        // Cash is shared by every counter: tick it once, not once per serving zone.
        void Update()=>Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            while(deltaTime>0.00001f)
            {
                float dt=Mathf.Min(.02f,deltaTime);deltaTime-=dt;
                TickFlights(dt);
                if(collector==null||!collector.gameObject.activeInHierarchy)continue;
                cooldown-=dt;
                if(cooldown<=0)
                {
                    if(TryBeginPickup(out _))cooldown+=PickupInterval;
                    else {cooldown=0;streaming=false;}
                }
            }
        }

        public bool TryBeginPickup(out TrashMotion motion)
        {
            motion = null;
            CashPickup pile = NearestInRange();
            if (pile == null || wallet == null)
                return false;
            Vector3 origin=origins[pile];
            long available=0, capacity=long.MaxValue-wallet.Coins;
            foreach(var pending in piles)
            {
                if(pending==null)continue;
                if(pending.IsCollecting)capacity=System.Math.Max(0,capacity-pending.Value);
                else if(origins[pending]==origin)available+=pending.Value;
            }
            if(capacity<=0)return false;
            if(!streaming||streamOrigin!=origin)
            {
                streaming=true;streamOrigin=origin;packetValue=CashCollectionFeel.PacketValue(available);
            }
            int value=(int)System.Math.Min(System.Math.Min(available,packetValue),capacity);
            Vector3 pickupOrigin=pile.transform.position+Vector3.up*(pile.Visual.localScale.y*CashPickup.BaseStackHeight);
            CashPickup captured;
            if(pile.Value>value)
            {
                // Split without DropAt: the income bonus was already applied when this money was earned.
                pile.SetValue(pile.Value-value);
                captured=CashPickup.CreatePacket(transform,pickupOrigin,value);
                piles.Add(captured);origins[captured]=origin;
            }
            else
            {
                captured=pile;
                int remaining=value-pile.Value;
                // Combine small sales into the same pulse, so hundreds of sales do not take minutes to collect.
                for(int i=piles.Count-1;i>=0&&remaining>0;i--)
                {
                    var other=piles[i];
                    if(other==null||other==captured||other.IsCollecting||origins[other]!=origin)continue;
                    int take=Mathf.Min(remaining,other.Value);remaining-=take;
                    other.SetValue(other.Value-take);
                    if(other.Value==0){piles.RemoveAt(i);origins.Remove(other);BurgerVisual.Release(other.gameObject);}
                }
                captured.SetValue(value);
                captured.transform.position=pickupOrigin;
            }
            captured.LaunchTo(collector, () => FinishCollect(captured,pickupOrigin));
            Restack();
            motion = captured.Motion;
            return motion != null;
        }

        void FinishCollect(CashPickup pile,Vector3 pickupOrigin)
        {
            Vector3 origin=origins[pile];
            piles.Remove(pile);origins.Remove(pile);
            int value = pile != null ? pile.Value : 0;
            if (pile != null) BurgerVisual.Release(pile.gameObject);
            if (value > 0 && wallet != null && wallet.CollectCoins(value))
                UI.FeedbackDirector.Current?.Cash(collector!=null?collector.TransformPoint(CashCollectionFeel.ReceiverOffset):pickupOrigin,value);
            else if(value>0)
            {
                // Another award may have filled the wallet during flight. Keep the exact uncredited amount.
                var returned=CashPickup.Create(transform,origin,value,CountNear(origin));
                piles.Add(returned);origins[returned]=origin;Restack();
            }
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
            totals.Clear();heights.Clear();
            foreach(var pile in piles)if(pile!=null&&!pile.IsCollecting&&origins.TryGetValue(pile,out var origin))
                totals[origin]=(totals.TryGetValue(origin,out var sum)?sum:0)+pile.StackHeight;
            foreach(var pile in piles)
            {
                if(pile==null||pile.IsCollecting||!origins.TryGetValue(pile,out var origin))continue;
                float compression=Mathf.Min(1,MaxStackHeight/totals[origin]);
                if(stackCompression.TryGetValue(origin,out var previous))compression=Mathf.Min(compression,previous);
                stackCompression[origin]=compression;
                float height=heights.TryGetValue(origin,out var current)?current:0;
                pile.transform.position=origin+Vector3.up*height;
                pile.Visual.localScale=new Vector3(1,pile.StackHeight/CashPickup.BaseStackHeight*compression,1);
                heights[origin]=height+pile.StackHeight*compression;
            }
            // A new stack at an empty location starts at its normal height.
            foreach(var origin in origins.Values)
                if(!totals.ContainsKey(origin))stackCompression.Remove(origin);
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

        void OnDestroy()
        {
            // TrashMotion detaches flights; they still belong to this cash floor's scene lifetime.
            foreach(var pile in piles)
                if(pile!=null&&pile.IsCollecting)BurgerVisual.Release(pile.gameObject);
        }
    }
}
