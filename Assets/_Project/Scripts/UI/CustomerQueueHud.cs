using BurgerShop.Customer;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class CustomerQueueHud : MonoBehaviour
    {
        CustomerQueue queue;
        Text label;
        int shownCount = -1;
        bool shownReady;

        public void Configure(CustomerQueue customerQueue, Text text)
        {
            queue = customerQueue;
            label = text;
            Refresh();
        }

        void LateUpdate() => Refresh();

        void Refresh()
        {
            if (queue == null || label == null) return;
            bool ready = queue.ReadyCustomer != null;
            if (shownCount == queue.Count && shownReady == ready) return;
            shownCount = queue.Count;
            shownReady = ready;
            string hint = ready ? "Next order: 1 BURGER" : queue.Count == 0 ? "Customers are arriving" : "Walking to the queue";
            label.text = $"CUSTOMERS  {queue.Count}/{queue.Capacity}\n{hint}";
        }
    }
}
