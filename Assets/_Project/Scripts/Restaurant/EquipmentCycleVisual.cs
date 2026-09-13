using UnityEngine;
namespace BurgerShop.Restaurant
{
    // Mirrors production progress; never produces items or changes timers.
    public sealed class EquipmentCycleVisual : MonoBehaviour
    {
        public Transform[] Lids;
        public GameObject Stream;
        ProductionStation station;
        void Update()
        {
            if(station==null)station=GetComponentInParent<ProductionStation>();
            if(station==null)return;
            float p=station.NormalizedProgress;bool full=station.Stock>=station.Capacity;
            if(Lids!=null)foreach(var lid in Lids)
            {
                float angle=full?50:p<.12f?Mathf.Lerp(50,5,p/.12f):p>.85f?Mathf.Lerp(5,50,(p-.85f)/.15f):5;
                lid.localRotation=Quaternion.Euler(angle,0,0);
            }
            if(Stream!=null)Stream.SetActive(!full&&p>.12f&&p<.88f);
        }
    }
}
