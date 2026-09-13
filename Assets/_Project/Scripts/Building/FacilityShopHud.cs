using System.Collections.Generic;
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
        Transform panel,toolbar,preview;
        Text status;
        bool open,placing,dragging,desktopMoved;
        readonly List<RaycastResult> uiHits=new List<RaycastResult>();
        internal Vector3 PreviewPosition=>point;
        float previousTimeScale,yaw;
        string placementError="";
        Vector3 checkedPoint;float checkedYaw,nextCheck;int checkedRevision=-1;bool checkedValid;
        FacilityInstance checkedItem;string checkedError="";
        Vector3 point;
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
            panel=HudChrome.Panel(transform,"Catalog",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(850,880),HudChrome.Cream).transform;
            HudChrome.Label(panel,"Title",new Vector2(0,1),new Vector2(1,1),new Vector2(.5f,1),new Vector2(0,-18),new Vector2(800,50),32,HudChrome.Ink,TextAnchor.MiddleCenter,true,true).text="SUPERMARKET";
            Button(panel,"MoveExisting","Move existing facilities",new Vector2(.5f,1),new Vector2(0,-80),new Vector2(550,65),()=>{panel.gameObject.SetActive(false);status.text="Click a facility to move it";});
            var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(Image),typeof(Mask)).transform;viewport.SetParent(panel,false);
            var vr=(RectTransform)viewport;vr.anchorMin=Vector2.zero;vr.anchorMax=Vector2.one;vr.offsetMin=new Vector2(25,90);vr.offsetMax=new Vector2(-25,-165);viewport.GetComponent<Mask>().showMaskGraphic=false;
            var content=new GameObject("Items",typeof(RectTransform)).transform;content.SetParent(viewport,false);
            var cr=(RectTransform)content;cr.anchorMin=new Vector2(0,1);cr.anchorMax=Vector2.one;cr.pivot=new Vector2(.5f,1);cr.sizeDelta=new Vector2(0,1000);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=vr;scroll.content=cr;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            int row=0;
            foreach(var offer in FacilityCatalog.Offers)
            {
                var kind=offer.Kind;
                var button=Button(content,"Buy_"+kind,offer.Name,new Vector2(.5f,1),new Vector2(0,-row*80),new Vector2(760,70),()=>Purchase(kind));
                buyButtons.Add(button);kinds.Add(kind);row++;
            }
            Button(panel,"Close","Close",new Vector2(.5f,0),new Vector2(0,12),new Vector2(250,65),Close);
            toolbar=HudChrome.Panel(transform,"PlacementControls",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,35),new Vector2(940,200),HudChrome.Cream).transform;
            status=HudChrome.Label(toolbar,"PlacementStatus",new Vector2(0,1),new Vector2(1,1),new Vector2(.5f,1),new Vector2(0,-12),new Vector2(900,70),24,HudChrome.Ink,TextAnchor.MiddleCenter,true,true);
            Button(toolbar,"RotateLeft","Rotate left",new Vector2(0,0),new Vector2(15,15),new Vector2(200,70),()=>Rotate(-FacilityCatalog.RotationStep));
            Button(toolbar,"RotateRight","Rotate right",new Vector2(0,0),new Vector2(230,15),new Vector2(200,70),()=>Rotate(FacilityCatalog.RotationStep));
            Button(toolbar,"Cancel","Cancel",new Vector2(0,0),new Vector2(465,15),new Vector2(170,70),CancelPlacement);
            Button(toolbar,"Done","Done",new Vector2(0,0),new Vector2(680,15),new Vector2(220,70),Done);
            panel.gameObject.SetActive(false);toolbar.gameObject.SetActive(false);
        }
        public void Open()
        {
            if(open){Close();return;}
            open=true;previousTimeScale=Time.timeScale;Time.timeScale=0;
            follow=Camera.main!=null?Camera.main.GetComponent<CameraFollow>():null;
            if(follow!=null){followWasEnabled=follow.enabled;follow.enabled=false;}
            layout.Discover();RefreshCatalog();panel.gameObject.SetActive(true);toolbar.gameObject.SetActive(true);status.text="Choose an item, or move an existing facility";
        }
        void RefreshCatalog()
        {
            int row=0;
            for(int i=0;i<buyButtons.Count;i++)
            {
                bool unlocked=layout.Unlocked(kinds[i]);buyButtons[i].gameObject.SetActive(unlocked);if(!unlocked)continue;
                ((RectTransform)buyButtons[i].transform).anchoredPosition=new Vector2(0,-row++*80);
                buyButtons[i].GetComponentInChildren<Text>().text=FacilityCatalog.Get(kinds[i]).Name+"     "+layout.Price(kinds[i])+" coins";
                buyButtons[i].interactable=layout.Wallet.Coins>=layout.Price(kinds[i]);
            }
            ((RectTransform)buyButtons[0].transform.parent).sizeDelta=new Vector2(0,row*80);
        }
        void Purchase(FacilityKind kind)
        {
            var candidate=layout.BeginPurchase(kind);if(candidate==null){status.text=layout.LastError;return;}
            StartPreview(candidate);panel.gameObject.SetActive(false);yaw=0;
        }
        void StartPreview(FacilityInstance source)
        {
            RemovePreview();placing=true;dragging=false;placementError="";checkedItem=null;yaw=source.transform.eulerAngles.y;
            point=source.transform.position;
            preview=new GameObject("FacilityGhost").transform;
            ghostMaterial=Core.RuntimeMaterials.Create(new Color(.25f,.9f,.4f));
            foreach(var renderer in source.GetComponentsInChildren<MeshRenderer>(true))
            {
                bool visible=true;for(var p=renderer.transform;p!=source.transform&&p!=null;p=p.parent)if(!p.gameObject.activeSelf){visible=false;break;}
                if(!visible)continue;
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
                var piece=new GameObject(renderer.name,typeof(MeshFilter),typeof(MeshRenderer)).transform;piece.SetParent(preview,false);
                piece.localPosition=source.transform.InverseTransformPoint(renderer.transform.position);
                piece.localRotation=Quaternion.Inverse(source.transform.rotation)*renderer.transform.rotation;
                piece.localScale=renderer.transform.lossyScale;
                piece.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                var materials=new Material[renderer.sharedMaterials.Length];for(int i=0;i<materials.Length;i++)materials[i]=ghostMaterial;
                piece.GetComponent<MeshRenderer>().sharedMaterials=materials;
            }
        }
        void Rotate(float delta){yaw+=delta;placementError="";}
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
            if(!placing||overUi)return;
            var previous=point;
            MovePreviewPointer(pointer,true,true,false);dragging=false;
            desktopMoved=point!=previous;
            if(clicked)PlaceCurrent();
        }
        // Only a gesture begun on the world may move the preview. Hover and UI presses never move it.
        internal void MovePreviewPointer(Vector2 pointer,bool pressed,bool held,bool overUi)
        {
            if(!placing)return;
            if(pressed)dragging=!overUi;
            if(!held){dragging=false;return;}
            if(!dragging||overUi||Camera.main==null)return;
            var ray=Camera.main.ScreenPointToRay(pointer);
            if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out var distance))
            {var next=ray.GetPoint(distance);if((next-point).sqrMagnitude>.000001f)placementError="";point=next;}
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
        void CancelPlacement(){dragging=false;layout.Cancel();RemovePreview();placing=false;status.text="Click a facility to move it";}
        public void Close()
        {
            if(!open)return;CancelPlacement();open=false;Time.timeScale=previousTimeScale;
            if(follow!=null)follow.enabled=followWasEnabled;
            panel.gameObject.SetActive(false);toolbar.gameObject.SetActive(false);
        }
        void RemovePreview(){if(preview!=null)BurgerVisual.Release(preview.gameObject);preview=null;if(ghostMaterial!=null)Destroy(ghostMaterial);ghostMaterial=null;}
        void Update()
        {
            if(!open)return;
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
                if(fingers>=2){dragging=false;Camera.main.transform.position=Core.StreetEnvironment.ClampCamera(Camera.main,Camera.main.transform.position-new Vector3(delta.x,0,delta.y)*.015f/fingers);return;}
            }
            if(!panel.gameObject.activeSelf&&keyboard!=null)
            {
                Vector3 pan=Vector3.zero;if(keyboard.wKey.isPressed)pan.z++;if(keyboard.sKey.isPressed)pan.z--;if(keyboard.aKey.isPressed)pan.x--;if(keyboard.dKey.isPressed)pan.x++;
                Camera.main.transform.position=Core.StreetEnvironment.ClampCamera(Camera.main,Camera.main.transform.position+pan*12*Time.unscaledDeltaTime);
            }
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
                    if(instance==null||!layout.BeginMove(instance))continue;StartPreview(instance);break;
                }
            }
        }
        void LateUpdate()
        {
            // Touch release has no active pointer; rotation buttons must still repaint the preview.
            if(!open||!placing||preview==null)return;
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
                (Application.isMobilePlatform?"Drag to position · rotate · tap Done to place":"Move mouse · Q/E rotate · click to place · Done to finish"):checkedError;

        }
        void OnDestroy(){Close();RemovePreview();}
    }
}
