using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec026028Tests
    {
        static void Invoke(object instance,string method,params object[] args) => instance.GetType().GetMethod(method,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(instance,args);
        [Test]
        public void CashPilesNeverBecomeBurgerCardsOrObscureOrders()
        {
            var root=new GameObject("CashLabelTest",typeof(RectTransform));
            try
            {
                var pile=new GameObject("CashPile",typeof(BurgerShop.Restaurant.CashPickup));pile.transform.SetParent(root.transform);
                var amount=new GameObject("Amount",typeof(TextMesh));amount.transform.SetParent(pile.transform);amount.GetComponent<TextMesh>().text="20";
                var labels=WorldLabelHud.Build(root.transform,root.transform);
                labels.Discover();
                Assert.That(labels.transform.Find("Card_Amount"),Is.Null);
                Assert.That(amount.GetComponent<Renderer>().enabled,Is.False);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [TestCase(0f,0f)][TestCase(.09f,0f)][TestCase(.1f,0f)][TestCase(.11f,.011111f)][TestCase(.5f,.444444f)][TestCase(1f,1f)]
        public void JoystickHasExactlyOneContinuousRadialDeadzone(float raw,float expected)
        {
            Assert.That(VirtualJoystick.MapInput(Vector2.right*raw).magnitude,Is.EqualTo(expected).Within(.00001f));
            Assert.That(VirtualJoystick.MapInput(Vector2.one.normalized*raw).magnitude,Is.EqualTo(expected).Within(.00001f));
        }
        [Test]
        public void PointerOwnershipCancellationAndReleaseAreRepeatable()
        {
            var root=new GameObject("PointerTest",typeof(RectTransform),typeof(Canvas),typeof(EventSystem));
            var pad=new GameObject("Pad",typeof(RectTransform));pad.transform.SetParent(root.transform);
            var joystick=pad.AddComponent<VirtualJoystick>();Invoke(joystick,"Awake");
            try
            {
                var system=root.GetComponent<EventSystem>();
                for(int i=0;i<10;i++)
                {
                    var primary=new PointerEventData(system){pointerId=1,position=Vector2.one*100};
                    var secondary=new PointerEventData(system){pointerId=2,position=-Vector2.one*100};
                    joystick.OnPointerDown(primary);var first=VirtualJoystick.Value;
                    joystick.OnPointerDown(secondary);joystick.OnDrag(secondary);Assert.That(VirtualJoystick.Value,Is.EqualTo(first));
                    joystick.OnPointerUp(secondary);Assert.That(joystick.HasPointer,Is.True);
                    joystick.OnPointerUp(primary);Assert.That(VirtualJoystick.Value,Is.EqualTo(Vector2.zero));
                    joystick.OnDrag(secondary);Assert.That(VirtualJoystick.Value,Is.EqualTo(Vector2.zero));
                    joystick.OnPointerDown(primary);joystick.OnCancel(primary);Assert.That(VirtualJoystick.Value,Is.EqualTo(Vector2.zero));
                    joystick.OnPointerDown(primary);Invoke(joystick,"OnApplicationPause",true);Assert.That(VirtualJoystick.Value,Is.EqualTo(Vector2.zero));
                    Invoke(joystick,"OnApplicationPause",false);Assert.That(joystick.HasPointer,Is.False);
                    joystick.OnPointerDown(primary);Invoke(joystick,"OnApplicationFocus",false);Assert.That(VirtualJoystick.Value,Is.EqualTo(Vector2.zero));
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [TestCase(0L,"0")][TestCase(999L,"999")][TestCase(12345L,"12,345")][TestCase(9999999L,"9,999,999")]
        public void ExactCoinAmountsFitWithoutRounding(long amount,string expected)
        {
            var root=new GameObject("HUD",typeof(RectTransform));var wallet=root.AddComponent<RestaurantWallet>();wallet.RestoreProgress(amount,0);
            try{var hud=SalesHud.Build(root.transform,wallet);var text=hud.GetComponent<Text>();Assert.That(text.text,Is.EqualTo(expected));Assert.That(text.preferredWidth,Is.LessThanOrEqualTo(text.rectTransform.rect.width-56));}
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void UpgradePricesUseRealCoinsAndMaxNeverHasANextPrice()
        {
            var root=new GameObject("HUD",typeof(RectTransform));
            try
            {
                var p=StatUpgradePopup.Build(root.transform,"PlayerUpgradePopup","Player upgrades","Speed","Carry");
                p.PaintStat(true,"Speed",0,"100%","115%",false,50,20);
                Assert.That(p.FirstLabel.text,Does.Contain("Need 30 more"));Assert.That(p.FirstButton.interactable,Is.False);
                int purchases=0;p.Bind(()=>purchases++,null);p.ClickFirst();Assert.That(purchases,Is.Zero);
                p.PaintStat(true,"Speed",5,"175%","175%",true,0,9999);
                Assert.That(p.FirstLabel.text,Does.Contain("MAX").And.Not.Contain("coins").And.Not.Contain("→"));
                p.PaintStat(true,"Speed",0,"100%","115%",false,50,50);p.ClickFirst();Assert.That(purchases,Is.EqualTo(1));
                foreach(var b in new[]{p.FirstButton,p.SecondButton,p.CloseButton}){var r=(RectTransform)b.transform;Assert.That(r.rect.width,Is.GreaterThanOrEqualTo(132));Assert.That(r.rect.height,Is.GreaterThanOrEqualTo(132));}
            }finally{Object.DestroyImmediate(root);}
        }
        [TestCase(1080,1920,0)][TestCase(1080,2358,100)][TestCase(1440,2560,120)]
        public void TopHudAndPanelRespectSafeAreaAndJoystick(int width,int height,int notch)
        {
            var root=new GameObject("Canvas",typeof(RectTransform));var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
            var r=(RectTransform)root.transform;r.sizeDelta=new Vector2(1080,1920f*height/width*1080/1920);
            try
            {
                safe.AddComponent<SafeAreaFitter>().Apply(new Rect(0,0,width,height-notch),width,height);
                var wallet=root.AddComponent<RestaurantWallet>();wallet.RestoreProgress(9999999,0);var goals=root.AddComponent<SessionGoalTracker>();goals.Configure(null,null,null,null,null,null);
                var stars=StarProgressHud.Build(safe.transform,goals);var sales=SalesHud.Build(safe.transform,wallet);var task=TaskCapsuleHud.Build(safe.transform,goals);
                var popup=StatUpgradePopup.Build(safe.transform,"PlayerUpgradePopup","Player upgrades","Speed","Carry");var sr=(RectTransform)safe.transform;
                Assert.That(HudChrome.OverlapsJoystickKeepout(popup.Panel,sr),Is.False);
                Assert.That(304f/sr.rect.height,Is.LessThanOrEqualTo(.18f));
                Assert.That(((RectTransform)task.transform).rect.width/sr.rect.width,Is.LessThanOrEqualTo(.85f));
                Assert.That(HudChrome.LocalRect((RectTransform)stars.transform,sr).Overlaps(HudChrome.LocalRect((RectTransform)sales.transform,sr)),Is.False);
            }finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void CashAggregationAndSoundThrottlingCannotChangeWallet()
        {
            var root=new GameObject("Wallet");var wallet=root.AddComponent<RestaurantWallet>();var aggregate=new PickupAccumulator();
            try
            {
                foreach(int amount in new[]{10,20,15}){wallet.CollectCoins(amount);aggregate.Add(amount);aggregate.Advance(.1f);}
                Assert.That(aggregate.Amount,Is.EqualTo(45));Assert.That(wallet.Coins,Is.EqualTo(45));
                Assert.That(wallet.TrySpend(40),Is.True);Assert.That(wallet.Coins,Is.EqualTo(5));
                aggregate.Advance(.6f);Assert.That(aggregate.Age,Is.GreaterThanOrEqualTo(.6f));
                var budget=new FeedbackBudget();Assert.That(budget.Accept(FeedbackSound.Spend,0),Is.True);
                Assert.That(budget.Accept(FeedbackSound.Spend,.19f),Is.False);Assert.That(budget.Accept(FeedbackSound.Spend,.20f),Is.True);
                Assert.That(budget.Accept(FeedbackSound.Cash,0),Is.True);Assert.That(budget.Accept(FeedbackSound.Cash,.119f),Is.False);Assert.That(budget.Accept(FeedbackSound.Cash,.12f),Is.True);
            }finally{Object.DestroyImmediate(root);}
        }
    }
}
