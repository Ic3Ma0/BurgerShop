using System.Collections.Generic;
using System.Linq;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BurgerShop.Building
{
    public sealed class FacilityShopHud : MonoBehaviour
    {
        FacilityLayout layout;
        Transform panel,toolbar,preview,backdrop,content;
        Button buyTab,ownedTab,expansionTab,pickScene;
        Text catalogHint,browseHint;
        ScrollRect catalogScroll;
        bool ownedPage,expansionPage;
        readonly List<Button> expansionButtons=new List<Button>();
        int worldInputFrame=-1;
        bool awaitDesktopMotion;
        Vector2 initialPointer;
        Vector2 fittedSize;
        readonly List<Button> ownedButtons=new List<Button>();
        readonly List<Text> prices=new List<Text>();
        readonly List<Renderer> hiddenOriginals=new List<Renderer>();
        internal bool IsOwnedPage=>ownedPage;
        public bool IsOpen=>open;
        Text status;
        bool open,placing,dragging,desktopMoved;
        readonly List<RaycastResult> uiHits=new List<RaycastResult>();
        internal Vector3 PreviewPosition=>point;
        float previousTimeScale,yaw;
        string placementError="";
        Vector3 checkedPoint;float checkedYaw,nextCheck;int checkedRevision=-1;bool checkedValid;
        FacilityInstance checkedItem;string checkedError="";
        Vector3 point,rawPoint;
        float rawYaw;
        bool alignmentMayAdvance;
        readonly AlignmentHold alignment=new AlignmentHold();
        PlacementAlignment.Pose alignedPose;
        LineRenderer alignmentGuide;Material alignmentMaterial;
        internal bool IsAligned=>alignment.Active;
        internal float PreviewYaw=>yaw;
        CameraFollow follow;
        bool followWasEnabled;
        Material ghostMaterial;
        readonly List<Button> buyButtons=new List<Button>();
        readonly List<FacilityKind> kinds=new List<FacilityKind>();
        public static FacilityShopHud Build(Transform parent,FacilityLayout layout)
        {
            var root=new GameObject("FacilityShop",typeof(RectTransform)).transform;root.SetParent(parent,false);
            var rect=(RectTransform)root;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var hud=root.gameObject.AddComponent<FacilityShopHud>();hud.layout=layout;hud.BuildUi();return hud;
        }
        Button Button(Transform parent,string name,string text,Vector2 anchor,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction clicked)
        {
            var image=HudChrome.Panel(parent,name,anchor,anchor,pos,size,HudChrome.Cream);
            image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(clicked);
            HudChrome.Label(image.transform,"Text",Vector2.zero,Vector2.one,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,25,HudChrome.Ink,TextAnchor.MiddleCenter,true,true);
            var label=image.GetComponentInChildren<Text>();var r=label.rectTransform;r.offsetMin=new Vector2(12,6);r.offsetMax=new Vector2(-12,-6);label.text=text;
            return button;
        }
        void BuildUi()
        {
            var cart=Button(transform,"ShoppingCart","",Vector2.one,new Vector2(-452,-24),new Vector2(96,96),Open);
            // Code-native cart pictogram, legible without relying on an emoji font.
            foreach(var line in new[]{new Vector4(0,10,44,6),new Vector4(0,-10,38,6),new Vector4(-23,10,6,38),new Vector4(22,0,6,25),new Vector4(-31,29,20,6)})
                {var stroke=HudChrome.Panel(cart.transform,"CartStroke",Vector2.one*.5f,Vector2.one*.5f,new Vector2(line.x,line.y),new Vector2(line.z,line.w),HudChrome.Ink);stroke.sprite=null;stroke.type=Image.Type.Simple;}
            foreach(int x in new[]{-12,16}){var wheel=HudChrome.Panel(cart.transform,"Wheel",Vector2.one*.5f,Vector2.one*.5f,new Vector2(x,-27),new Vector2(10,10),HudChrome.Ink);wheel.sprite=HudChrome.Circle();}
            backdrop=HudChrome.Panel(transform,"CatalogBackdrop",Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero,new Color(.07f,.10f,.11f,.52f)).transform;
            var shade=(RectTransform)backdrop;shade.anchorMax=Vector2.one;shade.offsetMin=shade.offsetMax=Vector2.zero;
            backdrop.GetComponent<Image>().raycastTarget=true;
            panel=HudChrome.Panel(transform,"Catalog",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(920,1100),HudChrome.Cream).transform;
            HudChrome.Label(panel,"Title",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-24),new Vector2(800,48),34,HudChrome.Ink,TextAnchor.MiddleCenter,true,false).text="SUPERMARKET";
            buyTab=Button(panel,"BuyTab","Buy",new Vector2(.5f,1),new Vector2(-196,-86),new Vector2(380,64),()=>ShowCatalog(false));
            ownedTab=Button(panel,"MoveExisting","My facilities",new Vector2(.5f,1),new Vector2(196,-86),new Vector2(380,64),()=>ShowCatalog(true));
            expansionTab=Button(panel,"ExpansionsTab","Expansions",new Vector2(.5f,1),Vector2.zero,new Vector2(250,64),ShowExpansions);
            catalogHint=HudChrome.Label(panel,"CatalogHint",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-164),new Vector2(840,44),26,HudChrome.Ink,TextAnchor.MiddleCenter,false,false);
            var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D)).transform;viewport.SetParent(panel,false);
            var vr=(RectTransform)viewport;vr.anchorMin=Vector2.zero;vr.anchorMax=Vector2.one;vr.offsetMin=new Vector2(26,104);vr.offsetMax=new Vector2(-26,-308);
            viewport.GetComponent<Image>().color=new Color(1,1,1,0);
            content=new GameObject("Items",typeof(RectTransform)).transform;content.SetParent(viewport,false);
            var cr=(RectTransform)content;cr.anchorMin=new Vector2(0,1);cr.anchorMax=Vector2.one;cr.pivot=new Vector2(.5f,1);cr.sizeDelta=Vector2.zero;
            catalogScroll=viewport.gameObject.AddComponent<ScrollRect>();catalogScroll.viewport=vr;catalogScroll.content=cr;catalogScroll.horizontal=false;catalogScroll.movementType=ScrollRect.MovementType.Clamped;
            foreach(var offer in FacilityCatalog.Offers)
            {
                var kind=offer.Kind;
                var button=Card("Buy_"+kind,kind,offer.Name,"Buy",()=>Purchase(kind),out var price);
                buyButtons.Add(button);kinds.Add(kind);prices.Add(price);
            }
            string[] expansionNames={"Drinks lounge","Restroom","Takeaway workshop"};
            FacilityKind[] photos={FacilityKind.ColaMachine,FacilityKind.TrashBin,FacilityKind.BagMachine};
            for(int i=0;i<expansionNames.Length;i++)
            {
                int choice=i;
                var card=Card(i==1?"BuyRestroom":"Expand_"+i,photos[i],expansionNames[i],"Build",()=>PurchaseExpansion(choice),out _);
                if(i==1)card.transform.Find("PhotoBackground/ProductPhoto").GetComponent<Image>().sprite=FacilityThumbnails.Restroom;
                var description=HudChrome.Label(card.transform,"Description",new Vector2(0,1),new Vector2(0,1),new Vector2(0,1),new Vector2(18,-248),new Vector2(380,138),23,HudChrome.Ink,TextAnchor.UpperLeft,false,false);
                description.horizontalOverflow=HorizontalWrapMode.Wrap;
                expansionButtons.Add(card);
            }
            pickScene=Button(panel,"PickInScene","Select in restaurant",new Vector2(.5f,0),new Vector2(-180,20),new Vector2(340,64),SelectInScene);
            browseHint=HudChrome.Label(panel,"BrowseHint",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(-180,20),new Vector2(340,64),24,HudChrome.Ink,TextAnchor.MiddleCenter,false,false);
            browseHint.text="Swipe to browse";
            Button(panel,"Close","Close",new Vector2(.5f,0),new Vector2(260,20),new Vector2(240,64),Close);
            toolbar=HudChrome.Panel(transform,"PlacementControls",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,35),new Vector2(940,200),HudChrome.Cream).transform;
            status=HudChrome.Label(toolbar,"PlacementStatus",new Vector2(0,1),new Vector2(1,1),new Vector2(.5f,1),new Vector2(0,-12),new Vector2(900,70),24,HudChrome.Ink,TextAnchor.MiddleCenter,true,true);
            Button(toolbar,"RotateLeft","Rotate left",new Vector2(0,0),new Vector2(15,15),new Vector2(200,70),()=>Rotate(-FacilityCatalog.RotationStep));
            Button(toolbar,"RotateRight","Rotate right",new Vector2(0,0),new Vector2(230,15),new Vector2(200,70),()=>Rotate(FacilityCatalog.RotationStep));
            Button(toolbar,"Cancel","Cancel",new Vector2(0,0),new Vector2(465,15),new Vector2(170,70),CancelPlacement);
            Button(toolbar,"Done","Done",new Vector2(0,0),new Vector2(680,15),new Vector2(220,70),Done);
            panel.gameObject.SetActive(false);backdrop.gameObject.SetActive(false);toolbar.gameObject.SetActive(false);
        }
        public void Open()
        {
            if(open){Close();return;}
            open=true;previousTimeScale=Time.timeScale;Time.timeScale=0;
            follow=Camera.main!=null?Camera.main.GetComponent<CameraFollow>():null;
            if(follow!=null){followWasEnabled=follow.enabled;follow.enabled=false;}
            ShowCatalog(false);
        }
        Button Card(string name,FacilityKind kind,string title,string action,UnityEngine.Events.UnityAction clicked,out Text detail)
        {
            var card=Button(content,name,"",new Vector2(0,1),Vector2.zero,new Vector2(420,308),clicked);
            var rect=(RectTransform)card.transform;rect.pivot=new Vector2(0,1);
            var label=card.GetComponentInChildren<Text>();label.text=title;label.fontSize=27;label.horizontalOverflow=HorizontalWrapMode.Wrap;
            label.alignment=TextAnchor.UpperLeft;
            var lr=label.rectTransform;lr.anchorMin=lr.anchorMax=new Vector2(0,1);lr.pivot=new Vector2(0,1);lr.anchoredPosition=new Vector2(18,-186);lr.sizeDelta=new Vector2(380,64);
            var photoBackground=HudChrome.Panel(card.transform,"PhotoBackground",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-10),new Vector2(392,162),new Color(.89f,.91f,.87f));
            HudChrome.Icon(photoBackground.transform,"ProductPhoto",FacilityThumbnails.Get(kind),Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(350,154),Color.white);
            detail=HudChrome.Label(card.transform,"Detail",new Vector2(0,0),new Vector2(0,0),new Vector2(0,0),new Vector2(20,16),new Vector2(228,42),28,HudChrome.Green,TextAnchor.MiddleLeft,true,false);
            var pill=HudChrome.Panel(card.transform,"ActionPill",new Vector2(1,0),new Vector2(1,0),new Vector2(-16,16),new Vector2(118,44),HudChrome.Green);
            HudChrome.Label(pill.transform,"Action",Vector2.zero,Vector2.one,Vector2.one*.5f,Vector2.zero,Vector2.zero,25,Color.white,TextAnchor.MiddleCenter,true,false).text=action;
            return card;
        }
        void ShowCatalog(bool owned)
        {
            ownedPage=owned;expansionPage=false;layout.Discover();RefreshCatalog();
            panel.gameObject.SetActive(true);backdrop.gameObject.SetActive(true);toolbar.gameObject.SetActive(false);
            catalogScroll.verticalNormalizedPosition=1;
            FitCatalog();
        }
        void SelectInScene()
        {
            worldInputFrame=Time.frameCount;
            panel.gameObject.SetActive(false);backdrop.gameObject.SetActive(false);toolbar.gameObject.SetActive(true);
            status.text="Tap a facility to move it · Done to finish";
        }
        void RefreshCatalog()
        {
            layout.GetComponent<Core.RestaurantArchitecture>()?.Refresh();
            RefreshExpansions();

            foreach(var old in ownedButtons){old.gameObject.SetActive(false);BurgerVisual.Release(old.gameObject);}ownedButtons.Clear();
            for(int i=0;i<buyButtons.Count;i++)
            {
                var offer=FacilityCatalog.Get(kinds[i]);
                int rank=layout.GetComponent<SessionGoalTracker>()?.Rank??1;
                bool unlocked=layout.Unlocked(kinds[i]);
                bool upcoming=!unlocked&&offer.Rank==rank+1;
                bool visible=!ownedPage&&!expansionPage&&(unlocked||upcoming);
                buyButtons[i].gameObject.SetActive(visible);
                buyButtons[i].interactable=unlocked&&layout.Wallet.Coins>=layout.Price(kinds[i]);
                prices[i].text=unlocked?layout.Price(kinds[i]).ToString("N0"):$"Unlocks at Lv.{offer.Rank}";
                prices[i].color=unlocked?HudChrome.Green:HudChrome.Ink;
                var pill=buyButtons[i].transform.Find("ActionPill");
                if(pill!=null)
                {
                    pill.GetComponent<Image>().color=buyButtons[i].interactable?HudChrome.Green:HudChrome.TrackNavy;
                    var action=pill.Find("Action")?.GetComponent<Text>();
                    if(action!=null)action.text=unlocked?"Buy "+ShopRanks.StarRewardCopy:"Locked";
                }
                var money=buyButtons[i].transform.Find("MoneyIcon");
                if(money==null)FoodIcons.Add(buyButtons[i].transform,FoodIcon.Coin,Vector2.zero,32).name="MoneyIcon";
                money=buyButtons[i].transform.Find("MoneyIcon");
                money.gameObject.SetActive(unlocked);
                var mr=(RectTransform)money;mr.anchorMin=mr.anchorMax=Vector2.zero;mr.pivot=new Vector2(0,.5f);mr.anchoredPosition=new Vector2(18,37);
                prices[i].rectTransform.anchoredPosition=unlocked?new Vector2(56,16):new Vector2(20,16);
            }
            if(ownedPage)
            {
                var counts=new Dictionary<FacilityKind,int>();
                foreach(var instance in layout.Instances.Where(CanSelect).OrderBy(f=>f.Kind).ThenBy(f=>f.Id,System.StringComparer.Ordinal))
                {
                    counts.TryGetValue(instance.Kind,out int number);counts[instance.Kind]=++number;
                    var button=Card("Move_"+instance.Id,instance.Kind,FacilityCatalog.Get(instance.Kind).Name+" #"+number,"Details",()=>{if(FacilityDetailsHud.Current!=null)FacilityDetailsHud.Current.Open(instance);else MoveExisting(instance);},out var detail);
                    detail.text="Level "+instance.Capture().level;ownedButtons.Add(button);
                }
            }
            buyTab.targetGraphic.color=!ownedPage&&!expansionPage?HudChrome.Green:HudChrome.TrackNavy;
            ownedTab.targetGraphic.color=ownedPage?HudChrome.Green:HudChrome.TrackNavy;
            expansionTab.targetGraphic.color=expansionPage?HudChrome.Green:HudChrome.TrackNavy;
            buyTab.GetComponentInChildren<Text>().color=!ownedPage&&!expansionPage?Color.white:HudChrome.Ink;
            ownedTab.GetComponentInChildren<Text>().color=ownedPage?Color.white:HudChrome.Ink;
            expansionTab.GetComponentInChildren<Text>().color=expansionPage?Color.white:HudChrome.Ink;
            catalogHint.text=ownedPage?"Choose a facility · Details / Move":expansionPage?"Plan your next space · equipment sold separately":"Choose a model · Pay after placement";
            pickScene.gameObject.SetActive(ownedPage);
            browseHint.gameObject.SetActive(!ownedPage);
        }
        internal void ShowExpansions()
        {
            ownedPage=false;expansionPage=true;layout.Discover();RefreshCatalog();
            panel.gameObject.SetActive(true);backdrop.gameObject.SetActive(true);toolbar.gameObject.SetActive(false);
            catalogScroll.verticalNormalizedPosition=1;FitCatalog();
        }
        void RefreshExpansions()
        {
            var wing=layout.GetComponentInChildren<ShopExpansion>();
            var room=layout.GetComponent<RestroomExpansion>();
            var west=layout.GetComponent<BagLine>();
            var goals=layout.GetComponent<SessionGoalTracker>();
            bool[] built={wing!=null&&wing.HasWing,room!=null&&room.Built,west!=null&&west.Expanded};
            int[] rank={ShopRanks.ColaWingRank,RestroomExpansion.UnlockRank,ShopRanks.WestRank};
            int[] cost={wing?.WingPad?.Remaining??ShopExpansion.WingCost,room?.Remaining??RestroomExpansion.Cost,0};
            int drinksOpen=ShopExpansion.WingCost+ShopExpansion.GrillCost+ShopExpansion.CounterCost;
            int westGear=FacilityCatalog.BagMachinePrice+FacilityCatalog.BagTablePrice+FacilityCatalog.BagCounterPrice;
            string[] purpose={
                $"Cola lounge and seating\nLand {ShopExpansion.WingCost}+{ShopExpansion.GrillCost}+{ShopExpansion.CounterCost}={drinksOpen} to open\nEquipment sold separately",
                $"Guest facilities · two cubicles\nToilets and washbasin included\nComplete room {RestroomExpansion.Cost}",
                $"Paper bags → packing → pickup\nLand included at Lv.{ShopRanks.WestRank}\nRequired equipment {FacilityCatalog.BagMachinePrice}+{FacilityCatalog.BagTablePrice}+{FacilityCatalog.BagCounterPrice}={westGear}"};
            for(int i=0;i<expansionButtons.Count;i++)
            {
                var card=expansionButtons[i];card.gameObject.SetActive(expansionPage);
                bool unlocked=goals!=null&&goals.Allows(rank[i]);
                string detail=built[i]?"Built":!unlocked?$"Unlocks at Lv.{rank[i]}":cost[i]==0?"Included":$"Remaining {cost[i]:N0}";
                card.transform.Find("Detail").GetComponent<Text>().text=detail;
                card.transform.Find("Description").GetComponent<Text>().text=purpose[i]+(!built[i]&&unlocked&&layout.Wallet.Coins<cost[i]?$"\nNeed {cost[i]-layout.Wallet.Coins:N0} more":"");
                card.interactable=!built[i]&&unlocked&&layout.Wallet.Coins>=cost[i];
                card.transform.Find("ActionPill/Action").GetComponent<Text>().text=built[i]?"Built":!unlocked?"Locked":cost[i]==0?"Build":"Build "+ShopRanks.StarRewardCopy;
                card.transform.Find("ActionPill").GetComponent<Image>().color=card.interactable?HudChrome.Green:HudChrome.TrackNavy;
            }
        }
        void PurchaseExpansion(int choice)
        {
            bool bought=choice==0?layout.GetComponentInChildren<ShopExpansion>()?.TryPurchaseWing()==true:
                choice==1?layout.GetComponent<RestroomExpansion>()?.TryPurchase()==true:layout.GetComponent<BagLine>()?.TryExpand()==true;
            if(bought){layout.RefreshNavigation();layout.GetComponent<Core.RestaurantArchitecture>()?.Refresh();}
            RefreshCatalog();FitCatalog();
        }
        internal static bool CanSelect(FacilityInstance instance)
        {
            if(instance==null||!instance.Available)return false;
            return instance.GetComponentsInChildren<MeshRenderer>().Any(r=>r.enabled&&r.GetComponent<MeshFilter>()!=null&&r.GetComponent<TextMesh>()==null);
        }
        internal void MoveExisting(FacilityInstance instance)
        {
            if(!CanSelect(instance)||!layout.BeginMove(instance))return;
            StartPreview(instance);SelectInScene();
            var camera=Camera.main;
            if(camera!=null)
            {
                var ray=camera.ViewportPointToRay(new Vector3(.5f,.57f,0));
                if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance))
                    camera.transform.position=Core.StreetEnvironment.ClampCamera(camera,camera.transform.position+point-ray.GetPoint(distance));
            }
            status.text="Move "+FacilityCatalog.Get(instance.Kind).Name+" · Done to save";
        }
        void FitCatalog()
        {
            Vector2 available=((RectTransform)transform).rect.size;
            if(available.x<1||available.y<1)return;
            fittedSize=available;
            var size=new Vector2(Mathf.Min(920,available.x-40),Mathf.Min(1200,available.y-64));
            ((RectTransform)panel).sizeDelta=size;
            float tabsWidth=(size.x-68)/3;
            var tabs=new[]{buyTab,expansionTab,ownedTab};
            for(int i=0;i<tabs.Length;i++)
            {((RectTransform)tabs[i].transform).sizeDelta=new Vector2(tabsWidth,64);((RectTransform)tabs[i].transform).anchoredPosition=new Vector2((i-1)*(tabsWidth+8),-86);}
            catalogHint.rectTransform.sizeDelta=new Vector2(size.x-48,44);
            catalogScroll.viewport.offsetMax=new Vector2(-26,-218);
            var cards=ownedPage?ownedButtons:expansionPage?expansionButtons:buyButtons;
            float cardHeight=expansionPage?460:308, pitch=cardHeight+20;
            int columns=size.x>=700?2:1,index=0;
            float width=(size.x-52-(columns-1)*20)/columns;
            foreach(var card in cards)
            {
                if(!card.gameObject.activeSelf)continue;
                var rect=(RectTransform)card.transform;rect.anchoredPosition=new Vector2((index%columns)*(width+20),-(index/columns)*pitch);rect.sizeDelta=new Vector2(width,cardHeight);index++;
                ((RectTransform)card.transform.Find("Text")).sizeDelta=new Vector2(width-36,64);
                ((RectTransform)card.transform.Find("PhotoBackground")).sizeDelta=new Vector2(width-24,162);
                ((RectTransform)card.transform.Find("PhotoBackground/ProductPhoto")).sizeDelta=new Vector2(width-42,154);
                if(expansionPage)((RectTransform)card.transform.Find("Description")).sizeDelta=new Vector2(width-36,148);
            }
            ((RectTransform)content).sizeDelta=new Vector2(0,Mathf.CeilToInt(index/(float)columns)*pitch);
            toolbar.localScale=Vector3.one*Mathf.Min(1,(available.x-32)/940);
        }
        void Purchase(FacilityKind kind)
        {
            var candidate=layout.BeginPurchase(kind);if(candidate==null){status.text=layout.LastError;return;}
            StartPreview(candidate);SelectInScene();yaw=0;
        }
        void StartPreview(FacilityInstance source)
        {
            RemovePreview();placing=true;dragging=false;placementError="";checkedItem=null;yaw=source.transform.eulerAngles.y;
            point=source.transform.position;rawPoint=point;rawYaw=yaw;alignment.Reset();alignmentMayAdvance=false;
            awaitDesktopMotion=true;initialPointer=Mouse.current!=null?Mouse.current.position.ReadValue():Vector2.zero;
            preview=new GameObject("FacilityGhost").transform;
            ghostMaterial=Core.RuntimeMaterials.Create(new Color(.25f,.9f,.4f));
            foreach(var renderer in source.GetComponentsInChildren<MeshRenderer>(true))
            {
                bool visible=true;for(var p=renderer.transform;p!=source.transform&&p!=null;p=p.parent)if(!p.gameObject.activeSelf){visible=false;break;}
                if(!visible)continue;
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null||!renderer.enabled||renderer.GetComponentInParent<Customer.CustomerAgent>()!=null||renderer.GetComponentInParent<RestaurantWorker>()!=null)continue;
                var piece=new GameObject(renderer.name,typeof(MeshFilter),typeof(MeshRenderer)).transform;piece.SetParent(preview,false);
                piece.localPosition=source.transform.InverseTransformPoint(renderer.transform.position);
                piece.localRotation=Quaternion.Inverse(source.transform.rotation)*renderer.transform.rotation;
                piece.localScale=renderer.transform.lossyScale;
                piece.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                var materials=new Material[renderer.sharedMaterials.Length];for(int i=0;i<materials.Length;i++)materials[i]=ghostMaterial;
                piece.GetComponent<MeshRenderer>().sharedMaterials=materials;
                if(layout.Moving==source){hiddenOriginals.Add(renderer);renderer.enabled=false;}
            }
            preview.SetPositionAndRotation(point,Quaternion.Euler(0,yaw,0));
        }
        void Rotate(float delta)
        {yaw+=delta;rawYaw=yaw;rawPoint=point;alignment.Reset();alignmentMayAdvance=false;placementError="";}
        internal void AdvanceAlignment(float seconds)
        {
            if(!placing||!alignmentMayAdvance)return;
            bool attempt=alignment.ShouldAttempt(rawPoint,seconds);
            if(!alignment.Active){point=rawPoint;yaw=rawYaw;}
            if(!attempt)return;
            var item=layout.Candidate!=null?layout.Candidate:layout.Moving;
            bool found=false;PlacementAlignment.Pose best=default;
            foreach(var neighbor in layout.Instances)
            {
                if(!PlacementAlignment.TryPose(item,rawPoint,rawYaw,neighbor,out var pose))continue;
                if(!found||pose.Score<best.Score){found=true;best=pose;}
            }
            if(!found||!layout.CanPlace(item,best.Position,best.Yaw,true))return;
            alignedPose=best;point=best.Position;yaw=best.Yaw;alignment.Lock(rawPoint);placementError="";
        }
        void DrawAlignment()
        {
            if(!alignment.Active){if(alignmentGuide!=null)alignmentGuide.enabled=false;return;}
            if(alignmentGuide==null)
            {
                alignmentGuide=new GameObject("PlacementAlignmentGuide").AddComponent<LineRenderer>();
                alignmentMaterial=Core.RuntimeMaterials.Create(new Color(.2f,.9f,1f),true);alignmentGuide.sharedMaterial=alignmentMaterial;
                alignmentGuide.positionCount=2;alignmentGuide.useWorldSpace=true;alignmentGuide.startWidth=alignmentGuide.endWidth=.045f;
                alignmentGuide.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;alignmentGuide.receiveShadows=false;
            }
            alignmentGuide.enabled=true;alignmentGuide.SetPosition(0,alignedPose.GuideStart);alignmentGuide.SetPosition(1,alignedPose.GuideEnd);
        }
        bool PlaceCurrent()
        {
            if(!placing)return true;
            if(!layout.Confirm(point,yaw)){placementError=layout.LastError;status.text=placementError;return false;}
            RemovePreview();placing=false;dragging=false;checkedItem=null;
            status.text="Placed. Select another facility, or Done.";
            return true;
        }
        void Done()
        {
            if(PlaceCurrent())Close();
        }
        // Match the user-approved desktop preview: hover positions the model, click commits.
        // UI hover/click freezes the last world pose; touch retains drag/release/Done semantics.
        internal void MoveDesktopPointer(Vector2 pointer,bool clicked,bool overUi)
        {
            if(overUi)alignmentMayAdvance=false;
            if(!placing||overUi)return;
            if(awaitDesktopMotion&&pointer==initialPointer&&!clicked)return;
            awaitDesktopMotion=false;
            var previous=point;
            MovePreviewPointer(pointer,true,true,false);dragging=false;
            desktopMoved=point!=previous;
            if(clicked)PlaceCurrent();
        }
        // Only a gesture begun on the world may move the preview. Hover and UI presses never move it.
        internal void MovePreviewPointer(Vector2 pointer,bool pressed,bool held,bool overUi)
        {
            if(!placing)return;
            if(overUi)alignmentMayAdvance=false;
            if(pressed)dragging=!overUi;
            if(!held){dragging=false;return;}
            if(!dragging||overUi||Camera.main==null)return;
            var ray=Camera.main.ScreenPointToRay(pointer);
            if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out var distance))
            {var next=ray.GetPoint(distance);if((next-rawPoint).sqrMagnitude>.000001f)placementError="";rawPoint=next;alignmentMayAdvance=true;if(!alignment.Active)point=next;AdvanceAlignment(0);}
        }
        bool OverUi(Vector2 pointer)
        {
            var canvas=GetComponentInParent<Canvas>();var camera=canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
            if(toolbar.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint((RectTransform)toolbar,pointer,camera))return true;
            if(EventSystem.current==null)return false;
            uiHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=pointer},uiHits);
            foreach(var hit in uiHits)if(hit.module is GraphicRaycaster)return true;
            return false;
        }
        void CancelPlacement()
        {
            bool moving=layout.Moving!=null;
            dragging=false;layout.Cancel();RemovePreview();placing=false;checkedItem=null;
            if(open)ShowCatalog(moving||ownedPage);
        }
        public void Close()
        {
            if(!open)return;
            open=false;dragging=false;layout.Cancel();RemovePreview();placing=false;checkedItem=null;Time.timeScale=previousTimeScale;
            if(follow!=null)follow.enabled=followWasEnabled;
            panel.gameObject.SetActive(false);backdrop.gameObject.SetActive(false);toolbar.gameObject.SetActive(false);
            if(isActiveAndEnabled)StartCoroutine(RefreshAfterPlacement());else layout.RefreshNavigation();
        }
        System.Collections.IEnumerator RefreshAfterPlacement()
        {
            // Destroyed preview/old facility colliders disappear at frame end, before rebaking.
            yield return null;
            if(!open&&layout!=null)layout.RefreshNavigation();
        }
        void OnDisable()
        {
            if(open)Close();
        }
        void RemovePreview(){alignment.Reset();alignmentMayAdvance=false;if(alignmentGuide!=null)BurgerVisual.Release(alignmentGuide.gameObject);alignmentGuide=null;if(alignmentMaterial!=null)BurgerVisual.Release(alignmentMaterial);alignmentMaterial=null;foreach(var renderer in hiddenOriginals)if(renderer!=null)renderer.enabled=true;hiddenOriginals.Clear();if(preview!=null)BurgerVisual.Release(preview.gameObject);preview=null;if(ghostMaterial!=null)BurgerVisual.Release(ghostMaterial);ghostMaterial=null;}
        void Update()
        {
            if(!open)return;
            if(fittedSize!=((RectTransform)transform).rect.size)FitCatalog();
            var keyboard=Keyboard.current;var mouse=Mouse.current;
            if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame){if(placing)CancelPlacement();else Close();return;}
            var touch=Touchscreen.current?.primaryTouch;
            bool touching=touch!=null&&touch.press.isPressed;
            if(Camera.main==null||(mouse==null&&!touching)){dragging=false;return;}
            Vector2 pointer=touching?touch.position.ReadValue():mouse.position.ReadValue();
            bool clicked=touching?touch.press.wasPressedThisFrame:mouse.leftButton.wasPressedThisFrame;
            bool held=touching||mouse!=null&&mouse.leftButton.isPressed;
            if(!panel.gameObject.activeSelf&&Touchscreen.current!=null)
            {
                int fingers=0;Vector2 delta=Vector2.zero;
                foreach(var t in Touchscreen.current.touches)if(t.press.isPressed){fingers++;delta+=t.delta.ReadValue();}
                if(fingers>=2){alignmentMayAdvance=false;dragging=false;Camera.main.transform.position=Core.StreetEnvironment.ClampCamera(Camera.main,Camera.main.transform.position-new Vector3(delta.x,0,delta.y)*.015f/fingers);return;}
            }
            if(!panel.gameObject.activeSelf&&keyboard!=null)
            {
                Vector3 pan=Vector3.zero;if(keyboard.wKey.isPressed)pan.z++;if(keyboard.sKey.isPressed)pan.z--;if(keyboard.aKey.isPressed)pan.x--;if(keyboard.dKey.isPressed)pan.x++;
                Camera.main.transform.position=Core.StreetEnvironment.ClampCamera(Camera.main,Camera.main.transform.position+pan*12*Time.unscaledDeltaTime);
            }
            if(Time.frameCount==worldInputFrame)return;
            bool ui=OverUi(pointer);
            if(placing)
            {
                if(keyboard!=null&&keyboard.qKey.wasPressedThisFrame)Rotate(-FacilityCatalog.RotationStep);
                if(keyboard!=null&&keyboard.eKey.wasPressedThisFrame)Rotate(FacilityCatalog.RotationStep);
                if(!Application.isMobilePlatform&&!touching)MoveDesktopPointer(pointer,clicked,ui);
                else MovePreviewPointer(pointer,clicked,held,ui);

            }
            else if(!ui&&!panel.gameObject.activeSelf&&clicked)
            {
                var hits=Physics.RaycastAll(Camera.main.ScreenPointToRay(pointer),500);
                System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
                foreach(var hit in hits)
                {
                    var instance=hit.collider.GetComponentInParent<FacilityInstance>();
                    if(!CanSelect(instance)||!layout.BeginMove(instance))continue;StartPreview(instance);break;
                }
            }
        }
        void LateUpdate()
        {
            // Touch release has no active pointer; rotation buttons must still repaint the preview.
            if(!open||!placing||preview==null)return;
            AdvanceAlignment(Time.unscaledDeltaTime);DrawAlignment();
            preview.SetPositionAndRotation(point,Quaternion.Euler(0,yaw,0));
            var item=layout.Candidate!=null?layout.Candidate:layout.Moving;
            bool dirty=checkedItem!=item||checkedPoint!=point||checkedYaw!=yaw||checkedRevision!=layout.Revision;
            if(dirty&&((!dragging&&!desktopMoved)||Time.unscaledTime>=nextCheck))
            {
                checkedValid=layout.CanPlace(item,point,yaw,true);
                checkedError=layout.LastError;checkedItem=item;checkedPoint=point;checkedYaw=yaw;checkedRevision=layout.Revision;
                nextCheck=Time.unscaledTime+.12f;dirty=false;
            }
            desktopMoved=false;
            // Never show green for a pose that has only passed the cheap overlap checks.
            bool valid=!dirty&&checkedValid&&string.IsNullOrEmpty(placementError);
            ghostMaterial.color=dirty?new Color(.95f,.75f,.25f):valid?new Color(.25f,.85f,.4f):new Color(.95f,.25f,.2f);
            status.text=!string.IsNullOrEmpty(placementError)?placementError:dirty?"Checking space and paths...":valid?
                alignment.Active?"Aligned · drag away to release · Done to place":(Application.isMobilePlatform?"Drag to position · rotate · tap Done to place":"Move mouse · Q/E rotate · click to place · Done to finish"):checkedError;

        }
        void OnDestroy(){Close();RemovePreview();}
    }
}
