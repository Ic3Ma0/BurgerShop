using BurgerShop.Core;
using BurgerShop.UI;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    // Decorative parts only: inventory anchors, work circles and collision stay unchanged.
    public sealed class CounterTierVisual : MonoBehaviour
    {
        float width, depth, height;
        FoodIcon product;
        Transform look;
        GrillUpgradeZone machine;
        BoxingStation packing;
        public int Level { get; private set; }

        public static CounterTierVisual Create(Transform parent, string name, Vector3 origin,
            float width, float depth, float height, FoodIcon product, int level = 1)
        {
            var existing = parent.Find(name);
            var view = existing != null ? existing.GetComponent<CounterTierVisual>() : null;
            if(view == null)
            {
                var obj = new GameObject(name); obj.transform.SetParent(parent, false);
                obj.transform.position = origin;
                view = obj.AddComponent<CounterTierVisual>();
                view.width = width; view.depth = depth; view.height = height; view.product = product;
            }
            view.SetLevel(level);
            return view;
        }
        public void Follow(GrillUpgradeZone source) { machine = source; SetLevel(source.Level); }
        public void Follow(BoxingStation source) { packing = source; SetLevel(source.WorkLevel); }
        void LateUpdate()
        {
            if(machine != null) SetLevel(machine.Level);
            else if(packing != null) SetLevel(packing.WorkLevel);
        }
        public void SetLevel(int value)
        {
            value = Mathf.Clamp(value, 1, 3);
            if(Level == value && look != null) return;
            Level = value;
            if(look != null) { look.gameObject.SetActive(false); BurgerVisual.Release(look.gameObject); }
            look = new GameObject("Look_Lv" + value).transform; look.SetParent(transform, false);
            BuildRestaurantCounter(value);
        }
        void BuildRestaurantCounter(int level)
        {
            bool packing=product==FoodIcon.Box;bool prep=packing&&width>3;
            var cream=RuntimeMaterials.Create(RestaurantStyle.Cream);
            var ink=RuntimeMaterials.Create(RestaurantStyle.Ink);
            var steel=RuntimeMaterials.Create(RestaurantStyle.Steel);
            var accent=RuntimeMaterials.Create(packing||product==FoodIcon.Cola?RestaurantStyle.Blue:RestaurantStyle.Red);
            var wood=RuntimeMaterials.Create(new Color(.63f,.39f,.20f));
            var glow=RuntimeMaterials.Create(new Color(.53f,.83f,.76f),true);
            look.gameObject.AddComponent<BurgerVisual>().OwnMaterials(cream,ink,steel,accent,wood,glow);
            RestaurantStyle.Block(look,"Worktop",new Vector3(0,height+.075f,0),new Vector3(width+.14f,.16f,depth+.14f),packing?steel:cream);
            Part("FrontPanel",new Vector3(0,height*.47f,-depth*.5f-.04f),new Vector3(width+.02f,height*.80f,.12f),cream);
            Part("BaseTrim",new Vector3(0,.10f,0),new Vector3(width+.06f,.15f,depth+.08f),ink);
            for(int side=-1;side<=1;side+=2)
            {
                Part("EndPanel",new Vector3(side*(width*.5f+.025f),height*.47f,0),new Vector3(.10f,height*.80f,depth),packing?accent:cream);
                Part("BrandBand",new Vector3(0,height*.72f,side*(depth*.5f+.12f)),new Vector3(width,.12f,.04f),accent);
            }
            int panels=width>3?3:2;
            for(int i=0;i<panels;i++)
            {
                float x=(i-(panels-1)*.5f)*width/panels;
                Part("DrawerHandle",new Vector3(x,height*.50f,-depth*.5f-.12f),new Vector3(.30f,.05f,.035f),ink);
                if(level>=2)Part("DrawerJoint",new Vector3(x+width/panels*.47f,height*.39f,-depth*.5f-.11f),new Vector3(.018f,height*.50f,.015f),steel);
            }
            if(prep)
            {
                RestaurantStyle.Block(look,"PrepBoard",new Vector3(width*.24f,height+.18f,0),new Vector3(width*.30f,.07f,depth*.70f),wood);
                Part("BoxDivider",new Vector3(-width*.36f,height+.35f,depth*.28f),new Vector3(width*.20f,.42f,.08f),accent);
                if(level>=2)Part("WrappingShelf",new Vector3(-width*.35f,height+.53f,depth*.20f),new Vector3(width*.23f,.055f,.40f),steel);
            }
            else
            {
                RestaurantStyle.Block(look,"RegisterBase",new Vector3(-width*.32f,height+.22f,0),new Vector3(.46f,.20f,.38f),ink);
                var monitor=RestaurantStyle.Block(look,"RegisterScreen",new Vector3(-width*.32f,height+.47f,.08f),new Vector3(.5f,.36f,.08f),ink);monitor.transform.localRotation=Quaternion.Euler(-12,0,0);
                Part("RegisterDisplay",new Vector3(-width*.32f,height+.48f,.026f),new Vector3(.38f,.23f,.025f),glow);
                RestaurantStyle.Block(look,"ServingTray",new Vector3(width*.24f,height+.19f,.1f),new Vector3(width*.32f,.05f,depth*.55f),steel);
            }
            if(level==3)
            {
                foreach(int side in new[]{-1,1})Part("MenuPost",new Vector3(side*width*.43f,height+.52f,depth*.38f),new Vector3(.06f,.8f,.06f),steel);
                RestaurantStyle.Block(look,"MenuHeader",new Vector3(0,height+.91f,depth*.38f),new Vector3(width*.94f,.32f,.14f),accent);
                Part("MenuLight",new Vector3(0,height+.72f,depth*.38f),new Vector3(width*.83f,.025f,.12f),cream);
                var badge=new GameObject("ProductBadge",typeof(SpriteRenderer));badge.transform.SetParent(look,false);
                badge.transform.localPosition=new Vector3(0,height+.91f,depth*.38f-.08f);badge.transform.localScale=Vector3.one*.25f;
                badge.GetComponent<SpriteRenderer>().sprite=FoodIcons.Get(product);
            }
        }
        void Part(string name, Vector3 position, Vector3 scale, Material material)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(look,false);
            obj.transform.localPosition=position;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=material;
            var collider=obj.GetComponent<Collider>();collider.enabled=false;BurgerVisual.Release(collider);
        }
    }
}
