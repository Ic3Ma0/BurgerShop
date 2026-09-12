using BurgerShop.Core;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class FurnitureVisual : MonoBehaviour
    {
        Material accent, dark;
        public static void Apply(DiningTable table)
        {
            var old=table.transform.Find("FurnitureDetail");
            if(old!=null){old.gameObject.SetActive(false);BurgerVisual.Release(old.gameObject);}
            foreach(string name in new[]{"ChairA","ChairB"})
            {var chair=table.transform.Find(name);if(chair!=null)foreach(var render in chair.GetComponentsInChildren<Renderer>())render.SetPropertyBlock(null);}
            if(table.FurnitureLevel==1)return;
            var root=new GameObject("FurnitureDetail");root.transform.SetParent(table.transform,false);
            var owner=root.AddComponent<FurnitureVisual>();
            owner.accent=RuntimeMaterials.Create(new Color(.15f,.44f,.8f));owner.dark=RuntimeMaterials.Create(new Color(.12f,.17f,.22f));
            foreach(string name in new[]{"ChairA","ChairB"})
            {
                var chair=table.transform.Find(name);if(chair==null)continue;
                foreach(var render in chair.GetComponentsInChildren<Renderer>())
                {var props=new MaterialPropertyBlock();props.SetColor("_BaseColor",render.name=="Seat"?new Color(.15f,.44f,.8f):new Color(.12f,.17f,.22f));render.SetPropertyBlock(props);}
                var detail=new GameObject(name+"Trim").transform;detail.SetParent(root.transform,false);detail.localPosition=chair.localPosition;detail.localRotation=chair.localRotation;
                owner.Part(detail,PrimitiveType.Cube,new Vector3(0,.31f,0),new Vector3(.66f,.07f,.6f),owner.dark);
                if(table.FurnitureLevel>=3)owner.Part(detail,PrimitiveType.Sphere,new Vector3(0,.75f,-.17f),new Vector3(.60f,.5f,.17f),owner.accent);
                if(table.FurnitureLevel>=4)
                    foreach(float x in new[]{-.3f,.3f})owner.Part(detail,PrimitiveType.Cube,new Vector3(x,.64f,0),new Vector3(.09f,.09f,.55f),owner.dark);
            }
            if(table.FurnitureLevel>=3)
                foreach(float x in new[]{-.63f,.63f})owner.Part(root.transform,PrimitiveType.Cube,new Vector3(x,.75f,0),new Vector3(.05f,.13f,1.25f),owner.accent);
        }
        void Part(Transform parent,PrimitiveType type,Vector3 position,Vector3 size,Material mat)
        {
            var obj=GameObject.CreatePrimitive(type);obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=size;
            obj.GetComponent<Renderer>().sharedMaterial=mat;var c=obj.GetComponent<Collider>();c.enabled=false;BurgerVisual.Release(c);
        }
        void OnDestroy(){if(accent!=null)BurgerVisual.Release(accent);if(dark!=null)BurgerVisual.Release(dark);}
    }
}
