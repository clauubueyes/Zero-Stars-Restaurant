using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Orders;
using Object = UnityEngine.Object;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M6ServiceTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();
        private readonly List<FoodItem> _foods = new List<FoodItem>();
        private readonly Vector3 _origin = new Vector3(2000, 0, 2000);
        private FoodDefinition _bun, _beef, _cheese;
        private DishDefinition _hamburger, _cheeseburger;
        private FoodSimulation _simulation;
        private CustomerServiceLoop _service;
        private CustomerMovement _customer;
        private CustomerDishCarrier _carrier;
        private DeliveryZone _delivery;
        private BoxCollider _pad;

        [SetUp]
        public void SetUp()
        {
            _bun = Definition("food.bun"); _beef = Definition("food.raw_beef_patty", true); _cheese = Definition("food.cheese");
            _hamburger = Recognition("Hamburger", _bun, _beef, _bun);
            _cheeseburger = Recognition("Cheeseburger", _bun, _beef, _cheese, _bun);
            _simulation = Create("FoodClock", _origin).AddComponent<FoodSimulation>(); _simulation.enabled = false;
            GameObject support = Create("Workbench", _origin + Vector3.up * 0.8f);
            support.AddComponent<BoxCollider>().size = new Vector3(8, 0.2f, 1.6f);
            GameObject customerObject = Create("Customer", _origin + new Vector3(5, 0, -3)); customerObject.SetActive(false);
            Rigidbody customerBody = customerObject.AddComponent<Rigidbody>(); customerBody.isKinematic = true; customerBody.useGravity = false;
            _customer = customerObject.AddComponent<CustomerMovement>();
            Transform anchor = Create("Dish anchor", customerObject.transform.position + new Vector3(0, 1.2f, 0.65f)).transform;
            anchor.SetParent(customerObject.transform, true);
            _carrier = customerObject.AddComponent<CustomerDishCarrier>(); Set(_carrier, "_anchor", anchor);
            Set(_customer, "_entryPoint", Create("Entrance", customerObject.transform.position).transform);
            Set(_customer, "_exitPoint", Create("Exit", customerObject.transform.position).transform);
            Set(_customer, "_arrivalPath", new[] { Create("Corner", _origin + new Vector3(0, 0, -3)).transform,
                Create("Waiting", _origin + new Vector3(0, 0, -2.5f)).transform });
            var config = ScriptableObject.CreateInstance<CustomerServiceConfiguration>(); _assets.Add(config);
            var first = new CustomerServiceConfiguration.MenuEntry(); Set(first, "_dish", _hamburger); Set(first, "_salePriceCents", 500);
            var second = new CustomerServiceConfiguration.MenuEntry(); Set(second, "_dish", _cheeseburger); Set(second, "_salePriceCents", 650);
            Set(config, "_menu", new[] { first, second });
            GameObject loop = Create("Service", _origin); loop.SetActive(false); _service = loop.AddComponent<CustomerServiceLoop>();
            Set(_service, "_configuration", config); Set(_service, "_customer", _customer); Set(_service, "_dishCarrier", _carrier); Set(_service, "_foodSimulation", _simulation);
            loop.SetActive(true);
            GameObject padObject = Create("DeliveryPad", _origin + new Vector3(-2, 0.915f, 0));
            _pad = padObject.AddComponent<BoxCollider>(); _pad.size = new Vector3(1.25f, 0.03f, 0.9f);
            GameObject delivery = Create("Delivery", _origin + new Vector3(-2, 1.45f, 0)); delivery.SetActive(false);
            BoxCollider sensor = delivery.AddComponent<BoxCollider>(); sensor.isTrigger = true; sensor.size = new Vector3(1.25f, 1.04f, 0.9f);
            _delivery = delivery.AddComponent<DeliveryZone>(); Set(_delivery, "_zone", sensor); Set(_delivery, "_support", _pad); Set(_delivery, "_service", _service);
            delivery.SetActive(true); Physics.SyncTransforms();
        }
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject item in _objects) if (item != null) Object.DestroyImmediate(item);
            foreach (ScriptableObject asset in _assets) if (asset != null) Object.DestroyImmediate(asset);
            _objects.Clear(); _assets.Clear(); _foods.Clear();
        }
        private GameObject Create(string name, Vector3 position)
        { var item = new GameObject(name); item.transform.position = position; _objects.Add(item); return item; }
        private FoodDefinition Definition(string id, bool cookable = false)
        { var asset = ScriptableObject.CreateInstance<FoodDefinition>(); _assets.Add(asset); Set(asset, "_id", id); Set(asset, "_isCookable", cookable); return asset; }
        private DishDefinition Recognition(string name, params FoodDefinition[] ingredients)
        {
            var asset = ScriptableObject.CreateInstance<DishDefinition>(); _assets.Add(asset);
            Set(asset, "_id", "dish." + name.ToLowerInvariant()); Set(asset, "_displayName", name); Set(asset, "_orderedIngredients", ingredients); return asset;
        }
        private FoodItem Food(FoodDefinition definition, Vector3 position, float freshness = 100, bool contaminated = false)
        {
            GameObject item = Create("Ingredient", position); item.SetActive(false);
            item.transform.localScale = definition == _bun ? new Vector3(0.4f, 0.22f, 0.4f) : definition == _beef ? new Vector3(0.4f, 0.12f, 0.4f) : new Vector3(0.32f, 0.06f, 0.32f);
            item.AddComponent<BoxCollider>(); Rigidbody body = item.AddComponent<Rigidbody>(); body.mass = 0.15f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.solverIterations = 12;
            item.AddComponent<Pickup>(); FoodItem food = item.AddComponent<FoodItem>();
            Set(food, "_definition", definition); Set(food, "_initialFreshnessPercent", freshness); Set(food, "_initiallyContaminated", contaminated);
            item.SetActive(true); _foods.Add(food); Set(_simulation, "_foods", _foods.ToArray()); Physics.SyncTransforms(); return food;
        }
        private AssemblySurface Surface(Vector3 position)
        {
            GameObject station = Create("Assembly", position); station.SetActive(false);
            GameObject tray = Create("Tray", position + Vector3.up * 0.025f); tray.transform.SetParent(station.transform, true);
            Rigidbody body = tray.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            tray.AddComponent<BoxCollider>().size = new Vector3(0.7f, 0.05f, 0.7f); DishItem dish = tray.AddComponent<DishItem>();
            BoxCollider sensor = Create("AssemblyVolume", position + Vector3.up * 0.6f).AddComponent<BoxCollider>();
            sensor.isTrigger = true; sensor.size = new Vector3(0.85f, 1.1f, 0.85f); sensor.transform.SetParent(station.transform, true);
            AssemblySurface surface = station.AddComponent<AssemblySurface>(); Set(surface, "_dish", dish); Set(surface, "_assemblyZone", sensor);
            Set(surface, "_simulation", _simulation); Set(surface, "_definitions", new[] { _hamburger, _cheeseburger });
            station.SetActive(true); Physics.SyncTransforms(); return surface;
        }
        private DishItem FinalDish(bool cheese = false, float freshness = 100, bool contaminated = false, double dose = 45, bool custom = false)
        {
            Vector3 at = _origin + new Vector3(0, 0.9f, 0); AssemblySurface surface = Surface(at);
            Food(_bun, at + Vector3.up * 0.17f);
            FoodItem patty = Food(_beef, at + Vector3.up * 0.34f, freshness, contaminated);
            patty.State.SetTemperature(120); patty.State.Advance(dose, new ThermalEnvironment(120, allowsCooking: true), 0);
            if (cheese) Food(_cheese, at + Vector3.up * 0.44f);
            Food(custom ? _cheese : _bun, at + Vector3.up * 0.58f);
            surface.RefreshComposition(); Assert.That(surface.TryInteract(new InteractionContext(null, null)), Is.True); return surface.Dish;
        }
        private void Ready(int force = -1)
        {
            if (force >= 0) Assert.That(_service.ForceNextOrder(force), Is.True);
            _service.Advance(1); _service.Advance(100); _service.Advance(1);
            Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
        }
        private void Place(DishItem dish)
        {
            Rigidbody body = dish.GetComponent<Rigidbody>(); body.interpolation = RigidbodyInterpolation.None;
            dish.transform.position = _pad.transform.position + Vector3.up;
            Physics.SyncTransforms(); dish.transform.position += Vector3.up * (_pad.bounds.max.y - dish.GetComponent<BoxCollider>().bounds.min.y);
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; Physics.SyncTransforms();
            Assert.That(dish.GetComponent<BoxCollider>().bounds.min.y, Is.EqualTo(_pad.bounds.max.y).Within(0.001f));
            Collider[] hits = Physics.OverlapBox(_delivery.transform.position, _delivery.GetComponent<BoxCollider>().size * 0.5f,
                Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            Assert.That(hits.Any(hit => hit.GetComponentInParent<DishItem>() == dish), Is.True,
                "Delivery must see the actual proxy. " + dish.GetComponent<BoxCollider>().bounds + " at " + _delivery.transform.position);
            Assert.That(dish.IsIntact, Is.True);
        }

        [TestCase(false, 500)]
        [TestCase(true, 650)]
        public void DeliveryCommitsConfiguredPaymentRetiresOriginalUnitsAndCannotProcessTwice(bool cheese, int amount)
        {
            Ready(cheese ? 1 : 0); DishItem dish = FinalDish(cheese); DishState original = dish.State;
            Guid[] ids = original.Components.Select(food => food.InstanceId).ToArray(); Place(dish); _delivery.Poll();
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(amount)); Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Pay));
            Assert.That(_service.LastResult.Evaluation.DeliveredDish.Ingredients.Select(food => food.InstanceId), Is.EqualTo(ids));
            Assert.That(dish.gameObject.activeInHierarchy, Is.True); Assert.That(original.IsSold, Is.True);
            Assert.That(_carrier.Dish, Is.SameAs(dish)); Assert.That(dish.transform.IsChildOf(_customer.transform), Is.True);
            Assert.That(dish.GetComponent<Pickup>().enabled, Is.False); Assert.That(dish.GetComponent<Rigidbody>().isKinematic, Is.True);
            Assert.That(dish.GetComponentsInChildren<Collider>().All(collider => !collider.enabled), Is.True);
            Assert.That(_simulation.Foods.Count, Is.EqualTo(ids.Length)); _delivery.Poll(); Assert.That(_service.TryDeliver(dish), Is.False);
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(amount));
            var receipt = _service.LastResult; _simulation.Advance(600);
            Assert.That(receipt.Evaluation.DeliveredDish.MinimumFreshnessPercent, Is.EqualTo(100));
            _service.Advance(10); Assert.That(dish.gameObject.activeInHierarchy, Is.True);
            Vector3 before = dish.transform.position; _service.Advance(0.1);
            Assert.That(dish.transform.position, Is.Not.EqualTo(before), "The same sold dish travels with the customer.");
            _service.Advance(100);
            Assert.That(dish.gameObject.activeSelf, Is.False); Assert.That(_carrier.Dish, Is.Null); Assert.That(_simulation.Foods, Is.Empty);
            Assert.That(_service.LastResult, Is.SameAs(receipt)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(amount));
        }

        [Test]
        public void DisabledCarrierRejectsBeforePaymentAndCancelledServiceRetiresItsSoldDish()
        {
            Ready(); DishItem dish = FinalDish(); Place(dish); _carrier.enabled = false; _delivery.Poll();
            Assert.That(_service.Ledger.BalanceCents, Is.Zero); Assert.That(dish.State.IsSold, Is.False);
            Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait)); Assert.That(dish.gameObject.activeSelf, Is.True);
            _carrier.enabled = true; _delivery.Poll(); Assert.That(_carrier.Dish, Is.SameAs(dish));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
            _service.enabled = false;
            Assert.That(_carrier.Dish, Is.Null); Assert.That(dish.gameObject.activeSelf, Is.False); Assert.That(_simulation.Foods, Is.Empty);
            Assert.That(_customer.gameObject.activeSelf, Is.False); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
        }

        [Test]
        public void RejectionKeepsDishAndRequiresANewPlacementBeforeNextCustomer()
        {
            Ready(1); DishItem dish = FinalDish(); Place(dish); _delivery.Poll();
            Assert.That(_service.LastResult.Accepted, Is.False); Assert.That(dish.gameObject.activeSelf, Is.True);
            Assert.That(_carrier.Dish, Is.Null); Assert.That(dish.GetComponent<Pickup>().enabled, Is.True);
            Assert.That(dish.State.IsSold, Is.False); Assert.That(_simulation.Foods.Count, Is.EqualTo(3));
            Assert.That(_service.Ledger.BalanceCents, Is.Zero);
            Assert.That(_service.ForceNextOrder(0), Is.True);
            _service.Advance(10); _service.Advance(100); _service.Advance(3); _service.Advance(100); _service.Advance(1);
            Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
            _delivery.Poll(); Assert.That(_service.Visit.Order.IsCompleted, Is.False);
            dish.transform.position += Vector3.right * 3; Physics.SyncTransforms(); _delivery.Poll();
            Place(dish); _delivery.Poll(); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
        }

        [Test]
        public void IndividualFoodBoxesDraftDishesAndFastOrAirborneFinalDishesAreIgnored()
        {
            Ready(); Food(_beef, _pad.transform.position + Vector3.up * 0.1f);
            GameObject box = Create("ArbitraryBox", _pad.transform.position + Vector3.up * 0.3f); box.AddComponent<BoxCollider>(); box.AddComponent<Rigidbody>(); box.AddComponent<Pickup>();
            AssemblySurface draft = Surface(_pad.transform.position + Vector3.up * 0.02f); _delivery.Poll();
            Assert.That(_service.Visit.Order.IsCompleted, Is.False);
            Object.DestroyImmediate(draft.gameObject); Object.DestroyImmediate(box); Object.DestroyImmediate(_foods[0].gameObject);
            DishItem dish = FinalDish(); Place(dish); Rigidbody body = dish.GetComponent<Rigidbody>(); body.position += Vector3.up * 0.3f;
            Physics.SyncTransforms(); _delivery.Poll(); Assert.That(_service.Visit.Order.IsCompleted, Is.False);
            Place(dish); body.linearVelocity = Vector3.right; _delivery.Poll(); Assert.That(_service.Visit.Order.IsCompleted, Is.False);
            body.linearVelocity = Vector3.zero; _delivery.Poll(); Assert.That(_service.Visit.Order.IsCompleted, Is.True);
        }

        [Test]
        public void MissingDisabledOrHeldDishesCannotBeDeliveredAndDisabledSensorsDoNotRetainPlacements()
        {
            Ready(); DishItem dish = FinalDish(); Place(dish); dish.gameObject.SetActive(false); _delivery.Poll();
            Assert.That(_service.Visit.Order.IsCompleted, Is.False); dish.gameObject.SetActive(true);
            GameObject actor = Create("Actor", _origin + new Vector3(-2, 0, -1.95f));
            Transform view = Create("View", actor.transform.position + Vector3.up * 1.65f).transform; view.SetParent(actor.transform, true);
            PhysicalCarry carry = actor.AddComponent<PhysicalCarry>(); Set(carry, "_viewTransform", view); Set(carry, "_actorRoot", actor.transform);
            Assert.That(carry.TryPickUp(dish.GetComponent<Pickup>()), Is.True); _delivery.Poll();
            Assert.That(_service.Visit.Order.IsCompleted, Is.False); carry.Drop();
            BoxCollider sensor = _delivery.GetComponent<BoxCollider>(); sensor.enabled = false; _delivery.Poll();
            Assert.That(_service.Visit.Order.IsCompleted, Is.False); sensor.enabled = true;
            Object.DestroyImmediate(dish.GetComponentsInChildren<FoodItem>()[0].gameObject); _delivery.Poll();
            Assert.That(dish.IsIntact, Is.False); Assert.That(_service.Visit.Order.IsCompleted, Is.False);
        }

        [TestCase(0, false, 45, false, true)]
        [TestCase(100, false, 90, false, true)]
        [TestCase(74, true, 45, false, true)]
        [TestCase(100, false, 45, true, false)]
        public void RuntimeEvaluationKeepsSafetySeparateAndCustomDishRemainsAvailable(float freshness, bool contaminated, double dose, bool custom, bool accepted)
        {
            Ready(); DishItem dish = FinalDish(freshness: freshness, contaminated: contaminated, dose: dose, custom: custom); Place(dish); _delivery.Poll();
            Assert.That(_service.LastResult.Accepted, Is.EqualTo(accepted));
            Assert.That(_service.LastResult.Evaluation.DeliveredDish.ContainsContamination, Is.EqualTo(contaminated));
            Assert.That(_service.LastResult.Evaluation.DeliveredDish.MinimumFreshnessPercent, Is.EqualTo(freshness));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(accepted ? 500 : 0));
            Assert.That(OrderFeedback.ResultText(_service.LastResult), Does.Contain("Correct order: " + (accepted ? "YES" : "NO")));
            Assert.That(OrderFeedback.ResultText(_service.LastResult), Does.Contain("Rotten ingredient: " + (freshness == 0 ? "YES" : "NO")));
            if (!accepted) Assert.That(dish.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void OneCustomerWalksWaitsLeavesAndNextVisitUsesNewIdentityAndForcedMenuOnce()
        {
            Assert.That(_service.Ledger.BalanceCents, Is.Zero); Assert.That(_service.Visit, Is.Null); Assert.That(_customer.gameObject.activeSelf, Is.False);
            _service.Advance(1); var first = _service.Visit; Assert.That(first.Stage, Is.EqualTo(CustomerStage.Enter));
            Vector3 start = _customer.transform.position; _service.Advance(0.1); Assert.That(_customer.transform.position, Is.Not.EqualTo(start));
            Assert.That(_service.Visit, Is.SameAs(first)); _service.Advance(100); Assert.That(first.Stage, Is.EqualTo(CustomerStage.Order));
            _service.Advance(1); Assert.That(first.Stage, Is.EqualTo(CustomerStage.Wait));
            DishItem dish = FinalDish(); Assert.That(_service.TryDeliver(dish), Is.True);
            _service.Advance(10); Assert.That(first.Stage, Is.EqualTo(CustomerStage.Leave)); _service.Advance(100);
            Assert.That(first.Stage, Is.EqualTo(CustomerStage.Finished)); Assert.That(_service.Visit, Is.Null); Assert.That(_customer.gameObject.activeSelf, Is.False);
            Assert.That(_service.ForceNextOrder(0), Is.True); Assert.That(_service.ForceNextOrder(99), Is.False);
            _service.Advance(2); Assert.That(_service.Visit, Is.Null); _service.Advance(1);
            Assert.That(_service.CustomerNumber, Is.EqualTo(2)); Assert.That(_service.Visit.InstanceId, Is.Not.EqualTo(first.InstanceId));
            Assert.That(_service.Visit.Order.InstanceId, Is.Not.EqualTo(first.Order.InstanceId)); Assert.That(_service.Visit.Order.Offer.Dish.Id, Is.EqualTo("dish.hamburger"));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
            _service.enabled = false; Assert.That(_service.Visit, Is.Null); Assert.That(_customer.gameObject.activeSelf, Is.False);
            _service.enabled = true; Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
        }

        [UnityTest]
        public IEnumerator PickupCookingPhysicalAssemblyFinalizationTransportAndDeliveryPreserveConcretePatty()
        {
            Ready(); AssemblySurface surface = Surface(_origin + Vector3.up * 0.9f);
            Food(_bun, _origin + Vector3.up * 1.06f);
            FoodItem patty = Food(_beef, _origin + new Vector3(2, 0.96f, 0));
            FoodItem top = Food(_bun, _origin + new Vector3(-0.85f, 1.01f, 0));
            FoodState original = patty.State; Guid pattyId = original.InstanceId;
            GrillHeatSource grill = Create("Grill", _origin + new Vector3(2, 0.9f, 0)).AddComponent<GrillHeatSource>();
            BoxCollider thermal = Create("HeatZone", _origin + new Vector3(2, 1.0f, 0)).AddComponent<BoxCollider>();
            thermal.isTrigger = true; thermal.size = new Vector3(0.8f, 0.22f, 0.8f); Set(grill, "_effectiveZone", thermal); Set(_simulation, "_heatSources", new HeatSource[] { grill });
            GameObject actor = Create("Player", _origin + new Vector3(2, 0, -1.95f));
            CharacterController capsule = actor.AddComponent<CharacterController>(); capsule.height = 1.8f; capsule.center = Vector3.up * 0.9f; capsule.radius = 0.3f;
            Transform view = Create("Camera", actor.transform.position + Vector3.up * 1.65f).transform; view.SetParent(actor.transform, true); view.LookAt(patty.transform.position);
            PhysicalCarry carry = actor.AddComponent<PhysicalCarry>(); Set(carry, "_viewTransform", view); Set(carry, "_actorRoot", actor.transform);
            Assert.That(patty.GetComponent<Pickup>().TryInteract(new InteractionContext(actor.transform, carry)), Is.True);
            _simulation.Advance(45); Assert.That(original.Cooking.Stage, Is.EqualTo(CookingStage.Cooked));
            Quaternion down = view.rotation;
            for (int frame = 0; frame < 35; frame++)
            { view.rotation = Quaternion.Slerp(down, Quaternion.identity, (frame + 1f) / 35f); yield return new WaitForFixedUpdate(); Assert.That(carry.HasHeldObject, Is.True); }
            for (int frame = 0; frame < 50; frame++)
            { actor.transform.position = _origin + new Vector3(Mathf.Lerp(2, 0, (frame + 1f) / 50f), 0, -1.95f); yield return new WaitForFixedUpdate(); Assert.That(carry.HasHeldObject, Is.True); }
            for (int frame = 0; frame < 30; frame++) yield return new WaitForFixedUpdate();
            carry.Drop(); for (int frame = 0; frame < 40; frame++) yield return new WaitForFixedUpdate();
            surface.RefreshComposition(); Assert.That(surface.Dish.State.Components.Count, Is.EqualTo(2));
            Assert.That(surface.Dish.State.Components.Any(food => ReferenceEquals(food, original)), Is.True);
            view.LookAt(top.transform.position); Assert.That(carry.TryPickUp(top.GetComponent<Pickup>()), Is.True);
            down = view.rotation;
            for (int frame = 0; frame < 35; frame++)
            { view.rotation = Quaternion.Slerp(down, Quaternion.identity, (frame + 1f) / 35f); yield return new WaitForFixedUpdate(); Assert.That(carry.HasHeldObject, Is.True); }
            for (int frame = 0; frame < 30; frame++) yield return new WaitForFixedUpdate();
            carry.Drop(); for (int frame = 0; frame < 40; frame++) yield return new WaitForFixedUpdate();
            surface.RefreshComposition(); Assert.That(surface.PreviewDefinition?.DisplayName, Is.EqualTo("Hamburger"),
                string.Join("; ", _foods.Select(food => food.Definition.Id + " " + (food.transform.position - _origin) + " member=" + surface.Dish.State.Components.Contains(food.State))));
            Assert.That(surface.TryInteract(new InteractionContext(actor.transform, carry)), Is.True);
            DishItem dish = surface.Dish; Guid? dishId = dish.State.InstanceId;
            view.LookAt(top.transform.position); Assert.That(carry.TryPickUp(dish.GetComponent<Pickup>()), Is.True);
            down = view.rotation;
            for (int frame = 0; frame < 35; frame++)
            { view.rotation = Quaternion.Slerp(down, Quaternion.identity, (frame + 1f) / 35f); yield return new WaitForFixedUpdate(); Assert.That(carry.HasHeldObject, Is.True); }
            for (int frame = 0; frame < 50; frame++)
            { actor.transform.position = _origin + new Vector3(Mathf.Lerp(0, -2, (frame + 1f) / 50f), 0, -1.95f); yield return new WaitForFixedUpdate(); Assert.That(carry.HasHeldObject, Is.True); Assert.That(_service.LastResult, Is.Null); }
            carry.Drop(); for (int frame = 0; frame < 50; frame++) yield return new WaitForFixedUpdate();
            Assert.That(_service.LastResult?.Accepted, Is.True); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
            Assert.That(_service.LastResult.Evaluation.DeliveredDish.InstanceId, Is.EqualTo(dishId));
            IngredientSnapshot deliveredPatty = _service.LastResult.Evaluation.DeliveredDish.Ingredients.Single(food => food.InstanceId == pattyId);
            Assert.That(deliveredPatty.CookingStage, Is.EqualTo(CookingStage.Cooked));
            Assert.That(deliveredPatty.FreshnessPercent, Is.EqualTo(original.FreshnessPercent));
            Assert.That(dish != null && dish.gameObject.activeInHierarchy, Is.True);
            Assert.That(_carrier.Dish, Is.SameAs(dish)); Assert.That(_simulation.Foods.Count, Is.EqualTo(3));
            Assert.That(carry.TryPickUp(dish.GetComponent<Pickup>()), Is.False);
            _service.Advance(10); _service.Advance(100); yield return null;
            Assert.That(_simulation.Foods, Is.Empty); Assert.That(dish == null, Is.True); // Native destruction at exit.
            _delivery.Poll(); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
        }
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
