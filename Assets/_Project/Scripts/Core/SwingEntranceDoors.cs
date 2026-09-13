using UnityEngine;
using BurgerShop.Customer;
using BurgerShop.Player;

namespace BurgerShop.Core
{
    public sealed class SwingEntranceDoors : MonoBehaviour
    {
        const float TriggerDistance=3.2f,Degrees=85f,Smoothing=12f,HoldSeconds=.3f,ScanInterval=.1f;
        Transform[] hinges;
        float scan,hold,direction=1;
        public void Configure(Transform[] leaves)=>hinges=leaves;
        void Update()
        {
            if(hinges==null||Time.deltaTime<=0)return;
            scan-=Time.deltaTime;hold-=Time.deltaTime;
            if(scan<=0)
            {
                scan=ScanInterval;Transform nearest=null;float distance=TriggerDistance;
                foreach(var customer in FindObjectsByType<CustomerAgent>(FindObjectsSortMode.None))Consider(customer.transform,ref nearest,ref distance);
                foreach(var player in FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None))Consider(player.transform,ref nearest,ref distance);
                if(nearest!=null)
                {
                    if(hold<=0)direction=nearest.position.z<RestaurantEntrance.Door.z?1:-1;
                    hold=HoldSeconds;
                }
            }
            float target=hold>0?Degrees*direction:0;
            for(int i=0;i<hinges.Length;i++)hinges[i].localRotation=Quaternion.Slerp(hinges[i].localRotation,Quaternion.Euler(0,(i==0?-1:1)*target,0),1-Mathf.Exp(-Smoothing*Time.deltaTime));
        }
        static void Consider(Transform candidate,ref Transform nearest,ref float distance)
        {
            var delta=candidate.position-RestaurantEntrance.Door;delta.y=0;
            if(delta.magnitude>=distance)return;distance=delta.magnitude;nearest=candidate;
        }
    }
}
