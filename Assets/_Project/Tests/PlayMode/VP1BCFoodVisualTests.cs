#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Presentation;

namespace ZeroStarRestaurant.Tests
{
    public sealed class VP1BCFoodVisualTests
    {
        [Test]
        public void PattyAppearanceReadsCookingOnOriginalUnitWithoutChangingAnyState()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Food/RawBeefPatty.prefab");
            var unit = Object.Instantiate(prefab); unit.transform.position = new Vector3(500, 500, 500);
            try
            {
                var food = unit.GetComponent<FoodItem>(); Assert.That(food.TryInitialize(), Is.True); unit.SetActive(true);
                var state = food.State; var id = state.InstanceId; state.Contaminate();
                var adapter = unit.GetComponentInChildren<FoodStageVisual>(); var renderer = adapter.GetComponentsInChildren<Renderer>().Single();
                adapter.Refresh(); Assert.That(renderer.sharedMaterial.name, Is.EqualTo("RawPatty"));
                state.SetTemperature(120); state.Advance(46, new ThermalEnvironment(120, 1, true), 0);
                Assert.That(state.Cooking.Stage, Is.EqualTo(CookingStage.Cooked));
                adapter.Refresh(); Assert.That(renderer.sharedMaterial.name, Is.EqualTo("CookedPatty"));
                state.Advance(50, new ThermalEnvironment(120, 1, true), 0);
                adapter.Refresh(); Assert.That(renderer.sharedMaterial.name, Is.EqualTo("BurntPatty"));
                var dose = state.Cooking.EquivalentSeconds; var age = state.AgeSeconds; var temperature = state.TemperatureCelsius;
                for (int i = 0; i < 20; i++) adapter.Refresh();
                Assert.That(food.State, Is.SameAs(state)); Assert.That(state.InstanceId, Is.EqualTo(id));
                Assert.That(state.Cooking.EquivalentSeconds, Is.EqualTo(dose)); Assert.That(state.AgeSeconds, Is.EqualTo(age));
                Assert.That(state.TemperatureCelsius, Is.EqualTo(temperature)); Assert.That(state.IsContaminated, Is.True);
                Assert.That(state.FreshnessPercent, Is.EqualTo(100));
                Assert.That(unit.GetComponentsInChildren<Collider>().Length, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(unit); }
        }
    }
}
#endif
