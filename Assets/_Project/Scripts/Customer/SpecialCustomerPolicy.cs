namespace BurgerShop.Customer
{
    public enum CustomerKind { Normal, Calling, BigEater }

    public sealed class SpecialCustomerPolicy
    {
        int ordinaryRemaining;
        public CustomerKind Next(bool unlocked, bool specialInQueue, float roll)
        {
            if (ordinaryRemaining > 0) { ordinaryRemaining--; return CustomerKind.Normal; }
            if (!unlocked || specialInQueue || roll < .85f) return CustomerKind.Normal;
            ordinaryRemaining = 3;
            return roll < .93f ? CustomerKind.Calling : CustomerKind.BigEater;
        }
    }
}
