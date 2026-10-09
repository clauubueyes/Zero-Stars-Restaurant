using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Hygiene;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M18SceneTests
    {
        [Test]
        public void FoodSafetyInstallationIsCompleteAndPreservesManualEditsOnRepeatedInstall()
        {
            var scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
            try
            {
                Component[] All() => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
                var surfaces = All().OfType<CleanableSurface>().ToArray(); Assert.That(surfaces.Length, Is.EqualTo(7));
                Assert.That(surfaces.All(surface => surface.FoodSafetySettings != null), Is.True);
                Assert.That(surfaces.All(surface => surface.GetComponent<SurfaceFoodContact>() != null), Is.True);
                var prep = surfaces.First(); prep.Support.transform.position += new Vector3(.1f, .2f, .3f);
                var originals = All(); var poses = originals.OfType<Transform>().ToDictionary(item => item, item => (item.position, item.rotation, item.localScale));
                var serialized = originals.OfType<MonoBehaviour>().ToDictionary(item => item, EditorJsonUtility.ToJson);
                M18FoodSafetyBuilder.ConfigureScene(scene); M18FoodSafetyBuilder.ConfigureScene(scene);
                Assert.That(All(), Is.EquivalentTo(originals));
                foreach (var pair in poses) Assert.That((pair.Key.position, pair.Key.rotation, pair.Key.localScale), Is.EqualTo(pair.Value));
                foreach (var pair in serialized) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void MissingSafetyComponentsAreAddedIncrementallyWithoutChangingM17DirtSettings()
        {
            var scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
            try
            {
                Component[] All() => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
                var surfaces = All().OfType<CleanableSurface>().ToArray();
                foreach (var surface in surfaces)
                {
                    var data = new SerializedObject(surface); data.FindProperty("_foodSafetySettings").objectReferenceValue = null;
                    data.FindProperty("_initialDirt").floatValue = .37f; data.ApplyModifiedPropertiesWithoutUndo();
                }
                Object.DestroyImmediate(surfaces[0].GetComponent<SurfaceFoodContact>());
                var supportPoses = surfaces.ToDictionary(surface => surface, surface => surface.Support.transform.position);
                M18FoodSafetyBuilder.ConfigureScene(scene);
                foreach (var surface in surfaces)
                {
                    Assert.That(surface.FoodSafetySettings, Is.Not.Null);
                    Assert.That(surface.GetComponent<SurfaceFoodContact>(), Is.Not.Null);
                    Assert.That(surface.Support.transform.position, Is.EqualTo(supportPoses[surface]));
                    Assert.That(new SerializedObject(surface).FindProperty("_initialDirt").floatValue, Is.EqualTo(.37f));
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
