using System.Collections.Generic;
using UnityEngine;
namespace BurgerShop.UI
{
    // Duplicate only rendering for 0.30s: colliders, roots, anchors and trigger ranges never scale.
    public sealed class VisualMeshPulse : MonoBehaviour
    {
        sealed class Part { public MeshRenderer Original,Copy;public Transform Transform; }
        readonly List<Part> parts=new List<Part>();float age;
        public static void Play(Transform root)
        {
            if(!Application.isPlaying||root==null || (FeedbackDirector.Current!=null&&!FeedbackDirector.Current.DecorationsEnabled))return;
            var old=root.GetComponent<VisualMeshPulse>();if(old!=null)return;
            var pulse=root.gameObject.AddComponent<VisualMeshPulse>();
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                var mesh=renderer.GetComponent<MeshFilter>();
                if(!renderer.enabled||mesh==null||renderer.GetComponent<TextMesh>()!=null)continue;
                var obj=new GameObject("FeedbackMesh",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(renderer.transform,false);
                obj.GetComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
                var copy=obj.GetComponent<MeshRenderer>();copy.sharedMaterials=renderer.sharedMaterials;copy.shadowCastingMode=renderer.shadowCastingMode;
                renderer.enabled=false;pulse.parts.Add(new Part{Original=renderer,Copy=copy,Transform=obj.transform});
            }
        }
        void Update()
        {
            age+=Time.deltaTime;float scale=1+.08f*Mathf.Sin(Mathf.Clamp01(age/.30f)*Mathf.PI);
            foreach(var p in parts)if(p.Transform!=null)p.Transform.localScale=Vector3.one*scale;
            if(age>=.30f)Destroy(this);
        }
        void OnDestroy(){foreach(var p in parts){if(p.Original!=null)p.Original.enabled=true;if(p.Transform!=null)Destroy(p.Transform.gameObject);}}
    }
}
