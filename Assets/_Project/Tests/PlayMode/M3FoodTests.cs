using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M3FoodTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private FoodDefinition _definition;
        private Transform _view;
        private PhysicalCarry _carry;
        private PlayerInteraction _interaction;
        private readonly Vector3 _origin = new Vector3(1000f, 1.65f, 1000f);

        [SetUp]
        public void SetUp()
        {
            GameObject actor = Create("FoodTestPlayer", _origin - Vector3.up * 1.65f);
            CharacterController capsule = actor.AddComponent<CharacterController>();
            capsule.height = 1.8f; capsule.radius = 0.3f; capsule.center = Vector3.up * 0.9f;
            _view = Create("FoodTestView", _origin).transform;
            _view.SetParent(actor.transform, true);
            _carry = actor.AddComponent<PhysicalCarry>();
            Set(_carry, "_viewTransform", _view); Set(_carry, "_actorRoot", actor.transform);
            InteractionDetector detector = actor.AddComponent<InteractionDetector>();
            Set(detector, "_origin", _view); Set(detector, "_actorRoot", actor.transform);
            _interaction = actor.AddComponent<PlayerInteraction>();
            Set(_interaction, "_carry", _carry); Set(_interaction, "_detector", detector);
            _definition = ScriptableObject.CreateInstance<FoodDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            _carry.Drop();
            foreach (GameObject item in _objects)
                if (item != null) Object.DestroyImmediate(item);
            _objects.Clear();
            Object.DestroyImmediate(_definition);
        }

        private GameObject Create(string name, Vector3 position)
        {
            var item = new GameObject(name);
            item.transform.position = position;
            _objects.Add(item);
            return item;
        }

        private FoodItem Food(float freshness = 100f, float mass = 0.15f, Vector3? position = null, Vector3? size = null)
        {
            GameObject item = Create("PhysicalFood", position ?? (_origin + Vector3.forward * 1.3f));
            item.SetActive(false);
            item.transform.localScale = size ?? Vector3.one * 0.4f;
            item.AddComponent<BoxCollider>();
            item.AddComponent<Rigidbody>().mass = mass;
            item.AddComponent<Pickup>();
            FoodItem food = item.AddComponent<FoodItem>();
            Set(food, "_definition", _definition); Set(food, "_initialFreshnessPercent", freshness);
            item.SetActive(true);
            Physics.SyncTransforms();
            return food;
        }

        private FoodSimulation Simulation(params FoodItem[] foods)
        {
            FoodSimulation simulation = Create("FoodTestSimulation", _origin).AddComponent<FoodSimulation>();
            Set(simulation, "_foods", foods);
            simulation.enabled = false; // Tests supply exact time; no automatic Update.
            return simulation;
        }

        [TestCase("food.raw_beef_patty", FoodCategory.Meat, 0.15f)]
        [TestCase("food.bun", FoodCategory.Bakery, 0.08f)]
        [TestCase("food.cheese", FoodCategory.Dairy, 0.025f)]
        public void FoodUsesGenericM2PickupWithoutReplacingState(string id, FoodCategory category, float mass)
        {
            Set(_definition, "_id", id); Set(_definition, "_category", category);
            string before = JsonUtility.ToJson(_definition);
            FoodItem food = Food(mass: mass);
            FoodState state = food.State;
            Pickup pickup = food.GetComponent<Pickup>();
            Assert.That(_interaction.TryInteract(), Is.True);
            Simulation(food).Advance(60.0);
            Assert.That(state.AgeSeconds, Is.EqualTo(60.0));
            Assert.That(state.FreshnessPercent, Is.EqualTo(90.0).Within(1e-9));
            _interaction.Drop();
            Assert.That(food.State, Is.SameAs(state));
            Assert.That(pickup.Body.useGravity, Is.True);
            Assert.That(_interaction.TryInteract(), Is.True);
            _interaction.Throw();
            Assert.That(food.State, Is.SameAs(state));
            Assert.That(pickup.Body.linearVelocity.magnitude, Is.LessThanOrEqualTo(12.001f));
            Assert.That(_interaction.TryInteract(), Is.True);
            Assert.That(_carry.HeldBody, Is.EqualTo(pickup.Body));
            Assert.That(JsonUtility.ToJson(_definition), Is.EqualTo(before));
        }

        [Test]
        public void UnitsOfTheSameDefinitionHaveIndependentState()
        {
            FoodItem fresh = Food();
            FoodItem aged = Food(6f, position: _origin + Vector3.right * 2f);
            Assert.That(fresh.Definition, Is.SameAs(aged.Definition));
            Assert.That(fresh.State, Is.Not.SameAs(aged.State));
            fresh.State.Advance(120.0, 35.0);
            fresh.State.Contaminate();
            Assert.That(aged.State.AgeSeconds, Is.Zero);
            Assert.That(aged.State.FreshnessPercent, Is.EqualTo(6.0).Within(1e-9));
            Assert.That(aged.State.TemperatureCelsius, Is.EqualTo(21.0));
            Assert.That(aged.State.IsContaminated, Is.False);
        }

        [Test]
        public void InitializedInactiveFoodKeepsAgingAndReactivationPreservesIdentity()
        {
            FoodItem food = Food();
            FoodState state = food.State;
            FoodSimulation simulation = Simulation(food);
            Assert.That(_interaction.TryInteract(), Is.True);
            food.gameObject.SetActive(false);
            Assert.That(_carry.HasHeldObject, Is.False);
            simulation.Advance(60.0);
            food.gameObject.SetActive(true);
            Assert.That(food.State, Is.SameAs(state));
            Assert.That(food.State.AgeSeconds, Is.EqualTo(60.0));
            Assert.That(_interaction.TryInteract(), Is.True);
        }

        [Test]
        public void DevelopmentAdvanceSkipsDestroyedFoodAndDoesNotChangeGlobalTime()
        {
            FoodItem destroyed = Food();
            FoodItem survivor = Food(position: _origin + Vector3.right * 2f);
            FoodSimulation simulation = Simulation(destroyed, survivor);
            float timeScale = Time.timeScale;
            Object.DestroyImmediate(destroyed.gameObject);
            Assert.DoesNotThrow(() => simulation.Advance(60.0));
            Assert.That(survivor.State.AgeSeconds, Is.EqualTo(60.0));
            Assert.That(Time.timeScale, Is.EqualTo(timeScale));
        }

        [UnityTest]
        public IEnumerator FlatFoodCanBeLiftedFromTheFloorWhileLookingUp()
        {
            GameObject floor = Create("FoodTestFloor", new Vector3(1000f, -0.25f, 1000f));
            floor.transform.localScale = new Vector3(10f, 0.5f, 10f);
            floor.AddComponent<BoxCollider>();
            FoodItem food = Food(position: new Vector3(1000f, 0.06f, 1002f), size: new Vector3(0.4f, 0.12f, 0.4f));
            FoodState state = food.State;
            Quaternion initialLook = Quaternion.LookRotation(food.transform.position - _view.position);
            _view.rotation = initialLook;
            Physics.SyncTransforms();
            Assert.That(_interaction.TryInteract(), Is.True);
            for (int frame = 0; frame < 30; frame++)
            {
                _view.rotation = Quaternion.Slerp(initialLook, Quaternion.identity, (frame + 1f) / 30f);
                yield return new WaitForFixedUpdate();
                Assert.That(_carry.HasHeldObject, Is.True);
            }
            for (int frame = 0; frame < 20; frame++) yield return new WaitForFixedUpdate();
            Assert.That(food.GetComponent<Rigidbody>().position.y, Is.GreaterThan(1.3f));
            Assert.That(food.GetComponent<Rigidbody>().isKinematic, Is.False);
            Assert.That(food.State, Is.SameAs(state));
            Assert.That(food.State.FreshnessPercent, Is.EqualTo(100.0));
        }

        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
