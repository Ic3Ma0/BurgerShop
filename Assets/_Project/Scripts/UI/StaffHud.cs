using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class StaffHud : MonoBehaviour
    {
        WorkerHiringZone hiring;
        Text status;
        Text details;
        Image progress;
        CanvasGroup panel;

        public void Configure(WorkerHiringZone zone, Text statusText, Text detailText, Image fill, CanvasGroup group)
        {
            hiring = zone;
            status = statusText;
            details = detailText;
            progress = fill;
            panel = group;
            if (status != null)
            {
                status.raycastTarget = false;
                HudChrome.HideFromDefaultScreen(status.gameObject);
                RectTransform rect = status.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-24f, 196f);
                rect.sizeDelta = new Vector2(460f, 36f);
                status.alignment = TextAnchor.LowerRight;
            }
            if (details != null) details.raycastTarget = false;
            HudChrome.Mute(fill);
            Refresh();
        }

        void LateUpdate() => Refresh();

        void Refresh()
        {
            if (hiring == null) return;
            if (panel != null)
            {
                panel.alpha = hiring.IsInRange ? 1f : 0f;
                panel.blocksRaycasts = false;
                panel.interactable = false;
            }
            if (progress != null) progress.fillAmount = hiring.Progress;
            RestaurantWorker worker = hiring.Worker;
            if (hiring.IsFull)
            {
                if (status != null)
                    status.text = worker != null
                        ? $"STAFF {hiring.HiredCount}/3  |  DELIVERED {hiring.TotalDeliveries}\n{worker.Activity}"
                        : "STAFF 3/3";
                if (details != null)
                    details.text = "STAFF FULL - 3 / 3\nStock, serve, and clear tables\nNo further hires";
                return;
            }
            if (hiring.HiredCount > 0)
            {
                if (status != null)
                    status.text = worker != null
                        ? $"STAFF {hiring.HiredCount}/3  |  DELIVERED {hiring.TotalDeliveries}\n{worker.Activity}"
                        : $"STAFF {hiring.HiredCount}/3";
                if (details != null)
                {
                    string hint = !hiring.IsAvailable ? "Hiring unavailable" : hiring.MissingCoins > 0
                        ? $"Need {hiring.MissingCoins} more coins" : $"Stay here to hire - {Mathf.FloorToInt(hiring.Progress * 100f)}%";
                    details.text = $"HIRE STAFF - {hiring.HireCost} COINS\n{hiring.HiredCount}/3 hired · next {hiring.HireCost}\n{hint}";
                }
                return;
            }
            if (status != null) status.text = $"STAFF AVAILABLE - {hiring.HireCost} COINS\nGo to the HR office";
            string firstHint = !hiring.IsAvailable ? "Hiring unavailable" : hiring.MissingCoins > 0
                ? $"Need {hiring.MissingCoins} more coins" : $"Stay here to hire - {Mathf.FloorToInt(hiring.Progress * 100f)}%";
            if (details != null)
                details.text = $"HIRE STAFF - {hiring.HireCost} COINS\nStocks counter, serves, clears tables\n{firstHint}";
        }
    }
}
