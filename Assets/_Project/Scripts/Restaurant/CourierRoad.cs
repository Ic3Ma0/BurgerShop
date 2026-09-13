using UnityEngine;

namespace BurgerShop.Restaurant
{
    // The lane geometry and riders share the same bend, keeping loaded trailers on the asphalt.
    public static class CourierRoad
    {
        public static readonly Vector3[] ExitPath={new Vector3(-28,0,41)};
        public static Vector3[] FullPath=>new[]{new Vector3(28,0,41),ExitPath[0]};
        public static void Build(Transform parent,Material asphalt,Material curb,bool sharedStreet=false)
        {
            var root=new GameObject("ExpressRoad").transform;root.SetParent(parent,false);
            if(!sharedStreet)Segment(root,FullPath[0],FullPath[1],asphalt,curb);
            CourierVisuals.Part(root,"StopLine",new Vector3(1,.045f,41),new Vector3(.13f,.025f,2.7f),curb);
            Mark(root,"STOP",new Vector3(2.4f,.065f,41),.22f);
            Mark(root,"EXPRESS",new Vector3(8,.065f,41),.18f);
            Arrow(root,new Vector3(5,.06f,41),false,curb);
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
