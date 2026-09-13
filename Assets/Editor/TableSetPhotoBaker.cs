using System.Collections.Generic;
using System.IO;
using BurgerShop.Building;
using BurgerShop.Customer;
using BurgerShop.Restaurant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BurgerShop.Editor
{
    // Render-only export in a temporary preview scene. Never opens or saves the player's scene/save.
    public static class TableSetPhotoBaker
    {
        [MenuItem("BurgerShop/Rebuild table set photos")]
        public static void Bake()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play before rebuilding catalog photos.");
            string directory=Path.Combine(Application.dataPath,"_Project/Resources/TableSets");
            Directory.CreateDirectory(directory);
            var scene=EditorSceneManager.NewPreviewScene();
            var root=new GameObject("CatalogPhotoStudio");SceneManager.MoveGameObjectToScene(root,scene);
            var inactive=new GameObject("InactiveModels");inactive.transform.SetParent(root.transform,false);inactive.SetActive(false);
            var cameraObject=new GameObject("PhotoCamera");cameraObject.transform.SetParent(root.transform,false);
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;
            camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0,0,0,0);
            camera.nearClipPlane=.01f;camera.farClipPlane=100;camera.transform.rotation=Quaternion.Euler(27,-35,0);
            var lamp=new GameObject("KeyLight");lamp.transform.SetParent(root.transform,false);
            var light=lamp.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.15f;lamp.transform.rotation=Quaternion.Euler(45,-35,0);
            var fill=new GameObject("FillLight");fill.transform.SetParent(root.transform,false);
            var fillLight=fill.AddComponent<Light>();fillLight.type=LightType.Directional;fillLight.intensity=.5f;fill.transform.rotation=Quaternion.Euler(30,150,0);
            try
            {
                for(int kind=0;kind<3;kind++)
                foreach(var set in TableSetCatalog.Choices)
                {
                    string key=((FacilityKind)kind)+"_"+set;
                    var source=DiningTable.Create(inactive.transform,Vector3.zero,(DiningTableKind)kind).transform;
                    source.GetComponent<DiningTable>().ApplySet(set);
                    var display=new GameObject(key+"Display");display.transform.SetParent(root.transform,false);
                    var materials=new HashSet<Material>();
                    bool any=false;Bounds bounds=new Bounds();
                    foreach(var renderer in source.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        foreach(var material in renderer.sharedMaterials)if(material!=null)materials.Add(material);
                        if(!VisibleModel(renderer,source.transform,false))continue;
                        var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
                        var part=new GameObject(renderer.name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(display.transform,false);
                        part.transform.SetPositionAndRotation(renderer.transform.position,renderer.transform.rotation);part.transform.localScale=renderer.transform.lossyScale;
                        part.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                        var copy=part.GetComponent<MeshRenderer>();copy.sharedMaterials=renderer.sharedMaterials;
                        copy.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                        if(any)bounds.Encapsulate(copy.bounds);else {bounds=copy.bounds;any=true;}
                    }
                    if(!any)throw new System.InvalidOperationException("No model for "+key);
                    float width=0,height=0;
                    for(int i=0;i<8;i++)
                    {
                        var corner=Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                        var projected=Quaternion.Inverse(camera.transform.rotation)*corner;
                        width=Mathf.Max(width,Mathf.Abs(projected.x));height=Mathf.Max(height,Mathf.Abs(projected.y));
                    }
                    camera.orthographicSize=Mathf.Max(height,width/(512f/384f))*1.08f;
                    camera.transform.position=bounds.center-camera.transform.forward*20;
                    Capture(camera,Path.Combine(directory,key+".png"));
                    Object.DestroyImmediate(display);Object.DestroyImmediate(source.gameObject);
                    foreach(var material in materials)if(material!=null)Object.DestroyImmediate(material);
                }
            }
            finally {EditorSceneManager.ClosePreviewScene(scene);}
            AssetDatabase.Refresh();
            foreach(string file in Directory.GetFiles(directory,"*.png"))
            {
                string path="Assets/_Project/Resources/TableSets/"+Path.GetFileName(file);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=512;importer.SaveAndReimport();
            }
            Debug.Log("Exported nine table set photos.");
        }

        static bool VisibleModel(MeshRenderer renderer,Transform source,bool includeFloor)
        {
            if(!renderer.enabled||renderer.GetComponent<TextMesh>()!=null||renderer.GetComponentInParent<CustomerAgent>()!=null)return false;
            for(var p=renderer.transform;p!=source&&p!=null;p=p.parent)
            {
                if(!p.gameObject.activeSelf)return false;
                string n=p.name;
                if(n.Contains("Badge")||n.Contains("PickupSpot")||n.Contains("Circle")||n.StartsWith("Dash_")||n.Contains("Label")||n.Contains("Progress")||
                    n.Contains("Upgrade")||(!includeFloor&&n.Contains("Floor"))||n.Contains("Road")||n.Contains("Lane")||n.Contains("StopPost")||
                    n=="ParcelInput"||n=="ParcelOutput"||n=="ParcelStock"||n=="BagCollect"||n=="BagWork"||n=="PickupServe"||n=="PackageDrop")return false;
            }
            return true;
        }
        static void Capture(Camera camera,string path)
        {
            var target=new RenderTexture(512,384,24,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;
            var image=new Texture2D(512,384,TextureFormat.RGBA32,false);
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,512,384),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally{camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(image);Object.DestroyImmediate(target);}
        }
    }
}
