using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using Object = UnityEngine.Object;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M7StorageTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly Vector3 _origin = new Vector3(3000, 0, 3000);
        private FoodDefinition _definition;
        private FoodPreservationSettings _preservation;
        private ColdStorage _fridge, _freezer;
        private FoodSimulation _simulation;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<FoodDefinition>(); Set(_definition, "_isCookable", true);
            _preservation = ScriptableObject.CreateInstance<FoodPreservationSettings>();
            _fridge = Storage("Fridge", _origin, 4); _freezer = Storage("Freezer", _origin + Vector3.right * 3, -18);
            _simulation = Create("FoodClock", _origin).AddComponent<FoodSimulation>(); _simulation.enabled = false;
            Set(_simulation, "_preservationSettings", _preservation);
            Set(_simulation, "_heatSources", new HeatSource[] { _fridge, _freezer });
        }
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject item in _objects) if (item != null) Object.DestroyImmediate(item);
            _objects.Clear(); Object.DestroyImmediate(_definition); Object.DestroyImmediate(_preservation);
        }
        private GameObject Create(string name, Vector3 position)
        { var item = new GameObject(name); item.transform.position = position; _objects.Add(item); return item; }
        private ColdStorage Storage(string name, Vector3 position, float temperature)
        {
            GameObject item = Create(name, position); item.SetActive(false);
            ColdStorage storage = item.AddComponent<ColdStorage>();
            BoxCollider zone = Create("Interior", position).AddComponent<BoxCollider>(); zone.transform.SetParent(item.transform, true);
            zone.isTrigger = true; zone.center = new Vector3(0, 1.575f, -0.025f); zone.size = new Vector3(1.4f, 1.35f, 1.25f);
            Set(storage, "_interior", zone); Set(storage, "_temperatureCelsius", temperature); item.SetActive(true); return storage;
        }
        private FoodItem Food(Vector3 position, float initialTemperature = 21)
        {
            GameObject item = Create("Food", position); item.SetActive(false); item.transform.localScale = new Vector3(0.4f, 0.12f, 0.4f);
            item.AddComponent<BoxCollider>(); item.AddComponent<Rigidbody>().useGravity = false; item.AddComponent<Pickup>();
            FoodItem food = item.AddComponent<FoodItem>(); Set(food, "_definition", _definition);
            Set(food, "_initialTemperatureCelsius", initialTemperature); item.SetActive(true); return food;
        }

        [Test]
        public void ThreePhysicalEnvironmentsCoolAtDifferentRatesWithoutDuplicateAging()
        {
            FoodItem ambient = Food(_origin + Vector3.left * 3 + Vector3.up);
            FoodItem chilled = Food(_origin + Vector3.up); FoodItem frozen = Food(_origin + new Vector3(3, 1, 0));
            Set(_simulation, "_foods", new[] { ambient, chilled, frozen, chilled });
            string settings = JsonUtility.ToJson(_preservation), definition = JsonUtility.ToJson(_definition);
            _simulation.Advance(10);
            Assert.That(chilled.State.TemperatureCelsius, Is.GreaterThan(4).And.LessThan(21));
            Assert.That(frozen.State.TemperatureCelsius, Is.GreaterThan(0).And.LessThan(chilled.State.TemperatureCelsius));
            _simulation.Advance(290);
            Assert.That(ambient.State.TemperatureCelsius, Is.EqualTo(21));
            Assert.That(frozen.State.TemperatureCelsius, Is.GreaterThan(-18).And.LessThan(-17.99));
            Assert.That(chilled.State.FreshnessPercent, Is.GreaterThan(ambient.State.FreshnessPercent));
            Assert.That(frozen.State.FreshnessPercent, Is.GreaterThan(chilled.State.FreshnessPercent));
            double before = frozen.State.FreshnessPercent; _simulation.Advance(600);
            Assert.That(before - frozen.State.FreshnessPercent, Is.EqualTo(0.1).Within(0.0001));
            foreach (FoodItem food in new[] { ambient, chilled, frozen }) Assert.That(food.State.AgeSeconds, Is.EqualTo(900));
            Assert.That(frozen.State.Cooking.Stage, Is.EqualTo(CookingStage.Raw));
            Assert.That(JsonUtility.ToJson(_preservation), Is.EqualTo(settings)); Assert.That(JsonUtility.ToJson(_definition), Is.EqualTo(definition));
        }

        [Test]
        public void MovingAmbientFridgeFreezerAmbientKeepsTheSameFoodStateAndWarmsGradually()
        {
            FoodItem food = Food(_origin + Vector3.left * 3 + Vector3.up); FoodState state = food.State;
            Guid id = state.InstanceId; state.Contaminate(); Set(_simulation, "_foods", new[] { food });
            _simulation.Advance(10); Assert.That(state.DeteriorationSeconds, Is.EqualTo(10));
            food.transform.position = _origin + Vector3.up; _simulation.Advance(120);
            Assert.That(state.TemperatureCelsius, Is.GreaterThan(4).And.LessThan(5));
            food.transform.position = _origin + new Vector3(3, 1, 0); _simulation.Advance(120);
            Assert.That(state.TemperatureCelsius, Is.LessThan(-17)); double cold = state.TemperatureCelsius;
            food.transform.position = _origin + Vector3.left * 3 + Vector3.up;
            Assert.That(state.TemperatureCelsius, Is.EqualTo(cold)); _simulation.Advance(10);
            Assert.That(state.TemperatureCelsius, Is.GreaterThan(cold).And.LessThan(0));
            Assert.That(food.State, Is.SameAs(state)); Assert.That(state.InstanceId, Is.EqualTo(id));
            Assert.That(state.AgeSeconds, Is.EqualTo(260)); Assert.That(state.IsContaminated, Is.True);
            Assert.That(state.Cooking.Stage, Is.EqualTo(CookingStage.Raw));
        }

        [TestCase(0.85f, 1f, 0f)]
        [TestCase(0f, 1f, -0.85f)]
        [TestCase(0f, 1f, 0.8f)]
        [TestCase(0f, 2.5f, 0f)]
        [TestCase(0f, 0.7f, 0f)]
        public void FoodBesideBehindAboveBelowOrJustOutsideTheMouthStaysAtAmbient(float x, float y, float z)
        {
            FoodItem food = Food(_origin + new Vector3(x, y, z)); Set(_simulation, "_foods", new[] { food });
            _simulation.Advance(60); Assert.That(food.State.TemperatureCelsius, Is.EqualTo(21));
            Assert.That(food.State.FreshnessPercent, Is.EqualTo(90).Within(0.0001));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void DisablingTransferSourceOrInteriorFallsBackToAmbientWithoutStaleRegistrations(int mode)
        {
            FoodItem food = Food(_origin + Vector3.up); Set(_simulation, "_foods", new[] { food });
            _simulation.Advance(120); double cold = food.State.TemperatureCelsius;
            if (mode == 0) _fridge.enabled = false;
            else if (mode == 1) _fridge.gameObject.SetActive(false);
            else if (mode == 2) _fridge.GetComponentInChildren<BoxCollider>().enabled = false;
            else if (mode == 3) _fridge.GetComponentInChildren<BoxCollider>().gameObject.SetActive(false);
            else if (mode == 4) Set(_fridge, "_transferMultiplier", 0f);
            else Object.DestroyImmediate(_fridge.gameObject);
            _simulation.Advance(1); Assert.That(food.State.TemperatureCelsius, Is.GreaterThan(cold).And.LessThan(21));
            Assert.That(food.State.AgeSeconds, Is.EqualTo(121)); Assert.That(food.State.Cooking.Stage, Is.EqualTo(CookingStage.Raw));
        }

        [Test]
        public void RotatedScaledInteriorUsesCurrentLocalFoodPositionsIncludingDishChildren()
        {
            _fridge.transform.rotation = Quaternion.Euler(0, 70, 0); _fridge.transform.localScale = new Vector3(-1.5f, 1f, 1.2f);
            var aggregate = Create("DishAggregate", _fridge.transform.TransformPoint(new Vector3(0, 1.1f, 0)));
            FoodItem food = Food(aggregate.transform.position); food.transform.SetParent(aggregate.transform, true);
            food.GetComponent<Collider>().enabled = false; // M5 finalization retires individual colliders, not FoodState.
            Set(_simulation, "_foods", new[] { food }); _simulation.Advance(120);
            Assert.That(food.State.TemperatureCelsius, Is.LessThan(5));
            aggregate.transform.position = _origin + Vector3.left * 5 + Vector3.up;
            double cold = food.State.TemperatureCelsius; _simulation.Advance(1);
            Assert.That(food.State.TemperatureCelsius, Is.GreaterThan(cold));
        }

        [Test]
        public void OverlappingRepeatedSourcesChooseOneEnvironmentAndOneAdvance()
        {
            _freezer.transform.position = _fridge.transform.position;
            FoodItem food = Food(_origin + Vector3.up); Set(_simulation, "_foods", new[] { food, food });
            Set(_simulation, "_heatSources", new HeatSource[] { _fridge, _freezer, _freezer });
            _simulation.Advance(60); Assert.That(food.State.TemperatureCelsius, Is.EqualTo(4 + 17 * Math.Exp(-2)).Within(0.0001));
            Set(_freezer, "_transferMultiplier", 3f); _simulation.Advance(60);
            Assert.That(food.State.TemperatureCelsius, Is.LessThan(0)); Assert.That(food.State.AgeSeconds, Is.EqualTo(120));
        }

        [Test]
        public void InvalidPreservationSettingsRejectBeforeAdvancingAnyUnit()
        {
            FoodItem food = Food(_origin + Vector3.up); Set(_simulation, "_foods", new[] { food });
            Set(_preservation, "_frozenAtCelsius", 6f);
            Assert.Throws<ArgumentException>(() => _simulation.Advance(60));
            Assert.That(food.State.AgeSeconds, Is.Zero); Assert.That(food.State.TemperatureCelsius, Is.EqualTo(21));
        }

        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
