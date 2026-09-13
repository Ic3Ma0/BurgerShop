using BurgerShop.Persistence;
using UnityEngine;
using UnityEngine.UI;
namespace BurgerShop.UI
{
    public sealed class OfflineSettleHud : MonoBehaviour
    {
        RestaurantPersistence persistence;Text story;Button close;
        public static void Build(Transform parent,RestaurantPersistence source)
        {
            var root=new GameObject("OfflineSettlement",typeof(RectTransform));root.transform.SetParent(parent,false);
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var hud=root.AddComponent<OfflineSettleHud>();hud.persistence=source;
            var overlay=root.AddComponent<Image>();overlay.color=new Color(0,0,0,.4f);
            var panel=HudChrome.Panel(root.transform,"AfterHoursCard",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(650,400),HudChrome.Cream);
            HudChrome.Label(panel.transform,"Title",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-28),new Vector2(600,60),32,HudChrome.Ink,TextAnchor.MiddleCenter,true,true).text="Your team kept the shop open";
            hud.story=HudChrome.Label(panel.transform,"Story",Vector2.one*.5f,Vector2.one*.5f,Vector2.one*.5f,new Vector2(0,10),new Vector2(580,190),24,HudChrome.Ink,TextAnchor.MiddleCenter,true,true);
            var button=HudChrome.Panel(panel.transform,"Continue",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,48),new Vector2(280,65),HudChrome.Green);
            button.raycastTarget=true; // HudChrome panels are decorative by default; this one is interactive.
            hud.close=button.gameObject.AddComponent<Button>();hud.close.targetGraphic=button;
            HudChrome.Label(button.transform,"Text",Vector2.zero,Vector2.one,Vector2.one*.5f,Vector2.zero,Vector2.zero,26,Color.white,TextAnchor.MiddleCenter,true,true).text="Continue";
            hud.close.onClick.AddListener(()=>{if(source.DismissOfflineReceipt())hud.Refresh();});
            hud.Refresh();
        }
        void LateUpdate()=>Refresh();
        // This component remains active even while the receipt is hidden.
        void Refresh()
        {
            if(persistence==null)return;
            bool visible=persistence.OfflineVisible;
            GetComponent<Image>().enabled=visible;
            foreach(Transform child in transform)child.gameObject.SetActive(visible);
            if(!visible)return;
            double minutes=System.TimeSpan.FromTicks(persistence.OfflineTicks).TotalMinutes;
            string duration=minutes>=60?$"{minutes/60:0.0} hours":$"{minutes:0.0} minutes";
            story.text=persistence.OfflineStaffCount==0?$"You were away for {duration}.\nHire an employee to earn while away.\n+0":$"While you were away for {duration},\nyour {persistence.OfflineStaffCount} employees minded the shop.\n+{persistence.OfflineGrant:N0} cash added\nUp to 8 hours of after-hours earnings.";
        }
    }
}
