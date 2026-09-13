using System.Collections.Generic;
using BurgerShop.Core;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    // A compact burger fits the existing .34 stock/carry spacing. Geometry is shared:
    // replenishing a busy restaurant does not rebuild meshes or add seed GameObjects.
    static class BurgerVisualFactory
    {
        static Mesh bottom, meat, cheeseSlice, lettuceLeaf, tomatoSlice, crown, sesame;

        public static Transform Create(Transform parent, int index)
        {
            EnsureGeometry();
            var root = new GameObject($"Burger_{index + 1}").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0, index * .34f, 0);
            var bun = RuntimeMaterials.Create(new Color(.95f, .59f, .17f));
            var patty = RuntimeMaterials.Create(new Color(.29f, .12f, .065f));
            var cheese = RuntimeMaterials.Create(new Color(1f, .75f, .09f));
            var lettuce = RuntimeMaterials.Create(new Color(.30f, .64f, .10f));
            var tomato = RuntimeMaterials.Create(new Color(.86f, .18f, .09f));
            var seeds = RuntimeMaterials.Create(new Color(1f, .91f, .67f));
            root.gameObject.AddComponent<BurgerVisual>().OwnMaterials(bun, patty, cheese, lettuce, tomato, seeds);
            Part(root, "BottomBun", bottom, bun);
            Part(root, "Patty", meat, patty);
            Part(root, "Cheese", cheeseSlice, cheese);
            Part(root, "Lettuce", lettuceLeaf, lettuce);
            Part(root, "Tomato", tomatoSlice, tomato);
            Part(root, "TopBun", crown, bun);
            Part(root, "Sesame", sesame, seeds);
            return root;
        }

        static void Part(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        static void EnsureGeometry()
        {
            if (crown != null) return;
            bottom = Rings("Bun heel", new[]{new Vector2(0,0),new Vector2(.23f,0),new Vector2(.27f,.018f),new Vector2(.27f,.050f),new Vector2(.245f,.073f),new Vector2(0,.073f)});
            meat = Rings("Seared patty", new[]{new Vector2(0,.071f),new Vector2(.245f,.071f),new Vector2(.272f,.09f),new Vector2(.266f,.126f),new Vector2(.24f,.142f),new Vector2(0,.142f)}, .008f);
            cheeseSlice = Rings("Melted cheese", new[]{new Vector2(0,.146f),new Vector2(.245f,.141f),new Vector2(.249f,.153f),new Vector2(0,.160f)}, 0, true);
            lettuceLeaf = Rings("Ruffled lettuce", new[]{new Vector2(0,.161f),new Vector2(.24f,.162f),new Vector2(.29f,.169f),new Vector2(.29f,.178f),new Vector2(.23f,.181f),new Vector2(0,.181f)}, .018f);
            tomatoSlice = Rings("Tomato slice", new[]{new Vector2(0,.181f),new Vector2(.245f,.181f),new Vector2(.257f,.191f),new Vector2(.249f,.207f),new Vector2(0,.208f)});
            crown = Rings("Sesame bun", new[]{new Vector2(0,.205f),new Vector2(.265f,.205f),new Vector2(.277f,.225f),new Vector2(.257f,.267f),new Vector2(.21f,.303f),new Vector2(.12f,.326f),new Vector2(0,.331f)});
            sesame = Seeds();
        }

        static Mesh Rings(string name, Vector2[] profile, float ruffle = 0, bool square = false)
        {
            const int segments = 32;
            var vertices = new Vector3[profile.Length * segments];
            var triangles = new List<int>();
            for (int ring = 0; ring < profile.Length; ring++)
                for (int i = 0; i < segments; i++)
                {
                    float a = i * Mathf.PI * 2 / segments;
                    float radius = profile[ring].x;
                    if (radius > 0)
                        radius = square ? Mathf.Min(.321f, radius / Mathf.Max(Mathf.Abs(Mathf.Cos(a)), Mathf.Abs(Mathf.Sin(a)))) : radius + Mathf.Sin(a * 9) * ruffle;
                    float y = profile[ring].y + (radius > .25f ? Mathf.Cos(a * 9) * ruffle * .4f : 0);
                    vertices[ring * segments + i] = new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
                    if (ring == 0) continue;
                    int b = (ring - 1) * segments + i, c = (ring - 1) * segments + (i + 1) % segments;
                    triangles.AddRange(new[]{b, b + segments, c, c, b + segments, c + segments});
                }
            return Mesh(name, vertices, triangles.ToArray());
        }

        static Mesh Seeds()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            // Follows the polygonal crown profile so seeds sit on the bun, not in mid-air.
            float[] radii = {0,.12f,.21f,.257f};
            float[] heights = {.331f,.326f,.303f,.267f};
            for (int i = 0; i < 10; i++)
            {
                float a = i * 2.4f, r = .046f + (i % 3) * .073f;
                int ring = r < .12f ? 0 : 1;
                float y = Mathf.Lerp(heights[ring], heights[ring + 1], (r - radii[ring]) / (radii[ring + 1] - radii[ring]));
                var center = new Vector3(Mathf.Cos(a) * r, y + .001f, Mathf.Sin(a) * r);
                var axis = new Vector3(Mathf.Cos(a + .7f), 0, Mathf.Sin(a + .7f)) * .018f;
                var side = Vector3.Cross(Vector3.up, axis).normalized * .006f;
                int n = vertices.Count;
                vertices.AddRange(new[]{center + axis, center + side, center - axis, center - side, center + Vector3.up * .005f});
                for (int j = 0; j < 4; j++) triangles.AddRange(new[]{n+j, n+(j+1)%4, n+4});
            }
            return Mesh("Sesame seeds", vertices.ToArray(), triangles.ToArray());
        }

        static Mesh Mesh(string name, Vector3[] vertices, int[] triangles)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetGeometry()
        {
            foreach (var mesh in new[]{bottom, meat, cheeseSlice, lettuceLeaf, tomatoSlice, crown, sesame})
                if (mesh != null) BurgerVisual.Release(mesh);
            bottom = meat = cheeseSlice = lettuceLeaf = tomatoSlice = crown = sesame = null;
        }
    }
}
