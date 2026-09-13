using UnityEngine;

namespace BurgerShop.Core
{
    // Street support is separate from owned/buildable land; distant scenery stays decorative.
    public static class StreetEnvironment
    {
        public static readonly Rect GroundBounds = Rect.MinMaxRect(-320,-320,330,330);
        public static readonly Rect CameraTravel = Rect.MinMaxRect(-32,-28,43,36);
        public const float GroundHeight=-.32f;
        public static readonly Rect WalkBounds = Rect.MinMaxRect(-42,-40,52,54);
        const float BoundaryHeight=4f, BoundaryThickness=.5f;
        public static void Build(Transform parent)
        {
            var root=new GameObject("StreetEnvironment").transform;root.SetParent(parent,false);
            Support(root,"StreetGroundSupport",new Vector3(WalkBounds.center.x,GroundHeight-.5f,WalkBounds.center.y),new Vector3(WalkBounds.width,1,WalkBounds.height));
            foreach(float x in new[]{WalkBounds.xMin,WalkBounds.xMax})
                Support(root,"StreetBoundaryWall",new Vector3(x,BoundaryHeight*.5f,WalkBounds.center.y),new Vector3(BoundaryThickness,BoundaryHeight,WalkBounds.height+BoundaryThickness));
            foreach(float z in new[]{WalkBounds.yMin,WalkBounds.yMax})
                Support(root,"StreetBoundaryWall",new Vector3(WalkBounds.center.x,BoundaryHeight*.5f,z),new Vector3(WalkBounds.width+BoundaryThickness,BoundaryHeight,BoundaryThickness));
            var grass=RuntimeMaterials.Create(new Color(.47f,.72f,.29f));
            var pavement=RuntimeMaterials.Create(new Color(.58f,.78f,.74f));
            var curb=RuntimeMaterials.Create(new Color(.89f,.88f,.80f));
            var asphalt=RuntimeMaterials.Create(new Color(.20f,.22f,.25f));
            var white=RuntimeMaterials.Create(new Color(.98f,.96f,.86f));
            var bark=RuntimeMaterials.Create(new Color(.45f,.30f,.18f));
            var leaves=RuntimeMaterials.Create(new Color(.30f,.60f,.22f));
            Part(root,"Landscape",new Vector3(5,GroundHeight-.1f,5),new Vector3(GroundBounds.width,.2f,GroundBounds.height),grass);
            // Complete street block around the existing restaurant and all expansion wings.
            Strip(root,"SouthWalk",new Vector3(5,-.23f,-23),new Vector3(79,.12f,5),curb,pavement);
            Strip(root,"NorthWalk",new Vector3(5,-.23f,36),new Vector3(79,.12f,5),curb,pavement);
            Strip(root,"WestWalk",new Vector3(-32,-.23f,6.5f),new Vector3(5,.12f,54),curb,pavement);
            Strip(root,"EastWalk",new Vector3(42,-.23f,6.5f),new Vector3(5,.12f,54),curb,pavement);
            Part(root,"SouthStreet",new Vector3(5,-.27f,-30),new Vector3(650,.08f,8),asphalt);
            foreach(float z in new[]{-30f,43f})
                Support(root,"StreetRoadSupport",new Vector3(WalkBounds.center.x,-.27f,z),new Vector3(WalkBounds.width,.08f,8));
            Part(root,"NorthStreet",new Vector3(5,-.27f,43),new Vector3(650,.08f,8),asphalt);
            Strip(root,"FarSouthWalk",new Vector3(5,-.23f,-37),new Vector3(650,.12f,5),curb,pavement);
            Strip(root,"FarNorthWalk",new Vector3(5,-.23f,50),new Vector3(650,.12f,5),curb,pavement);
            for(int x=-312;x<328;x+=8)
                foreach(float z in new[]{-30f,43f})Part(root,"LaneDash",new Vector3(x,-.22f,z),new Vector3(3,.02f,.14f),white);
            foreach(float z in new[]{-30f,43f})for(int i=0;i<8;i++)
                Part(root,"Crosswalk",new Vector3(-31,-.21f,z-3.5f+i),new Vector3(3,.02f,.5f),white);
            foreach(float x in new[]{-40f,50f})for(int z=-16;z<=30;z+=12)
            {
                Part(root,"TreeTrunk",new Vector3(x,.5f,z),new Vector3(.35f,1.6f,.35f),bark);
                Part(root,"TreeCrown",new Vector3(x,1.65f,z),new Vector3(2.8f,2.2f,2.8f),leaves,PrimitiveType.Sphere);
            }
        }
        static void Strip(Transform root,string name,Vector3 position,Vector3 size,Material edge,Material top)
        {
            Part(root,name+"Curb",position,size,edge);
            Support(root,name+"Support",position+Vector3.up*.04f,new Vector3(size.x,.2f,size.z));
            Part(root,name,position+Vector3.up*.07f,new Vector3(size.x-.3f,.04f,size.z-.3f),top);
        }
        static void Support(Transform root,string name,Vector3 position,Vector3 size)
        {
            // Clip long decorative streets to the finite playable block.
            float minX=Mathf.Max(position.x-size.x*.5f,WalkBounds.xMin-BoundaryThickness);
            float maxX=Mathf.Min(position.x+size.x*.5f,WalkBounds.xMax+BoundaryThickness);
            float minZ=Mathf.Max(position.z-size.z*.5f,WalkBounds.yMin-BoundaryThickness);
            float maxZ=Mathf.Min(position.z+size.z*.5f,WalkBounds.yMax+BoundaryThickness);
            var go=new GameObject(name);go.transform.SetParent(root,false);
            go.transform.localPosition=new Vector3((minX+maxX)*.5f,position.y,(minZ+maxZ)*.5f);
            go.AddComponent<BoxCollider>().size=new Vector3(maxX-minX,size.y,maxZ-minZ);
        }
        static void Part(Transform root,string name,Vector3 position,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cube)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root,false);
            go.transform.localPosition=position;go.transform.localScale=size;
            var collider=go.GetComponent<Collider>();collider.enabled=false;
            if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);
            go.GetComponent<Renderer>().sharedMaterial=material;
        }
        public static Vector3 ClampCamera(Camera camera,Vector3 position)
        {
            if(camera==null)return position;
            var current=camera.transform.position;
            // Limit pan relative to the point seen at the center of the screen.
            var plane=new Plane(Vector3.up,new Vector3(0,GroundHeight,0));
            var centerRay=camera.ViewportPointToRay(new Vector3(.5f,.5f,0));
            if(plane.Raycast(centerRay,out float centerDistance))
            {
                var offset=centerRay.GetPoint(centerDistance)-current;
                position.x=Mathf.Clamp(position.x,CameraTravel.xMin-offset.x,CameraTravel.xMax-offset.x);
                position.z=Mathf.Clamp(position.z,CameraTravel.yMin-offset.z,CameraTravel.yMax-offset.z);
            }
            float minX=float.PositiveInfinity,maxX=float.NegativeInfinity,minZ=minX,maxZ=maxX;
            for(int i=0;i<4;i++)
            {
                var ray=camera.ViewportPointToRay(new Vector3(i%2,i/2,0));
                if(!plane.Raycast(ray,out float distance))continue;
                var offset=ray.GetPoint(distance)-current;
                minX=Mathf.Min(minX,offset.x);maxX=Mathf.Max(maxX,offset.x);
                minZ=Mathf.Min(minZ,offset.z);maxZ=Mathf.Max(maxZ,offset.z);
            }
            if(!float.IsInfinity(minX))
            {
                position.x=Mathf.Clamp(position.x,GroundBounds.xMin-minX,GroundBounds.xMax-maxX);
                position.z=Mathf.Clamp(position.z,GroundBounds.yMin-minZ,GroundBounds.yMax-maxZ);
            }
            return position;
        }
    }
}
