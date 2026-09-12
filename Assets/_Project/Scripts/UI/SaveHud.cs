using BurgerShop.Persistence;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class SaveHud : MonoBehaviour
    {
        RestaurantPersistence persistence;
        Text label;
        public void Configure(RestaurantPersistence save, Text text) { persistence = save; label = text; }
        void LateUpdate()
        {
            if (persistence != null && label != null) label.text = persistence.LoadResult == SaveLoadResult.NewerVersion || persistence.LoadResult == SaveLoadResult.Unreadable || persistence.LoadResult == SaveLoadResult.Unavailable ? persistence.Status : "";
        }
    }
}
