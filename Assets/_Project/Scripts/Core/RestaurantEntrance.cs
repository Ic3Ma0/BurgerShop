using UnityEngine;
using BurgerShop.Restaurant;

namespace BurgerShop.Core
{
    public static class RestaurantEntrance
    {
        public static readonly Vector3 Outside=new Vector3(-33,0,-23);
        public static readonly Vector3 Corner=new Vector3(-12,0,-23);
        public static readonly Vector3 Door=new Vector3(-12,0,-15);
        public static Vector3[] Arrival=>new[]{Outside,Corner,Door,ShopLayout.Entrance};
        public static void Build(Transform parent)
        {
            var root=new GameObject("RestaurantEntrance").transform;root.SetParent(parent,false);
            var blue=RuntimeMaterials.Create(new Color(.12f,.40f,.88f));
            var red=RuntimeMaterials.Create(new Color(1,.22f,.12f));
            var white=RuntimeMaterials.Create(new Color(.93f,.95f,.86f));
            var glass=RuntimeMaterials.Create(new Color(.62f,.84f,.89f));
            foreach(float x in new[]{-14f,-10f})CourierVisuals.Part(root,"DoorPillar",new Vector3(x,1.7f,-15),new Vector3(.35f,3.4f,.5f),blue,true);
            CourierVisuals.Part(root,"DoorHeader",new Vector3(-12,3.35f,-15),new Vector3(4.35f,.65f,.6f),blue);
            CourierVisuals.Part(root,"DoorCrown",new Vector3(-12,3.85f,-15),new Vector3(1.9f,.5f,.6f),blue);
            var canopy=CourierVisuals.Part(root,"RedAwning",new Vector3(-12,2.9f,-15.65f),new Vector3(4.2f,.16f,1.3f),red);
            canopy.transform.rotation=Quaternion.Euler(-16,0,0);
            var hinges=new Transform[2];
            for(int i=0;i<2;i++)
            {
                float sign=i==0?1:-1;
                var hinge=new GameObject("DoorHinge").transform;hinge.SetParent(root,false);hinge.position=new Vector3(i==0?-13.8f:-10.2f,0,-15);
                hinges[i]=hinge;
                var frame=CourierVisuals.Part(hinge,"SwingDoorFrame",new Vector3(sign*.9f,1.2f,0),new Vector3(1.8f,2.4f,.10f),white);
                var leaf=CourierVisuals.Part(hinge,"SwingGlass",new Vector3(sign*.9f,1.25f,-.065f),new Vector3(1.55f,2.0f,.04f),glass);
                CourierVisuals.Part(hinge,"DoorHandle",new Vector3(sign*1.55f,1.1f,-.14f),new Vector3(.07f,.4f,.12f),blue);
            }
            root.gameObject.AddComponent<SwingEntranceDoors>().Configure(hinges);
            CourierVisuals.Part(root,"EntranceWalk",new Vector3(-12,-.09f,-19),new Vector3(4,.18f,8),white,true);
            // Match the blue sidewalk, without the former pen-like rails.
            // The continuous StreetEnvironment sidewalk already provides matching surface and collision.
            foreach(float x in new[]{-15.5f,-8.5f})
            {
                CourierVisuals.Part(root,"DoorPlanter",new Vector3(x,.25f,-16),new Vector3(1,.5f,1),white,true);
                CourierVisuals.Part(root,"DoorGreenery",new Vector3(x,.7f,-16),new Vector3(.9f,.7f,.9f),RuntimeMaterials.Create(new Color(.35f,.65f,.2f)));
            }
        }
    }
}
