using System.Collections;
using BurgerShop.Player;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec027GameplayTests : SaveIsolatedGameplayTest
    {
        [UnityTest]
        public IEnumerator RealMotorStartsStopsTurnsAndCameraKeepsItsOrientation()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();Time.captureDeltaTime=1f/60;
            var motor=Object.FindFirstObjectByType<PlayerMotor>();motor.SendMessage("OnApplicationFocus",true);
            var joystick=Object.FindFirstObjectByType<VirtualJoystick>();var pad=(RectTransform)joystick.transform;
            var controller=motor.GetComponent<CharacterController>();controller.enabled=false;motor.transform.position=new Vector3(4,1.05f,-4);controller.enabled=true;
            var camera=Camera.main;var follow=camera.GetComponent<CameraFollow>();follow.Snap();var initialRotation=camera.transform.rotation;
            Vector2 center=RectTransformUtility.WorldToScreenPoint(null,pad.position);
            var evt=new PointerEventData(EventSystem.current){pointerId=101,position=RectTransformUtility.WorldToScreenPoint(null,pad.TransformPoint(new Vector3(110,0,0)))};
            joystick.OnPointerDown(evt);yield return null;yield return null;
            Assert.That(motor.CommandedVelocity.magnitude,Is.EqualTo(motor.MoveSpeed).Within(.01f));
            for(int i=0;i<20;i++)yield return null;
            evt.position=RectTransformUtility.WorldToScreenPoint(null,pad.TransformPoint(new Vector3(0,110,0)));joystick.OnDrag(evt);
            int steps=0;
            var forward=camera.transform.forward;forward.y=0;var desired=Quaternion.LookRotation(forward.normalized);
            do{yield return null;steps++;}while(Quaternion.Angle(motor.transform.rotation,desired)>5 && steps<20);
            Assert.That(steps/60f,Is.InRange(.10f,.18f));
            Assert.That(Quaternion.Angle(initialRotation,camera.transform.rotation),Is.LessThan(.001f));
            joystick.OnPointerUp(evt);yield return null;yield return null;
            Assert.That(motor.LastInput,Is.EqualTo(Vector2.zero));Assert.That(motor.CommandedVelocity,Is.EqualTo(Vector3.zero));
            var stopped=motor.transform.position;var lag=camera.transform.position;follow.Snap();var target=camera.transform.position;camera.transform.position=lag;
            float error=Vector3.Distance(lag,target);
            for(int i=0;i<15;i++)yield return null;
            Assert.That(Vector3.Distance(camera.transform.position,target),Is.LessThanOrEqualTo(error*.051f+.001f));
            Assert.That(ShopDistance(motor.transform.position,stopped),Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(initialRotation,camera.transform.rotation),Is.LessThan(.001f));
            yield return new ExitPlayMode();
        }
        static float ShopDistance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
    }
}
