using BurgerShop.Core;
using BurgerShop.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class SalesHud : MonoBehaviour
    {
        RestaurantWallet wallet;
        Text label;
        readonly NumberPunch punch = new NumberPunch();
        readonly CoinRoll roll = new CoinRoll();
        long shownCoins = -1;
        long lastPayment;
        float paymentUntil;
        Color restColor = Color.white;

        public bool IsPunching => punch.IsActive;
        public float PunchScale => punch.Scale;
        public long DisplayedCoins => roll.Value;

        public static SalesHud Build(Transform parent, RestaurantWallet earnings)
        {
            var hud = new GameObject("SalesStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            hud.transform.SetParent(parent, false);
            RectTransform rect = hud.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-56f, -40f);
            rect.sizeDelta = new Vector2(352f, 64f);
            Text text = hud.GetComponent<Text>();
            HudChrome.Style(text, 40, HudChrome.Ink, TextAnchor.MiddleRight, true, true);
            HudChrome.Icon(hud.transform, "CoinIcon", FoodIcons.Get(FoodIcon.Coin), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 0f), new Vector2(48f, 48f), Color.white);
            var card = HudChrome.Panel(parent, "CoinCard", Vector2.one, Vector2.one,
                new Vector2(-32,-24), new Vector2(400, 96), HudChrome.Cream);
            card.transform.SetSiblingIndex(hud.transform.GetSiblingIndex());
            var component = hud.AddComponent<SalesHud>();
            component.Configure(earnings, text);
            return component;
        }

        public void Configure(RestaurantWallet earnings, Text text)
        {
            wallet = earnings;
            label = text;
            if (label != null)
            {
                label.raycastTarget = false;
                restColor = label.color;
            }
            shownCoins = -1;
            paymentUntil = 0f;
            Advance(0f);
        }

        void LateUpdate() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            SyncWallet();
            punch.Advance(deltaTime);
            roll.Advance(deltaTime);
            if (deltaTime > 0f) paymentUntil = Mathf.Max(0f, paymentUntil - deltaTime);
            Paint();
        }

        void SyncWallet()
        {
            if (wallet == null) return;
            if (shownCoins < 0)
            {
                shownCoins = wallet.Coins;
                roll.Snap(shownCoins);
                return;
            }
            if (wallet.Coins == shownCoins) return;
            lastPayment = wallet.Coins - shownCoins;
            paymentUntil = 1.4f;
            if (lastPayment > 0)
            {
                roll.Play(roll.Value, wallet.Coins);
                punch.Play();
            }
            else
            {
                roll.Snap(wallet.Coins);
                punch.Play();
            }
            shownCoins = wallet.Coins;
        }

        void Paint()
        {
            if (label == null) return;
            string flash = "";
            label.text = $"{roll.Value:N0}{flash}";
            label.color = restColor;
            transform.localScale = new Vector3(punch.Scale, punch.Scale, 1f);
        }
    }
}
