using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Hygiene;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M17SceneTests
    {
        [Test]
        public void FirstInstallationAddsComponentsWithoutRestoringEarlierUserLayoutOrMaterials()
        {
            var scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
            try
            {
                Component[] All() => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
                Object.DestroyImmediate(scene.GetRootGameObjects().Single(root => root.name == "Hygiene"));
                Object.DestroyImmediate(All().OfType<CleaningTool>().Single().gameObject);
                Object.DestroyImmediate(All().OfType<CleaningInteraction>().Single());
                var bench = All().OfType<BoxCollider>().Single(item => item.name == "AssemblyWorkbench");
                bench.transform.position += new Vector3(.12f, .03f, -.08f);
                bench.transform.rotation = Quaternion.Euler(0, 7, 0);
                var originals = All();
                var poses = originals.OfType<Transform>().ToDictionary(item => item, item => (item.position, item.rotation, item.localScale));
                var materials = originals.OfType<Renderer>().ToDictionary(item => item, item => item.sharedMaterials);
                M17HygieneBuilder.ConfigureScene(scene);
                foreach (var original in originals) Assert.That(All(), Does.Contain(original));
                foreach (var pair in poses) Assert.That((pair.Key.position, pair.Key.rotation, pair.Key.localScale), Is.EqualTo(pair.Value));
                foreach (var pair in materials) Assert.That(pair.Key.sharedMaterials, Is.EqualTo(pair.Value));
                Assert.That(All().OfType<CleanableSurface>().Count(), Is.EqualTo(7));
                Assert.That(All().OfType<CleaningInteraction>().Count(), Is.EqualTo(1));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void IncrementalHygieneInstallerPreservesUserEditsAndIsIdempotent()
        {
            var scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
            var added = new GameObject("User decoration"); SceneManager.MoveGameObjectToScene(added, scene);
            try
            {
                added.transform.SetPositionAndRotation(new Vector3(18, 5, -9), Quaternion.Euler(12, 31, 7)); added.transform.localScale = new Vector3(2, 3, 4);
                Component[] All() => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
                var originals = All();
                var poses = originals.OfType<Transform>().ToDictionary(item => item, item => (item.position, item.rotation, item.localScale));
                var materials = originals.OfType<Renderer>().ToDictionary(item => item, item => item.sharedMaterials);
                var references = originals.OfType<MonoBehaviour>().ToDictionary(item => item, EditorJsonUtility.ToJson);
                M17HygieneBuilder.ConfigureScene(scene); M17HygieneBuilder.ConfigureScene(scene);
                Assert.That(All(), Is.EquivalentTo(originals));
                foreach (var pair in poses) Assert.That((pair.Key.position, pair.Key.rotation, pair.Key.localScale), Is.EqualTo(pair.Value));
                foreach (var pair in materials) Assert.That(pair.Key.sharedMaterials, Is.EqualTo(pair.Value));
                foreach (var pair in references) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value));
                var surfaces = All().OfType<CleanableSurface>().ToArray(); Assert.That(surfaces.Length, Is.EqualTo(7));
                foreach (var surface in surfaces) Assert.DoesNotThrow(surface.Validate);
                Assert.That(All().OfType<CleaningTool>().Count(), Is.EqualTo(1));
                Assert.That(All().OfType<GrillHeatSource>().Single().CleanableSurface, Is.SameAs(surfaces.Single(surface => surface.DisplayName == "Grill")));
                Assert.That(All().OfType<DirtSurfaceView>().Count(), Is.EqualTo(7));
                var stains = All().OfType<Renderer>().Where(renderer => renderer.name.StartsWith("Residue ")).ToArray();
                Assert.That(stains.Length, Is.EqualTo(42)); Assert.That(stains.All(stain => stain.GetComponent<Collider>() == null), Is.True);
                Assert.That(All().OfType<SurfaceFoodContact>().Count(), Is.EqualTo(7));
                var grillContact = surfaces.Single(surface => surface.DisplayName == "Grill").GetComponent<SurfaceFoodContact>();
                Assert.That(new SerializedObject(grillContact).FindProperty("_dirtPerContact").floatValue, Is.Zero);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
