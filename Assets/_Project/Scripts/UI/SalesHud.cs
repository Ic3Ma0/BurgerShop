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
            rect.anchoredPosition = new Vector2(-18f, -12f);
            rect.sizeDelta = new Vector2(210f, 58f);
            Text text = hud.GetComponent<Text>();
            HudChrome.Style(text, 36, Color.white, TextAnchor.MiddleRight, true, true);
            HudChrome.Icon(hud.transform, "CoinIcon", HudChrome.Bill(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 0f), new Vector2(52f, 34f), Color.white);
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
                roll.Play(shownCoins, wallet.Coins);
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
            string flash = paymentUntil > 0f ? $"\n{(lastPayment > 0 ? "+" : "")}{lastPayment}" : "";
            label.text = $"{roll.Value}{flash}";
            label.color = paymentUntil > 0f && lastPayment < 0
                ? new Color(1f, 0.94f, 0.62f, 1f)
                : restColor;
            transform.localScale = new Vector3(punch.Scale, punch.Scale, 1f);
        }
    }
}
