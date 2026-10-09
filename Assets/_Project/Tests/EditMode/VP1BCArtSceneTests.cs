using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Presentation;

namespace ZeroStarRestaurant.Tests
{
    public sealed class VP1BCArtSceneTests
    {
        [Test]
        public void ShellsCannotBlockInteractionMovementThermalOrCleaningQueries()
        {
            var scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
            try
            {
                var art = scene.GetRootGameObjects().Select(r => r.GetComponent<RestaurantArtPass>()).Single(p => p != null);
                Assert.That(art.Shells.Length, Is.GreaterThan(20));
                foreach (var shell in art.Shells)
                {
                    Assert.That(shell.GetComponentsInChildren<Collider>(true), Is.Empty, shell.transform.parent.name);
                    Assert.That(shell.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
                    Assert.That(shell.GetComponentsInChildren<Light>(true), Is.Empty);
                    foreach (var behaviour in shell.GetComponentsInChildren<MonoBehaviour>(true))
                        Assert.That(behaviour, Is.TypeOf<FoodStageVisual>());
                }
                foreach (var path in new[] { "Food/Bun", "Food/RawBeefPatty", "Food/Cheese", "Plate" })
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/" + path + ".prefab");
                    var visual = prefab.transform.Find("Visual_VP1BC"); Assert.That(visual, Is.Not.Null);
                    Assert.That(visual.GetComponentsInChildren<Collider>(true), Is.Empty);
                    Assert.That(prefab.GetComponent<BoxCollider>().enabled, Is.True);
                    Assert.That(prefab.activeSelf, Is.False, "Procurement still prepares inactive original units.");
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        [Test]
        public void IncrementalInstallAndComparisonPreserveManualPosesAndAllGameplaySerialization()
        {
            var scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
            try
            {
                Component[] All() => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)).ToArray();
                var art = All().OfType<RestaurantArtPass>().Single(); art.ArtOff();
                var shells = art.Shells.ToArray(); Object.DestroyImmediate(art.gameObject);
                foreach (var shell in shells) Object.DestroyImmediate(shell);
                var procurement = All().OfType<Transform>().Single(t => t.name == "ProcurementArea"); procurement.position += new Vector3(.19f, 0, -.08f);
                var poses = All().OfType<Transform>().ToDictionary(t => t, t => (t.localPosition, t.localRotation, t.localScale, t.parent));
                var rules = All().Where(c => c is Collider || c is Rigidbody || c is MonoBehaviour).ToDictionary(c => c, EditorJsonUtility.ToJson);
                var lights = All().OfType<Light>().ToDictionary(c => c, EditorJsonUtility.ToJson);
                var originals = All().OfType<Renderer>().ToDictionary(r => r, r => r.sharedMaterials);
                art = VP1BCArtBuilder.ConfigureScene(scene);
                foreach (var pair in poses) Assert.That((pair.Key.localPosition, pair.Key.localRotation, pair.Key.localScale, pair.Key.parent), Is.EqualTo(pair.Value), pair.Key.name);
                foreach (var pair in rules) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value), pair.Key.name);
                foreach (var pair in originals) Assert.That(pair.Key.sharedMaterials, Is.EqualTo(pair.Value));
                var manual = art.Shells.First().transform; manual.localPosition += Vector3.right * .035f;
                var count = All().Length; var position = manual.localPosition;
                VP1BCArtBuilder.ConfigureScene(scene); VP1BCArtBuilder.ConfigureScene(scene);
                Assert.That(All().Length, Is.EqualTo(count)); Assert.That(manual.localPosition, Is.EqualTo(position));
                art.ArtOff(); art.ArtOn();
                foreach (var pair in rules) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value));
                art.ArtOff();
                foreach (var pair in lights) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
