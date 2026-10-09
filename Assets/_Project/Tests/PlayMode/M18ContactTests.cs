using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;
using ZeroStarRestaurant.Interaction;
using Object = UnityEngine.Object;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M18ContactTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private readonly Vector3 _origin = new Vector3(1400, 0, 1400);
        private FoodSafetySettings _safety;
        private HygieneSettings _hygiene;
        private FoodSimulation _simulation;

        [SetUp]
        public void SetUp()
        {
            _safety = ScriptableObject.CreateInstance<FoodSafetySettings>(); _objects.Add(_safety);
            _hygiene = ScriptableObject.CreateInstance<HygieneSettings>(); _objects.Add(_hygiene);
            _simulation = Create("Clock", _origin).AddComponent<FoodSimulation>(); _simulation.enabled = false;
        }
        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--) if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }
        private GameObject Create(string name, Vector3 position)
        { var owner = new GameObject(name); owner.transform.position = position; _objects.Add(owner); return owner; }
        private CleanableSurface Surface(Vector3? offset = null)
        {
            var support = Create("Support", _origin + (offset ?? Vector3.zero) - Vector3.up * .1f).AddComponent<BoxCollider>();
            support.size = new Vector3(2, .2f, 2);
            var owner = Create("Prep", support.transform.position); owner.SetActive(false);
            var surface = owner.AddComponent<CleanableSurface>();
            Set(surface, "_settings", _hygiene); Set(surface, "_support", support); Set(surface, "_foodSafetySettings", _safety);
            var contact = owner.AddComponent<SurfaceFoodContact>(); Set(contact, "_surface", surface); Set(contact, "_simulation", _simulation);
            Set(contact, "_pollAutomatically", false); owner.SetActive(true); return surface;
        }
        private FoodItem Food(bool meat, Vector3? offset = null)
        {
            var definition = ScriptableObject.CreateInstance<FoodDefinition>(); _objects.Add(definition);
            Set(definition, "_id", meat ? "patty" : "bun"); Set(definition, "_category", meat ? FoodCategory.Meat : FoodCategory.Bakery);
            Set(definition, "_isCookable", meat);
            var owner = Create(meat ? "Patty" : "Bun", _origin + (offset ?? Vector3.zero) + Vector3.up * .06f); owner.SetActive(false);
            owner.AddComponent<BoxCollider>().size = new Vector3(.4f, .12f, .4f);
            owner.AddComponent<Rigidbody>().useGravity = false; owner.AddComponent<Pickup>();
            var food = owner.AddComponent<FoodItem>(); Set(food, "_definition", definition); owner.SetActive(true);
            Assert.That(_simulation.Register(food), Is.True); return food;
        }
        private static void Move(FoodItem food, Vector3 delta) => food.GetComponent<Rigidbody>().position += delta;
        private static void Poll(CleanableSurface surface) => surface.GetComponent<SurfaceFoodContact>().Poll();

        [Test]
        public void RealSupportContactContaminatesOnceAndBothStatesPersistAfterRemoval()
        {
            var surface = Surface(); var patty = Food(true); Poll(surface);
            Assert.That(surface.Contamination.Intensity, Is.EqualTo(.4).Within(1e-7));
            var contact = surface.GetComponent<SurfaceFoodContact>();
            for (int i = 0; i < 500; i++) Poll(surface);
            Assert.That(contact.SafetyTransferCount, Is.EqualTo(1)); Assert.That(contact.ContactEntryCount, Is.EqualTo(1));
            Move(patty, Vector3.up * 2); Poll(surface); var bun = Food(false); Poll(surface);
            Assert.That(bun.State.Contamination.Intensity, Is.EqualTo(.2).Within(1e-7));
            var trace = bun.State.Contamination.Snapshot()[0]; Assert.That(trace.FoodUnitId, Is.EqualTo(patty.State.InstanceId));
            Assert.That(trace.LastSourceId, Is.EqualTo(surface.State.SurfaceId));
            Move(bun, Vector3.up * 2); Poll(surface); surface.DevelopmentSanitizeSurface();
            Assert.That(bun.State.IsContaminated, Is.True); Assert.That(contact.SafetyTransferCount, Is.EqualTo(2));
        }

        [Test]
        public void DirtySafeSurfaceDoesNotContaminateAndCleaningIsNotSanitizing()
        {
            var surface = Surface(); surface.DevelopmentMakeFilthy(); var bun = Food(false); Poll(surface);
            Assert.That(bun.State.IsContaminated, Is.False); surface.DevelopmentContaminateSurface();
            surface.State.Clean(100, 1); Assert.That(surface.State.Amount, Is.Zero); Assert.That(surface.Contamination.IsContaminated, Is.True);
            surface.DevelopmentMakeFilthy(); surface.DevelopmentSanitizeSurface();
            Assert.That(surface.State.Amount, Is.EqualTo(1)); Assert.That(surface.Contamination.IsContaminated, Is.False);
        }

        [Test]
        public void NewlyContaminatedSurfaceRequiresANewContactRatherThanTransferringEveryFrame()
        {
            var surface = Surface(); var bun = Food(false); Poll(surface); surface.DevelopmentContaminateSurface();
            for (int i = 0; i < 100; i++) Poll(surface);
            Assert.That(bun.State.IsContaminated, Is.False);
            Move(bun, Vector3.up); Poll(surface); Move(bun, Vector3.down); Poll(surface);
            Assert.That(bun.State.IsContaminated, Is.True); Assert.That(surface.GetComponent<SurfaceFoodContact>().SafetyTransferCount, Is.EqualTo(2));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void AirOutsideRegionInactiveAndDisabledColliderAreNotPhysicalContacts(int mode)
        {
            var surface = Surface(); var patty = Food(true);
            if (mode == 0) Move(patty, Vector3.up * .2f);
            if (mode == 1) { Set(surface, "_regionCenter", Vector2.right * .6f); Set(surface, "_regionSize", new Vector2(.3f, .3f)); }
            if (mode == 2) patty.gameObject.SetActive(false);
            if (mode == 3) patty.GetComponent<Collider>().enabled = false;
            Poll(surface); Assert.That(surface.Contamination.IsContaminated, Is.False);
            Assert.That(surface.GetComponent<SurfaceFoodContact>().SafetyTransferCount, Is.Zero);
        }

        [Test]
        public void HeldFoodDoesNotTransferUntilReleasedOntoSupport()
        {
            var surface = Surface(); var patty = Food(true);
            var actor = Create("Player", _origin + Vector3.back); var carry = actor.AddComponent<PhysicalCarry>();
            var view = Create("View", _origin + new Vector3(0, 1, -1)).transform; view.LookAt(_origin);
            Set(carry, "_viewTransform", view); Set(carry, "_actorRoot", actor.transform); Physics.SyncTransforms();
            Assert.That(carry.TryPickUp(patty.GetComponent<Pickup>()), Is.True); Poll(surface);
            Assert.That(surface.Contamination.IsContaminated, Is.False); carry.Drop(); Poll(surface);
            Assert.That(surface.Contamination.IsContaminated, Is.True);
        }

        [Test]
        public void RetiredDishGeometryOnlyTransfersFromTheIngredientThatTouchesTheSurface()
        {
            var surface = Surface(); var bun = Food(false); var patty = Food(true, Vector3.up * .12f);
            var dish = Create("Dish", _origin + Vector3.up * .06f).AddComponent<DishItem>();
            dish.GetComponent<BoxCollider>().size = new Vector3(.01f, .01f, .01f);
            Assert.That(dish.State.TryAdd(bun.State) && dish.State.TryAdd(patty.State), Is.True);
            Assert.That(dish.FinalizeAssembly(new[] { bun, patty }, Array.Empty<DishProfile>()), Is.True);
            dish.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
            Poll(surface); Assert.That(surface.Contamination.IsContaminated, Is.False);
            surface.DevelopmentContaminateSurface(); dish.transform.position += Vector3.up;
            Poll(surface); dish.transform.position -= Vector3.up; Poll(surface);
            Assert.That(bun.State.IsContaminated, Is.True); Assert.That(patty.State.IsContaminated, Is.False);
        }

        [Test]
        public void SeparateRegionsOnTheSameFloorDoNotShareSanitaryState()
        {
            var first = Surface(); first.Support.size = new Vector3(6, .2f, 2);
            Set(first, "_regionCenter", new Vector2(-1.5f, 0)); Set(first, "_regionSize", new Vector2(2, 2));
            var second = Surface(); Set(second, "_support", first.Support);
            Set(second, "_regionCenter", new Vector2(1.5f, 0)); Set(second, "_regionSize", new Vector2(2, 2));
            var patty = Food(true, Vector3.left * 1.5f); Poll(first); Poll(second);
            Assert.That(first.Contamination.IsContaminated, Is.True); Assert.That(second.Contamination.IsContaminated, Is.False);
            Move(patty, Vector3.up * 2); var bun = Food(false, Vector3.right * 1.5f); Poll(second);
            Assert.That(bun.State.IsContaminated, Is.False);
        }

        [Test]
        public void SimultaneousEntrantsSeeCapturedSurfaceExposureRegardlessOfRegisteredOrder()
        {
            var surface = Surface(); var raw = Food(true, Vector3.left * .5f); var bun = Food(false, Vector3.right * .5f);
            Poll(surface); Assert.That(surface.Contamination.IsContaminated, Is.True); Assert.That(bun.State.IsContaminated, Is.False);
            Assert.That(raw.State.IsContaminated, Is.False); Assert.That(surface.GetComponent<SurfaceFoodContact>().SafetyTransferCount, Is.EqualTo(2));
        }

        private static void Set(object target, string field, object value)
            => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }
}
