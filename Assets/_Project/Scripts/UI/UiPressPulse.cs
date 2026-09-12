using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace BurgerShop.UI
{
    public sealed class UiPressPulse : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,ICancelHandler
    {
        Vector3 rest;float age=1,duration=.1f,peak;bool down;
        void Awake(){rest=transform.localScale;}
        public static void Pulse(Transform target,float peak,float seconds)
        {
            if(FeedbackDirector.Current!=null&&!FeedbackDirector.Current.DecorationsEnabled)return;
            var p=target.GetComponent<UiPressPulse>();if(p==null)p=target.gameObject.AddComponent<UiPressPulse>();
            p.peak=peak;p.duration=seconds;p.age=0;
        }
        public void OnPointerDown(PointerEventData e){var b=GetComponent<Button>();if(b!=null&&!b.interactable)return;down=true;transform.localScale=rest*.96f;}
        public void OnPointerUp(PointerEventData e){down=false;peak=-.04f;duration=.1f;age=0;}
        public void OnCancel(BaseEventData e){down=false;age=1;transform.localScale=rest;}
        void Update(){if(down)return;age+=Time.deltaTime;transform.localScale=rest*(1+peak*(peak>0?Mathf.Sin(Mathf.Clamp01(age/duration)*Mathf.PI):1-Mathf.Clamp01(age/duration)));}
        void OnDisable(){down=false;age=1;transform.localScale=rest;}
    }
}
