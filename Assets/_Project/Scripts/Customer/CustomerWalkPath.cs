using System.Collections.Generic;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.Customer
{
    // Stable personal route geometry; never writes queue order, service state or gameplay RNG.
    public sealed class CustomerWalkPath
    {
        readonly Vector3[] points;
        readonly float[] distances;
        public float Length => distances[distances.Length-1];
        public static float Pace(int ticket) => 0.94f + ((ticket*37)%13)*.01f;

        public CustomerWalkPath(IReadOnlyList<Vector3> source, int ticket, int fixedFrom = int.MaxValue)
        {
            var knots=new List<Vector3>();
            foreach(var p in source)if(knots.Count==0||Vector3.Distance(knots[knots.Count-1],p)>.01f)knots.Add(p);
            if(knots.Count==0)knots.Add(Vector3.zero);
            var layout=Building.FacilityLayout.Current;
            if(layout!=null)
            {
                for(int i=1;i<knots.Count;i++)
                {
                    if(Clear(knots[i-1],knots[i]))continue;
                    var detour=layout.Route(knots[i-1],knots[i]);
                    if(detour==null)break; // The movement guard waits; never author a direct fallback.
                    knots.RemoveAt(i);knots.InsertRange(i,detour);i+=detour.Length-1;
                }
            }
            // Personal walking lane, not a per-frame random wobble. Queue slots and endpoints stay exact.
            float lane=.22f+(ticket*17%7)*.065f;
            var personal=knots.ToArray();
            for(int i=1;i<knots.Count-1&&i<fixedFrom;i++)
            {
                var direction=(knots[i+1]-knots[i-1]).normalized;
                var offset=Vector3.Cross(Vector3.up,direction)*lane;
                Vector3 candidate=knots[i]+offset;
                if(Clear(knots[i-1],candidate)&&Clear(candidate,knots[i+1]))personal[i]=candidate;
            }
            var curved=new List<Vector3>{personal[0]};
            for(int i=1;i<personal.Length-1;i++)
            {
                var incoming=personal[i]-personal[i-1];var outgoing=personal[i+1]-personal[i];
                float radius=Mathf.Min(.7f+(ticket*11%5)*.1f,Mathf.Min(incoming.magnitude,outgoing.magnitude)*.35f);
                if(i>=fixedFrom||Vector3.Dot(incoming.normalized,outgoing.normalized)<-.5f){curved.Add(personal[i]);continue;}
                var a=personal[i]-incoming.normalized*radius;var b=personal[i]+outgoing.normalized*radius;
                var samples=new Vector3[13];samples[0]=a;bool clear=Clear(curved[curved.Count-1],a);
                for(int j=1;j<=12;j++)
                {
                    float t=j/12f;samples[j]=(1-t)*(1-t)*a+2*(1-t)*t*personal[i]+t*t*b;
                    clear &= Clear(samples[j-1],samples[j]);
                }
                if(clear)curved.AddRange(samples);else curved.Add(knots[i]);
            }
            if(personal.Length>1)curved.Add(personal[personal.Length-1]);
            points=curved.ToArray();distances=new float[points.Length];
            for(int i=1;i<points.Length;i++)distances[i]=distances[i-1]+Vector3.Distance(points[i-1],points[i]);
        }
        public Vector3 At(float distance)
        {
            for(int i=1;i<points.Length;i++)if(distance<=distances[i])
                return Vector3.Lerp(points[i-1],points[i],Mathf.InverseLerp(distances[i-1],distances[i],distance));
            return points[points.Length-1];
        }
        public Vector3[] Points => (Vector3[])points.Clone();
        public static bool Clear(Vector3 a,Vector3 b)
        {
            return ActorObstacles.Clear(a,b);
        }
    }
}
