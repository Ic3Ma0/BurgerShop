using System;
using UnityEngine;

namespace BurgerShop.Economy
{
    public sealed class PartsWallet : MonoBehaviour
    {
        public long Balance { get; private set; }
        public event Action Changed;
        public void Restore(long value)
        {
            if(value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Balance=value; Changed?.Invoke();
        }
        public bool TryCollect(int quantity)
        {
            if(quantity<=0||Balance>long.MaxValue-quantity)return false;
            Balance+=quantity;Changed?.Invoke();return true;
        }
    }
}
