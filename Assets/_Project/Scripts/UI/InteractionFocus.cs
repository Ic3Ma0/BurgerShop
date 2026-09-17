using System;
using System.Collections.Generic;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.UI;
namespace BurgerShop.UI
{
    public sealed class InteractionFocus : MonoBehaviour
    {
        sealed class Zone{public Vector3 Position;public Func<bool> Near,Active;public string Verb;}
        readonly List<Zone> zones=new List<Zone>();
        BurgerInventory player;Image card;Text label;LineRenderer ring;float scan;
        public bool IsWorking {get;private set;}
        public string Verb => label!=null?label.text:"";
        public static InteractionFocus Build(Transform parent,BurgerInventory carrier)
        {
            // BS-SPEC-061: Serve/Waiting/Stock card blocked the character. Gameplay zones stay; the HUD hint does not.
            return null;
        }
        void Discover()
        {
            zones.Clear();
            foreach(var p in FindObjectsByType<BurgerPickupZone>(FindObjectsSortMode.None))
                zones.Add(new Zone{Position=p.PickupPosition,Near=()=>p!=null&&p.IsInRange,Active=()=>p!=null&&p.CanCollect,Verb="Collect"});
            foreach(var d in FindObjectsByType<CounterDropZone>(FindObjectsSortMode.None))
                zones.Add(new Zone{Position=d.DropPosition,Near=()=>d!=null&&d.IsInRangeOf(player.transform),Active=()=>d!=null&&(d.AcceptsBoxes?player.BoxedCount:player.LooseCount)>0,Verb="Stock"});
            foreach(var s in FindObjectsByType<BurgerServingZone>(FindObjectsSortMode.None))
                zones.Add(new Zone{Position=s.ServingPosition,Near=()=>s!=null&&s.IsActorInRange(player.transform),Active=()=>s!=null&&(s.ReadyToSell||s.IsHandoffActive),Verb="Serve"});
            foreach(var b in FindObjectsByType<BoxingStation>(FindObjectsSortMode.None))
                zones.Add(new Zone{Position=b.CirclePosition,Near=()=>b!=null&&b.IsActorInRange(player.transform),Active=()=>b!=null&&(b.ProcessingCount>0||b.PendingTransfers>0),Verb="Packing"});
            foreach(var l in FindObjectsByType<DriveThruLane>(FindObjectsSortMode.None))
                zones.Add(new Zone{Position=l.WindowPosition,Near=()=>l!=null&&l.IsActorInRange(player.transform),Active=()=>l!=null&&(l.ReadyToSell||l.IsHandoffActive),Verb="Serve"});
        }
        void LateUpdate()
        {
            if(card==null||label==null||player==null)return;
            if((scan-=Time.deltaTime)<=0){scan=.5f;Discover();}
            Zone best=null;float distance=float.MaxValue;
            foreach(var z in zones)if(z.Near()){float d=ShopLayout.Horizontal(z.Position,player.transform.position);if(d<distance || (best!=null&&!best.Active()&&z.Active())){best=z;distance=d;}}
            IsWorking=best!=null&&best.Active();card.enabled=best!=null;label.enabled=best!=null;ring.enabled=IsWorking;
            if(best==null)return;
            label.text=IsWorking?best.Verb:player.IsFull?"FULL":"Waiting";label.color=IsWorking?HudChrome.Green:HudChrome.Ink;
            if(IsWorking)for(int i=0;i<49;i++){float a=i*Mathf.PI*2/48;ring.SetPosition(i,best.Position+new Vector3(Mathf.Cos(a)*.8f,.04f,Mathf.Sin(a)*.8f));}
        }
        void OnDestroy(){if(ring!=null){Restaurant.BurgerVisual.Release(ring.sharedMaterial);Restaurant.BurgerVisual.Release(ring.gameObject);}}
    }
}
