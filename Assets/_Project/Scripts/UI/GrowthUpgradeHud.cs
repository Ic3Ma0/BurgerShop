using BurgerShop.Economy;
using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class GrowthUpgradeHud : MonoBehaviour
    {
        GrowthUpgrades growth; SessionGoalTracker goals; RestaurantWallet wallet;
        Image panel, currentIcon, nextIcon; Text body, buyLabel; Button buy;
        GrowthUpgrades.Offer selected;
        bool rankOpen, readyShown, initialized;
        int previewRank, previewLevel;
        string dismissed;
        float nextClick;
        public static void Build(Transform parent,GrowthUpgrades upgrades,SessionGoalTracker tracker,RestaurantWallet earnings)
        {
            var obj=new GameObject("GrowthUpgradeHud",typeof(RectTransform));obj.transform.SetParent(parent,false);
            var ui=obj.AddComponent<GrowthUpgradeHud>();ui.growth=upgrades;ui.goals=tracker;ui.wallet=earnings;
            ui.panel=HudChrome.Panel(parent,"FacilityUpgradePanel",new Vector2(1,0),new Vector2(1,0),new Vector2(-32,190),new Vector2(600,352),HudChrome.Cream);ui.panel.raycastTarget=true;
            ui.body=HudChrome.Label(ui.panel.transform,"Preview",Vector2.zero,Vector2.one,Vector2.one*.5f,new Vector2(0,48),new Vector2(-40,-120),28,HudChrome.Ink,TextAnchor.UpperLeft,true,false);
            ui.currentIcon=FoodIcons.Add(ui.panel.transform,FoodIcon.Burger,new Vector2(-160,-55),58);
            ui.nextIcon=FoodIcons.Add(ui.panel.transform,FoodIcon.Burger,new Vector2(-70,-55),58);
            ui.buy=ui.Button(ui.panel.transform,"Buy",new Vector2(1,0),new Vector2(1,0),new Vector2(-20,18),new Vector2(310,90),out ui.buyLabel);
            ui.buy.onClick.AddListener(ui.Purchase);
            var close=ui.Button(ui.panel.transform,"Close",Vector2.one,Vector2.one,new Vector2(-8,-8),new Vector2(88,72),out var closeText);closeText.text="X";
            close.onClick.AddListener(()=>{ui.rankOpen=false;ui.dismissed=ui.selected?.Id;ui.panel.gameObject.SetActive(false);});
            var star=FindFirstObjectByType<StarProgressHud>();
            if(star!=null)
            {
                var image=star.GetComponent<Image>();image.raycastTarget=true;
                var button=star.gameObject.AddComponent<Button>();button.targetGraphic=image;
                button.onClick.AddListener(()=>{ui.rankOpen=true;ui.previewRank=tracker.Rank;});
            }
            ui.panel.gameObject.SetActive(false);
        }
        Button Button(Transform parent,string name,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size,out Text text)
        {
            var image=HudChrome.Panel(parent,name,anchor,pivot,position,size,HudChrome.Gold);image.raycastTarget=true;
            text=HudChrome.Label(image.transform,"Text",Vector2.zero,Vector2.one,Vector2.one*.5f,Vector2.zero,Vector2.zero,28,HudChrome.Ink,TextAnchor.MiddleCenter,true,false);
            var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;image.gameObject.AddComponent<UiPressPulse>();return b;
        }
        void LateUpdate()
        {
            bool ready=!goals.IsMaxRank&&goals.Stars>=goals.StarCap;
            if(initialized&&ready&&!readyShown)FeedbackDirector.Current?.World(growth.Player.transform.position,"Stars ready!",.9f);
            initialized=true;readyShown=ready;
            if(rankOpen)
            {
                panel.gameObject.SetActive(true);currentIcon.enabled=nextIcon.enabled=false;
                string[] unlocks={"Boxing table","Extra grill","Extra counter","Drive-thru","West expansion"};
                body.text=goals.IsMaxRank?$"Lv.6 MAX\nStored stars: {goals.Stars}":$"Shop Lv.{goals.Rank} → {goals.Rank+1}\nUnlock: {unlocks[goals.Rank-1]}\nStars: {goals.Stars}/{goals.StarCap}";
                bool expand=goals.IsMaxRank&&BagLine.Current!=null&&!BagLine.Current.Expanded;
                if(expand)body.text+="\nWest area · Bag production & PICKUP\nExpansion is free";
                buy.interactable=expand||(ready&&previewRank==goals.Rank);buyLabel.text=expand?"Expand":goals.IsMaxRank?"MAX":ready?"Upgrade":"Need more stars";
                return;
            }
            selected=null;float best=.9f;
            foreach(var offer in growth.Offers)
            {
                if(offer.Target==null||!offer.Target.gameObject.activeInHierarchy)continue;
                float distance=ShopLayout.Horizontal(growth.Player.transform.position,offer.Position);
                if(distance<best){best=distance;selected=offer;}
            }
            if(selected==null)dismissed=null;
            panel.gameObject.SetActive(selected!=null&&selected.Id!=dismissed);if(selected==null||selected.Id==dismissed)return;
            int level=growth.Level(selected.Id);previewLevel=level;bool max=level>selected.Costs.Length;
            long need=max?0:System.Math.Max(0,selected.Costs[level-1]-wallet.Coins);
            body.text=$"{selected.Title} · Lv.{level}\n{selected.Benefit(level)}"+(max?"\nMAX":$"\n→ {selected.Benefit(level+1)}\n+2 Stars");
            buy.interactable=!max&&need==0;buyLabel.text=max?"MAX":need>0?$"Need {need} more":$"Upgrade · {selected.Costs[level-1]}";
            currentIcon.enabled=nextIcon.enabled=true;
            bool table=selected.Id.StartsWith("table-");
            currentIcon.sprite=FoodIcons.Get(table?(FoodIcon)((int)FoodIcon.Chair1+level-1):FoodIcon.Burger);
            nextIcon.sprite=FoodIcons.Get(table?(FoodIcon)((int)FoodIcon.Chair1+Mathf.Min(3,level)):FoodIcon.Check);
        }
        void Purchase()
        {
            if(Time.unscaledTime<nextClick)return;nextClick=Time.unscaledTime+.25f;
            if(rankOpen){if(goals.IsMaxRank){if(BagLine.Current!=null&&BagLine.Current.TryExpand())rankOpen=false;}else if(goals.TryUpgradeRank(previewRank))rankOpen=false;return;}
            if(selected!=null&&ShopLayout.Horizontal(growth.Player.transform.position,selected.Position)<=.9f)growth.TryBuy(selected.Id,previewLevel);
        }
    }
}
