using System.Collections.Generic;
using BurgerShop.Customer;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    // Presentation only: legacy TextMesh values remain owned by the gameplay component.
    // Screen-space cards keep a consistent readable size, with ready orders placed first.
    [DefaultExecutionOrder(200)]
    public sealed class WorldLabelHud : MonoBehaviour
    {
        sealed class Entry
        {
            public TextMesh Source;
            public int Id;
            public Image Card, Icon;
            public Text Text;
            public bool Order, Critical;
            public CustomerAgent Customer;
            public float Distance,NextPulse;public string Previous;
        }
        readonly List<Entry> entries = new List<Entry>();
        readonly HashSet<int> known = new HashSet<int>();
        readonly List<Rect> occupied = new List<Rect>();
        RectTransform space;
        Transform player;
        float scan;
        public int VisibleCount { get; private set; }
        public static WorldLabelHud Build(Transform parent, Transform actor)
        {
            var root = new GameObject("WorldLabels",typeof(RectTransform));root.transform.SetParent(parent,false);
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            root.transform.SetAsFirstSibling();
            var hud=root.AddComponent<WorldLabelHud>();hud.space=rect;hud.player=actor;hud.Discover();return hud;
        }
        public void Discover()
        {
            foreach(var source in FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(source.name=="CourierRoadMark" || source.name=="WingBuildGuide" || source.name=="ShopRankCopy"
                    || source.name=="RestroomTrashLabel" || source.name=="CubicleStatus")continue;
                var renderer=source.GetComponent<Renderer>();if(renderer!=null)renderer.enabled=false;
                // Cash already has bill meshes, wallet amount and an aggregated pickup receipt.
                // A card per transient pile both mislabels money and obscures live orders.
                if(source.GetComponentInParent<CashPickup>()!=null)continue;
                string n=source.name;
                // Duplicate station headings / internal indicators are replaced by one authoritative card.
                if(n=="WorkerLabel"||n=="RawCount"||n=="BoxCount"||n=="ProcessCount"||n=="TrainingSign"||n=="DriveThruMark"||n=="StopSign"||n=="UpgradeMaxBadge"||n=="UpgradeLevelPop")continue;
                if(!known.Add(source.GetInstanceID()))continue;
                if(n.EndsWith("UnlockPadLabel"))
                {
                    var pad=source.transform.parent.Find(n.Substring(0,n.Length-5));
                    if(pad!=null)foreach(Transform child in pad)if(child.name.StartsWith("FacilityIcon"))foreach(var r in child.GetComponentsInChildren<Renderer>())r.enabled=false;
                }
                bool order=n=="OrderQuantity"||n=="BagOrderQuantity"||n=="CourierOrderQuantity";
                if(order || n=="CounterStockCount" || n=="ColaStockCount" || n=="PackageStockCount")
                    foreach(var r in source.transform.parent.GetComponentsInChildren<Renderer>(true))r.enabled=false;
                // Only customer and vehicle order icons remain in the world-space HUD.
                if(!order)continue;
                var card=HudChrome.Panel(transform,"Card_"+n,Vector2.zero,Vector2.one*.5f,Vector2.zero,new Vector2(240,80),HudChrome.Cream);
                var text=HudChrome.Label(card.transform,"Value",Vector2.zero,Vector2.one,Vector2.one*.5f,Vector2.zero,Vector2.zero,28,HudChrome.Ink,TextAnchor.MiddleLeft,true,false);
                text.rectTransform.offsetMin=new Vector2(64,8);text.rectTransform.offsetMax=new Vector2(-16,-8);
                text.horizontalOverflow=HorizontalWrapMode.Wrap;
                var icon=HudChrome.Icon(card.transform,"Item",FoodIcons.Get(ChooseIcon(n)),new Vector2(0,.5f),Vector2.one*.5f,new Vector2(32,0),new Vector2(40,40),Color.white);
                if(order)
                {
                    card.enabled=false;
                    icon.rectTransform.sizeDelta=new Vector2(32,32);
                    text.fontSize=26;
                    text.rectTransform.offsetMin=new Vector2(38,0);
                    text.rectTransform.offsetMax=Vector2.zero;
                    text.horizontalOverflow=HorizontalWrapMode.Overflow;
                    var outline=text.gameObject.AddComponent<Outline>();
                    outline.effectColor=HudChrome.Cream;outline.effectDistance=new Vector2(1,-1);
                }
                entries.Add(new Entry{Id=source.GetInstanceID(),Source=source,Card=card,Text=text,Icon=icon,Order=order,Customer=source.GetComponentInParent<CustomerAgent>()});
            }
        }
        static FoodIcon ChooseIcon(string n) => n.Contains("Cola")?FoodIcon.Cola:n=="BagMachineLabel"?FoodIcon.EmptyBag:n.Contains("Bag")?FoodIcon.Bagged: n.Contains("Box")||n.Contains("Package")||n.Contains("Window") ? FoodIcon.Box : n.Contains("Boost")||n.Contains("Hr")?FoodIcon.Speed:n.Contains("Trash")?FoodIcon.Clean:n.Contains("Unlock")||n.Contains("Buy")?FoodIcon.Lock:FoodIcon.Burger;
        void LateUpdate() => RefreshNow();
        public void RefreshNow()
        {
            if(space==null||player==null||Camera.main==null)return;
            if((scan-=Time.deltaTime)<=0){scan=.5f;Discover();}
            for(int i=entries.Count-1;i>=0;i--)
            {
                var e=entries[i];
                if(e.Source==null){known.Remove(e.Id);Destroy(e.Card.gameObject);entries.RemoveAt(i);continue;}
                e.Distance=ShopLayout.Horizontal(player.position,e.Source.transform.position);
                e.Critical=e.Order || e.Source.text.Contains("Remaining");
            }
            // List.Sort is unstable for equal priorities. With many orders it used to
            // reshuffle the same entries each frame, swapping their vertical avoidance slots.
            entries.Sort((a,b)=>{int priority=Priority(b).CompareTo(Priority(a));return priority!=0?priority:a.Id.CompareTo(b.Id);});
            occupied.Clear();VisibleCount=0;
            foreach(var e in entries) Paint(e);
        }
        static int Priority(Entry e) => e.Customer!=null&&e.Customer.QueueIndex==0?10000:e.Critical?5000:(int)(1000-e.Distance*10);
        void Paint(Entry e)
        {
            bool visible=e.Source.gameObject.activeInHierarchy && !string.IsNullOrEmpty(e.Source.text);
            if(e.Customer!=null && (e.Customer.IsDeparting || !e.Customer.HasOrdered)) visible=false;
            Vector3 screen=Camera.main.WorldToScreenPoint(e.Source.transform.position);
            if(screen.z<=0)visible=false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(space,screen,GetComponentInParent<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay?null:GetComponentInParent<Canvas>().worldCamera,out var local);
            local+=space.rect.size*.5f;
            string copy=e.Source.text;
            bool detailed=e.Distance<=3.2f;
            if(e.Source.name=="BoxingLabel")
            {
                var box=e.Source.GetComponentInParent<BoxingStation>();
                if(box!=null){copy=$"RAW {box.InputCount}/{BoxingStation.InputCapacity}  BOX {box.OutputCount}/{BoxingStation.OutputCapacity}"; detailed=ShopLayout.Horizontal(player.position,box.CirclePosition)<=BoxingStation.WorkRadius+2;}
            }
            if(!detailed&&!e.Critical)copy="";
            if(!detailed&&e.Source.name=="UpgradeMarker")visible=false;
            if(e.Source.name=="CourierOrderQuantity")e.Icon.sprite=FoodIcons.Get(FoodIcon.RedParcel);
            else if(e.Source.name=="BagOrderQuantity")e.Icon.sprite=FoodIcons.Get(FoodIcon.Bagged);
            else if(e.Customer!=null && !e.Customer.CanAcceptOrder)e.Icon.sprite=FoodIcons.Get(FoodIcon.Phone);
            else if(e.Customer!=null&&e.Customer.Product==KitchenProduct.Cola)e.Icon.sprite=FoodIcons.Get(FoodIcon.Cola);
            else if(e.Order)e.Icon.sprite=FoodIcons.Get(e.Source.transform.parent.name=="CarOrderBubble"?FoodIcon.Box:FoodIcon.Burger);
            if(e.Order)
            {
                copy=e.Customer!=null&&!e.Customer.CanAcceptOrder
                    ? Mathf.CeilToInt(e.Customer.CallingRemaining)+"s"
                    : copy.StartsWith("x")?copy.Substring(1):copy;
            }
            var size=e.Order?new Vector2(92,40):string.IsNullOrEmpty(copy)?new Vector2(56,56):new Vector2(e.Source.name=="BoxingLabel"?360:256,copy.Contains("\n")?104:72);
            // Never put world labels over the top HUD or fixed joystick.
            if(local.y>space.rect.height-328||local.y<64||local.x<0||local.x>space.rect.width)visible=false;
            local.x=Mathf.Clamp(local.x,size.x/2+16,space.rect.width-size.x/2-16);
            var rect=new Rect(local-size/2,size);
            for(int attempts=0;attempts<5 && Overlaps(rect);attempts++){local.y-=size.y+8;rect.position=local-size/2;}
            if(Overlaps(rect)||rect.Overlaps(HudChrome.JoystickKeepout()))visible=false;
            e.Card.gameObject.SetActive(visible);
            if(!visible)return;
            occupied.Add(rect);VisibleCount++;
            e.Card.rectTransform.anchoredPosition=local;e.Card.rectTransform.sizeDelta=size;
            e.Icon.rectTransform.anchoredPosition=new Vector2(e.Order?18:string.IsNullOrEmpty(copy)?28:32,0);
            if(e.Previous!=null && e.Previous!=copy && Time.time>=e.NextPulse && (e.Order || e.Source.name.Contains("StockCount") || e.Source.name=="BoxingLabel"))
            { UiPressPulse.Pulse(e.Icon.transform,.06f,.12f); e.NextPulse=Time.time+.15f; }
            e.Previous=copy;e.Text.text=copy;
        }
        bool Overlaps(Rect rect){foreach(var other in occupied)if(rect.Overlaps(other))return true;return false;}
    }
}
