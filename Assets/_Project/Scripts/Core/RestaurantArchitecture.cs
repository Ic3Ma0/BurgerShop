using System.Collections.Generic;
using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.Rendering;

namespace BurgerShop.Core
{
    // Surface decoration only: ownership, colliders, work points and saved furniture remain authoritative.
    public sealed class RestaurantArchitecture : MonoBehaviour
    {
        const float SurfaceY = .008f, TileSize = 1.5f, Joint = .035f;
        Material cream, trim, mint, warm, grout, red, plan;
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly HashSet<Transform> dressed = new HashSet<Transform>();
        ShopExpansion wing;
        BagLine west;
        RestroomExpansion restroom;
        CourierLine courier;
        Transform drinkPlot, westPlot, restroomPlot, courierPlot;
        int previousState = -1;

        public void RedressMainFloor(Transform floor)
        {
            for(int i=floor.childCount-1;i>=0;i--){var child=floor.GetChild(i);child.gameObject.SetActive(false);BurgerVisual.Release(child.gameObject);}
            dressed.Remove(floor);previousState=-1;
        }
        public void Configure()
        {
            cream = RuntimeMaterials.Create(new Color(.94f,.91f,.82f));
            trim = RuntimeMaterials.Create(new Color(.17f,.28f,.32f));
            mint = RuntimeMaterials.Create(new Color(.69f,.83f,.79f));
            warm = RuntimeMaterials.Create(new Color(.92f,.79f,.62f));
            grout = RuntimeMaterials.Create(new Color(.82f,.84f,.79f));
            red = RuntimeMaterials.Create(new Color(.79f,.23f,.17f));
            plan = RuntimeMaterials.Create(new Color(.77f,.85f,.64f));
            wing = GetComponentInChildren<ShopExpansion>();
            west = GetComponent<BagLine>();
            restroom = GetComponent<RestroomExpansion>();
            courier = GetComponent<CourierLine>();
            drinkPlot = Plot("DrinksPlannedLand", Rect.MinMaxRect(23.7f,-7.7f,37.7f,11.2f), "DRINKS & SEATING");
            westPlot = Plot("TakeawayPlannedLand", Rect.MinMaxRect(-26.7f,-8.7f,-15.3f,8.7f), "TAKEAWAY");
            restroomPlot = Plot("RestroomPlannedLand", Rect.MinMaxRect(-4.8f,-20.8f,3.8f,-15.3f), "WC");
            courierPlot = Plot("CourierPlannedLand", Rect.MinMaxRect(-14.7f,15.3f,14.7f,39.5f), "DELIVERY");
            Refresh();
        }
        void Update() => Refresh();
        public void Refresh()
        {
            var goals = GetComponent<BurgerShop.UI.SessionGoalTracker>();
            int rank = goals != null ? goals.Rank : 0;
            int state = (wing != null && wing.HasWing ? 1 : 0) | (west != null && west.Expanded ? 2 : 0)
                | (restroom != null && restroom.Built ? 4 : 0) | (courier != null && courier.AreaOpen ? 8 : 0)
                | (rank << 8);
            if (state == previousState || cream == null) return;
            previousState = state;
            drinkPlot.gameObject.SetActive((state & 1) == 0 && RankAllows(goals, ShopRanks.ColaWingRank));
            westPlot.gameObject.SetActive((state & 2) == 0 && RankAllows(goals, ShopRanks.WestRank));
            restroomPlot.gameObject.SetActive((state & 4) == 0 && RankAllows(goals, RestroomExpansion.UnlockRank));
            courierPlot.gameObject.SetActive((state & 8) == 0 && RankAllows(goals, ShopRanks.CourierRank));
            // Snapshot once per actual region change; never scan scene geometry while dragging.
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (dressed.Contains(child) || child.GetComponent<Renderer>() == null) continue;
                string name = child.name;
                if (name == "Floor" && child.parent == transform)
                {
                    dressed.Add(child);
                    child.GetComponent<Renderer>().sharedMaterial = warm;
                    float kitchenEdge=ShopLayout.SmallFootprint?-2:1;
                    Surface(child,"KitchenTiles",Rect.MinMaxRect(kitchenEdge,-14.6f,(MainHallExpansion.Current?.Bounds.xMax??15)-.4f,(MainHallExpansion.Current?.Bounds.yMax??15)-.4f),cream,grout);
                    Surface(child,"DiningTiles",Rect.MinMaxRect(-14.6f,-14.6f,kitchenEdge,(MainHallExpansion.Current?.Bounds.yMax??15)-.4f),warm,warm);
                }
                else if (name == "WingBackFloor")
                {
                    dressed.Add(child);
                    Surface(child,"DrinkServiceTiles",Rect.MinMaxRect(23.6f,-3.7f,37.8f,11.3f),cream,mint);
                    Surface(child,"LoungeTiles",Rect.MinMaxRect(23.6f,-7.8f,37.8f,-3.7f),warm,warm);
                    Inlay(child,Rect.MinMaxRect(24,-7.5f,37.4f,-4),mint);
                }
                else if (name == "WingCorridorFloor")
                {
                    dressed.Add(child);
                    Surface(child,"GalleryTiles",Rect.MinMaxRect(15.2f,-8,23.4f,-4.3f),cream,mint);
                    Gateway(child,ShopLayout.SideDoor,ShopLayout.SideDoorHalf,"DRINKS",mint);
                }
                else if (name == "WestFloor" || name == "CourierFloor" || name == "CarServiceFloor"
                    || name == "RestroomFloor" || name == "HrFloor")
                {
                    dressed.Add(child);
                    var b = child.GetComponent<Renderer>().bounds;
                    var rect = Rect.MinMaxRect(b.min.x+.12f,b.min.z+.12f,b.max.x-.12f,b.max.z-.12f);
                    Surface(child,"WorkTiles",rect,cream,name == "RestroomFloor" ? mint : grout);
                    if(name == "WestFloor")Gateway(child,new Vector3(-15,0,0),1.5f,"TAKEAWAY",red);
                }
                else if (IsWall(child))
                {
                    dressed.Add(child);
                    DressWall(child);
                }
            }
        }
        static bool RankAllows(BurgerShop.UI.SessionGoalTracker goals, int required) =>
            goals != null && goals.Allows(required);
        static bool IsWall(Transform part)
        {
            string n = part.name;
            return n.StartsWith("Wall") || n.StartsWith("WingWall") || n.Contains("DoorPlug")
                || n == "OldWestWall" || n == "WestWall" || n == "ExpansionEdge" || n == "OldNorthWall"
                || n == "CourierSideWall" || n == "CarServiceSide" || n == "EntranceWallLeft"
                || n.StartsWith("RestroomWall") || n.StartsWith("HrWall");
        }
        void DressWall(Transform wall)
        {
            var r = wall.GetComponent<Renderer>();
            var b = r.bounds;
            r.sharedMaterial = cream;
            bool horizontal = b.size.x > b.size.z;
            Vector3 size = new Vector3(b.size.x+.018f,.14f,b.size.z+.018f);
            Piece(wall,"ArchitectureSkirting",new Vector3(b.center.x,b.min.y+.09f,b.center.z),size,trim);
            size.y = .085f;
            Piece(wall,"ArchitectureCoping",new Vector3(b.center.x,b.max.y+.01f,b.center.z),size,
                wall.name.StartsWith("Wing") || wall.name.StartsWith("Restroom") ? mint : red);
            // Recessed broad panels on outer walls add rhythm without extending into circulation.
            float length = horizontal ? b.size.x : b.size.z;
            if (length < 5 || wall.name.Contains("DoorPlug")) return;
            int count = Mathf.FloorToInt(length/4);
            for(int i=0;i<count;i++)
            {
                float along = (i+.5f)*length/count-length*.5f;
                Vector3 p = b.center + (horizontal ? Vector3.right : Vector3.forward)*along;
                p.y = b.min.y+b.size.y*.55f;
                Vector3 s = horizontal ? new Vector3(Mathf.Min(2.6f,length/count-.5f),b.size.y*.42f,b.size.z+.024f)
                    : new Vector3(b.size.x+.024f,b.size.y*.42f,Mathf.Min(2.6f,length/count-.5f));
                Piece(wall,"ArchitectureWallPanel",p,s,mint);
            }
        }
        void Surface(Transform parent,string name,Rect rect,Material first,Material second)
        {
            // Two submeshes instead of one renderer/collider per tile.
            var vertices = new List<Vector3>();
            var a = new List<int>();var b = new List<int>();
            int rows = Mathf.CeilToInt(rect.height/TileSize), columns = Mathf.CeilToInt(rect.width/TileSize);
            for(int z=0;z<rows;z++) for(int x=0;x<columns;x++)
            {
                float x0=rect.xMin+x*TileSize+Joint, z0=rect.yMin+z*TileSize+Joint;
                float x1=Mathf.Min(rect.xMax,rect.xMin+(x+1)*TileSize)-Joint;
                float z1=Mathf.Min(rect.yMax,rect.yMin+(z+1)*TileSize)-Joint;
                if(x1<=x0||z1<=z0)continue;
                int i=vertices.Count;
                vertices.Add(new Vector3(x0,SurfaceY,z0));vertices.Add(new Vector3(x0,SurfaceY,z1));
                vertices.Add(new Vector3(x1,SurfaceY,z1));vertices.Add(new Vector3(x1,SurfaceY,z0));
                var indices=(x+z)%2==0?a:b;indices.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.subMeshCount=2;
            mesh.SetTriangles(a,0);mesh.SetTriangles(b,1);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterials=new[]{first,second};
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            go.transform.SetParent(parent,true); // mesh vertices are in world space, despite scaled floor parents
        }
        void Gateway(Transform parent,Vector3 center,float halfWidth,string title,Material color)
        {
            foreach(int side in new[]{-1,1})
                Piece(parent,"ArchitectureDoorPost",center+new Vector3(0,1.3f,side*(halfWidth+.13f)),new Vector3(.5f,2.6f,.22f),cream);
            Piece(parent,"ArchitectureDoorHeader",center+new Vector3(0,2.7f,0),new Vector3(.5f,.48f,halfWidth*2+.5f),color);
            var label=new GameObject("ArchitectureDoorSign").AddComponent<TextMesh>();label.transform.SetParent(parent,true);
            label.transform.position=center+new Vector3(-.265f,2.7f,0);label.transform.rotation=Quaternion.Euler(0,90,0);
            label.text=title;label.fontSize=48;label.characterSize=.075f;label.anchor=TextAnchor.MiddleCenter;label.color=Color.white;
        }
        void Inlay(Transform parent,Rect rect,Material material)
        {
            foreach(float x in new[]{rect.xMin,rect.xMax})
                Piece(parent,"FloorBorder",new Vector3(x,SurfaceY+.005f,rect.center.y),new Vector3(.075f,.004f,rect.height),material);
            foreach(float z in new[]{rect.yMin,rect.yMax})
                Piece(parent,"FloorBorder",new Vector3(rect.center.x,SurfaceY+.005f,z),new Vector3(rect.width,.004f,.075f),material);
        }
        Transform Plot(string name,Rect rect,string title)
        {
            var root=new GameObject(name).transform;root.SetParent(transform,false);
            const float y=-.19f, dash=1.1f, pitch=1.8f;
            foreach(float z in new[]{rect.yMin,rect.yMax}) for(float x=rect.xMin;x<rect.xMax;x+=pitch)
                Piece(root,"PlotMark",new Vector3(x+Mathf.Min(dash,rect.xMax-x)*.5f,y,z),new Vector3(Mathf.Min(dash,rect.xMax-x),.018f,.10f),plan);
            foreach(float x in new[]{rect.xMin,rect.xMax}) for(float z=rect.yMin;z<rect.yMax;z+=pitch)
                Piece(root,"PlotMark",new Vector3(x,y,z+Mathf.Min(dash,rect.yMax-z)*.5f),new Vector3(.10f,.018f,Mathf.Min(dash,rect.yMax-z)),plan);
            var marks=root.GetComponentsInChildren<MeshFilter>();
            var combines=new CombineInstance[marks.Length];
            for(int i=0;i<marks.Length;i++)combines[i]=new CombineInstance{mesh=marks[i].sharedMesh,transform=marks[i].transform.localToWorldMatrix};
            var outline=new Mesh{name=name+"Outline"};outline.CombineMeshes(combines);meshes.Add(outline);
            foreach(var mark in marks){mark.gameObject.SetActive(false);BurgerVisual.Release(mark.gameObject);}
            var combined=new GameObject("PlotOutline",typeof(MeshFilter),typeof(MeshRenderer));combined.transform.SetParent(root,true);
            combined.GetComponent<MeshFilter>().sharedMesh=outline;combined.GetComponent<MeshRenderer>().sharedMaterial=plan;
            combined.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            var label=new GameObject("PlotTitle").AddComponent<TextMesh>();label.transform.SetParent(root,false);
            label.transform.position=new Vector3(rect.center.x,y+.02f,rect.center.y);label.transform.rotation=Quaternion.Euler(90,0,0);
            label.text=title;label.fontSize=48;label.characterSize=.12f;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.94f,.94f,.80f);
            return root;
        }
        static void Piece(Transform parent,string name,Vector3 world,Vector3 scale,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=world;go.transform.localScale=scale;
            var c=go.GetComponent<Collider>();c.enabled=false;BurgerVisual.Release(c);
            var r=go.GetComponent<Renderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;
            go.transform.SetParent(parent,true);
        }
        void OnDestroy()
        {
            foreach(var m in meshes)if(m!=null)BurgerVisual.Release(m);
            foreach(var m in new[]{cream,trim,mint,warm,grout,red,plan})if(m!=null)BurgerVisual.Release(m);
        }
    }
}
