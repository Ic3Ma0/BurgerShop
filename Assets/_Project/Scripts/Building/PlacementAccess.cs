using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurgerShop.Building
{
    // Conservative reachability check used when confirming a layout. Each cell represents
    // enough floor for the carrier, not just a point that could fit through a model crack.
    public static class PlacementAccess
    {
        public const float CellSize=.5f;
        public const float CarrierRadius=.4f;
        public const int MaxGridCells=1000000;
        public static bool AllReachable(IReadOnlyList<Rect> floors,IReadOnlyList<PlacementFootprint> obstacles,
            Vector2 origin,IReadOnlyList<Vector2> destinations)
        {
            if(floors==null||floors.Count==0||destinations==null||!Finite(origin))return false;
            Rect bounds=floors[0];
            foreach(var floor in floors)
            {
                if(!Finite(new Vector2(floor.xMin,floor.yMin))||!Finite(new Vector2(floor.xMax,floor.yMax))||floor.width<=0||floor.height<=0)return false;
                bounds=Rect.MinMaxRect(Mathf.Min(bounds.xMin,floor.xMin),Mathf.Min(bounds.yMin,floor.yMin),Mathf.Max(bounds.xMax,floor.xMax),Mathf.Max(bounds.yMax,floor.yMax));
            }
            if(bounds.width/CellSize>MaxGridCells||bounds.height/CellSize>MaxGridCells)return false;
            int width=Mathf.CeilToInt(bounds.width/CellSize),height=Mathf.CeilToInt(bounds.height/CellSize);
            if(width<=0||height<=0||(long)width*height>MaxGridCells)return false;
            var visited=new bool[width*height];var states=new byte[visited.Length];
            Vector2 Point(int id)=>new Vector2(bounds.xMin+(id%width+.5f)*CellSize,bounds.yMin+(id/width+.5f)*CellSize);
            bool Walkable(int id)
            {
                if(states[id]!=0)return states[id]==1;
                var footprint=new PlacementFootprint(Point(id),Vector2.one*(CellSize+CarrierRadius*2),0);
                bool okay=PlacementGeometry.CoveredByFloor(footprint,floors);
                if(okay&&obstacles!=null)foreach(var obstacle in obstacles)
                    if(PlacementGeometry.Overlaps(footprint,obstacle)){okay=false;break;}
                states[id]=(byte)(okay?1:2);return okay;
            }
            int Cell(Vector2 point)
            {
                if(!Finite(point))return -1;
                int x=Mathf.FloorToInt((point.x-bounds.xMin)/CellSize),y=Mathf.FloorToInt((point.y-bounds.yMin)/CellSize);
                return x>=0&&x<width&&y>=0&&y<height?y*width+x:-1;
            }
            int start=Cell(origin);if(start<0||!Walkable(start))return false;
            var queue=new Queue<int>();queue.Enqueue(start);visited[start]=true;
            void Add(int id){if(!visited[id]&&Walkable(id)){visited[id]=true;queue.Enqueue(id);}}
            while(queue.Count>0)
            {
                int id=queue.Dequeue(),x=id%width,y=id/width;
                if(x>0)Add(id-1);if(x+1<width)Add(id+1);
                if(y>0)Add(id-width);if(y+1<height)Add(id+width);
            }
            foreach(var destination in destinations)
            {int cell=Cell(destination);if(cell<0||!visited[cell])return false;}
            return true;
        }
        static bool Finite(Vector2 point)=>PlacementGeometry.Finite(point.x)&&PlacementGeometry.Finite(point.y);
    }
}
