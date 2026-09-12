using BurgerShop.Player;
using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.UI;
namespace BurgerShop.UI
{
    public sealed class CarryHud : MonoBehaviour
    {
        BurgerInventory inventory; TrashInventory trash; BurgerPickupZone pickup; Text label;
        CanvasGroup group; Image card; float emptyTime;
        Image foodIcon,trashIcon; float scan; CounterDropZone[] drops; BoxingStation boxing;
        public void Configure(BurgerInventory carrier, BurgerPickupZone zone, Text text, GrillUpgradeZone upgradeZone=null, WorkerHiringZone hiringZone=null, TrashInventory trashBag=null)
        {
            inventory=carrier;pickup=zone;trash=trashBag;label=text;
            group=gameObject.GetComponent<CanvasGroup>(); if(group==null)group=gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts=false;group.interactable=false;group.alpha=0;
            HudChrome.Style(label,28,HudChrome.Ink,TextAnchor.MiddleCenter,true,false);
            var r=label.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.pivot=Vector2.one*.5f;r.sizeDelta=new Vector2(376,80);
            card=HudChrome.Panel(transform.parent,"CarryCard",new Vector2(.5f,0),Vector2.one*.5f,Vector2.zero,new Vector2(392,88),HudChrome.Cream);
            card.transform.SetSiblingIndex(transform.GetSiblingIndex());
            foodIcon=FoodIcons.Add(transform,FoodIcon.Burger,new Vector2(-164,0),40);
            trashIcon=FoodIcons.Add(transform,FoodIcon.Clean,new Vector2(-164,-20),32);
            Refresh(0);
        }
        void LateUpdate()=>Refresh(Time.deltaTime);
        public void Refresh(float dt)
        {
            if(inventory==null||label==null)return;
            bool near=pickup!=null&&pickup.IsInRange;
            if((scan-=dt)<=0 || drops==null){scan=.5f;drops=FindObjectsByType<CounterDropZone>(FindObjectsSortMode.None);boxing=FindFirstObjectByType<BoxingStation>();}
            foreach(var drop in drops)if(drop!=null)near|=drop.IsInRangeOf(inventory.transform);
            if(boxing!=null)near|=boxing.IsActorInRange(inventory.transform);
            bool show=inventory.Count>0||near||(trash!=null&&trash.Count>0);
            emptyTime=show?0:emptyTime+dt;group.alpha=show?1:Mathf.Clamp01(1-(emptyTime-1)*5);
            // Empty on initialization should not flash an unsolicited badge.
            if(dt==0&&!show)group.alpha=0;
            string mix=inventory.BoxedCount>0?$"  BOX {inventory.BoxedCount}":"";
            mix += inventory.EmptyBagCount>0?$"  BAGS {inventory.EmptyBagCount}":"";
            mix += inventory.BaggedCount>0?$"  PACKED {inventory.BaggedCount}":"";
            string waste=trash!=null&&trash.Count>0?$"\nTRASH {trash.Count}":"";
            foodIcon.sprite=FoodIcons.Get(inventory.BoxedCount==inventory.Count&&inventory.Count>0?FoodIcon.Box:FoodIcon.Burger);
            trashIcon.enabled=trash!=null&&trash.Count>0;
            foodIcon.rectTransform.anchoredPosition=new Vector2(-164,trashIcon.enabled?20:0);
            label.text=$"{inventory.Count}/{inventory.Capacity}{mix}"+(inventory.Count>inventory.Capacity?"\nUnload to collect":inventory.IsFull?"  FULL":"")+waste;
            if(Camera.main!=null)
            {
                Vector3 p=Camera.main.WorldToScreenPoint(inventory.transform.position+Vector3.up*.2f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent,p,GetComponentInParent<Canvas>()!=null && GetComponentInParent<Canvas>().renderMode!=RenderMode.ScreenSpaceOverlay?GetComponentInParent<Canvas>().worldCamera:null,out var local);
                label.rectTransform.anchoredPosition=new Vector2(local.x,local.y+((RectTransform)transform.parent).rect.height*.5f-80);
            }
            card.rectTransform.anchoredPosition=label.rectTransform.anchoredPosition;
            Color c=HudChrome.Cream;c.a=group.alpha;card.color=c;
        }
    }
}
