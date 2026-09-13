using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using UnityEngine;

namespace BurgerShop.Building
{
    public static class FacilityFactory
    {
        // The caller keeps this parent inactive until the placement transaction commits.
        // No global registration or wallet mutation is performed while constructing a preview.
        public static FacilityInstance Create(Transform parent, FacilityKind kind, string id,
            BurgerInventory player, RestaurantWallet wallet, PartsWallet parts, CashFloor cash, DiningArea dining)
        {
            var holder=new GameObject("Facility_"+kind).transform;holder.SetParent(parent,false);
            Transform model=null;Vector3 pivot=Vector3.zero;
            switch(kind)
            {
                case FacilityKind.PairTable:case FacilityKind.FourSeatTable:case FacilityKind.SquareTable:
                    model=DiningTable.Create(holder,Vector3.zero,(DiningTableKind)(int)kind).transform;break;
                case FacilityKind.BurgerMachine:
                    model=ExpandableGrill.Create(holder,Vector3.zero,player,wallet).transform;break;
                case FacilityKind.ColaMachine:
                    model=ExpandableGrill.CreateColaStarter(holder,player,wallet).transform;pivot=ShopLayout.Cola;break;
                case FacilityKind.TrashBin:
                    var bin=TrashBin.Create(holder,Vector3.zero);bin.Configure(player.GetComponent<TrashInventory>());model=bin.transform;break;
                case FacilityKind.BlueBoxTable:
                    var box=BoxingStation.Create(holder,true,false);box.BindPlayer(player);model=box.transform;pivot=ShopLayout.BoxingTable;break;
                case FacilityKind.CarCounter:
                    var pack=BoxingStation.Create(holder,false,true);pack.BindPlayer(player);
                    DriveThruLane.Create(holder,pack,wallet,cash,player,false);model=pack.transform;pivot=ShopLayout.PackageCounter;break;
                case FacilityKind.BagMachine:case FacilityKind.BagTable:case FacilityKind.BagCounter:
                    var bag=BagLine.CreateFacility(holder,kind,id,wallet,null,null,null,player,cash);
                    model=bag.transform;pivot=kind==FacilityKind.BagMachine?BagLine.Machine:kind==FacilityKind.BagTable?BagLine.Workbench:BagLine.Counter;break;
                case FacilityKind.RedBoxMachine:case FacilityKind.CourierTray:
                    var courier=CourierLine.CreateFacility(holder,kind==FacilityKind.RedBoxMachine,wallet,parts,player,cash);
                    model=courier.transform;pivot=kind==FacilityKind.RedBoxMachine?CourierLine.Machine:CourierLine.Counter;break;
                case FacilityKind.BurgerCounter:case FacilityKind.ColaCounter:
                    model=CreateCounter(holder,kind==FacilityKind.ColaCounter,player,wallet,cash,dining);break;
                default:throw new System.ArgumentOutOfRangeException(nameof(kind));
            }
            // Shift the assembly, retaining all authored relative port/road coordinates.
            foreach(Transform child in holder)child.position-=pivot;
            foreach(var feedback in holder.GetComponentsInChildren<StationUpgradeFeedback>(true))feedback.RebasePlacement();
            var instance=holder.gameObject.AddComponent<FacilityInstance>();instance.Configure(id,kind,true);
            AddPorts(instance);
            return instance;
        }

        static Transform CreateCounter(Transform parent,bool cola,BurgerInventory player,RestaurantWallet wallet,CashFloor cash,DiningArea dining)
        {
            var root=new GameObject(cola?"ColaCounter":"BurgerCounter").transform;root.SetParent(parent,false);
            var body=RuntimeMaterials.Create(new Color(.12f,.21f,.26f));var top=RuntimeMaterials.Create(new Color(.38f,.70f,.78f));
            root.gameObject.AddComponent<BurgerVisual>().OwnMaterials(body,top);
            CourierVisuals.Part(root,"Body",new Vector3(0,.5f,0),new Vector3(3.2f,1,1.4f),body,true);
            CourierVisuals.Part(root,"Top",new Vector3(0,1.05f,0),new Vector3(3.35f,.12f,1.55f),top,true);
            var product=cola?KitchenProduct.Cola:KitchenProduct.Burger;
            var queue=root.gameObject.AddComponent<CustomerQueue>();queue.Product=product;
            queue.Configure(new Vector3(0,0,-7),new Vector3(0,0,-6),new[]{new Vector3(0,0,-1.8f),new Vector3(0,0,-3.4f),new Vector3(0,0,-5)},Vector3.zero);
            var stock=ShopFixtures.CreateCounterStock(root,Vector3.up*1.05f,false,product);
            var circle=ShopFixtures.CreateCashierCircle(root,ShopLayout.StaffCircleOffset);
            var drop=root.gameObject.AddComponent<CounterDropZone>();drop.Configure(stock,circle,1.05f,.25f,false,product);
            root.gameObject.AddComponent<BurgerServingZone>().Configure(queue,player,wallet,circle,ShopLayout.Exit,stock,dining,drop,10,cash);
            return root;
        }

        public static void AddPorts(FacilityInstance instance)
        {
            instance.Ports.Clear();
            void Add(Vector3 point)=>instance.Ports.Add(instance.transform.InverseTransformPoint(point));
            foreach(var p in instance.GetComponentsInChildren<BurgerPickupZone>(true))Add(p.PickupPoint.position);
            foreach(var d in instance.GetComponentsInChildren<CounterDropZone>(true))if(d.enabled)Add(d.DropPosition);
            foreach(var t in instance.GetComponentsInChildren<DiningTable>(true))
            {Add(t.WaitPosition);for(int i=0;i<t.SeatCount;i++)Add(t.SeatPosition(i));}
            foreach(var b in instance.GetComponentsInChildren<BoxingStation>(true))if(b.WorkRoot.gameObject.activeSelf)Add(b.CirclePosition);
            foreach(var b in instance.GetComponentsInChildren<BagLine>(true))
            {if(b.MachineBuilt)Add(b.MachinePosition);if(b.TableBuilt)Add(b.WorkPosition);if(b.CounterBuilt)Add(b.ServingPosition);}
            foreach(var d in instance.GetComponentsInChildren<DriveThruLane>(true))Add(d.WindowPosition);
            foreach(var q in instance.GetComponentsInChildren<CustomerQueue>(true))foreach(var p in q.QueuePositions)Add(p);
            foreach(var t in instance.GetComponentsInChildren<Transform>(true))
                if(t.name=="BoxingCircle"||t.name=="PackageDrop"||t.name=="ParcelInput"||t.name=="ParcelOutput"||t.name=="ParcelStock"||t.name=="BagCollect"||t.name=="BagWork"||t.name=="PickupServe")
                {var local=instance.transform.InverseTransformPoint(t.position);if(!instance.Ports.Contains(local))instance.Ports.Add(local);}

        }
    }
}
