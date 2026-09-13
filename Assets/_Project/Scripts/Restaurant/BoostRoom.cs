using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class BoostRoom : MonoBehaviour
    {
        public Transform BoostPoint { get; private set; }
        public TextMesh BoostLabel { get; private set; }
        public Vector3 StationPosition => ShopLayout.BoostStation;
        public Vector3 DoorPosition => ShopLayout.BoostDoor;

        public static BoostRoom Create(Transform parent, Material wall, Material floor)
        {
            GameObject root = new GameObject("BoostRoom");
            root.transform.SetParent(parent, false);
            BoostRoom room = root.AddComponent<BoostRoom>();
            room.Build(wall, floor);
            return room;
        }

        public bool Contains(Vector3 point) => ShopLayout.ContainsBoostRoom(point);

        void Build(Material wall, Material floor)
        {
            // Spec 046: the former training room is now an open drive-through work bay.
            CreatePart("CarServiceFloor",PrimitiveType.Cube,new Vector3(11,-.1f,-21),new Vector3(8.2f,.2f,13),floor,true);
            var trim=BurgerShop.Core.RuntimeMaterials.Create(new Color(.16f,.43f,.78f));
            foreach(float x in new[]{6.9f,15.1f})CreatePart("CarServiceSide",PrimitiveType.Cube,new Vector3(x,.85f,-21),new Vector3(.2f,1.7f,12),trim,true);
            var cream=BurgerShop.Core.RuntimeMaterials.Create(new Color(.94f,.94f,.84f));
            var red=BurgerShop.Core.RuntimeMaterials.Create(new Color(.96f,.23f,.13f));
            foreach(float x in new[]{8.25f,13.75f})
                CreatePart("ServiceWindowWall",PrimitiveType.Cube,new Vector3(x,1.2f,-27.15f),new Vector3(2.7f,2.4f,.3f),cream,true);
            CreatePart("ServiceWindowLandmarkSill",PrimitiveType.Cube,new Vector3(11,.85f,-27.15f),new Vector3(2.8f,.2f,.75f),trim,true);
            CreatePart("ServiceWindowHeader",PrimitiveType.Cube,new Vector3(11,2.3f,-27.15f),new Vector3(3,.25f,.35f),trim,true);
            CreatePart("ServiceWindowAwning",PrimitiveType.Cube,new Vector3(11,2.55f,-27.55f),new Vector3(4,.16f,1.25f),red,false);
            foreach(float x in new[]{9.55f,12.45f})CreatePart("ServiceWindowLandmarkFrame",PrimitiveType.Cube,new Vector3(x,1.65f,-27.15f),new Vector3(.12f,1.3f,.4f),trim,true);
            var point=new GameObject("BoostPoint");point.transform.SetParent(transform,false);point.transform.position=ShopLayout.BoostPoint;BoostPoint=point.transform;
            BoostLabel=new GameObject("BoostLabel").AddComponent<TextMesh>();BoostLabel.transform.SetParent(transform,false);
            BoostLabel.transform.position=ShopLayout.BoostPoint+Vector3.up;BoostLabel.text="Player upgrades";
            ShopFixtures.CreateActionCircle(transform,"PlayerUpgradePoint",ShopLayout.BoostPoint,new Color(.3f,.65f,.9f));
        }

        GameObject CreatePart(string name, PrimitiveType type, Vector3 position, Vector3 scale,
            Material material, bool keepCollider)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(transform, false);
            part.transform.position = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            SolidOccupancy.Apply(collider, keepCollider);
            return part;
        }
    }
}
