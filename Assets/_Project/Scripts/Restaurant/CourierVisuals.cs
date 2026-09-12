using BurgerShop.Core;
using BurgerShop.UI;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public static class CourierVisuals
    {
        public static Transform RedParcel(Transform parent)
        {
            var root=new GameObject("RedDeliveryParcel").transform;root.SetParent(parent,false);
            var red=RuntimeMaterials.Create(new Color(.86f,.06f,.09f));var white=RuntimeMaterials.Create(HudChrome.Cream);
            root.gameObject.AddComponent<BurgerVisual>().OwnMaterials(red,white);
            Part(root,"Box",new Vector3(0,.12f,0),new Vector3(.65f,.24f,.60f),red);
            Part(root,"Lid",new Vector3(0,.25f,0),new Vector3(.69f,.04f,.64f),red);
            Part(root,"Band",new Vector3(0,.274f,0),new Vector3(.13f,.016f,.58f),white);
            Part(root,"CrossBand",new Vector3(0,.276f,0),new Vector3(.63f,.016f,.10f),white);
            return root;
        }
        public static Transform PartsPile(Transform parent)
        {
            var root=new GameObject("PartsReward").transform;root.SetParent(parent,false);
            var metal=RuntimeMaterials.Create(new Color(.72f,.77f,.80f));var blue=RuntimeMaterials.Create(new Color(.16f,.4f,.88f));
            root.gameObject.AddComponent<BurgerVisual>().OwnMaterials(metal,blue);
            for(int n=0;n<3;n++)
            {
                var nut=BagVisualFactory.Part(root,"Nut",PrimitiveType.Cylinder,new Vector3(0,.08f+n*.13f,0),new Vector3(.43f,.055f,.43f),metal);
                BagVisualFactory.Part(root,"BoltCenter",PrimitiveType.Cylinder,new Vector3(0,.145f+n*.13f,0),new Vector3(.21f,.014f,.21f),blue);
                for(int side=0;side<6;side++)
                {
                    float a=side*Mathf.PI/3;
                    var tooth=Part(root,"GearTooth",new Vector3(Mathf.Sin(a)*.14f,.145f+n*.13f,Mathf.Cos(a)*.14f),new Vector3(.09f,.024f,.09f),blue);
                    tooth.transform.localRotation=Quaternion.Euler(0,side*60,0);
                }
            }
            return root;
        }
        public static GameObject Part(Transform parent,string name,Vector3 pos,Vector3 size,Material mat,bool solid=false)
            =>BagVisualFactory.Part(parent,name,PrimitiveType.Cube,pos,size,mat,solid);
    }

    public sealed class BicycleCourier : MonoBehaviour
    {
        public bool HasCargoTrailer {get;private set;}
        public int Quantity {get;private set;}
        public int Received {get;private set;}
        public int Remaining=>Quantity-Received;
        public bool Leaving {get;private set;}
        public bool Finished {get;private set;}
        Transform rack,legs;TextMesh order;
        int exitStep;
        public static BicycleCourier Create(Transform parent,Vector3 spawn,int quantity,bool cargoTrailer=false)
        {
            var root=new GameObject("BicycleCourier");root.transform.SetParent(parent,false);root.transform.position=spawn;
            var rider=root.AddComponent<BicycleCourier>();rider.Quantity=Mathf.Max(1,quantity);rider.HasCargoTrailer=cargoTrailer;rider.Build();return rider;
        }
        void Build()
        {
            var frame=RuntimeMaterials.Create(new Color(1f,.64f,.05f));var tyre=RuntimeMaterials.Create(new Color(.10f,.12f,.15f));
            var steel=RuntimeMaterials.Create(new Color(.75f,.81f,.83f));var coat=RuntimeMaterials.Create(new Color(.15f,.40f,.85f));var skin=RuntimeMaterials.Create(new Color(.76f,.52f,.33f));
            var red=RuntimeMaterials.Create(new Color(.95f,.12f,.06f));
            var green=RuntimeMaterials.Create(new Color(.12f,.58f,.24f));
            gameObject.AddComponent<BurgerVisual>().OwnMaterials(frame,tyre,steel,coat,skin,red,green);
            foreach(float z in new[]{-.68f,.68f})
            {
                var wheel=BagVisualFactory.Part(transform,"Wheel",PrimitiveType.Cylinder,new Vector3(0,.40f,z),new Vector3(.76f,.055f,.76f),tyre);
                wheel.transform.localRotation=Quaternion.Euler(0,0,90);
                var hub=BagVisualFactory.Part(transform,"WheelHub",PrimitiveType.Cylinder,new Vector3(0,.40f,z),new Vector3(.50f,.059f,.50f),steel);
                hub.transform.localRotation=Quaternion.Euler(0,0,90);
            }
            Bar("LowerFrame",new Vector3(0,.45f,-.68f),new Vector3(0,.50f,.68f),.065f,frame);
            Bar("SeatTube",new Vector3(0,.45f,.05f),new Vector3(0,1.05f,-.24f),.07f,frame);
            Bar("TopTube",new Vector3(0,.95f,-.24f),new Vector3(0,1.03f,.45f),.07f,frame);
            Bar("FrontFork",new Vector3(0,.40f,.68f),new Vector3(0,1.15f,.42f),.07f,steel);
            Bar("RearFrame",new Vector3(0,.45f,-.68f),new Vector3(0,.95f,-.24f),.06f,frame);
            CourierVisuals.Part(transform,"Saddle",new Vector3(0,1.04f,-.24f),new Vector3(.36f,.09f,.24f),tyre);
            CourierVisuals.Part(transform,"Handlebars",new Vector3(0,1.2f,.42f),new Vector3(.56f,.06f,.06f),steel);
            var torso=BagVisualFactory.Part(transform,"Rider",PrimitiveType.Capsule,new Vector3(0,1.40f,-.18f),new Vector3(.42f,.36f,.42f),coat);
            torso.transform.localRotation=Quaternion.Euler(15,0,0);
            BagVisualFactory.Part(transform,"Head",PrimitiveType.Sphere,new Vector3(0,1.89f,-.04f),Vector3.one*.30f,skin);
            BagVisualFactory.Part(transform,"Helmet",PrimitiveType.Sphere,new Vector3(0,2.01f,-.04f),new Vector3(.43f,.32f,.44f),red);
            BagVisualFactory.Part(transform,"HelmetBlueStripe",PrimitiveType.Sphere,new Vector3(0,2.035f,-.04f),new Vector3(.14f,.33f,.45f),coat);
            CourierVisuals.Part(transform,"DarkVisor",new Vector3(0,1.91f,.16f),new Vector3(.32f,.13f,.07f),tyre);
            CourierVisuals.Part(transform,"RedFrontShield",new Vector3(0,.85f,.54f),new Vector3(.30f,.48f,.13f),red);
            BagVisualFactory.Part(transform,"Headlamp",PrimitiveType.Sphere,new Vector3(0,1.05f,.65f),Vector3.one*.18f,steel);
            Bar("LeftArm",new Vector3(-.21f,1.6f,-.10f),new Vector3(-.23f,1.2f,.42f),.10f,skin);
            Bar("RightArm",new Vector3(.21f,1.6f,-.10f),new Vector3(.23f,1.2f,.42f),.10f,skin);
            legs=new GameObject("Pedalling").transform;legs.SetParent(transform,false);legs.localPosition=new Vector3(0,.8f,0);
            CourierVisuals.Part(legs,"LeftLeg",new Vector3(-.16f,0,-.12f),new Vector3(.14f,.6f,.14f),tyre);
            CourierVisuals.Part(legs,"RightLeg",new Vector3(.16f,0,.12f),new Vector3(.14f,.6f,.14f),tyre);
            rack=new GameObject("ParcelRack").transform;rack.SetParent(transform,false);rack.localPosition=new Vector3(0,.95f,-.68f);
            CourierVisuals.Part(rack,"Rack",Vector3.zero,new Vector3(.70f,.06f,.64f),steel);
            if(HasCargoTrailer)
            {
                rack.localPosition=new Vector3(0,.59f,-1.55f);
                rack.Find("Rack").localScale=new Vector3(1.12f,.10f,1.18f);
                rack.Find("Rack").GetComponent<Renderer>().sharedMaterial=green;
                foreach(int side in new[]{-1,1})
                {
                    CourierVisuals.Part(rack,"CargoSide",new Vector3(side*.55f,.13f,0),new Vector3(.09f,.28f,1.2f),green);
                    CourierVisuals.Part(rack,"CargoEnd",new Vector3(0,.13f,side*.56f),new Vector3(1.12f,.28f,.09f),green);
                    var wheel=BagVisualFactory.Part(rack,"TrailerWheel",PrimitiveType.Cylinder,new Vector3(side*.66f,-.29f,0),new Vector3(.48f,.08f,.48f),tyre);
                    wheel.transform.localRotation=Quaternion.Euler(0,0,90);
                    var hub=BagVisualFactory.Part(rack,"TrailerHub",PrimitiveType.Cylinder,new Vector3(side*.75f,-.29f,0),new Vector3(.24f,.015f,.24f),coat);
                    hub.transform.localRotation=Quaternion.Euler(0,0,90);
                }
                Bar("TowBar",new Vector3(0,.5f,-.65f),new Vector3(0,.5f,-1.4f),.09f,frame);
            }
            var bubble=new GameObject("CourierOrderBubble").transform;bubble.SetParent(transform,false);bubble.localPosition=Vector3.up*2.45f;
            order=ShopFixtures.CreateStationLabel(bubble,"CourierOrderQuantity",bubble.position,"x"+Quantity);
        }
        void Bar(string name,Vector3 a,Vector3 b,float width,Material mat)
        {
            var obj=CourierVisuals.Part(transform,name,(a+b)*.5f,new Vector3(width,(b-a).magnitude,width),mat);
            obj.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
        }
        public bool Arrived(Vector3 stop)=>ShopLayout.Horizontal(transform.position,stop)<.08f;
        public void Advance(float dt,Vector3 stop)
        {
            if(Finished||dt<=0)return;
            Vector3 target=stop;
            if(Leaving)
            {
                var exit=CourierRoad.ExitPath;
                target=exit[exitStep];
                if(Arrived(target)){exitStep++;if(exitStep==exit.Length){Finished=true;return;}target=exit[exitStep];}
            }
            Vector3 delta=target-transform.position;delta.y=0;
            if(delta.sqrMagnitude>.0025f)
            {
                transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),dt*260);
                transform.position=Vector3.MoveTowards(transform.position,target,dt*3);
                legs.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*10)*18,0,0);
            }
        }
        public void Receive(Transform parcel)
        {
            if(Remaining<=0)return;
            parcel.SetParent(rack,false);parcel.localPosition=Vector3.up*(.06f+Received*.28f);Received++;
            order.text="x"+Remaining;
        }
        public void Depart(){Leaving=true;order.gameObject.SetActive(false);}
    }
}
