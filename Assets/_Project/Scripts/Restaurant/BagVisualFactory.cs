using BurgerShop.Core;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public static class BagVisualFactory
    {
        public static Transform Create(Transform parent,bool filled)
        {
            var root=new GameObject(filled?"BaggedBurger":"EmptyBag");root.transform.SetParent(parent,false);
            var paper=RuntimeMaterials.Create(new Color(.86f,.65f,.36f));
            var ink=RuntimeMaterials.Create(filled?new Color(.85f,.2f,.15f):new Color(.3f,.2f,.12f));
            root.AddComponent<BurgerVisual>().OwnMaterials(paper,ink);
            Part(root.transform,"Paper",PrimitiveType.Cube,new Vector3(0,.26f,0),new Vector3(.55f,.52f,filled?.32f:.10f),paper);
            Part(root.transform,"Fold",PrimitiveType.Cube,new Vector3(0,.52f,0),new Vector3(.58f,.07f,filled?.34f:.12f),paper);
            foreach(float x in new[]{-.16f,.16f})Part(root.transform,"Handle",PrimitiveType.Cube,new Vector3(x,.65f,0),new Vector3(.045f,.27f,.05f),ink);
            Part(root.transform,"HandleTop",PrimitiveType.Cube,new Vector3(0,.77f,0),new Vector3(.36f,.045f,.05f),ink);
            if(filled)
            {
                Part(root.transform,"Brand",PrimitiveType.Cube,new Vector3(0,.28f,-.17f),new Vector3(.4f,.28f,.025f),ink);
                var burger=BurgerVisualFactory.Create(root.transform,0);burger.localPosition=new Vector3(0,.22f,-.22f);burger.localScale=Vector3.one*.28f;
            }
            return root.transform;
        }
        public static GameObject Part(Transform parent,string name,PrimitiveType type,Vector3 local,Vector3 size,Material material,bool solid=false)
        {
            var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=local;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid){var c=obj.GetComponent<Collider>();c.enabled=false;BurgerVisual.Release(c);}return obj;
        }
    }
}
