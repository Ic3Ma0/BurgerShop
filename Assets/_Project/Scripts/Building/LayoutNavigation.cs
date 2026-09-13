using System.Collections.Generic;
using UnityEngine;

namespace BurgerShop.Building
{
    // Navigation is rebuilt after a committed edit; preview movement never changes live routes.
    public sealed class LayoutNavigation
    {
        const float Step=.5f;
        readonly Rect bounds;
        readonly int width,height;
        readonly bool[] walkable;
        int[] regions;
        public LayoutNavigation(IReadOnlyList<Rect> floors,IReadOnlyList<PlacementFootprint> obstacles,float radius=.4f)
        {
            bounds=floors[0];foreach(var floor in floors)bounds=Rect.MinMaxRect(Mathf.Min(bounds.xMin,floor.xMin),Mathf.Min(bounds.yMin,floor.yMin),Mathf.Max(bounds.xMax,floor.xMax),Mathf.Max(bounds.yMax,floor.yMax));
            width=Mathf.CeilToInt(bounds.width/Step);height=Mathf.CeilToInt(bounds.height/Step);walkable=new bool[width*height];
            for(int i=0;i<walkable.Length;i++)
            {
                var footprint=new PlacementFootprint(Point(i),Vector2.one*radius*2,0);
                bool covered=false;
                var p=footprint.Center;var half=footprint.HalfSize;
                var cell=Rect.MinMaxRect(p.x-half.x,p.y-half.y,p.x+half.x,p.y+half.y);
                bool intersects=false;
                foreach(var floor in floors){if(floor.xMin<=cell.xMin&&floor.xMax>=cell.xMax&&floor.yMin<=cell.yMin&&floor.yMax>=cell.yMax){covered=true;break;}if(floor.Overlaps(cell))intersects=true;}
                if(!covered&&(!intersects||!PlacementGeometry.CoveredByFloor(footprint,floors)))continue;
                walkable[i]=true;
            }
            // Rasterize only cells near each obstacle, instead of testing every obstacle
            // against every cell in the entire restaurant on each preview update.
            foreach(var obstacle in obstacles)
            {
                if(!obstacle.IsValid){System.Array.Clear(walkable,0,walkable.Length);break;}
                var right=obstacle.Right;var up=obstacle.Up;
                var extent=new Vector2(Mathf.Abs(right.x)*obstacle.HalfSize.x+Mathf.Abs(up.x)*obstacle.HalfSize.y,
                    Mathf.Abs(right.y)*obstacle.HalfSize.x+Mathf.Abs(up.y)*obstacle.HalfSize.y)+Vector2.one*radius;
                var min=obstacle.Center-extent;var max=obstacle.Center+extent;
                int x0=Mathf.Max(0,Mathf.FloorToInt((min.x-bounds.xMin)/Step));
                int x1=Mathf.Min(width-1,Mathf.FloorToInt((max.x-bounds.xMin)/Step));
                int y0=Mathf.Max(0,Mathf.FloorToInt((min.y-bounds.yMin)/Step));
                int y1=Mathf.Min(height-1,Mathf.FloorToInt((max.y-bounds.yMin)/Step));
                for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
                {
                    int i=y*width+x;
                    if(walkable[i]&&PlacementGeometry.Overlaps(new PlacementFootprint(Point(i),Vector2.one*radius*2,0),obstacle))walkable[i]=false;
                }
            }
        }
        Vector2 Point(int i)=>new Vector2(bounds.xMin+(i%width+.5f)*Step,bounds.yMin+(i/width+.5f)*Step);
        int Nearest(Vector2 point)
        {
            int x=Mathf.FloorToInt((point.x-bounds.xMin)/Step),y=Mathf.FloorToInt((point.y-bounds.yMin)/Step),best=-1;float distance=2.25f;
            for(int dy=-3;dy<=3;dy++)for(int dx=-3;dx<=3;dx++)
            {int nx=x+dx,ny=y+dy;if(nx<0||nx>=width||ny<0||ny>=height)continue;int i=ny*width+nx;if(!walkable[i])continue;float d=(Point(i)-point).sqrMagnitude;if(d<distance){distance=d;best=i;}}
            return best;
        }
        // Flood once, then all service-port checks are constant-time. Do not build a path per port.
        public bool CanReach(Vector3 from,Vector3 to)
        {
            int start=Nearest(new Vector2(from.x,from.z)),end=Nearest(new Vector2(to.x,to.z));
            if(start<0||end<0)return false;
            if(regions==null)
            {
                regions=new int[walkable.Length];int region=0;var pending=new Queue<int>();
                for(int seed=0;seed<walkable.Length;seed++)
                {
                    if(!walkable[seed]||regions[seed]!=0)continue;
                    region++;regions[seed]=region;pending.Enqueue(seed);
                    void Add(int n){if(walkable[n]&&regions[n]==0){regions[n]=region;pending.Enqueue(n);}}
                    while(pending.Count>0)
                    {int i=pending.Dequeue(),x=i%width,y=i/width;if(x>0)Add(i-1);if(x+1<width)Add(i+1);if(y>0)Add(i-width);if(y+1<height)Add(i+width);}
                }
            }
            return regions[start]==regions[end];
        }
        public Vector3[] Route(Vector3 from,Vector3 to)
        {
            int start=Nearest(new Vector2(from.x,from.z)),end=Nearest(new Vector2(to.x,to.z));
            if(start<0||end<0)return null;
            var previous=new int[walkable.Length];for(int i=0;i<previous.Length;i++)previous[i]=-1;
            var queue=new Queue<int>();queue.Enqueue(start);previous[start]=start;
            void Add(int id,int source){if(walkable[id]&&previous[id]<0){previous[id]=source;queue.Enqueue(id);}}
            while(queue.Count>0&&previous[end]<0)
            {
                int i=queue.Dequeue(),x=i%width,y=i/width;
                if(x>0)Add(i-1,i);if(x+1<width)Add(i+1,i);if(y>0)Add(i-width,i);if(y+1<height)Add(i+width,i);
            }
            if(previous[end]<0)return null;
            var cells=new List<int>();for(int i=end;i!=start;i=previous[i])cells.Add(i);cells.Add(start);cells.Reverse();
            var route=new List<Vector3>();
            for(int i=0;i<cells.Count;i++)
            {
                if(i>0&&i+1<cells.Count&&cells[i]-cells[i-1]==cells[i+1]-cells[i])continue;
                var p=Point(cells[i]);route.Add(new Vector3(p.x,from.y,p.y));
            }
            route.Add(new Vector3(to.x,from.y,to.z));return route.ToArray();
        }
    }
}
