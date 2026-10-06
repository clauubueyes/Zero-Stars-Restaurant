using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using Object = UnityEngine.Object;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M5AssemblyTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();
        private readonly Vector3 _origin = new Vector3(1000f, 0f, 1000f);
        private FoodDefinition _bun, _beef, _cheese;
        private DishDefinition _hamburger, _cheeseburger;
        private FoodSimulation _simulation;

        [SetUp]
        public void SetUp()
        {
            _bun = Definition("food.bun"); _beef = Definition("food.raw_beef_patty", true); _cheese = Definition("food.cheese");
            _hamburger = Recognition("Hamburger", _bun, _beef, _bun);
            _cheeseburger = Recognition("Cheeseburger", _bun, _beef, _cheese, _bun);
            _simulation = Create("Clock", _origin).AddComponent<FoodSimulation>(); _simulation.enabled = false;
        }
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject item in _objects) if (item != null) Object.DestroyImmediate(item);
            _objects.Clear(); foreach (ScriptableObject asset in _assets) if (asset != null) Object.DestroyImmediate(asset);
            _assets.Clear();
        }
        private GameObject Create(string name, Vector3 position)
        { var item = new GameObject(name); item.transform.position = position; _objects.Add(item); return item; }
        private FoodDefinition Definition(string id, bool cookable = false)
        {
            var asset = ScriptableObject.CreateInstance<FoodDefinition>(); _assets.Add(asset);
            Set(asset, "_id", id); Set(asset, "_isCookable", cookable); return asset;
        }
        private DishDefinition Recognition(string name, params FoodDefinition[] ingredients)
        {
            var asset = ScriptableObject.CreateInstance<DishDefinition>(); _assets.Add(asset);
            Set(asset, "_id", "dish." + name.ToLowerInvariant()); Set(asset, "_displayName", name); Set(asset, "_orderedIngredients", ingredients);
            return asset;
        }
        private FoodItem Food(FoodDefinition definition, Vector3 position, Vector3? size = null, float freshness = 100f, bool contaminated = false)
        {
            GameObject item = Create("Ingredient", position); item.SetActive(false);
            item.transform.localScale = size ?? new Vector3(0.4f, 0.12f, 0.4f); item.AddComponent<BoxCollider>();
            Rigidbody body = item.AddComponent<Rigidbody>(); body.mass = 0.15f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.interpolation = RigidbodyInterpolation.Interpolate;
            item.AddComponent<Pickup>(); FoodItem food = item.AddComponent<FoodItem>();
            Set(food, "_definition", definition); Set(food, "_initialFreshnessPercent", freshness); Set(food, "_initiallyContaminated", contaminated);
            item.SetActive(true); Physics.SyncTransforms(); return food;
        }
        private AssemblySurface Surface(Vector3? origin = null)
        {
            Vector3 position = origin ?? _origin;
            GameObject station = Create("Station", position); station.SetActive(false);
            GameObject tray = Create("Tray", position + Vector3.up * 0.025f); tray.transform.SetParent(station.transform, true);
            Rigidbody body = tray.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            tray.AddComponent<BoxCollider>().size = new Vector3(0.7f, 0.05f, 0.7f); DishItem dish = tray.AddComponent<DishItem>();
            GameObject zoneObject = Create("AssemblyZone", position + Vector3.up * 0.57f); zoneObject.transform.SetParent(station.transform, true);
            BoxCollider zone = zoneObject.AddComponent<BoxCollider>(); zone.isTrigger = true; zone.size = new Vector3(0.85f, 1.1f, 0.85f);
            AssemblySurface surface = station.AddComponent<AssemblySurface>();
            Set(surface, "_dish", dish); Set(surface, "_assemblyZone", zone); Set(surface, "_simulation", _simulation);
            Set(surface, "_definitions", new[] { _hamburger, _cheeseburger });
            station.SetActive(true);
            Physics.SyncTransforms(); return surface;
        }
        private void Register(params FoodItem[] foods) => Set(_simulation, "_foods", foods);
        private static InteractionContext EmptyHands => new InteractionContext(null, null);

        [Test]
        public void PhysicalOrderedStackRecognizesCheeseburgerAndKeepsOriginalInstances()
        {
            FoodItem bottom = Food(_bun, _origin + Vector3.up * 0.16f), patty = Food(_beef, _origin + Vector3.up * 0.33f),
                cheese = Food(_cheese, _origin + Vector3.up * 0.45f), top = Food(_bun, _origin + Vector3.up * 0.58f);
            Register(top, cheese, bottom, patty, patty); AssemblySurface surface = Surface(); surface.RefreshComposition();
            Assert.That(surface.Dish.State.Components, Is.EqualTo(new[] { bottom.State, patty.State, cheese.State, top.State }));
            Assert.That(surface.PreviewDefinition.DisplayName, Is.EqualTo("Cheeseburger"));
            string assetBefore = JsonUtility.ToJson(_cheeseburger);
            Assert.That(surface.TryInteract(EmptyHands), Is.True);
            Assert.That(surface.Dish.State.DisplayName, Is.EqualTo("Cheeseburger"));
            Assert.That(surface.Dish.State.Components[1], Is.SameAs(patty.State));
            Assert.That(surface.Dish.IsIntact, Is.True);
            Assert.That(JsonUtility.ToJson(_cheeseburger), Is.EqualTo(assetBefore));
            Assert.That(surface.TryInteract(EmptyHands), Is.False);
        }

        [Test]
        public void AddingRemovingAndReorderingUsePhysicalPoseAndNeverDuplicate()
        {
            FoodItem a = Food(_bun, _origin + Vector3.up * 0.16f), b = Food(_beef, _origin + Vector3.up * 0.33f);
            Register(a, b); AssemblySurface surface = Surface(); surface.RefreshComposition();
            surface.RefreshComposition(); Assert.That(surface.Dish.State.Components.Count, Is.EqualTo(2));
            b.GetComponent<Rigidbody>().position = _origin + Vector3.right * 2f; surface.RefreshComposition();
            Assert.That(surface.Dish.State.Components, Is.EqualTo(new[] { a.State }));
            b.GetComponent<Rigidbody>().position = _origin + Vector3.up * 0.1f; surface.RefreshComposition();
            Assert.That(surface.Dish.State.Components, Is.EqualTo(new[] { b.State, a.State }));
        }

        [Test]
        public void SideBySideKnownIngredientsFinalizeAsCustomDish()
        {
            FoodItem a = Food(_bun, _origin + new Vector3(-0.25f, 0.16f, 0f)),
                b = Food(_beef, _origin + Vector3.up * 0.33f), c = Food(_bun, _origin + new Vector3(0.25f, 0.5f, 0f));
            Register(a, b, c); AssemblySurface surface = Surface(); surface.RefreshComposition();
            Assert.That(surface.PreviewDefinition, Is.Null);
            Assert.That(surface.TryInteract(EmptyHands), Is.True);
            Assert.That(surface.Dish.State.DisplayName, Is.EqualTo("Custom Dish"));
        }

        [Test]
        public void EmptyConfirmationFailsAndSensorDisableReleasesDraft()
        {
            AssemblySurface surface = Surface(); Assert.That(surface.TryInteract(EmptyHands), Is.False);
            FoodItem food = Food(_beef, _origin + Vector3.up * 0.16f); Register(food); surface.RefreshComposition();
            Assert.That(surface.Dish.State.Components.Count, Is.EqualTo(1));
            surface.GetComponentsInChildren<BoxCollider>().Single(collider => collider.isTrigger).enabled = false;
            Assert.That(surface.TryInteract(EmptyHands), Is.False);
            Assert.That(surface.Dish.State.Components.Count, Is.Zero);
            Assert.That(new DishState().TryAdd(food.State), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisabledOrDestroyedFoodLeavesDraftWithoutStaleComposition(bool destroy)
        {
            FoodItem food = Food(_beef, _origin + Vector3.up * 0.16f); Register(food); AssemblySurface surface = Surface();
            surface.RefreshComposition(); if (destroy) Object.DestroyImmediate(food.gameObject); else food.gameObject.SetActive(false);
            Assert.DoesNotThrow(surface.RefreshComposition); Assert.That(surface.Dish.State.Components.Count, Is.Zero);
        }

        [Test]
        public void OverlappingSurfacesCannotClaimTheSameUnitAndDisableAllowsTransfer()
        {
            FoodItem food = Food(_beef, _origin + Vector3.up * 0.16f); Register(food);
            AssemblySurface first = Surface(), second = Surface(); first.RefreshComposition(); second.RefreshComposition();
            Assert.That(first.Dish.State.Components.Count + second.Dish.State.Components.Count, Is.EqualTo(1));
            first.enabled = false; second.RefreshComposition();
            Assert.That(second.Dish.State.Components[0], Is.SameAs(food.State));
        }

        [Test]
        public void FinalizedFoodContinuesAgingExactlyOnceAndLiveAggregatesPreserveCookingAndContamination()
        {
            FoodItem food = Food(_beef, _origin + Vector3.up * 0.16f, freshness: 0f, contaminated: true);
            food.State.SetTemperature(120.0); food.State.Advance(45.0, new ThermalEnvironment(120.0, allowsCooking: true), 0.0);
            Register(food, food); AssemblySurface surface = Surface(); surface.RefreshComposition();
            FoodState state = food.State; double age = state.AgeSeconds, dose = state.Cooking.EquivalentSeconds;
            Assert.That(surface.TryInteract(EmptyHands), Is.True);
            _simulation.Advance(60.0);
            Assert.That(state.AgeSeconds, Is.EqualTo(age + 60.0));
            Assert.That(state.Cooking.EquivalentSeconds, Is.EqualTo(dose));
            Assert.That(surface.Dish.State.Components[0], Is.SameAs(state));
            Assert.That(surface.Dish.State.ContainsSpoiledOrRotten, Is.True);
            Assert.That(surface.Dish.State.ContainsContamination, Is.True);
            Assert.That(state.TemperatureCelsius, Is.LessThan(120.0));
        }

        [Test]
        public void PickingAnIngredientRemovesItFromDraftAndHeldHandsCannotFinalize()
        {
            FoodItem food = Food(_beef, _origin + Vector3.up * 0.16f);
            FoodItem other = Food(_cheese, _origin + new Vector3(0.3f, 0.16f, 0f)); Register(food, other);
            AssemblySurface surface = Surface(); surface.RefreshComposition();
            GameObject actor = Create("Actor", _origin + Vector3.back * 1.4f);
            Transform view = Create("View", actor.transform.position + Vector3.up * 1.65f).transform; view.SetParent(actor.transform, true);
            view.LookAt(food.transform.position);
            PhysicalCarry carry = actor.AddComponent<PhysicalCarry>(); Set(carry, "_actorRoot", actor.transform); Set(carry, "_viewTransform", view);
            var context = new InteractionContext(actor.transform, carry);
            Assert.That(food.GetComponent<Pickup>().TryInteract(context), Is.True);
            surface.RefreshComposition();
            Assert.That(surface.Dish.State.Components, Is.EqualTo(new[] { other.State }));
            Assert.That(surface.TryInteract(context), Is.False);
            carry.Drop(); surface.RefreshComposition();
            Assert.That(surface.Dish.State.Components.Count, Is.EqualTo(2));
        }

        [Test]
        public void FinalizedIdentitySurvivesDisableButMissingPhysicalIngredientIsReported()
        {
            FoodItem food = Food(_beef, _origin + Vector3.up * 0.16f); Register(food);
            AssemblySurface surface = Surface(); surface.RefreshComposition();
            Assert.That(surface.TryInteract(EmptyHands), Is.True);
            DishItem dish = surface.Dish; DishState state = dish.State; Guid? id = state.InstanceId;
            dish.gameObject.SetActive(false); _simulation.Advance(10.0); dish.gameObject.SetActive(true);
            Assert.That(dish.State, Is.SameAs(state)); Assert.That(state.InstanceId, Is.EqualTo(id));
            Assert.That(dish.IsIntact, Is.True);
            Object.DestroyImmediate(food.gameObject);
            Assert.That(dish.IsIntact, Is.False);
            Assert.That(state.Components.Count, Is.EqualTo(1)); // Retains original conceptual state, never invented replacements.
            Assert.DoesNotThrow(() => _simulation.Advance(10.0));
            Object.DestroyImmediate(dish.gameObject); Assert.That(state.IsDisposed, Is.True);
        }

        [UnityTest]
        public IEnumerator PickupCookingAssemblyAndAggregateTransportKeepTheSameFoodState()
        {
            AssemblySurface surface = Surface();
            GameObject support = Create("Workbench", _origin - Vector3.up * 0.1f);
            support.AddComponent<BoxCollider>().size = new Vector3(6f, 0.2f, 3f);
            FoodItem food = Food(_beef, _origin + new Vector3(2f, 0.06f, 0f)); Register(food);
            FoodState state = food.State;
            GrillHeatSource grill = Create("Grill", _origin + Vector3.right * 2f).AddComponent<GrillHeatSource>();
            BoxCollider thermalZone = Create("ThermalZone", _origin + new Vector3(2f, 0.1f, 0f)).AddComponent<BoxCollider>();
            thermalZone.isTrigger = true; thermalZone.size = new Vector3(0.8f, 0.22f, 0.8f); Set(grill, "_effectiveZone", thermalZone);
            Set(_simulation, "_heatSources", new HeatSource[] { grill });
            GameObject actor = Create("Actor", _origin + new Vector3(2f, 0f, -1.4f));
            CharacterController capsule = actor.AddComponent<CharacterController>(); capsule.height = 1.8f; capsule.center = Vector3.up * 0.9f; capsule.radius = 0.3f;
            Transform view = Create("View", actor.transform.position + Vector3.up * 1.65f).transform; view.SetParent(actor.transform, true);
            view.LookAt(food.transform.position);
            PhysicalCarry carry = actor.AddComponent<PhysicalCarry>(); Set(carry, "_actorRoot", actor.transform); Set(carry, "_viewTransform", view);
            var context = new InteractionContext(actor.transform, carry); Physics.SyncTransforms();
            Assert.That(food.GetComponent<Pickup>().TryInteract(context), Is.True);
            _simulation.Advance(45.0); // Held food still physically inside grill's effective zone, before moving.
            Assert.That(state.Cooking.Stage, Is.EqualTo(CookingStage.Cooked));
            Quaternion initialDown = view.rotation;
            for (int frame = 0; frame < 30; frame++)
            {
                view.rotation = Quaternion.Slerp(initialDown, Quaternion.identity, (frame + 1f) / 30f);
                yield return new WaitForFixedUpdate(); Assert.That(carry.HasHeldObject, Is.True);
            }
            for (int frame = 0; frame < 40; frame++)
            {
                actor.transform.position = _origin + new Vector3(Mathf.Lerp(2f, 0f, (frame + 1f) / 40f), 0f, -1.4f);
                yield return new WaitForFixedUpdate(); Assert.That(carry.HasHeldObject, Is.True);
            }
            Quaternion placementLook = Quaternion.LookRotation(_origin + new Vector3(0f, 0.1f, 0.15f) - view.position);
            for (int frame = 0; frame < 30; frame++)
            {
                view.rotation = Quaternion.Slerp(Quaternion.identity, placementLook, (frame + 1f) / 30f);
                yield return new WaitForFixedUpdate(); Assert.That(carry.HasHeldObject, Is.True);
            }
            carry.Drop();
            for (int frame = 0; frame < 40; frame++) yield return new WaitForFixedUpdate();
            surface.RefreshComposition(); Assert.That(surface.Dish.State.Components[0], Is.SameAs(state));
            Assert.That(surface.TryInteract(EmptyHands), Is.True);
            Guid foodId = state.InstanceId; Guid? dishId = surface.Dish.State.InstanceId;
            actor.transform.position = _origin + Vector3.back * 1.4f;
            view.LookAt(surface.Dish.transform.position); Physics.SyncTransforms();
            Assert.That(surface.Dish.GetComponent<Pickup>().TryInteract(context), Is.True);
            Quaternion down = view.rotation;
            for (int frame = 0; frame < 40; frame++)
            {
                view.rotation = Quaternion.Slerp(down, Quaternion.identity, (frame + 1f) / 40f);
                yield return new WaitForFixedUpdate(); Assert.That(carry.HasHeldObject, Is.True);
            }
            for (int frame = 0; frame < 30; frame++) yield return new WaitForFixedUpdate();
            Assert.That(surface.Dish.transform.position.y, Is.GreaterThan(1f));
            Assert.That(food.transform.IsChildOf(surface.Dish.transform), Is.True);
            Assert.That(food.GetComponent<Rigidbody>().isKinematic, Is.True);
            Assert.That(food.GetComponentsInChildren<Collider>().All(collider => !collider.enabled), Is.True);
            Assert.That(food.GetComponent<Pickup>().enabled, Is.False);
            Assert.That(surface.Dish.State.Components[0], Is.SameAs(state));
            Assert.That(state.InstanceId, Is.EqualTo(foodId)); Assert.That(surface.Dish.State.InstanceId, Is.EqualTo(dishId));
            _simulation.Advance(10.0); Assert.That(state.Cooking.Stage, Is.EqualTo(CookingStage.Cooked));
            carry.Throw();
            for (int frame = 0; frame < 15; frame++) yield return new WaitForFixedUpdate();
            Assert.That(surface.Dish.IsIntact, Is.True);
            Assert.That((food.transform.position - surface.Dish.transform.position).magnitude, Is.LessThan(1f));
        }

        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
