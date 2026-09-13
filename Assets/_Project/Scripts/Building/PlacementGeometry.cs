using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurgerShop.Building
{
    public readonly struct PlacementFootprint
    {
        public readonly Vector2 Center;
        public readonly Vector2 HalfSize;
        public readonly float Angle;
        public PlacementFootprint(Vector2 center,Vector2 size,float angle)
        {Center=center;HalfSize=size*.5f;Angle=angle;}
        public bool IsValid=>PlacementGeometry.Finite(Center.x)&&PlacementGeometry.Finite(Center.y)
            &&PlacementGeometry.Finite(HalfSize.x)&&PlacementGeometry.Finite(HalfSize.y)
            &&PlacementGeometry.Finite(Angle)&&HalfSize.x>0&&HalfSize.y>0;
        public Vector2 Right=>new Vector2(Mathf.Cos(Angle*Mathf.Deg2Rad),Mathf.Sin(Angle*Mathf.Deg2Rad));
        public Vector2 Up=>new Vector2(-Right.y,Right.x);
        public Vector2[] Corners=>new[]{Center-Right*HalfSize.x-Up*HalfSize.y,Center+Right*HalfSize.x-Up*HalfSize.y,
            Center+Right*HalfSize.x+Up*HalfSize.y,Center-Right*HalfSize.x+Up*HalfSize.y};
        public bool Contains(Vector2 point,float clearance=0)
        {
            Vector2 d=point-Center;
            return Mathf.Abs(Vector2.Dot(d,Right))<=HalfSize.x+clearance
                &&Mathf.Abs(Vector2.Dot(d,Up))<=HalfSize.y+clearance;
        }
    }

    public static class PlacementGeometry
    {
        public static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);

        public static bool Overlaps(PlacementFootprint a,PlacementFootprint b,float clearance=0)
        {
            if(!a.IsValid||!b.IsValid||!Finite(clearance)||clearance<0)return true;
            Vector2 d=b.Center-a.Center;
            var ar=a.Right;var au=new Vector2(-ar.y,ar.x);
            var br=b.Right;var bu=new Vector2(-br.y,br.x);
            return !Separated(d,ar,ar,au,a.HalfSize,br,bu,b.HalfSize,clearance)
                && !Separated(d,au,ar,au,a.HalfSize,br,bu,b.HalfSize,clearance)
                && !Separated(d,br,ar,au,a.HalfSize,br,bu,b.HalfSize,clearance)
                && !Separated(d,bu,ar,au,a.HalfSize,br,bu,b.HalfSize,clearance);
        }
        static bool Separated(Vector2 d,Vector2 axis,Vector2 ar,Vector2 au,Vector2 ah,Vector2 br,Vector2 bu,Vector2 bh,float clearance)
        {
            float a=Mathf.Abs(Vector2.Dot(ar,axis))*ah.x+Mathf.Abs(Vector2.Dot(au,axis))*ah.y;
            float b=Mathf.Abs(Vector2.Dot(br,axis))*bh.x+Mathf.Abs(Vector2.Dot(bu,axis))*bh.y;
            return Mathf.Abs(Vector2.Dot(d,axis))>=a+b+clearance;
        }

        // Exact area coverage for a rotated rectangle over a union of axis-aligned floor regions.
        // Merely testing corners would incorrectly accept a footprint bridging a locked gap.
        public static bool CoveredByFloor(PlacementFootprint footprint,IReadOnlyList<Rect> floors)
        {
            if(!footprint.IsValid||floors==null||floors.Count==0)return false;
            var remaining=new List<List<Vector2>> {new List<Vector2>(footprint.Corners)};
            foreach(var floor in floors)
            {
                if(!ValidRect(floor))continue;
                var next=new List<List<Vector2>>();
                foreach(var polygon in remaining)Subtract(polygon,floor,next);
                remaining=next;
                if(remaining.Count==0)return true;
            }
            double area=0;foreach(var polygon in remaining)area+=Area(polygon);
            return area<0.000001;
        }
        static bool ValidRect(Rect r)=>Finite(r.xMin)&&Finite(r.xMax)&&Finite(r.yMin)&&Finite(r.yMax)&&r.width>0&&r.height>0;
        static void Subtract(List<Vector2> polygon,Rect rect,List<List<Vector2>> result)
        {
            // Partition outside strips, carrying only the inside portion to each following cut.
            var rest=polygon;
            Vector2[] normals={Vector2.right,Vector2.left,Vector2.up,Vector2.down};
            float[] edges={rect.xMin,-rect.xMax,rect.yMin,-rect.yMax};
            for(int i=0;i<4&&rest.Count>0;i++)
            {
                var outside=Clip(rest,-normals[i],-edges[i]);
                if(outside.Count>=3&&Area(outside)>0.000001)result.Add(outside);
                rest=Clip(rest,normals[i],edges[i]);
            }
        }
        static List<Vector2> Clip(List<Vector2> points,Vector2 normal,float edge)
        {
            var output=new List<Vector2>();if(points.Count==0)return output;
            Vector2 prev=points[points.Count-1];float previous=Vector2.Dot(prev,normal)-edge;
            foreach(var current in points)
            {
                float distance=Vector2.Dot(current,normal)-edge;
                if((distance>=0)!=(previous>=0))output.Add(prev+(current-prev)*(previous/(previous-distance)));
                if(distance>=0)output.Add(current);
                prev=current;previous=distance;
            }
            return output;
        }
        static double Area(List<Vector2> polygon)
        {
            double sum=0;for(int i=0;i<polygon.Count;i++)
            {var a=polygon[i];var b=polygon[(i+1)%polygon.Count];sum+=(double)a.x*b.y-(double)b.x*a.y;}
            return Math.Abs(sum)*.5;
        }
    }
}
