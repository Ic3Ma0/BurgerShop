using System.Collections.Generic;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.Core
{
    // Each actor owns its generated assets. Shared Unity primitive meshes are never destroyed.
    [ExecuteAlways]
    public sealed class CharacterVisualResources : MonoBehaviour
    {
        internal readonly List<Material> Materials = new List<Material>();
        readonly List<Mesh> meshes = new List<Mesh>();

        internal void Combine(Transform joint)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(Transform child in joint)
            {
                var filter=child.GetComponent<MeshFilter>();
                var renderer=child.GetComponent<MeshRenderer>();
                if(filter==null || renderer==null || !renderer.enabled)continue;
                var material=renderer.sharedMaterial;
                if(!groups.TryGetValue(material,out var parts))groups[material]=parts=new List<CombineInstance>();
                parts.Add(new CombineInstance { mesh=filter.sharedMesh,transform=Matrix4x4.TRS(child.localPosition,child.localRotation,child.localScale) });
                renderer.enabled=false; // Named source anchors remain available to existing callers.
            }
            foreach(var group in groups)
            {
                var mesh=new Mesh { name="Character surface" };
                mesh.CombineMeshes(group.Value.ToArray(),true,true);meshes.Add(mesh);
                var surface=new GameObject("CharacterSurface",typeof(MeshFilter),typeof(MeshRenderer));
                surface.transform.SetParent(joint,false);
                surface.GetComponent<MeshFilter>().sharedMesh=mesh;
                surface.GetComponent<MeshRenderer>().sharedMaterial=group.Key;
            }
        }

        void OnDestroy()
        {
            foreach(var mesh in meshes)if(mesh!=null)BurgerVisual.Release(mesh);
            foreach(var material in Materials)if(material!=null)BurgerVisual.Release(material);
        }
    }
}
