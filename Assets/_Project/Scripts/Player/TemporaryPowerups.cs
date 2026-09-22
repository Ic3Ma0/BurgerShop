using BurgerShop.Economy;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.Player
{
    public enum PowerupKind { Gloves, Skates }

    public sealed class TemporaryPowerups : MonoBehaviour
    {
        public float GlovesRemaining { get; private set; }
        public float SkatesRemaining { get; private set; }
        public bool HasGroundCard => card != null;
        public Vector3 CardPosition => cardPosition;
        BurgerInventory inventory;
        PlayerMotor motor;
        RestaurantWallet wallet;
        GameObject card;
        Vector3 cardPosition;
        PowerupKind cardKind;
        float wait = 30, cardAge;
        bool paused, unfocused;
        Image panel;
        Text label;
        Image gloveIcon, skateIcon;

        public void Configure(BurgerInventory carrier, PlayerMotor movement, RestaurantWallet earnings, Transform hud)
        {
            inventory = carrier; motor = movement; wallet = earnings;
            if (hud == null) return;
            panel = HudChrome.Panel(hud,"Powerups",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-296),new Vector2(500,80),HudChrome.Cream);
            label = HudChrome.Label(panel.transform,"Timers",Vector2.zero,Vector2.one,Vector2.one*.5f,Vector2.zero,Vector2.zero,28,HudChrome.Ink,TextAnchor.MiddleCenter,true,false);
            gloveIcon = FoodIcons.Add(panel.transform,FoodIcon.Gloves,new Vector2(-218,0),40);
            skateIcon = FoodIcons.Add(panel.transform,FoodIcon.Skates,new Vector2(20,0),40);
            Paint();
        }

        public void Activate(PowerupKind kind)
        {
            if (kind == PowerupKind.Gloves) GlovesRemaining = 30; else SkatesRemaining = 30;
            Apply();
            FeedbackDirector.Current?.World(transform.position,kind == PowerupKind.Gloves ? "Carry x1.8" : "Speed x1.5",.9f,transform);
        }

        public void Advance(float seconds)
        {
            if (seconds <= 0 || paused || unfocused) return;
            GlovesRemaining = Mathf.Max(0,GlovesRemaining-seconds);
            SkatesRemaining = Mathf.Max(0,SkatesRemaining-seconds);
            Apply();
            if (HasGroundCard)
            {
                cardAge += seconds;
                card.transform.position = cardPosition + Vector3.up*(.25f+.07f*Mathf.Sin(cardAge*3));
                if (ShopLayout.Horizontal(transform.position,cardPosition) <= .8f)
                { Activate(cardKind); ClearCard(); }
                else if (cardAge >= 30) ClearCard();
                return;
            }
            if (wallet == null || wallet.CompletedSales < 5) return;
            wait -= seconds;
            if (wait > 0) return;
            for (int attempt=0;attempt<32;attempt++)
            {
                var point = new Vector3(Random.Range(-13f,13f),0,Random.Range(-13f,13f));
                if (TrySpawn(point,Random.value<.5f?PowerupKind.Gloves:PowerupKind.Skates)) return;
            }
            wait = 5;
        }

        public bool TrySpawn(Vector3 point, PowerupKind kind)
        {
            if (HasGroundCard || !IsValidPoint(point)) return false;
            cardPosition=point; cardKind=kind; cardAge=0;
            card=new GameObject("Powerup_"+kind);card.transform.SetParent(transform.parent,false);card.transform.position=point+Vector3.up*.25f;
            var sprite=card.AddComponent<SpriteRenderer>();sprite.sprite=FoodIcons.Get(kind==PowerupKind.Gloves?FoodIcon.Gloves:FoodIcon.Skates);
            card.transform.rotation=Quaternion.Euler(65,0,0);card.transform.localScale=Vector3.one*1.2f;
            var shadow=new GameObject("GroundShadow");shadow.transform.SetParent(card.transform,false);
            var sr=shadow.AddComponent<SpriteRenderer>();sr.sprite=HudChrome.Circle();sr.color=new Color(0,0,0,.18f);shadow.transform.localPosition=new Vector3(0,-.12f,.03f);shadow.transform.localScale=new Vector3(1.1f,.65f,1);
            return true;
        }

        public bool IsValidPoint(Vector3 p)
        {
            if (!ShopLayout.ContainsHall(p) || Mathf.Abs(p.x)>13 || Mathf.Abs(p.z)>13) return false;
            if (Physics.CheckCapsule(p+Vector3.up*.6f,p+Vector3.up*1.5f,.45f,~0,QueryTriggerInteraction.Ignore)) return false;
            foreach (var c in FindObjectsByType<Customer.CustomerAgent>(FindObjectsSortMode.None))
                if (ShopLayout.Horizontal(c.transform.position,p)<1.5f) return false;
            foreach (var c in FindObjectsByType<BurgerInventory>(FindObjectsSortMode.None))
                if (ShopLayout.Horizontal(c.transform.position,p)<1.5f) return false;
            foreach (var v in ShopLayout.QueueSlots) if (ShopLayout.Horizontal(v,p)<1.5f) return false;
            foreach (var table in FindObjectsByType<DiningTable>(FindObjectsSortMode.None))
                if (ShopLayout.Horizontal(table.Center,p)<3f) return false;
            var upgrades=GetComponentInParent<GrowthUpgrades>();
            if(upgrades!=null)foreach(var offer in upgrades.Offers)if(ShopLayout.Horizontal(offer.Position,p)<2.4f)return false;
            Vector3[] zones={ShopLayout.GrillPickup,ShopLayout.ExtraGrillPickup,ShopLayout.UpgradeSpot,ShopLayout.ExtraGrillUpgrade,
                ShopLayout.ServingCircle,ShopLayout.ExtraServingCircle,ShopLayout.TableUnlock,ShopLayout.GrillUnlock,
                ShopLayout.CounterUnlock,ShopLayout.BoxingUnlock,ShopLayout.BoxingCircle,ShopLayout.PackageDrop,
                ShopLayout.DriveThruCircle,ShopLayout.DriveThruUnlock,ShopLayout.TrashBin};
            foreach(var v in zones) if(ShopLayout.Horizontal(v,p)<2.5f)return false;
            return true;
        }

        void ClearCard(){ if(card!=null) BurgerVisual.Release(card);card=null;wait=Random.Range(45f,75f); }
        void Apply()
        {
            if(inventory!=null)inventory.CapacityMultiplier=GlovesRemaining>0?1.8f:1;
            if(motor!=null)motor.TemporarySpeedMultiplier=SkatesRemaining>0?1.5f:1;
            Paint();
        }
        void Paint()
        {
            if(panel==null)return;
            panel.gameObject.SetActive(GlovesRemaining>0||SkatesRemaining>0);
            gloveIcon.enabled=GlovesRemaining>0;skateIcon.enabled=SkatesRemaining>0;
            label.text=(GlovesRemaining>0?$"  {Mathf.CeilToInt(GlovesRemaining)}s":"")+"        "+(SkatesRemaining>0?$"  {Mathf.CeilToInt(SkatesRemaining)}s":"");
        }
        void Update()=>Advance(Time.deltaTime);
        public void SetPaused(bool value)=>paused=value;
        void OnApplicationPause(bool value)=>SetPaused(value);
        void OnApplicationFocus(bool value)=>unfocused=!value;
        void OnDisable(){GlovesRemaining=SkatesRemaining=0;Apply();ClearCard();}
    }
}
