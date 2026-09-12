using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class CashPickup : MonoBehaviour
    {
        public const int Amount = 10;
        public const float FlyDuration = 0.18f;
        public const float BillWidth = 0.42f;
        public const float BillThickness = 0.03f;
        public const float BillDepth = 0.26f;
        public const float FaceTilt = 0f;
        public const float BobAmplitude = 0f;
        public const float BobRadians = 2.6f;
        public const float SpinDegreesPerSecond = 0f;
        public static readonly Color BillColor = new Color(.38f,.88f,.10f);
        public static readonly Color StripeColor = new Color(.70f,1f,.30f);

        public int Value { get; private set; }
        public bool IsCollecting { get; private set; }
        public TrashMotion Motion { get; private set; }
        public Transform Visual { get; private set; }
        public TextMesh AmountLabel { get; private set; }

        float age;
        float phase;

        public static CashPickup Create(Transform parent, Vector3 position, int amount, int stackIndex)
        {
            Transform root = BuildBills(parent, stackIndex);
            root.position = position;
            root.rotation = Quaternion.Euler(0f, 0f, 0f);
            CashPickup pickup = root.gameObject.AddComponent<CashPickup>();
            pickup.Value = Mathf.Max(1, amount);
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

        public void LaunchTo(Transform collector, System.Action arrived)
        {
            if (IsCollecting || collector == null) return;
            IsCollecting = true;
            Motion = gameObject.AddComponent<TrashMotion>();
            Motion.Launch(collector, new Vector3(0f, 1.45f, 0.1f), Vector3.up * 0.4f, Vector3.one * 0.2f, arrived,FlyDuration);
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
            Visual.localPosition = new Vector3(0f, 0.04f + bob, 0f);
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

        static Transform BuildBills(Transform parent, int index)
        {
            GameObject root = new GameObject("Cash_" + index);
            root.transform.SetParent(parent, false);
            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            Material bill = BurgerShop.Core.RuntimeMaterials.Create(BillColor, true);
            Material stripe = BurgerShop.Core.RuntimeMaterials.Create(StripeColor, true);
            root.AddComponent<BurgerVisual>().OwnMaterials(bill,stripe);
            for(int layer=0;layer<3;layer++)for(int i=0;i<6;i++)
            {
                int col=i%2,row=i/2;
                Vector3 pos=new Vector3((col-.5f)*.46f,.016f+layer*.04f,(row-1f)*.32f);
                Part(visual,layer==0?"Bill_"+i:"StackBill_"+layer+"_"+i,pos,new Vector3(BillWidth,BillThickness,BillDepth),bill);
                if(layer!=2)continue;
                foreach(int side in new[]{-1,1})
                {
                    Part(visual,"BillBorder",pos+new Vector3(side*.19f,.018f,0),new Vector3(.015f,.006f,.23f),stripe);
                    Part(visual,"BillBorder",pos+new Vector3(0,.018f,side*.11f),new Vector3(.39f,.006f,.015f),stripe);
                }
                // A geometric S and stem avoid floating text labels being culled by the HUD.
                for(int bar=0;bar<3;bar++)Part(visual,"MoneyS",pos+new Vector3(0,.020f,(bar-1)*.064f),new Vector3(.12f,.008f,.018f),stripe);
                Part(visual,"MoneyS",pos+new Vector3(-.05f,.020f,-.032f),new Vector3(.018f,.008f,.064f),stripe);
                Part(visual,"MoneyS",pos+new Vector3(.05f,.020f,.032f),new Vector3(.018f,.008f,.064f),stripe);
                Part(visual,"MoneyStem",pos+new Vector3(0,.022f,0),new Vector3(.012f,.008f,.18f),stripe);
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
