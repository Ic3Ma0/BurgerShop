using System.Collections.Generic;
using UnityEngine;
namespace BurgerShop.UI
{
    // Material property blocks keep the original shared materials and physical table intact.
    public sealed class TableCleanFlash : MonoBehaviour
    {
        sealed class Part { public Renderer Renderer; public MaterialPropertyBlock Original; public Color Color; }
        readonly List<Part> parts=new List<Part>();
        float age;
        public static void Play(Transform table)
        {
            if(!Application.isPlaying || table==null || (FeedbackDirector.Current!=null&&!FeedbackDirector.Current.DecorationsEnabled))return;
            if(table.GetComponent<TableCleanFlash>()!=null)return;
            var flash=table.gameObject.AddComponent<TableCleanFlash>();
            foreach(var r in table.GetComponentsInChildren<MeshRenderer>())
            {
                if(r.GetComponent<TextMesh>()!=null || r.sharedMaterial==null || !r.sharedMaterial.HasProperty("_BaseColor"))continue;
                var original=new MaterialPropertyBlock();r.GetPropertyBlock(original);
                flash.parts.Add(new Part{Renderer=r,Original=original,Color=r.sharedMaterial.GetColor("_BaseColor")});
            }
        }
        void Update()
        {
            age+=Time.deltaTime;
            foreach(var p in parts)
            {
                if(p.Renderer==null)continue;
                var block=new MaterialPropertyBlock();p.Renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor",Color.Lerp(p.Color,Color.white,.32f*Mathf.Sin(Mathf.Clamp01(age/.35f)*Mathf.PI)));
                p.Renderer.SetPropertyBlock(block);
            }
            if(age>=.35f)Destroy(this);
        }
        void OnDestroy(){foreach(var p in parts)if(p.Renderer!=null)p.Renderer.SetPropertyBlock(p.Original);}
    }
}
