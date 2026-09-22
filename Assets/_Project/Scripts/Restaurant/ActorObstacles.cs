using UnityEngine;
using System.Collections.Generic;
using BurgerShop.Building;
using BurgerShop.Player;
using BurgerShop.Customer;

namespace BurgerShop.Restaurant
{
    // All walking actors use the same ground-relative body clearance. Actor pivots
    // differ (customers at ground level, employees at 1.05m), so do not add pivot Y.
    public static class ActorObstacles
    {
        public const float Radius = .3f;
        static readonly Collider[] hits = new Collider[128];
        public static bool IsObstacle(Collider c)
        {
            if (!SolidOccupancy.BlocksPlayer(c) || c is CharacterController) return false;
            if (c.GetComponentInParent<PlayerMotor>() != null || c.GetComponentInParent<CustomerAgent>() != null
                || c.GetComponentInParent<RestaurantWorker>() != null) return false;
            return c.bounds.max.y > .2f && c.bounds.min.y < 1.8f;
        }
        public static Vector3[] Route(Vector3 from,Vector3 to)
        {
            if(FacilityLayout.Current!=null)return FacilityLayout.Current.Route(from,to);
            // Standalone/legacy assemblies still obey the same physical obstacles.
            Physics.SyncTransforms();
            var obstacles=new List<PlacementFootprint>();
            foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if(!IsObstacle(c))continue;
                var b=c.bounds;
                obstacles.Add(new PlacementFootprint(new Vector2(b.center.x,b.center.z),new Vector2(b.size.x,b.size.z),0));
            }
            var floor=Rect.MinMaxRect(Mathf.Min(from.x,to.x)-8,Mathf.Min(from.z,to.z)-8,Mathf.Max(from.x,to.x)+8,Mathf.Max(from.z,to.z)+8);
            return new LayoutNavigation(new[]{floor},obstacles,Radius+.05f).Route(from,to);
        }
        public static bool Clear(Vector3 from, Vector3 to)
        {
            from.y = to.y = 0;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from,to)/.12f));
            for(int step=0;step<=steps;step++)
            {
                var p=Vector3.Lerp(from,to,step/(float)steps);
                int count=Physics.OverlapCapsuleNonAlloc(p+Vector3.up*.55f,p+Vector3.up*1.45f,Radius,hits,~0,QueryTriggerInteraction.Ignore);
                if(count==hits.Length)return false;
                for(int i=0;i<count;i++)if(IsObstacle(hits[i]))return false;
            }
            return true;
        }
    }
}
