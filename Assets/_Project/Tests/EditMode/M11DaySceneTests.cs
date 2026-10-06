using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Restaurant;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M11DaySceneTests
    {
        private Scene _scene;
        [SetUp] public void SetUp() => _scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
        [TearDown] public void TearDown() => EditorSceneManager.ClosePreviewScene(_scene);
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        [Test]
        public void DayQueueAndFeedbackHaveLocalExplicitReferencesWithoutChangingFoodDriver()
        {
            RestaurantDayController day = Components<RestaurantDayController>().Single();
            Assert.DoesNotThrow(day.Validate); Assert.That(day.Queue, Is.SameAs(Components<CustomerQueueController>().Single()));
            Assert.That(day.Queue.DayController, Is.SameAs(day));
            Assert.That(Components<RestaurantDayFeedback>(), Has.Length.EqualTo(1));
            Assert.That(new SerializedObject(Components<RestaurantDayFeedback>().Single()).FindProperty("_day").objectReferenceValue, Is.SameAs(day));
            Assert.That(Components<FoodSimulation>(), Has.Length.EqualTo(1));
            Assert.That(new SerializedObject(Components<FoodSimulation>().Single()).FindProperty("_developmentTimeMultiplier").floatValue, Is.EqualTo(1));
        }
        [Test]
        public void InstallingAgainPreservesConfigurationAndAllSceneObjects()
        {
            RestaurantDayController day = Components<RestaurantDayController>().Single();
            var data = new SerializedObject(day); data.FindProperty("_worldSecondsPerSimulationSecond").floatValue = 12; data.ApplyModifiedPropertiesWithoutUndo();
            Component[] original = Components<Component>(); M11RestaurantDayBuilder.ConfigureScene(_scene); M11RestaurantDayBuilder.ConfigureScene(_scene);
            Assert.That(Components<Component>(), Is.EquivalentTo(original));
            Assert.That(new SerializedObject(day).FindProperty("_worldSecondsPerSimulationSecond").floatValue, Is.EqualTo(12));
        }
        [TestCase("_openingHour", 24)] [TestCase("_closingMinute", 60)] [TestCase("_closingHour", 8)]
        [TestCase("_worldSecondsPerSimulationSecond", -1)]
        public void InvalidInspectorConfigurationIsRejected(string field, int value)
        {
            RestaurantDayController day = Components<RestaurantDayController>().Single(); var data = new SerializedObject(day);
            if (field == "_worldSecondsPerSimulationSecond") data.FindProperty(field).floatValue = value;
            else data.FindProperty(field).intValue = value;
            data.ApplyModifiedPropertiesWithoutUndo(); Assert.Throws<System.ArgumentException>(day.Validate);
        }
    }
}
