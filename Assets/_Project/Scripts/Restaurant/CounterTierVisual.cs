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
            if(product==FoodIcon.Box){BuildBlueCounter(value);return;}
            Color accent = product == FoodIcon.Cola ? new Color(.15f,.43f,.78f)
                : product == FoodIcon.Bagged ? HudChrome.Green : HudChrome.Tomato;
            Material paint = RuntimeMaterials.Create(accent);
            Material cream = RuntimeMaterials.Create(HudChrome.Cream);
            Material metal = RuntimeMaterials.Create(value == 3 ? HudChrome.Gold : new Color(.45f,.50f,.55f));
            Material dark = RuntimeMaterials.Create(new Color(.13f,.18f,.21f));
            Material light = RuntimeMaterials.Create(new Color(.45f,1f,.83f), true);
            look.gameObject.AddComponent<BurgerVisual>().OwnMaterials(paint,cream,metal,dark,light);
            float front = -depth * .5f - .025f;
            Part("FrontPanel", new Vector3(0,height*.48f,front), new Vector3(width*.94f,height*.75f,.06f), value == 1 ? cream : paint);
            Part("CounterRim", new Vector3(0,height+.08f,0), new Vector3(width+.08f,.10f,depth+.06f), value == 3 ? metal : paint);
            Part("FrontStripe", new Vector3(0,height*.66f,front-.04f), new Vector3(width*.92f,.08f,.025f), value == 1 ? paint : cream);
            if(value >= 2)
            {
                Part("RegisterBase", new Vector3(-width*.30f,height+.20f,0),new Vector3(.40f,.18f,.35f),dark);
                Part("RegisterScreen", new Vector3(-width*.30f,height+.43f,.04f),new Vector3(.46f,.36f,.09f),dark);
                Part("Display", new Vector3(-width*.30f,height+.43f,-.012f),new Vector3(.36f,.24f,.018f),light);
                for(int i=0;i<3;i++) Part("Drawer_"+i,new Vector3(width*.20f,height*(.25f+i*.15f),front-.04f),new Vector3(width*.35f,.055f,.035f),metal);
                Part("LeftTrim",new Vector3(-width*.45f,height*.48f,front-.045f),new Vector3(.09f,height*.80f,.04f),metal);
                Part("RightTrim",new Vector3(width*.45f,height*.48f,front-.045f),new Vector3(.09f,height*.80f,.04f),metal);
            }
            if(value == 3)
            {
                // Posts at the back leave the stock and handoff area visible from the game camera.
                for(int side=-1;side<=1;side+=2)
                    Part("CanopyPost_"+side,new Vector3(side*width*.44f,height+.65f,depth*.38f),new Vector3(.10f,1.30f,.10f),metal);
                Part("Canopy",new Vector3(0,height+1.27f,depth*.30f),new Vector3(width+.12f,.24f,.45f),paint);
                Part("CanopyLight",new Vector3(0,height+1.12f,depth*.30f),new Vector3(width*.90f,.035f,.38f),light);
                var badge = new GameObject("ProductBadge",typeof(SpriteRenderer));badge.transform.SetParent(look,false);
                badge.transform.localPosition=new Vector3(0,height+1.27f,depth*.30f-.235f);
                badge.transform.localScale=Vector3.one*.34f;
                badge.GetComponent<SpriteRenderer>().sprite=FoodIcons.Get(product);
                Part("FootRail",new Vector3(0,.14f,front-.02f),new Vector3(width*.92f,.09f,.10f),metal);
            }
        }
        void BuildBlueCounter(int level)
        {
            var cyan=RuntimeMaterials.Create(new Color(.40f,.71f,.78f));
            var dark=RuntimeMaterials.Create(new Color(.08f,.16f,.21f));
            var trim=RuntimeMaterials.Create(level==3?new Color(.74f,.90f,.94f):new Color(.25f,.48f,.56f));
            look.gameObject.AddComponent<BurgerVisual>().OwnMaterials(cyan,dark,trim);
            Part("BlueWorktop",new Vector3(0,height+.07f,0),new Vector3(width+.10f,.12f,depth+.08f),cyan);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<(width>3?3:2);i++)
            {
                int count=width>3?3:2;float panel=width/count;
                float x=-width*.5f+panel*(i+.5f);
                Part("BlueCabinet",new Vector3(x,height*.47f,side*(depth*.5f+.02f)),new Vector3(panel-.10f,height*.77f,.045f),cyan);
                Part("Handle",new Vector3(x,height*.68f,side*(depth*.5f+.05f)),new Vector3(.28f,.045f,.035f),dark);
            }
            Part("BaseTrim",new Vector3(0,.09f,0),new Vector3(width+.05f,.12f,depth+.06f),dark);
            if(level>=2)Part("UpgradeTrim",new Vector3(0,height*.82f,-depth*.5f-.055f),new Vector3(width,.045f,.035f),trim);
            if(level==3)foreach(int side in new[]{-1,1})Part("PremiumCorner",new Vector3(side*(width*.5f-.05f),height*.5f,-depth*.5f-.055f),new Vector3(.09f,height*.8f,.05f),trim);
            if(width>3)
            {
                for(int i=0;i<40;i++)
                {
                    float angle=i*Mathf.PI*2/40;
                    Part("IngredientRing",new Vector3(1.2f+Mathf.Sin(angle)*.47f,height+.138f,Mathf.Cos(angle)*.47f),new Vector3(.06f,.012f,.06f),trim);
                }
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
