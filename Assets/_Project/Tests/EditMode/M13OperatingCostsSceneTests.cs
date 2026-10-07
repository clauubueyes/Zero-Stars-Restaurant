using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Restaurant;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M13OperatingCostsSceneTests
    {
        private Scene _scene;
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        [SetUp] public void SetUp() => _scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
        [TearDown] public void TearDown() => EditorSceneManager.ClosePreviewScene(_scene);
        [Test]
        public void CostsUseTheExistingDayLedgerAndSupplyWithOnlyRentConfigured()
        {
            RestaurantOperatingCosts costs = Components<RestaurantOperatingCosts>().Single();
            Assert.DoesNotThrow(costs.Validate); Assert.DoesNotThrow(costs.Day.Validate);
            Assert.That(costs.Service, Is.SameAs(Components<CustomerServiceLoop>().Single()));
            Assert.That(costs.Electricity, Is.SameAs(Components<RestaurantElectricity>().Single()));
            Assert.That(costs.Day, Is.SameAs(Components<RestaurantDayController>().Single()));
            Assert.That(costs.Day.OperatingCosts, Is.SameAs(costs));
            Assert.That(new SerializedObject(costs.Day).FindProperty("_requiresOperatingCosts").boolValue, Is.True);
            Assert.That(new SerializedObject(Components<OperatingCostsFeedback>().Single()).FindProperty("_costs").objectReferenceValue, Is.SameAs(costs));
            OperatingCostPolicy policy = AssetDatabase.LoadAssetAtPath<OperatingCostSettings>(M13OperatingCostsBuilder.SettingsPath).CreatePolicy();
            Assert.That(policy.ElectricityCentsPerKilowattHour, Is.EqualTo(30));
            Assert.That(policy.FixedCosts, Has.Count.EqualTo(1)); Assert.That(policy.FixedCosts[0].Id, Is.EqualTo("rent"));
            Assert.That(policy.FixedCosts[0].AmountCents, Is.EqualTo(300));
        }
        [Test]
        public void ReinstallingPreservesComponentsTransformsAndCustomSettings()
        {
            RestaurantOperatingCosts costs = Components<RestaurantOperatingCosts>().Single();
            OperatingCostSettings custom = Object.Instantiate(AssetDatabase.LoadAssetAtPath<OperatingCostSettings>(M13OperatingCostsBuilder.SettingsPath));
            try
            {
                var data = new SerializedObject(custom); data.FindProperty("_electricityCentsPerKilowattHour").intValue = 85; data.ApplyModifiedPropertiesWithoutUndo();
                data = new SerializedObject(costs); data.FindProperty("_settings").objectReferenceValue = custom; data.ApplyModifiedPropertiesWithoutUndo();
                Component[] original = Components<Component>(); Vector3[] positions = Components<Transform>().Select(item => item.position).ToArray();
                M13OperatingCostsBuilder.ConfigureScene(_scene); M13OperatingCostsBuilder.ConfigureScene(_scene);
                Assert.That(Components<Component>(), Is.EquivalentTo(original));
                Assert.That(Components<Transform>().Select(item => item.position), Is.EqualTo(positions));
                Assert.That(new SerializedObject(costs).FindProperty("_settings").objectReferenceValue, Is.SameAs(custom));
                Assert.That(custom.CreatePolicy().ElectricityCentsPerKilowattHour, Is.EqualTo(85));
            }
            finally { Object.DestroyImmediate(custom); }
        }
        [Test]
        public void M12UpgradeAddsOnlyAccountingComponentsAndReferences()
        {
            RestaurantDayController day = Components<RestaurantDayController>().Single();
            Object.DestroyImmediate(Components<OperatingCostsFeedback>().Single()); Object.DestroyImmediate(day.OperatingCosts);
            var data = new SerializedObject(day); data.FindProperty("_operatingCosts").objectReferenceValue = null;
            data.FindProperty("_requiresOperatingCosts").boolValue = false; data.ApplyModifiedPropertiesWithoutUndo();
            Transform[] original = Components<Transform>(); Vector3[] positions = original.Select(item => item.position).ToArray();
            M13OperatingCostsBuilder.ConfigureScene(_scene); M13OperatingCostsBuilder.ConfigureScene(_scene);
            Assert.That(Components<Transform>(), Is.EquivalentTo(original)); Assert.That(original.Select(item => item.position), Is.EqualTo(positions));
            Assert.That(Components<RestaurantOperatingCosts>(), Has.Length.EqualTo(1));
            Assert.That(Components<OperatingCostsFeedback>(), Has.Length.EqualTo(1)); Assert.DoesNotThrow(day.Validate);
        }
        [Test]
        public void MissingRequiredAccountingReferenceCannotBypassSettlement()
        {
            var data = new SerializedObject(Components<RestaurantDayController>().Single());
            data.FindProperty("_operatingCosts").objectReferenceValue = null; data.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<System.ArgumentException>(Components<RestaurantDayController>().Single().Validate);
        }
    }
}
