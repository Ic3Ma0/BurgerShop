using BurgerShop.Customer;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.Economy
{
    // Samples existing live state; never changes production, customers or payments.
    public static class InvestmentObservation
    {
        public static void Read(Component root, InvestmentPriority policy, float operatingTime)
        {
            foreach (var product in new[] { KitchenProduct.Burger, KitchenProduct.Cola })
            {
                string line = product.ToString();
                int sourceStock = 0, downstream = 0, waiting = 0;
                foreach (var machine in root.GetComponentsInChildren<ProductionStation>())
                    if (machine.Product == product) sourceStock += machine.Stock;
                foreach (var carrier in root.GetComponentsInChildren<Player.BurgerInventory>())
                    sourceStock += product == KitchenProduct.Cola ? carrier.ColaCount : carrier.LooseCount;
                foreach (var serving in root.GetComponentsInChildren<BurgerServingZone>())
                {
                    var queue = serving.GetComponent<CustomerQueue>();
                    if (queue == null || queue.Product != product || serving.GetComponentInParent<BagLine>() != null) continue;
                    downstream += serving.ServiceableStock;
                    if (queue.ReadyCustomer != null) waiting += queue.Count;
                }
                policy.Observe(InvestmentNeed.Production, line, waiting > 0 && sourceStock == 0 && downstream == 0, operatingTime);
                policy.Observe(InvestmentNeed.Transport, line, waiting > 0 && sourceStock > 0 && downstream == 0, operatingTime);
                policy.Observe(InvestmentNeed.Service, line, waiting > 1 && downstream > 0, operatingTime);
            }
            bool full = false, dirty = false;
            foreach (var area in root.GetComponentsInChildren<DiningArea>())
            {
                int seats = 0, occupied = 0;
                foreach (var table in area.Tables)
                    if (table != null && table.gameObject.activeInHierarchy)
                    { seats += table.SeatCount; occupied += table.OccupiedSeats; dirty |= table.OutstandingTrash > 0; }
                full |= seats > 0 && occupied >= seats;
            }
            policy.Observe(InvestmentNeed.Seats, "dining", full && !dirty, operatingTime);
        }
    }
}
