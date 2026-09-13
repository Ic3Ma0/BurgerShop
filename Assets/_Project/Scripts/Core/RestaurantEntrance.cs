using UnityEngine;
using BurgerShop.Restaurant;

namespace BurgerShop.Core
{
    public static class RestaurantEntrance
    {
        public static readonly Vector3 Outside=new Vector3(-12,0,-23);
        public static readonly Vector3 Door=new Vector3(-12,0,-15);
        public static Vector3[] Arrival=>new[]{Outside,Door,ShopLayout.Entrance};
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
            // Open glazed leaves keep an unobstructed passage instead of a solid fake door.
            foreach(float x in new[]{-13.8f,-10.2f})
            {
                CourierVisuals.Part(root,"OpenDoorFrame",new Vector3(x,1.2f,-15.5f),new Vector3(.12f,2.4f,1),white);
                CourierVisuals.Part(root,"OpenGlassLeaf",new Vector3(x,1.2f,-15.5f),new Vector3(.14f,2.1f,.8f),glass);
            }
            foreach(float x in new[]{-14f,-10f})CourierVisuals.Part(root,"EntranceRail",new Vector3(x,.3f,-19.5f),new Vector3(.18f,.6f,7),blue,true);
            CourierVisuals.Part(root,"WalkEnd",new Vector3(-12,.3f,-23.4f),new Vector3(4,.6f,.18f),blue,true);
            CourierVisuals.Part(root,"EntranceWalk",new Vector3(-12,-.09f,-19),new Vector3(4,.18f,8),white,true);
        }
    }
}
