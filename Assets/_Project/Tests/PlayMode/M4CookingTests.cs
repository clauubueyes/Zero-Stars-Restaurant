using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M4CookingTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly Vector3 _origin = new Vector3(1000f, 0f, 1000f);
        private FoodDefinition _definition;
        private GrillHeatSource _grill;
        private BoxCollider _zone;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<FoodDefinition>();
            Set(_definition, "_isCookable", true);
            GameObject surface = Create("Grill", _origin - Vector3.up * 0.1f);
            surface.transform.localScale = new Vector3(2.5f, 0.2f, 1.5f);
            surface.AddComponent<BoxCollider>();
            _grill = surface.AddComponent<GrillHeatSource>();
            _zone = Create("ThermalZone", _origin + Vector3.up * 0.1f).AddComponent<BoxCollider>();
            _zone.size = new Vector3(2.4f, 0.22f, 1.4f); _zone.isTrigger = true;
            Set(_grill, "_effectiveZone", _zone);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject item in _objects) if (item != null) Object.DestroyImmediate(item);
            _objects.Clear();
            Object.DestroyImmediate(_definition);
        }

        private GameObject Create(string name, Vector3 position)
        {
            var item = new GameObject(name); item.transform.position = position;
            _objects.Add(item); return item;
        }

        private FoodItem Food(float freshness = 100f, bool contaminated = false, Vector3? position = null)
        {
            GameObject item = Create("Patty", position ?? (_origin + Vector3.up * 0.07f));
            item.SetActive(false);
            item.transform.localScale = new Vector3(0.4f, 0.12f, 0.4f);
            item.AddComponent<BoxCollider>();
            Rigidbody body = item.AddComponent<Rigidbody>(); body.mass = 0.15f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            item.AddComponent<Pickup>();
            FoodItem food = item.AddComponent<FoodItem>();
            Set(food, "_definition", _definition); Set(food, "_initialFreshnessPercent", freshness);
            Set(food, "_initiallyContaminated", contaminated);
            item.SetActive(true); Physics.SyncTransforms();
            return food;
        }

        private FoodSimulation Simulation(params FoodItem[] foods)
        {
            FoodSimulation driver = Create("FoodClock", _origin).AddComponent<FoodSimulation>();
            driver.enabled = false; Set(driver, "_foods", foods); Set(driver, "_heatSources", new HeatSource[] { _grill });
            return driver;
        }

        [Test]
        public void LeavingZoneStopsHeatAndCookingImmediatelyAndReplacementResumesSameState()
        {
            FoodItem food = Food(); FoodState state = food.State;
            FoodSimulation driver = Simulation(food);
            driver.Advance(30.0);
            double hot = state.TemperatureCelsius, dose = state.Cooking.EquivalentSeconds;
            Assert.That(dose, Is.GreaterThan(0.0));
            food.GetComponent<Rigidbody>().position = _origin + Vector3.right * 3f;
            driver.Advance(10.0);
            Assert.That(state.TemperatureCelsius, Is.LessThan(hot).And.GreaterThan(21.0));
            Assert.That(state.Cooking.EquivalentSeconds, Is.EqualTo(dose));
            food.GetComponent<Rigidbody>().position = _origin + Vector3.up * 0.07f;
            driver.Advance(10.0);
            Assert.That(food.State, Is.SameAs(state));
            Assert.That(state.Cooking.EquivalentSeconds, Is.GreaterThan(dose));
            Assert.That(state.AgeSeconds, Is.EqualTo(50.0));
        }

        [Test]
        public void MultipleFoodsAndDuplicateCollidersOrReferencesAdvanceOnlyOnce()
        {
            FoodItem fresh = Food(position: _origin + new Vector3(-0.4f, 0.07f, 0f));
            FoodItem rotten = Food(0f, true, _origin + new Vector3(0.4f, 0.07f, 0f));
            GameObject child = Create("ExtraFoodCollider", fresh.transform.position);
            child.transform.SetParent(fresh.transform, true); child.AddComponent<BoxCollider>().size = Vector3.one * 0.1f;
            FoodSimulation driver = Simulation(fresh, rotten, fresh);
            Set(driver, "_heatSources", new HeatSource[] { _grill, _grill });
            string definitionBefore = JsonUtility.ToJson(_definition);
            driver.Advance(45.0);
            Assert.That(fresh.State.AgeSeconds, Is.EqualTo(45.0));
            Assert.That(fresh.State.Cooking.Stage, Is.EqualTo(CookingStage.Cooked));
            Assert.That(rotten.State.Cooking.EquivalentSeconds, Is.EqualTo(fresh.State.Cooking.EquivalentSeconds));
            Assert.That(rotten.State.Condition, Is.EqualTo(FoodCondition.Rotten));
            Assert.That(rotten.State.IsContaminated, Is.True);
            Assert.That(fresh.State.Condition, Is.EqualTo(FoodCondition.Fresh));
            Assert.That(JsonUtility.ToJson(_definition), Is.EqualTo(definitionBefore));
        }

        [TestCase(0f, 180f)]
        [TestCase(4f, 21f)]
        public void ZeroPowerOrColdGrillCannotCook(float power, float temperature)
        {
            FoodItem food = Food(); FoodSimulation driver = Simulation(food);
            Set(_grill, "_transferMultiplier", power); Set(_grill, "_temperatureCelsius", temperature);
            driver.Advance(1000.0);
            Assert.That(food.State.Cooking.Stage, Is.EqualTo(CookingStage.Raw));
            Assert.That(food.State.TemperatureCelsius, Is.EqualTo(21.0).Within(1e-9));
        }

        [Test]
        public void NonCookableFoodCanWarmOnTheGrillButCannotCook()
        {
            Set(_definition, "_isCookable", false);
            FoodItem food = Food(); Simulation(food).Advance(45.0);
            Assert.That(food.State.TemperatureCelsius, Is.GreaterThan(150.0));
            Assert.That(food.State.Cooking, Is.Null);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void DisabledOrDestroyedFoodCannotRetainAHeatingRegistration(int mode)
        {
            FoodItem food = Food(), survivor = Food(position: _origin + new Vector3(0.5f, 0.07f, 0f));
            FoodState state = food.State; FoodSimulation driver = Simulation(food, survivor);
            driver.Advance(10.0); double dose = state.Cooking.EquivalentSeconds;
            if (mode == 0) food.enabled = false;
            else if (mode == 1) food.gameObject.SetActive(false);
            else if (mode == 2) Object.DestroyImmediate(food.gameObject);
            else food.GetComponent<Collider>().enabled = false;
            Assert.DoesNotThrow(() => driver.Advance(10.0));
            Assert.That(state.Cooking.EquivalentSeconds, Is.EqualTo(dose));
            Assert.That(survivor.State.AgeSeconds, Is.EqualTo(20.0));
            if (mode == 2) return;
            Assert.That(state.AgeSeconds, Is.EqualTo(20.0)); // Existing M3 inactive aging policy.
            food.enabled = true; food.gameObject.SetActive(true); driver.Advance(10.0);
            if (mode == 3)
            {
                food.GetComponent<Collider>().enabled = true;
                driver.Advance(10.0);
            }
            Assert.That(food.State, Is.SameAs(state));
            Assert.That(state.Cooking.EquivalentSeconds, Is.GreaterThan(dose));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void DisabledDestroyedGrillOrZoneStopsHeatWithoutStaleEntries(int mode)
        {
            FoodItem food = Food(); FoodSimulation driver = Simulation(food);
            driver.Advance(30.0); double hot = food.State.TemperatureCelsius, dose = food.State.Cooking.EquivalentSeconds;
            if (mode == 0) _grill.enabled = false;
            else if (mode == 1) Object.DestroyImmediate(_grill.gameObject);
            else if (mode == 2) _zone.enabled = false;
            else Object.DestroyImmediate(_zone.gameObject);
            Assert.DoesNotThrow(() => driver.Advance(10.0));
            Assert.That(food.State.TemperatureCelsius, Is.LessThan(hot));
            Assert.That(food.State.Cooking.EquivalentSeconds, Is.EqualTo(dose));
        }

        [TestCase(0f, 0.5f, 0f)]
        [TestCase(3f, 0.07f, 0f)]
        [TestCase(0f, -0.5f, 0f)]
        public void FoodAboveBesideOrBelowEffectiveZoneDoesNotHeat(float x, float y, float z)
        {
            FoodItem food = Food(position: _origin + new Vector3(x, y, z));
            Simulation(food).Advance(60.0);
            Assert.That(food.State.Cooking.Stage, Is.EqualTo(CookingStage.Raw));
            Assert.That(food.State.TemperatureCelsius, Is.EqualTo(21.0));
        }

        [Test]
        public void RotatedEffectiveZoneUsesItsActualOrientedVolume()
        {
            FoodItem food = Food(position: _origin + new Vector3(0f, 0.07f, 0.95f));
            FoodSimulation driver = Simulation(food);
            driver.Advance(10.0);
            Assert.That(food.State.TemperatureCelsius, Is.EqualTo(21.0));
            _zone.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            driver.Advance(10.0);
            Assert.That(food.State.TemperatureCelsius, Is.GreaterThan(60.0));
            Assert.That(food.State.Cooking.EquivalentSeconds, Is.GreaterThan(0.0));
        }

        [Test]
        public void CompetingSourcesResolveOneEnvironmentUsingStrongestCoupling()
        {
            FoodItem food = Food(); FoodSimulation driver = Simulation(food);
            GrillHeatSource cold = Create("ColdTestSource", _origin).AddComponent<GrillHeatSource>();
            Set(cold, "_effectiveZone", _zone); Set(cold, "_temperatureCelsius", 21f);
            Set(cold, "_transferMultiplier", 0.5f);
            Set(driver, "_heatSources", new HeatSource[] { cold, _grill });
            driver.Advance(45.0);
            Assert.That(food.State.AgeSeconds, Is.EqualTo(45.0));
            Assert.That(food.State.Cooking.Stage, Is.EqualTo(CookingStage.Cooked));
        }

        [UnityTest]
        public IEnumerator GenericM2PickupDropLandsOnGrillCooksAndCanBePickedUpAgain()
        {
            FoodItem food = Food(position: _origin + Vector3.up);
            FoodState state = food.State; FoodSimulation driver = Simulation(food);
            GameObject actor = Create("CookingTestPlayer", _origin + Vector3.back * 1.4f);
            CharacterController capsule = actor.AddComponent<CharacterController>();
            capsule.height = 1.8f; capsule.radius = 0.3f; capsule.center = Vector3.up * 0.9f;
            Transform view = Create("CookingTestCamera", actor.transform.position + Vector3.up * 1.65f).transform;
            view.SetParent(actor.transform, true); view.LookAt(food.transform.position);
            PhysicalCarry carry = actor.AddComponent<PhysicalCarry>();
            Set(carry, "_actorRoot", actor.transform); Set(carry, "_viewTransform", view);
            var context = new InteractionContext(actor.transform, carry);
            Physics.SyncTransforms();
            Assert.That(food.GetComponent<Pickup>().TryInteract(context), Is.True);
            for (int frame = 0; frame < 20; frame++) yield return new WaitForFixedUpdate();
            Assert.That(carry.HasHeldObject, Is.True);
            carry.Drop();
            for (int frame = 0; frame < 60; frame++) yield return new WaitForFixedUpdate();
            Assert.That(food.transform.position.y, Is.InRange(0.03f, 0.1f));
            Assert.That(_grill.TryGetEnvironment(food, out _), Is.True);
            driver.Advance(45.0);
            Assert.That(state.Cooking.Stage, Is.EqualTo(CookingStage.Cooked));
            Assert.That(food.GetComponent<Pickup>().TryInteract(context), Is.True);
            Quaternion previousLook = view.rotation;
            // Respect M2's existing maximum target-error release: lift gradually instead
            // of snapping a floor-height object directly to a distant horizontal target.
            for (int frame = 0; frame < 30; frame++)
            {
                view.rotation = Quaternion.Slerp(previousLook, Quaternion.identity, (frame + 1f) / 30f);
                yield return new WaitForFixedUpdate();
                Assert.That(carry.HasHeldObject, Is.True);
            }
            for (int frame = 0; frame < 40; frame++) yield return new WaitForFixedUpdate();
            double dose = state.Cooking.EquivalentSeconds, hot = state.TemperatureCelsius;
            driver.Advance(10.0);
            Assert.That(state.Cooking.EquivalentSeconds, Is.EqualTo(dose));
            Assert.That(state.TemperatureCelsius, Is.LessThan(hot));
            Assert.That(food.State, Is.SameAs(state));
            Assert.That(food.GetComponent<Rigidbody>().isKinematic, Is.False);
            Assert.That(carry.HeldBody, Is.EqualTo(food.GetComponent<Rigidbody>()));
            carry.Drop();
        }

        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
