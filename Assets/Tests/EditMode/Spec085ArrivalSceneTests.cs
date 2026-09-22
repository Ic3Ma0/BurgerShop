using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BurgerShop.Building;
using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Persistence;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec085ArrivalSceneTests : SaveIsolatedGameplayTest
    {
        [UnityTest] public IEnumerator MovedRotatedCounterRoutesFromDoorToItsCurrentSlots()
        {
            // Real old-layout save: its fixed indoor Entrance used to overshoot the queue.
            var save=new RestaurantSaveData{version=18,compactStart=true,mainHallBuilt=true,grillLevel=1,coins=777};
            Assert.That(new LocalSaveStore(SaveDirectory).Save(save),Is.True);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            for(int i=0;i<15;i++)yield return null;
            var q=MainKitchen<CustomerQueue>();q.enabled=false;q.SendMessage("OnApplicationFocus",true);
            var layout=FacilityLayout.Current;layout.Discover();
            var counter=q.GetComponentInParent<FacilityInstance>();Assert.That(counter,Is.Not.Null);
            Assert.That(layout.BeginMove(counter),Is.True);
            Assert.That(layout.Confirm(new Vector3(6,0,-4),90),Is.True,layout.LastError);
            q.Advance(.01f);
            Vector3[] slots=q.QueuePositions;
            var route=(Vector3[])typeof(CustomerQueue).GetField("route",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(q);
            var world=System.Array.ConvertAll(route,q.transform.TransformPoint);
            Assert.That(Vector3.Distance(world[0],RestaurantEntrance.Outside),Is.LessThan(.001f));
            Assert.That(Vector3.Distance(world[world.Length-1],slots[0]),Is.LessThan(.001f));
            Assert.That(System.Array.Exists(world,p=>Vector3.Distance(p,RestaurantEntrance.Door)<.05f),Is.True);
            var points=new List<string>{"ticket,x,z"};var previous=new Dictionary<CustomerAgent,Vector3>();
            for(int step=0;step<4200;step++)
            {
                foreach(var c in q.Customers)previous[c]=c.transform.position;
                q.Advance(1/60f);
                foreach(var c in q.Customers)
                {
                    var p=c.transform.position;
                    Assert.That(CustomerWalkPath.Clear(p,p),Is.True,"actual obstacle at "+p);
                    if(previous.TryGetValue(c,out var old)&&(p-old).sqrMagnitude>.000001f)
                    {
                        Assert.That(Vector3.Dot(c.transform.forward,(p-old).normalized),Is.GreaterThan(.99f));
                        if(old.z > -14.2f && old.x < -3f)
                            Assert.That(p.z-old.z,Is.GreaterThanOrEqualTo(-.003f),"doorway backstep: "+old+" -> "+p);
                    }
                    if(step%12==0)points.Add(c.TicketNumber+","+p.x.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+p.z.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
                }
                if(q.Count==3&&q.Customers.All(c=>c.HasReachedSlot))break;
                if(step%300==0)yield return null;
            }
            Assert.That(q.ReadyCustomer,Is.Not.Null);Assert.That(q.Count,Is.EqualTo(3));
            for(int i=0;i<3;i++)
            {Assert.That(Vector3.Distance(q.Customers[i].transform.position,slots[i]),Is.LessThan(.001f));Assert.That(q.Customers[i].TicketNumber,Is.EqualTo(i+1));}
            File.WriteAllLines("/tmp/bs085-moved-route.csv",points);
            yield return new ExitPlayMode();
        }
    }
}
