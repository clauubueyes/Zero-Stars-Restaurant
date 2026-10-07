using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M12ElectricitySceneTests
    {
        private Scene _scene;
        [SetUp] public void SetUp() => _scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
        [TearDown] public void TearDown() => EditorSceneManager.ClosePreviewScene(_scene);
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        [Test]
        public void EveryThermalApplianceRequiresTheSingleLocalSupplyWithExplicitMeterAndFeedback()
        {
            RestaurantElectricity supply = Components<RestaurantElectricity>().Single(); Assert.DoesNotThrow(supply.Validate);
            Assert.That(new SerializedObject(supply).FindProperty("_initiallyOn").boolValue, Is.False);
            HeatSource[] sources = Components<HeatSource>(); Assert.That(sources, Has.Length.EqualTo(3));
            Assert.That(supply.Appliances.Count, Is.EqualTo(3));
            foreach (HeatSource source in sources)
            {
                Assert.That(source.RequiresElectricity, Is.True); Assert.That(source.Electricity, Is.Not.Null);
                Assert.That(source.Electricity.Supply, Is.SameAs(supply));
                Assert.That(source.Electricity.ThermalSource, Is.SameAs(source));
                Assert.That(supply.Appliances, Does.Contain(source.Electricity));
                Assert.That(source.IsOperational, Is.False);
                Assert.That(source.Electricity.RatedWatts, Is.EqualTo(source is GrillHeatSource ? 2000 : source.name == "Fridge" ? 150 : 200));
            }
            ElectricitySwitch button = Components<ElectricitySwitch>().Single(); Assert.That(button.Supply, Is.SameAs(supply));
            Assert.That(button.GetComponent<Collider>().isTrigger, Is.False);
            Assert.That(Components<ElectricityFeedback>(), Has.Length.EqualTo(1));
            Assert.That(new SerializedObject(Components<ElectricityFeedback>().Single()).FindProperty("_supply").objectReferenceValue, Is.SameAs(supply));
            Assert.That(Components<FoodSimulation>(), Has.Length.EqualTo(1));
        }

        [Test]
        public void ReinstallationPreservesAllComponentsConfigurationAndExistingTransforms()
        {
            RestaurantElectricity supply = Components<RestaurantElectricity>().Single();
            var data = new SerializedObject(supply); data.FindProperty("_initiallyOn").boolValue = true; data.ApplyModifiedPropertiesWithoutUndo();
            var appliance = new SerializedObject(supply.Appliances[0]); appliance.FindProperty("_ratedWatts").floatValue = 3456; appliance.ApplyModifiedPropertiesWithoutUndo();
            Component[] original = Components<Component>();
            Vector3[] positions = Components<Transform>().Select(item => item.position).ToArray();
            M12ElectricityBuilder.ConfigureScene(_scene); M12ElectricityBuilder.ConfigureScene(_scene);
            Assert.That(Components<Component>(), Is.EquivalentTo(original));
            Assert.That(Components<Transform>().Select(item => item.position), Is.EqualTo(positions));
            Assert.That(supply.Appliances[0].RatedWatts, Is.EqualTo(3456));
            Assert.That(new SerializedObject(supply).FindProperty("_initiallyOn").boolValue, Is.True);
        }

        [Test]
        public void BrokenSupplyReferenceInvalidPowerAndMissingRequiredDependencyFailClosed()
        {
            RestaurantElectricity supply = Components<RestaurantElectricity>().Single(); ElectricalAppliance appliance = supply.Appliances[0];
            var data = new SerializedObject(appliance); data.FindProperty("_ratedWatts").floatValue = -1; data.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<System.ArgumentException>(supply.Validate);
            data.FindProperty("_ratedWatts").floatValue = 2000; data.FindProperty("_supply").objectReferenceValue = null; data.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<System.ArgumentException>(supply.Validate);
            data = new SerializedObject(appliance.ThermalSource); data.FindProperty("_electricity").objectReferenceValue = null; data.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(appliance.ThermalSource.IsOperational, Is.False);
        }
    }
}
