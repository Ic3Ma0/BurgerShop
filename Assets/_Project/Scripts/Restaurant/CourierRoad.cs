using UnityEngine;

namespace BurgerShop.Restaurant
{
    // The lane geometry and riders share the same bend, keeping loaded trailers on the asphalt.
    public static class CourierRoad
    {
        public static readonly Vector3[] ExitPath=CreateExitPath();
        static Vector3[] CreateExitPath()
        {
            var path=new Vector3[18];
            for(int i=0;i<=16;i++)
            {
                float angle=i*Mathf.PI/16;
                path[i]=new Vector3(-10-2*Mathf.Sin(angle),0,28-2*Mathf.Cos(angle));
            }
            path[17]=new Vector3(16,0,30);
            return path;
        }
        public static void Build(Transform parent,Material asphalt,Material curb)
        {
            var root=new GameObject("ExpressRoad").transform;root.SetParent(parent,false);
            Segment(root,new Vector3(16,0,26),ExitPath[0],asphalt,curb);
            for(int i=1;i<ExitPath.Length;i++)Segment(root,ExitPath[i-1],ExitPath[i],asphalt,curb);
            CourierVisuals.Part(root,"StopLine",new Vector3(1,.045f,26),new Vector3(.13f,.025f,2.7f),curb);
            Mark(root,"STOP",new Vector3(2.4f,.065f,26),.22f);
            Mark(root,"EXPRESS",new Vector3(8,.065f,26),.18f);
            Arrow(root,new Vector3(5,.06f,26),false,curb);
            Arrow(root,new Vector3(-4,.06f,30),true,curb);
        }
        static void Segment(Transform root,Vector3 a,Vector3 b,Material asphalt,Material curb)
        {
            var delta=b-a;var rotation=Quaternion.LookRotation(delta);
            var edge=CourierVisuals.Part(root,"PaleCurb",(a+b)*.5f-new Vector3(0,.04f,0),new Vector3(3.25f,.10f,delta.magnitude+.22f),curb);
            edge.transform.rotation=rotation;
            var lane=CourierVisuals.Part(root,"BlackLane",(a+b)*.5f+new Vector3(0,.018f,0),new Vector3(2.9f,.025f,delta.magnitude+.22f),asphalt);
            lane.transform.rotation=rotation;
        }
        static void Mark(Transform root,string text,Vector3 position,float size)
        {
            // Painted road lettering, not a floating station HUD or billboard.
            var label=new GameObject("CourierRoadMark").AddComponent<TextMesh>();
            label.transform.SetParent(root,false);label.transform.position=position;
            label.transform.rotation=Quaternion.Euler(90,0,0);
            label.text=text;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;
            label.fontSize=64;label.characterSize=size;label.color=Color.white;
        }
        static void Arrow(Transform root,Vector3 position,bool right,Material material)
        {
            float direction=right?1:-1;
            CourierVisuals.Part(root,"ArrowStem",position,new Vector3(.85f,.02f,.12f),material);
            foreach(int side in new[]{-1,1})
            {
                var wing=CourierVisuals.Part(root,"ArrowHead",position+new Vector3(direction*.36f,0,side*.16f),new Vector3(.5f,.02f,.12f),material);
                wing.transform.rotation=Quaternion.Euler(0,side*direction*45,0);
            }
        }
    }
}
