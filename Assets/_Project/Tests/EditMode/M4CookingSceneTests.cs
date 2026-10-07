using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M4CookingSceneTests
    {
        private Scene _scene;
        [SetUp] public void SetUp() => _scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
        [TearDown] public void TearDown() => EditorSceneManager.ClosePreviewScene(_scene);
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        [Test]
        public void GrillHasSolidSurfaceThinTriggerAndSingleExplicitDriver()
        {
            GrillHeatSource grill = Components<GrillHeatSource>().Single();
            Collider surface = grill.GetComponentsInChildren<Collider>().Single(c => c.name == "GrillHotSurface");
            Assert.That(surface.isTrigger, Is.False);
            var config = new SerializedObject(grill);
            var zone = (BoxCollider)config.FindProperty("_effectiveZone").objectReferenceValue;
            Assert.That(zone.isTrigger, Is.True);
            Assert.That(zone.size.y, Is.InRange(0.1f, 0.25f));
            Assert.That(zone.bounds.size.x, Is.GreaterThan(2f));
            Assert.That(zone.bounds.min.y, Is.EqualTo(surface.bounds.max.y).Within(0.02f));
            Assert.That(zone.transform.IsChildOf(grill.transform), Is.True);
            Assert.That(config.FindProperty("_temperatureCelsius").floatValue, Is.EqualTo(180f));
            Assert.That(config.FindProperty("_transferMultiplier").floatValue, Is.EqualTo(4f));
            var driver = new SerializedObject(Components<FoodSimulation>().Single());
            Assert.That(driver.FindProperty("_heatSources").arraySize, Is.EqualTo(Components<HeatSource>().Length));
            Assert.That(driver.FindProperty("_heatSources").GetArrayElementAtIndex(0).objectReferenceValue, Is.EqualTo(grill));
            Assert.That(driver.FindProperty("_foods").arraySize, Is.EqualTo(Components<FoodItem>().Length));
        }

        [Test]
        public void OnlyBeefIsCookableAndCookingFixturesCoverColdRottenAndContaminatedUnits()
        {
            FoodItem[] foods = Components<FoodItem>();
            // Preserve M3/M4's eight fixtures while M5 adds assembly supplies.
            FoodItem[] baseline = _scene.GetRootGameObjects().Where(root => root.name == "FoodTestZone" || root.name == "CookingTestZone")
                .SelectMany(root => root.GetComponentsInChildren<FoodItem>(true)).ToArray();
            Assert.That(baseline, Has.Length.EqualTo(8));
            foreach (FoodItem food in foods)
                Assert.That(food.Definition.CreateProfile().IsCookable, Is.EqualTo(food.Definition.Id == "food.raw_beef_patty"));
            FoodItem[] fixtures = _scene.GetRootGameObjects().Single(root => root.name == "CookingTestZone")
                .GetComponentsInChildren<FoodItem>(true);
            Assert.That(fixtures, Has.Length.EqualTo(4));
            Assert.That(fixtures.Select(f => f.Definition).Distinct().Count(), Is.EqualTo(1));
            Assert.That(fixtures.Select(f => new SerializedObject(f).FindProperty("_initialTemperatureCelsius").floatValue),
                Does.Contain(-18f));
            Assert.That(fixtures.Select(f => new SerializedObject(f).FindProperty("_initialFreshnessPercent").floatValue),
                Does.Contain(0f));
            Assert.That(fixtures.Count(f => new SerializedObject(f).FindProperty("_initiallyContaminated").boolValue), Is.EqualTo(1));
            foreach (FoodItem food in fixtures)
            {
                Assert.That(food.GetComponent<Pickup>(), Is.Not.Null);
                Assert.That(food.GetComponent<Rigidbody>().isKinematic, Is.False);
                Collider prep = Components<Collider>().Single(collider => collider.name == "CookingPrepBench");
                Assert.That(prep.bounds.Contains(new Vector3(food.transform.position.x, prep.bounds.center.y, food.transform.position.z)), Is.True);
            }
        }
    }
}
