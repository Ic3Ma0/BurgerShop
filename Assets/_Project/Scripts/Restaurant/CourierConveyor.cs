using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    // Owns each item while in transit. The receiver must accept before ownership moves.
    public sealed class CourierConveyor
    {
        sealed class Load { public Transform Item; public float Distance; }
        readonly List<Load> loads=new List<Load>();
        readonly List<Transform> slats=new List<Transform>();
        readonly Vector3[] path;
        readonly float length;
        readonly Transform root;
        float phase;
        const float Speed=2.5f,Spacing=.95f;
        public int Count=>loads.Count;
        public bool CanLoad=>loads.Count<8&&(loads.Count==0||loads[loads.Count-1].Distance>=Spacing);
        public CourierConveyor(Transform parent,string name,Vector3[] route,Material bed,Material rail)
        {
            path=route;root=new GameObject(name).transform;root.SetParent(parent,false);
            rail=BurgerShop.Core.RuntimeMaterials.Create(new Color(.40f,.43f,.45f));
            root.gameObject.AddComponent<BurgerVisual>().OwnMaterials(rail);
            for(int i=1;i<path.Length;i++)
            {
                Vector3 delta=path[i]-path[i-1];length+=delta.magnitude;
                var deck=CourierVisuals.Part(root,"BeltBed",(path[i]+path[i-1])*.5f-Vector3.up*.12f,new Vector3(1.2f,.22f,delta.magnitude+.10f),bed);
                deck.transform.rotation=Quaternion.LookRotation(delta);
                Vector3 side=Vector3.Cross(Vector3.up,delta.normalized)*.65f;
                foreach(int sign in new[]{-1,1})
                {
                    var edge=CourierVisuals.Part(root,"SideRail",(path[i]+path[i-1])*.5f+side*sign,new Vector3(.10f,.18f,delta.magnitude+.10f),rail);
                    edge.transform.rotation=deck.transform.rotation;
                }
            }
            for(float d=0;d<length;d+=.32f)
                slats.Add(CourierVisuals.Part(root,"MovingChainSlat",Vector3.zero,new Vector3(1.15f,.045f,.24f),rail).transform);
            PoseSlats();
        }
        public void LoadItem(Transform item)
        {
            if(!CanLoad)throw new InvalidOperationException("Conveyor entrance is occupied");
            item.SetParent(root,true);item.localScale=Vector3.one;
            loads.Add(new Load{Item=item});item.position=At(0,out _)+Vector3.up*.06f;
        }
        public void Advance(float dt,Func<Transform,bool> receive)
        {
            bool moved=loads.Count==0;
            for(int i=0;i<loads.Count;i++)
            {
                var load=loads[i];float limit=i==0?length:loads[i-1].Distance-Spacing;
                float next=Mathf.Min(load.Distance+Speed*dt,limit);
                moved|=next>load.Distance;load.Distance=next;
                load.Item.position=At(next,out var direction)+Vector3.up*.06f;
                load.Item.rotation=Quaternion.LookRotation(direction);
            }
            if(loads.Count>0&&loads[0].Distance>=length-.001f&&receive(loads[0].Item))loads.RemoveAt(0);
            if(moved){phase=(phase+Speed*dt)%.32f;PoseSlats();}
        }
        void PoseSlats()
        {
            for(int i=0;i<slats.Count;i++)
            {
                slats[i].position=At(Mathf.Min(length,i*.32f+phase),out var direction);
                slats[i].rotation=Quaternion.LookRotation(direction);
            }
        }
        Vector3 At(float d,out Vector3 direction)
        {
            for(int i=1;i<path.Length;i++)
            {
                Vector3 delta=path[i]-path[i-1];float segment=delta.magnitude;
                if(d<=segment||i==path.Length-1){direction=delta.normalized;return Vector3.Lerp(path[i-1],path[i],d/segment);}
                d-=segment;
            }
            direction=Vector3.forward;return path[0];
        }
        // Round corners using quadratic interpolation, with straight runs between them.
        public static Vector3[] Rounded(params Vector3[] corners)
        {
            var points=new List<Vector3>{corners[0]};
            for(int i=1;i<corners.Length-1;i++)
            {
                float radius=Mathf.Min(.8f,Mathf.Min(Vector3.Distance(corners[i],corners[i-1]),Vector3.Distance(corners[i],corners[i+1]))*.45f);
                Vector3 a=Vector3.MoveTowards(corners[i],corners[i-1],radius),b=Vector3.MoveTowards(corners[i],corners[i+1],radius);
                points.Add(a);
                for(int step=1;step<=8;step++)
                {float t=step/8f;points.Add((1-t)*(1-t)*a+2*(1-t)*t*corners[i]+t*t*b);}
            }
            points.Add(corners[corners.Length-1]);return points.ToArray();
        }
    }
}
