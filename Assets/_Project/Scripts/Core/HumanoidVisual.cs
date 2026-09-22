using UnityEngine;
using BurgerShop.Restaurant;
namespace BurgerShop.Core
{
    // Visual limbs only: movement, collider and carried-item anchors stay owned by gameplay.
    public sealed class HumanoidVisual : MonoBehaviour
    {
        Transform[] arms=new Transform[2],legs=new Transform[2];Vector3 previous;float phase;
        bool phonePose;
        public void SetPhonePose()
        {
            phonePose=true;
            arms[1].localRotation=Quaternion.Euler(-150,0,0);
        }
        public static void Add(Transform root,float feet,Material shirt,Material skin,Material trousers)
        {
            var rig=root.gameObject.AddComponent<HumanoidVisual>();rig.previous=root.position;
            for(int i=0;i<2;i++)
            {
                float side=i==0?-1:1;
                var leg=new GameObject(i==0?"LeftLeg":"RightLeg").transform;leg.SetParent(root,false);leg.localPosition=new Vector3(side*.18f,feet+.65f,0);rig.legs[i]=leg;
                Part(leg,"Trouser",new Vector3(0,-.25f,0),new Vector3(.23f,.5f,.25f),trousers);
                Part(leg,"Shoe",new Vector3(0,-.57f,.07f),new Vector3(.25f,.16f,.4f),trousers);
                var arm=new GameObject(i==0?"LeftArm":"RightArm").transform;arm.SetParent(root,false);arm.localPosition=new Vector3(side*.4f,feet+1.18f,0);rig.arms[i]=arm;
                Part(arm,"Sleeve",new Vector3(0,-.14f,0),new Vector3(.22f,.30f,.25f),shirt);
                Part(arm,"Hand",new Vector3(0,-.37f,.025f),new Vector3(.18f,.21f,.20f),skin);
            }
        }
        static void Part(Transform parent,string name,Vector3 p,Vector3 size,Material mat)
        {
            var type=name=="Hand" || name=="Shoe"?PrimitiveType.Sphere:PrimitiveType.Capsule;
            if(type==PrimitiveType.Capsule)size.y*=.5f;
            BagVisualFactory.Part(parent,name,type,p,size,mat);
        }
        void LateUpdate()
        {
            float distance=Vector3.Distance(transform.position,previous);previous=transform.position;
            if(Time.deltaTime<=0)return;
            bool walking=distance>.001f&&distance<1f;if(walking)phase+=distance*8;
            float angle=walking?Mathf.Sin(phase)*25:0;
            for(int i=0;i<2;i++)
            {
                float a=i==0?angle:-angle;
                legs[i].localRotation=Quaternion.Slerp(legs[i].localRotation,Quaternion.Euler(a,0,0),1-Mathf.Exp(-20*Time.deltaTime));
                arms[i].localRotation=Quaternion.Slerp(arms[i].localRotation,Quaternion.Euler(phonePose && i==1?-150:-a*.65f,0,0),1-Mathf.Exp(-20*Time.deltaTime));
            }
        }
    }
}
