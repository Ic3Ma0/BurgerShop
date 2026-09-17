using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class CashPickup : MonoBehaviour
    {
        public const int Amount = 10;
        public const float FlyDuration = Economy.CashCollectionFeel.FlightSeconds;
        public const float BillWidth = 0.42f;
        public const float BillThickness = 0.03f;
        public const float BillDepth = 0.26f;
        public const float FaceTilt = 0f;
        public const float BobAmplitude = 0f;
        public const float BobRadians = 2.6f;
        public const float SpinDegreesPerSecond = 0f;
        public static readonly Color BillColor = BurgerShop.Core.BanknoteLook.Green;
        public static readonly Color StripeColor = BurgerShop.Core.BanknoteLook.Ink;

        public const float BaseStackHeight=.12f;
        float initialHeight;
        int initialValue;
        public float StackHeight=>initialHeight * (Value / (float)initialValue);
        public int Value { get; private set; }
        public bool IsCollecting { get; private set; }
        public TrashMotion Motion { get; private set; }
        public Transform Visual { get; private set; }
        public TextMesh AmountLabel { get; private set; }

        float age;
        float phase;

        public static CashPickup Create(Transform parent, Vector3 position, int amount, int stackIndex)
            => Create(parent, position, amount, stackIndex, false);

        internal static CashPickup CreatePacket(Transform parent, Vector3 position, int amount)
            => Create(parent, position, amount, 0, true);

        static CashPickup Create(Transform parent, Vector3 position, int amount, int stackIndex, bool packet)
        {
            Transform root = BuildBills(parent, stackIndex, packet);
            root.position = position;
            root.rotation = Quaternion.Euler(0f, 0f, 0f);
            CashPickup pickup = root.gameObject.AddComponent<CashPickup>();
            pickup.Value = Mathf.Max(1, amount);
            pickup.initialValue = pickup.Value;
            pickup.initialHeight = BaseStackHeight*Mathf.Clamp(Mathf.CeilToInt(pickup.Value/(float)Amount),1,20);
            pickup.Visual = root.Find("Visual");
            pickup.AmountLabel = pickup.Visual.Find("Amount").GetComponent<TextMesh>();
            pickup.AmountLabel.text = pickup.Value.ToString();
            pickup.phase = stackIndex * 0.85f;
            pickup.TickIdle(0f);
            SphereCollider trigger = root.gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.62f;
            trigger.center = new Vector3(0f, 0.06f, 0f);
            return pickup;
        }

        internal void SetValue(int value)
        {
            Value = value;
            AmountLabel.text = value.ToString();
        }

        public void LaunchTo(Transform collector, System.Action arrived)
        {
            if (IsCollecting || collector == null) return;
            IsCollecting = true;
            // A tall floor stack becomes one thin bundle in the stream.
            Visual.localScale = Economy.CashCollectionFeel.PacketScale;
            foreach(Transform child in Visual)
            {
                bool keep = child.name == "Bill_0" || child.name.EndsWith("_0") || child.name == "BillFace_0";
                child.gameObject.SetActive(keep);
                if(keep)child.localPosition = new Vector3(0,child.localPosition.y,0);
            }
            Motion = gameObject.AddComponent<TrashMotion>();
            // Lift out of the stack's silhouette before curving into the chest, so tall piles cannot hide the stream.
            Vector3 towardCamera=Camera.main!=null?Camera.main.transform.position-collector.position:Vector3.back;
            towardCamera.y=0;
            Vector3 arc=Vector3.up*Economy.CashCollectionFeel.ArcHeight+towardCamera.normalized*Economy.CashCollectionFeel.ArcTowardCamera;
            Motion.Launch(collector, Economy.CashCollectionFeel.ReceiverOffset, arc, Vector3.one * 0.2f, arrived,FlyDuration);
        }

        public void Advance(float deltaTime)
        {
            if (IsCollecting) Motion?.Advance(deltaTime);
            else TickIdle(deltaTime);
        }

        void TickIdle(float deltaTime)
        {
            if (Visual == null) return;
            age += Mathf.Max(0f, deltaTime);
            float bob = Mathf.Sin(age * BobRadians + phase) * BobAmplitude;
            Visual.localPosition = new Vector3(0f, bob, 0f);
            Visual.localRotation = Quaternion.Euler(FaceTilt, age * SpinDegreesPerSecond, 0f);
            FaceAmountTowardCamera();
        }

        void FaceAmountTowardCamera()
        {
            if (AmountLabel == null) return;
            Camera cam = Camera.main;
            if (cam != null)
            {
                AmountLabel.transform.rotation = Quaternion.LookRotation(
                    AmountLabel.transform.position - cam.transform.position, Vector3.up);
                return;
            }
            AmountLabel.transform.localRotation = Quaternion.Euler(28f, 180f, 0f);
        }

        static Transform BuildBills(Transform parent, int index, bool packet)
        {
            GameObject root = new GameObject("Cash_" + index);
            root.transform.SetParent(parent, false);
            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            Material bill = BurgerShop.Core.RuntimeMaterials.Create(BillColor, true);
            Material stripe = BurgerShop.Core.RuntimeMaterials.Create(Color.white, true);
            stripe.mainTexture=BurgerShop.Core.BanknoteLook.Texture;
            root.AddComponent<BurgerVisual>().OwnMaterials(bill,stripe);
            for(int layer=0;layer<3;layer++)for(int i=0;i<(packet?1:6);i++)
            {
                int col=i%2,row=i/2;
                Vector3 pos=new Vector3((col-.5f)*.46f,.016f+layer*.04f,(row-1f)*.32f);
                Part(visual,layer==0?"Bill_"+i:"StackBill_"+layer+"_"+i,pos,new Vector3(BillWidth,BillThickness,BillDepth),bill);
                if(layer!=2)continue;
                Part(visual,i==0?"BillFace_0":"BillFace",pos+Vector3.up*.019f,new Vector3(BillWidth,.006f,BillDepth),stripe);

            }
            TextMesh label = new GameObject("Amount").AddComponent<TextMesh>();
            label.transform.SetParent(visual, false);
            label.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.09f;
            label.fontSize = 52;
            label.color = new Color(1f, 0.98f, 0.72f);
            label.text = Amount.ToString();
            label.GetComponent<Renderer>().enabled=false; // Amount is shown by wallet/pickup receipt, not on every layer.
            return root.transform;
        }

        static void Part(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            collider.enabled = false;
            BurgerVisual.Release(collider);
        }
    }
}
