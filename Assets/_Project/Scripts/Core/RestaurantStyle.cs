using UnityEngine;
using BurgerShop.Restaurant;
namespace BurgerShop.Core
{
    // Shared, original geometry and palette for readable restaurant equipment.
    public static class RestaurantStyle
    {
        public static readonly Color Cream=new Color(.94f,.91f,.81f), Red=new Color(.83f,.19f,.12f),
            Steel=new Color(.66f,.73f,.75f), Ink=new Color(.12f,.18f,.20f), Blue=new Color(.15f,.43f,.60f);
        public static GameObject Block(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool solid=false)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(EquipmentMesh));
            go.transform.SetParent(parent,false);go.transform.localPosition=position;
            go.GetComponent<MeshRenderer>().sharedMaterial=material;
            var mesh=new Mesh{name=name+" Rounded"};
            const int ring=16;var vertices=new Vector3[ring*4];var triangles=new System.Collections.Generic.List<int>();
            float bevel=Mathf.Min(.065f,Mathf.Min(size.y*.22f,Mathf.Min(size.x,size.z)*.1f));
            float radius=Mathf.Min(.12f,Mathf.Min(size.x,size.z)*.2f);
            for(int layer=0;layer<4;layer++)
            {
                float inset=layer==0||layer==3?bevel:0;
                float y=layer==0?-size.y*.5f:layer==1?-size.y*.5f+bevel:layer==2?size.y*.5f-bevel:size.y*.5f;
                for(int corner=0;corner<4;corner++)for(int step=0;step<4;step++)
                {
                    float a=(corner*90+step*30)*Mathf.Deg2Rad;
                    float cx=(corner==0||corner==3?1:-1)*(size.x*.5f-radius);
                    float cz=(corner<2?1:-1)*(size.z*.5f-radius);
                    vertices[layer*ring+corner*4+step]=new Vector3(cx+Mathf.Cos(a)*(radius-inset),y,cz+Mathf.Sin(a)*(radius-inset));
                }
            }
            for(int layer=0;layer<3;layer++)for(int i=0;i<ring;i++)
            {
                int a=layer*ring+i,b=layer*ring+(i+1)%ring,c=a+ring,d=b+ring;
                triangles.AddRange(new[]{a,c,b,b,c,d});
            }
            for(int i=1;i<ring-1;i++){triangles.AddRange(new[]{0,i,i+1,48,48+i+1,48+i});}
            mesh.vertices=vertices;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<EquipmentMesh>().Mesh=mesh;
            if(solid)go.AddComponent<BoxCollider>().size=size;
            return go;
        }
    }
    public sealed class EquipmentMesh : MonoBehaviour
    {
        public Mesh Mesh;
        void OnDestroy(){if(Mesh!=null)BurgerVisual.Release(Mesh);}
    }
}
