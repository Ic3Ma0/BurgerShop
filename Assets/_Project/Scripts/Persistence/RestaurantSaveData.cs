using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BurgerShop.Persistence
{
    [Serializable]
    public sealed class RestaurantSaveData
    {
        public int version;
        public long coins;
        public int completedSales;
        public int grillLevel;
        public bool workerHired;
        public int workerDeliveries;

        public bool IsValid => version == 1 && coins >= 0 && completedSales >= 0
            && grillLevel >= 1 && grillLevel <= 3 && workerDeliveries >= 0
            && workerDeliveries <= completedSales && (workerHired || workerDeliveries == 0);

        internal string Checksum()
        {
            string value = string.Join("|", version.ToString(CultureInfo.InvariantCulture),
                coins.ToString(CultureInfo.InvariantCulture), completedSales.ToString(CultureInfo.InvariantCulture),
                grillLevel.ToString(CultureInfo.InvariantCulture), workerHired ? "1" : "0",
                workerDeliveries.ToString(CultureInfo.InvariantCulture));
            using (SHA256 hash = SHA256.Create())
                return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
    }
}
