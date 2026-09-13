using UnityEngine;

namespace BurgerShop.Building
{
    // Preview assistance only: callers still validate the proposed pose against all placement rules.
    public static class PlacementAlignment
    {
        public const float CaptureDistance=.22f, ReleaseDistance=.5f, NeighborGap=1.4f;
        public const float AngleTolerance=6f, EdgeGap=.12f, SettleSeconds=.25f, StillRadius=.06f;
        public readonly struct Pose
        {
            public readonly Vector3 Position, GuideStart, GuideEnd;
            public readonly float Yaw, Score;
            public Pose(Vector3 position,float yaw,float score,Vector3 start,Vector3 end)
            {Position=position;Yaw=yaw;Score=score;GuideStart=start;GuideEnd=end;}
        }
        public static bool TryPose(FacilityInstance item,Vector3 raw,float yaw,FacilityInstance neighbor,out Pose pose)
        {
            pose=default;
            if(item==null||neighbor==null||item==neighbor||!neighbor.Available)return false;
            float correction=Mathf.DeltaAngle(yaw,neighbor.transform.eulerAngles.y);
            if(correction>90)correction-=180;if(correction< -90)correction+=180;
            if(Mathf.Abs(correction)>AngleTolerance)return false;
            float alignedYaw=yaw+correction;
            var a=item.Footprint(raw,alignedYaw);var b=neighbor.Footprint(neighbor.transform.position,neighbor.transform.eulerAngles.y);
            var right=a.Right;var up=a.Up;Vector2 delta=a.Center-b.Center;
            float x=Vector2.Dot(delta,right),z=Vector2.Dot(delta,up);
            float width=a.HalfSize.x+b.HalfSize.x,depth=a.HalfSize.y+b.HalfSize.y;
            float best=float.MaxValue;Vector2 chosen=delta;Vector2 axis=up;float line=0;
            // Adjacent along X: align depth center or matching front/back edges. Along Z is symmetric.
            TryAxis(x,z,width,a.HalfSize.y,b.HalfSize.y,right,up,ref best,ref chosen,ref axis,ref line);
            TryAxis(z,x,depth,a.HalfSize.x,b.HalfSize.x,up,right,ref best,ref chosen,ref axis,ref line);
            if(best==float.MaxValue)return false;
            Vector2 shift=chosen-delta;
            var position=raw+new Vector3(shift.x,0,shift.y);
            Vector2 center=b.Center+axis*line;
            Vector2 along=new Vector2(-axis.y,axis.x);
            float span=Mathf.Max(width,depth)+NeighborGap;
            Vector2 start=center-along*span,end=center+along*span;
            pose=new Pose(position,alignedYaw,shift.sqrMagnitude+Mathf.Abs(correction)*.001f,
                new Vector3(start.x,.07f,start.y),new Vector3(end.x,.07f,end.y));return true;
        }
        static void TryAxis(float side,float alignment,float sum,float ownHalf,float otherHalf,Vector2 sideAxis,Vector2 alignAxis,
            ref float best,ref Vector2 chosen,ref Vector2 guideAxis,ref float guideLine)
        {
            float gap=Mathf.Abs(side)-sum;
            if(gap< -CaptureDistance||gap>NeighborGap)return;
            for(int edge=-1;edge<=1;edge++)
            {
                float target=edge*(otherHalf-ownHalf),error=Mathf.Abs(target-alignment);
                if(error>CaptureDistance)continue;
                float adjacent=Mathf.Sign(side)*(sum+EdgeGap);
                float sideTarget=Mathf.Abs(side-adjacent)<=CaptureDistance?adjacent:side;
                // A slight overlap must be corrected, not preserved as an apparent alignment.
                if(Mathf.Abs(sideTarget)<sum+EdgeGap-.001f)continue;
                float score=(sideTarget-side)*(sideTarget-side)+error*error;
                if(score>=best)continue;best=score;chosen=sideAxis*sideTarget+alignAxis*target;
                guideAxis=alignAxis;guideLine=edge*otherHalf;
            }
        }
    }
    public sealed class AlignmentHold
    {
        Vector3 anchor,lockedRaw;float elapsed;bool initialized,attempted;
        public bool Active {get;private set;}
        public void Reset(){initialized=false;attempted=false;elapsed=0;Active=false;}
        public bool ShouldAttempt(Vector3 raw,float seconds)
        {
            if(Active)
            {
                if(Vector3.Distance(raw,lockedRaw)<=PlacementAlignment.ReleaseDistance)return false;
                Reset();
            }
            if(!initialized||Vector3.Distance(raw,anchor)>PlacementAlignment.StillRadius)
            {initialized=true;anchor=raw;elapsed=0;attempted=false;return false;}
            elapsed+=Mathf.Max(0,seconds);
            if(attempted||elapsed<PlacementAlignment.SettleSeconds)return false;
            attempted=true;return true;
        }
        public void Lock(Vector3 raw){lockedRaw=raw;Active=true;}
    }
}
