using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurgerShop.Building
{
    // One geometry source for placement coverage, asphalt rendering and vehicle waypoints.
    // The shop moves a road together with its counter; it must not move only the mesh.
    public sealed class VehicleRoadLayout
    {
        public const float CarWidth=3.4f;
        public const float CourierWidth=3.25f;
        readonly Vector3[] points;
        readonly float width;
        public VehicleRoadLayout(IReadOnlyList<Vector3> centerline,float roadWidth)
        {
            if(centerline==null||centerline.Count<2||!PlacementGeometry.Finite(roadWidth)||roadWidth<=0)
                throw new ArgumentException("Invalid road centerline or width");
            width=roadWidth;points=new Vector3[centerline.Count];
            for(int i=0;i<points.Length;i++)
            {
                Vector3 p=centerline[i];
                if(!PlacementGeometry.Finite(p.x)||!PlacementGeometry.Finite(p.y)||!PlacementGeometry.Finite(p.z)
                    ||(i>0&&(p-points[i-1]).sqrMagnitude<.000001f))throw new ArgumentException("Road points must be finite and distinct");
                points[i]=p;
            }
        }
        public Vector3[] WorldPoints(Vector3 position,float yaw)
        {
            if(!PlacementGeometry.Finite(position.x)||!PlacementGeometry.Finite(position.y)||!PlacementGeometry.Finite(position.z)||!PlacementGeometry.Finite(yaw))
                throw new ArgumentException("Invalid road pose");
            var world=new Vector3[points.Length];var rotation=Quaternion.Euler(0,yaw,0);
            for(int i=0;i<points.Length;i++)world[i]=position+rotation*points[i];
            return world;
        }
        public PlacementFootprint[] Footprints(Vector3 position,float yaw)
        {
            var world=WorldPoints(position,yaw);var shapes=new PlacementFootprint[world.Length-1];
            for(int i=1;i<world.Length;i++)
            {
                Vector3 delta=world[i]-world[i-1],mid=(world[i]+world[i-1])*.5f;
                // Include the half-width end caps so turns cannot clip a wall between segments.
                shapes[i-1]=new PlacementFootprint(new Vector2(mid.x,mid.z),new Vector2(delta.magnitude+width,width),Mathf.Atan2(delta.z,delta.x)*Mathf.Rad2Deg);
            }
            return shapes;
        }
        public bool CanPlace(Vector3 position,float yaw,IReadOnlyList<Rect> floors,IReadOnlyList<PlacementFootprint> obstacles)
        {
            foreach(var shape in Footprints(position,yaw))
            {
                if(!PlacementGeometry.CoveredByFloor(shape,floors))return false;
                if(obstacles!=null)foreach(var obstacle in obstacles)if(PlacementGeometry.Overlaps(shape,obstacle))return false;
            }
            return true;
        }
    }
}
