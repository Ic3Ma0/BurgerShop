using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BurgerShop.Building;
using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec090ChairSceneTests : SaveIsolatedGameplayTest
    {
        [UnityTest] public IEnumerator SampleSceneShowsContinuousChairUse()
        {
            var save=new RestaurantSaveData{version=18,compactStart=true,mainHallBuilt=true,grillLevel=1};
            Assert.That(new LocalSaveStore(SaveDirectory).Save(save),Is.True);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            for(int i=0;i<12;i++)yield return null;
            var layout=FacilityLayout.Current;
            var table=DiningTable.Create(layout.transform,new Vector3(4,0,-5));
            table.transform.rotation=Quaternion.Euler(0,90,0);Physics.SyncTransforms();layout.RefreshNavigation();
            var guest=CustomerAgent.Create(layout.transform,77,new Vector3(0,0,-9),1);guest.enabled=false;
            guest.BeginDeparture(null,new[]{new Vector3(0,0,-9)},0,DiningArea.Wrap(table),alreadyReceived:true);
            var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;
            camera.transform.position=table.Center+new Vector3(-6,8,-6);camera.transform.LookAt(table.Center);
            camera.orthographic=true;camera.orthographicSize=3.5f;
            var phase=typeof(CustomerAgent).GetField("phase",BindingFlags.Instance|BindingFlags.NonPublic);
            bool sit=false,eat=false,leave=false,done=false;
            Directory.CreateDirectory("/tmp/bs090-chair-review");
            var trace=new List<string>{"frame,phase,x,z,stepDistance"};
            for(int i=0;i<2400;i++)
            {
                var before=guest.transform.position;guest.AdvanceDeparture(1/60f);
                if(guest==null||guest.DepartureComplete){done=true;break;}
                Assert.That(Vector3.Distance(before,guest.transform.position),Is.LessThanOrEqualTo(.0321f));
                string state=phase.GetValue(guest).ToString();
                trace.Add(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2:F6},{3:F6},{4:F6}",i,state,guest.transform.position.x,guest.transform.position.z,Vector3.Distance(before,guest.transform.position)));
                bool capture=(state=="Seating"&&!sit)||(state=="Eating"&&!eat)||(state=="LeavingSeat"&&!leave);
                if(capture)
                {
                    if(state=="Seating")sit=true;if(state=="Eating")eat=true;if(state=="LeavingSeat")leave=true;
                    var rt=new RenderTexture(640,640,24);var old=camera.targetTexture;var active=RenderTexture.active;
                    camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                    var image=new Texture2D(640,640,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,640),0,0);image.Apply();
                    File.WriteAllBytes("/tmp/bs090-chair-review/"+state+".png",image.EncodeToPNG());
                    camera.targetTexture=old;RenderTexture.active=active;Object.Destroy(image);Object.Destroy(rt);
                }
                if(i%30==0)yield return null;
            }
            File.WriteAllLines("/tmp/bs090-chair-review/trajectory.csv",trace);
            Assert.That(sit&&eat&&leave&&done,Is.True,"All chair phases and departure must finish");
            Assert.That(table.TrashCount,Is.EqualTo(2));Assert.That(table.OccupiedSeats,Is.Zero);
            yield return new ExitPlayMode();
        }
    }
}
