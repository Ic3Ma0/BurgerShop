using System;
using System.Collections.Generic;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.Building
{
    [Serializable]
    public sealed class FacilityPlacementRecord
    {
        public string id;
        public int kind;
        public bool purchased;
        public float x,z,yaw;
        public int level=1,tableSet,investment;
        public bool IsValid => !string.IsNullOrEmpty(id) && id.Length<=100 && Enum.IsDefined(typeof(FacilityKind),kind)
            && PlacementGeometry.Finite(x)&&PlacementGeometry.Finite(z)&&PlacementGeometry.Finite(yaw)
            && x>=-27 && x<=38 && z>=-28 && z<=40 && yaw>=0&&yaw<360 && level>=1&&level<=4
            && tableSet>=0&&tableSet<=3 && investment>=0&&investment<=TableSetCatalog.MaxCost
            && (tableSet==0 || TableSetCatalog.IsPaidInFull(tableSet,investment))
            && (kind<=(int)FacilityKind.SquareTable || (tableSet==0&&investment==0&&level<=3));
    }

    // A placement identity owns its complete service assembly, not just the decorative mesh.
    public sealed class FacilityInstance : MonoBehaviour
    {
        public string Id {get;private set;}
        public FacilityKind Kind {get;private set;}
        public bool Purchased {get;private set;}
        public Vector3 LocalCenter {get;private set;}
        public Vector2 Size {get;private set;}
        public readonly List<Vector3> Ports = new List<Vector3>();
        public Action Moved;
        public bool Available => gameObject.activeInHierarchy;
        public void Configure(string id,FacilityKind kind,bool purchased)
        { Id=id;Kind=kind;Purchased=purchased;Measure(); }
        public void Measure()
        {
            bool any=false; Bounds bounds=new Bounds();
            foreach(var c in GetComponentsInChildren<Collider>(true))
            {
                bool hidden=false; for(var p=c.transform;p!=transform&&p!=null;p=p.parent)if(!p.gameObject.activeSelf&&!p.name.StartsWith("Look_Lv")){hidden=true;break;}
                if(hidden)continue;
                if(!c.enabled||c.isTrigger||c.GetComponentInParent<Customer.CustomerAgent>()!=null)continue;
                if(c.name=="Road"||c.name.Contains("Floor")||c.name.Contains("Lane")||c.name.StartsWith("Stop")
                    ||c.name=="DriveThruMark")continue;
                // Transform collider corners instead of its world AABB: rotation cannot inflate the footprint.
                if(!(c is BoxCollider box))continue;
                for(int i=0;i<8;i++)
                {
                    Vector3 corner=box.center+Vector3.Scale(box.size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    Vector3 point=transform.InverseTransformPoint(c.transform.TransformPoint(corner));
                    if(!any){bounds=new Bounds(point,Vector3.zero);any=true;}else bounds.Encapsulate(point);
                }
            }
            if(!any)bounds=Kind==FacilityKind.RedBoxMachine?new Bounds(Vector3.up,new Vector3(2.5f,2.1f,1.6f)):new Bounds(Vector3.zero,new Vector3(1,1,1));
            LocalCenter=bounds.center;Size=new Vector2(Mathf.Max(.6f,bounds.size.x),Mathf.Max(.6f,bounds.size.z));
        }
        public PlacementFootprint Footprint(Vector3 position,float yaw)
        {
            Vector3 center=position+Quaternion.Euler(0,yaw,0)*LocalCenter;
            return new PlacementFootprint(new Vector2(center.x,center.z),Size,-yaw);
        }
        public FacilityPlacementRecord Capture()
        {
            var grill=GetComponentInChildren<GrillUpgradeZone>(true);
            var table=GetComponentInChildren<DiningTable>(true);
            var box=GetComponentInChildren<BoxingStation>(true);
            var stock=GetComponentInChildren<CounterStock>(true);
            var bag=GetComponentInChildren<BagLine>(true);
            var lane=GetComponentInChildren<DriveThruLane>(true);
            int bagLevel=bag!=null?(Kind==FacilityKind.BagMachine?bag.MachineLevel:Kind==FacilityKind.BagTable?bag.TableLevel:bag.CounterLevel):1;
            return new FacilityPlacementRecord {id=Id,kind=(int)Kind,purchased=Purchased,x=transform.position.x,z=transform.position.z,
                yaw=Mathf.Repeat(transform.eulerAngles.y,360),level=lane!=null?lane.ServiceLevel:grill!=null?grill.Level:table!=null?table.FurnitureLevel:box!=null?box.WorkLevel:stock!=null?stock.ServiceLevel:bagLevel,
                investment=GetComponentInChildren<TableUpgradeZone>(true)?.Invested??0,
                tableSet=table!=null?(int)table.SetId:0};
        }
        public void Apply(FacilityPlacementRecord row)
        {
            transform.SetPositionAndRotation(new Vector3(row.x,0,row.z),Quaternion.Euler(0,row.yaw,0));
            if(Purchased)
            {
                GetComponentInChildren<GrillUpgradeZone>(true)?.RestoreLevel(Mathf.Min(3,row.level));
                var table=GetComponentInChildren<DiningTable>(true);
                if(table!=null){table.ApplySet((TableSetId)row.tableSet);table.SetFurnitureLevel(row.level);GetComponentInChildren<TableUpgradeZone>(true)?.Restore(row.tableSet,row.investment);}
                var stock=GetComponentInChildren<CounterStock>(true);if(stock!=null)stock.ServiceLevel=Mathf.Min(3,row.level);
                var lane=GetComponentInChildren<DriveThruLane>(true);if(lane!=null)lane.ServiceLevel=Mathf.Clamp(row.level,1,3);
                var boxing=GetComponentInChildren<BoxingStation>(true);if(boxing!=null)boxing.WorkLevel=Mathf.Min(3,row.level);
                var bag=GetComponentInChildren<BagLine>(true);if(bag!=null){if(Kind==FacilityKind.BagMachine)bag.MachineLevel=row.level;else if(Kind==FacilityKind.BagTable)bag.TableLevel=row.level;else bag.CounterLevel=row.level;}
            }
            Moved?.Invoke();
        }
    }
}
