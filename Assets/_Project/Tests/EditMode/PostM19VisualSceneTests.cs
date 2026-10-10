using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZeroStarRestaurant.Editor;

namespace ZeroStarRestaurant.Tests
{
    public sealed class PostM19VisualSceneTests
    {
        [Test]
        public void RepairIsIncrementalIdempotentAndPreservesFunctionalPosesPhysicsAndUserObjects()
        {
            var scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
            try
            {
                Component[] All() => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)).ToArray();
                var transforms = All().OfType<Transform>().ToArray();
                bool Visual(Transform t) => t != null && (t.name == "Visual_VP1BC" || Visual(t.parent));
                // Recreate the faulty visual layers in memory; never restore an older scene over the user's scene.
                foreach (var shell in transforms.Where(t => t.name == "Visual_VP1BC"))
                {
                    foreach (Transform piece in shell)
                    {
                        if (piece.name is "TileWainscot" or "PaintWainscot" or "Skirting" or "WainscotTrim")
                        {
                            var filter = piece.GetComponent<MeshFilter>(); var size = filter.sharedMesh.bounds.size;
                            int axis = size.x > size.z ? 2 : 0; size[axis] = .004f;
                            var backing = shell.Find("Cladding").GetComponent<MeshFilter>().sharedMesh.bounds;
                            var p = piece.localPosition; p[axis] = Mathf.Sign(p[axis]) * (backing.extents[axis] + .002f);
                            piece.localPosition = p; filter.sharedMesh = VP1BCArtAssets.Box(size); piece.GetComponent<Renderer>().enabled = true;
                        }
                    }
                    if (shell.parent.name == "ProcurementFloor" || shell.parent.name == "ServiceCounterVolume")
                    {
                        var piece = shell.Find("Cladding"); piece.localPosition = Vector3.zero;
                        piece.GetComponent<MeshFilter>().sharedMesh = VP1BCArtAssets.Box(shell.parent.GetComponent<Collider>().bounds.size);
                    }
                    if (shell.parent.name == "TrayVisual") shell.Find("Cladding").GetComponent<Renderer>().enabled = true;
                }
                var manual = transforms.Single(t => t.name == "IngredientProcurementStation"); manual.position += new Vector3(.13f, 0, -.07f);
                var user = new GameObject("User prop", typeof(BoxCollider)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(user, scene);
                user.transform.SetPositionAndRotation(new Vector3(12, 1, 2), Quaternion.Euler(5, 15, 25)); user.GetComponent<BoxCollider>().isTrigger = true;
                var components = All();
                var poses = components.OfType<Transform>().Where(t => !Visual(t)).ToDictionary(t => t, t => EditorJsonUtility.ToJson(t));
                var rules = components.Where(c => c is Collider || c is Rigidbody || c is MonoBehaviour || c is Light).ToDictionary(c => c, EditorJsonUtility.ToJson);
                Assert.That(PostM19VisualFixBuilder.ConfigureScene(scene), Is.Not.Empty);
                foreach (var pair in poses) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value), pair.Key.name);
                foreach (var pair in rules) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value), pair.Key.name);
                Assert.That(All(), Is.EquivalentTo(components), "No duplicate shells or components can be created.");
                var manualVisual = transforms.First(t => t.name == "Skirting");
                manualVisual.localPosition += Vector3.up * .027f;
                var first = All().ToDictionary(c => c, EditorJsonUtility.ToJson);
                Assert.That(PostM19VisualFixBuilder.ConfigureScene(scene), Is.Empty);
                Assert.That(PostM19VisualFixBuilder.ConfigureScene(scene), Is.Empty);
                foreach (var pair in first) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        [Test]
        public void InstalledSceneHasOneFloorSurfaceDistinctWallLayersAndNoDuplicateSupportOrCounterTop()
        {
            var scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
            try
            {
                var transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                Transform Find(string name) => transforms.Single(t => t.name == name);
                var main = Find("Floor").Find("Visual_VP1BC/Cladding").GetComponent<Renderer>().bounds;
                var annex = Find("ProcurementFloor").Find("Visual_VP1BC/Cladding").GetComponent<Renderer>().bounds;
                Assert.That(annex.max.x, Is.EqualTo(main.min.x).Within(.0001f)); Assert.That(annex.max.y, Is.EqualTo(main.max.y));
                var counter = Find("ServiceCounterVolume").Find("Visual_VP1BC");
                Assert.That(counter.Find("Cladding").GetComponent<Renderer>().bounds.max.y,
                    Is.EqualTo(counter.Find("CounterTop").GetComponent<Renderer>().bounds.min.y).Within(.0001f));
                foreach (var shell in transforms.Where(t => t.name == "Visual_VP1BC"))
                {
                    Assert.That(shell.GetComponentsInChildren<Collider>(true), Is.Empty);
                    var pieces = shell.Cast<Transform>().ToArray();
                    if (shell.parent.name.Contains("Lintel"))
                        Assert.That(pieces.Where(t => t.name != "Cladding").Select(t => t.GetComponent<Renderer>().enabled), Is.All.False);
                    var wainscot = pieces.FirstOrDefault(t => t.name.EndsWith("Wainscot"));
                    if (wainscot == null || !wainscot.GetComponent<Renderer>().enabled) continue;
                    int axis = wainscot.GetComponent<MeshFilter>().sharedMesh.bounds.size.x > .1f ? 2 : 0;
                    float Face(Transform t) => Mathf.Abs(t.localPosition[axis]) + t.GetComponent<MeshFilter>().sharedMesh.bounds.extents[axis];
                    foreach (var trim in pieces.Where(t => t.name is "Skirting" or "WainscotTrim"))
                        Assert.That(Face(trim) - Face(wainscot), Is.GreaterThanOrEqualTo(.015f), shell.parent.name);
                }
                foreach (var tray in transforms.Where(t => t.name == "TrayVisual"))
                    Assert.That(tray.Find("Visual_VP1BC/Cladding").GetComponent<Renderer>().enabled, Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
