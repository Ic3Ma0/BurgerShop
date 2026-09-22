namespace BurgerShop.Customer
{
    public enum CustomerKind { Normal, Calling, BigEater }

    public sealed class SpecialCustomerPolicy
    {
        public const float SpecialThreshold = .85f;
        public const float BigEaterThreshold = .93f;
        public const int OrdinaryGap = 3;
        int ordinaryRemaining;
        public CustomerKind Next(bool unlocked, bool specialInQueue, float roll)
        {
            if (ordinaryRemaining > 0) { ordinaryRemaining--; return CustomerKind.Normal; }
            if (!unlocked || specialInQueue || roll < SpecialThreshold) return CustomerKind.Normal;
            ordinaryRemaining = OrdinaryGap;
            return roll < BigEaterThreshold ? CustomerKind.Calling : CustomerKind.BigEater;
        }
    }
}
