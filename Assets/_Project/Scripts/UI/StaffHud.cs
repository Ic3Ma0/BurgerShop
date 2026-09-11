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
        bool? landscapeLayout;

        public void Configure(WorkerHiringZone zone, Text statusText, Text detailText, Image fill, CanvasGroup group)
        {
            hiring = zone;
            status = statusText;
            details = detailText;
            progress = fill;
            panel = group;
            Refresh();
            UpdateLayout();
        }

        void OnEnable() => Canvas.preWillRenderCanvases += UpdateLayout;
        void OnDisable() => Canvas.preWillRenderCanvases -= UpdateLayout;
        void LateUpdate() => Refresh();

        void UpdateLayout()
        {
            if (status == null || status.canvas == null) return;
            Rect canvasRect = ((RectTransform)status.canvas.transform).rect;
            bool landscape = canvasRect.width > canvasRect.height;
            if (landscapeLayout == landscape) return;
            landscapeLayout = landscape;
            RectTransform rect = status.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(landscape ? 0f : 0.5f, 1f);
            rect.anchoredPosition = landscape ? new Vector2(32f, -40f) : new Vector2(0f, -405f);
            rect.sizeDelta = new Vector2(landscape ? 600f : 1000f, 100f);
            status.alignment = landscape ? TextAnchor.UpperLeft : TextAnchor.UpperCenter;
        }

        void Refresh()
        {
            if (hiring == null) return;
            panel.alpha = hiring.IsInRange ? 1f : 0f;
            progress.fillAmount = hiring.Progress;
            RestaurantWorker worker = hiring.Worker;
            if (hiring.IsHired)
            {
                status.text = worker != null
                    ? $"STAFF {worker.Inventory.Count}/{worker.Inventory.Capacity}  |  DELIVERED {worker.CompletedDeliveries}\n{worker.Activity}"
                    : "STAFF unavailable";
                details.text = worker != null ? $"STAFF HIRED\nAuto pickup and delivery\nDelivered {worker.CompletedDeliveries} burgers" : "STAFF unavailable\nAlready hired - no further charge";
                return;
            }
            status.text = $"STAFF AVAILABLE - {hiring.HireCost} COINS\nFind the cyan hiring spot";
            string hint = !hiring.IsAvailable ? "Hiring unavailable" : hiring.MissingCoins > 0
                ? $"Need {hiring.MissingCoins} more coins" : $"Stay here to hire - {Mathf.FloorToInt(hiring.Progress * 100f)}%";
            details.text = $"HIRE STAFF - {hiring.HireCost} COINS\nAuto pickup + delivery, carries 2\n{hint}";
        }

        void OnDestroy()
        {
            if (progress != null && progress.sprite != null) BurgerVisual.Release(progress.sprite);
        }
    }
}
