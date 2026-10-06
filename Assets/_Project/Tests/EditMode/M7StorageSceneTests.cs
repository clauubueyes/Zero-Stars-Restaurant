using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M7StorageSceneTests
    {
        private Scene _scene;
        [SetUp] public void SetUp() => _scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
        [TearDown] public void TearDown() => EditorSceneManager.ClosePreviewScene(_scene);
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        [Test]
        public void TwoOpenCabinetsAreExplicitThermalSourcesForTheSingleExistingFoodClock()
        {
            ColdStorage[] storage = Components<ColdStorage>(); Assert.That(storage.Select(item => item.name), Is.EquivalentTo(new[] { "Fridge", "Freezer" }));
            var simulation = new SerializedObject(Components<FoodSimulation>().Single());
            SerializedProperty sources = simulation.FindProperty("_heatSources");
            Assert.That(sources.arraySize, Is.EqualTo(3));
            Assert.That(Enumerable.Range(0, sources.arraySize).Select(index => sources.GetArrayElementAtIndex(index).objectReferenceValue),
                Is.EquivalentTo(Components<HeatSource>()));
            var settings = (FoodPreservationSettings)simulation.FindProperty("_preservationSettings").objectReferenceValue;
            Assert.That(settings, Is.Not.Null); FoodPreservationProfile profile = settings.CreateProfile();
            Assert.That(profile.RateAt(21), Is.EqualTo(1)); Assert.That(profile.RateAt(4), Is.EqualTo(0.1).Within(0.0001));
            Assert.That(profile.RateAt(-18), Is.EqualTo(0.001).Within(0.0001));
            Assert.That(Components<FoodItem>(), Has.Length.EqualTo(18));
            Assert.That(simulation.FindProperty("_foods").arraySize, Is.EqualTo(18));
        }

        [TestCase("Fridge", 4f)]
        [TestCase("Freezer", -18f)]
        public void CabinetInteriorMatchesASolidShelfClearOpeningAndReadableDevelopmentLabel(string name, float temperature)
        {
            ColdStorage storage = Components<ColdStorage>().Single(item => item.name == name);
            var data = new SerializedObject(storage); var interior = (BoxCollider)data.FindProperty("_interior").objectReferenceValue;
            Assert.That(data.FindProperty("_temperatureCelsius").floatValue, Is.EqualTo(temperature));
            Assert.That(data.FindProperty("_transferMultiplier").floatValue, Is.EqualTo(2)); Assert.That(interior.isTrigger, Is.True);
            Collider[] solids = storage.GetComponentsInChildren<Collider>().Where(collider => !collider.isTrigger).ToArray();
            Assert.That(solids, Has.Length.EqualTo(6)); Assert.That(solids.Any(collider => collider.name.Contains("Door") || collider.name.Contains("Front")), Is.False);
            Collider shelf = solids.Single(collider => collider.name == "Shelf");
            Assert.That(interior.bounds.min.y, Is.EqualTo(shelf.bounds.max.y).Within(0.001));
            Vector3 front = storage.transform.TransformPoint(new Vector3(0, 1.65f, -0.5f));
            Assert.That(front.x, Is.GreaterThan(storage.transform.position.x)); Assert.That(interior.bounds.Contains(front), Is.True);
            TextMesh label = storage.GetComponentInChildren<TextMesh>();
            Assert.That(label.text, Does.Contain(name)); Assert.That(label.font, Is.Not.Null);
            Assert.That(storage.GetComponent<Rigidbody>(), Is.Null);
        }
    }
}
