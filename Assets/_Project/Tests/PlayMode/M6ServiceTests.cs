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
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Orders;
using Object = UnityEngine.Object;

namespace ZeroStarRestaurant.Tests
{
    public sealed partial class M6ServiceTests
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
            var first = new CustomerServiceConfiguration.MenuEntry(); Set(first, "_dish", _hamburger); Set(first, "_salePriceCents", 500); Set(first, "_ingredientWeights", new[] { 3, 4, 3 });
            var second = new CustomerServiceConfiguration.MenuEntry(); Set(second, "_dish", _cheeseburger); Set(second, "_salePriceCents", 650); Set(second, "_ingredientWeights", new[] { 3, 4, 3, 3 });
            Set(config, "_menu", new[] { first, second });
            GameObject loop = Create("Service", _origin); loop.SetActive(false); _service = loop.AddComponent<CustomerServiceLoop>();
            Set(_service, "_configuration", config); Set(_service, "_customer", _customer); Set(_service, "_dishCarrier", _carrier); Set(_service, "_foodSimulation", _simulation);
            loop.SetActive(true);
            GameObject padObject = Create("DeliveryPad", _origin + new Vector3(-2, 0.915f, 0));
            _pad = padObject.AddComponent<BoxCollider>(); _pad.size = new Vector3(1.25f, 0.03f, 0.9f);
            GameObject delivery = Create("Delivery", _origin + new Vector3(-2, 1.45f, 0)); delivery.SetActive(false);
            BoxCollider sensor = delivery.AddComponent<BoxCollider>(); sensor.isTrigger = true; sensor.size = new Vector3(1.25f, 1.04f, 0.9f);
            _delivery = delivery.AddComponent<DeliveryZone>(); Set(_delivery, "_zone", sensor); Set(_delivery, "_support", _pad); Set(_delivery, "_service", _service);
            Set(_service, "_deliveryZone", _delivery);
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
        private void ReadyIrrelevantOrder()
        {
            Set(_service, "_offers", new[] { new OrderOffer(_hamburger.CreateProfile(), 500),
                new OrderOffer(new DishProfile("dish.cheeseplate", "Cheese plate", new[] { "food.cheese" }), 150) });
            Ready(1);
        }
        private void Ready(int force = -1)
        {
            if (force >= 0) Assert.That(_service.ForceNextOrder(force), Is.True);
            _service.Advance(1); _service.Advance(100); _service.Advance(1);
            Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
        }
        private void Place(DishItem dish)
        {
            PlaceAt(dish, Vector2.zero);
            Collider[] hits = Physics.OverlapBox(_delivery.transform.position, _delivery.GetComponent<BoxCollider>().size * 0.5f,
                Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            Assert.That(hits.Any(hit => hit.GetComponentInParent<DishItem>() == dish), Is.True,
                "Delivery must see the actual proxy. " + dish.GetComponent<BoxCollider>().bounds + " at " + _delivery.transform.position);
            Assert.That(dish.IsIntact, Is.True);
        }
        private void PlaceAt(DishItem dish, Vector2 offset, float yaw = 0f)
        {
            Rigidbody body = dish.GetComponent<Rigidbody>(); body.interpolation = RigidbodyInterpolation.None;
            dish.transform.SetPositionAndRotation(_pad.transform.position + new Vector3(offset.x, 1f, offset.y), Quaternion.Euler(0f, yaw, 0f));
            Physics.SyncTransforms(); dish.transform.position += Vector3.up * (_pad.bounds.max.y - dish.GetComponent<BoxCollider>().bounds.min.y);
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; Physics.SyncTransforms();
            Assert.That(dish.GetComponent<BoxCollider>().bounds.min.y, Is.EqualTo(_pad.bounds.max.y).Within(0.001f));
        }

        [TestCase(0f, 0f, true)]
        [TestCase(0.625f, 0f, true)]
        [TestCase(-0.625f, 0f, true)]
        [TestCase(0f, 0.45f, true)]
        [TestCase(0f, -0.45f, true)]
        [TestCase(0.78f, 0f, true)]
        [TestCase(-0.78f, 0f, true)]
        [TestCase(0f, 0.6f, true)]
        [TestCase(0f, -0.6f, true)]
        [TestCase(0.66f, 0.47f, true)]
        [TestCase(0.95f, 0f, false)]
        [TestCase(0.9f, 0.75f, false)]
        [TestCase(1.1f, 0f, false)]
        [TestCase(0f, 0.9f, false)]
        public void DeliveryUsesReasonableDishOverlapAtCenterEdgesAndOutside(float x, float z, bool expected)
        {
            Ready(); DishItem dish = FinalDish(); PlaceAt(dish, new Vector2(x, z));
            _delivery.Poll(); _delivery.Poll();
            Assert.That(_service.Visit.Order.IsCompleted, Is.EqualTo(expected));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(expected ? 500 : 0));
            if (!expected) Assert.That(dish.gameObject.activeInHierarchy && dish.GetComponent<Pickup>().enabled, Is.True);
        }

        [TestCase(false, false, 0.5f)]
        [TestCase(false, false, 1f)]
        [TestCase(false, false, 2f)]
        [TestCase(true, false, 0.5f)]
        [TestCase(true, false, 1f)]
        [TestCase(true, false, 2f)]
        [TestCase(false, true, 0.5f)]
        [TestCase(false, true, 1f)]
        [TestCase(false, true, 2f)]
        public void PartialPlacementScalesWithHamburgerCheeseburgerAndCustom(bool cheese, bool custom, float scale)
        {
            Ready(cheese ? 1 : 0); DishItem dish = FinalDish(cheese, custom: custom);
            dish.transform.localScale = Vector3.one * scale; Physics.SyncTransforms();
            PlaceAt(dish, new Vector2(_pad.bounds.extents.x + dish.GetComponent<BoxCollider>().bounds.extents.x * 0.3f, 0f));
            _delivery.Poll(); _delivery.Poll();
            Assert.That(_service.Visit.Order.IsCompleted, Is.True); Assert.That(_service.LastResult.Accepted, Is.True);
            Assert.That(_service.LastResult.Evaluation.CorrectOrder, Is.EqualTo(!custom));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(custom ? cheese ? 500 : 350 : cheese ? 650 : 500));
            Assert.That(_carrier.Dish, Is.SameAs(dish)); Assert.That(dish.State.IsSold, Is.True);
        }

        [TestCase(0.8f, 0f, 45f, true)]
        [TestCase(0f, 0.6f, 90f, true)]
        [TestCase(1.2f, 0.8f, 45f, false)]
        public void RotatedAggregateUsesPhysicalVolumeInsteadOfItsCenter(float x, float z, float yaw, bool expected)
        {
            Ready(); DishItem dish = FinalDish(); PlaceAt(dish, new Vector2(x, z), yaw); _delivery.Poll();
            Assert.That(_service.Visit.Order.IsCompleted, Is.EqualTo(expected));
        }

        [Test]
        public void PartialOverlapStillRejectsAnAirborneOrFastPassingDish()
        {
            Ready(); DishItem dish = FinalDish(); PlaceAt(dish, new Vector2(0.78f, 0f));
            Rigidbody body = dish.GetComponent<Rigidbody>(); body.linearVelocity = Vector3.left;
            _delivery.Poll(); Assert.That(_service.Visit.Order.IsCompleted, Is.False);
            body.linearVelocity = Vector3.zero; body.position += Vector3.up * 0.2f; Physics.SyncTransforms();
            _delivery.Poll(); Assert.That(_service.Visit.Order.IsCompleted, Is.False);
            PlaceAt(dish, new Vector2(0.78f, 0f)); _delivery.Poll(); Assert.That(_service.LastResult.Accepted, Is.True);
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
        public void DisabledDishProxyCannotCompletePaymentWithoutTransfer()
        {
            Ready(); DishItem dish = FinalDish(); Place(dish);
            BoxCollider proxy = dish.GetComponent<BoxCollider>(); proxy.enabled = false;
            Assert.That(_service.TryDeliver(dish), Is.False);
            Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
            Assert.That(_service.Ledger.BalanceCents, Is.Zero); Assert.That(dish.State.IsSold, Is.False);
            Assert.That(_carrier.Dish, Is.Null);
            proxy.enabled = true; _delivery.Poll();
            Assert.That(_service.LastResult.Accepted, Is.True); Assert.That(_carrier.Dish, Is.SameAs(dish));
        }

        [Test]
        public void FailedPaymentLeavesDishAvailableAndCarrierEmpty()
        {
            Ready(); DishItem dish = FinalDish(); Place(dish);
            var ledger = new PaymentLedger(long.MaxValue);
            Assert.That(_carrier.TryReceive(dish, _service.Visit.Order, ledger, out OrderResult result), Is.False);
            Assert.That(result, Is.Null); Assert.That(_carrier.Dish, Is.Null);
            Assert.That(_service.Visit.Order.IsCompleted, Is.False); Assert.That(dish.State.IsSold, Is.False);
            Assert.That(dish.GetComponent<Pickup>().enabled, Is.True);
            Assert.That(dish.GetComponent<Rigidbody>().isKinematic, Is.False);
            Assert.That(dish.GetComponent<BoxCollider>().enabled, Is.True);
            _delivery.Poll(); Assert.That(_carrier.Dish, Is.SameAs(dish));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
        }

        [Test]
        public void RejectionKeepsDishAndDoesNotBlockTheNextMatchingCustomer()
        {
            ReadyIrrelevantOrder(); DishItem dish = FinalDish(); Place(dish); _delivery.Poll();
            Assert.That(_service.LastResult.Accepted, Is.False); Assert.That(dish.gameObject.activeSelf, Is.True);
            Assert.That(_carrier.Dish, Is.Null); Assert.That(dish.GetComponent<Pickup>().enabled, Is.True);
            Assert.That(dish.State.IsSold, Is.False); Assert.That(_simulation.Foods.Count, Is.EqualTo(3));
            Assert.That(_service.Ledger.BalanceCents, Is.Zero);
            Assert.That(_service.ForceNextOrder(0), Is.True);
            _service.Advance(10); _service.Advance(100); _service.Advance(3); _service.Advance(100); _service.Advance(1);
            Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
            _delivery.Poll(); Assert.That(_service.Visit.Order.IsCompleted, Is.True);
            Assert.That(_carrier.Dish, Is.SameAs(dish));
            _delivery.Poll(); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
        }

        [Test]
        public void EachOrderEvaluatesTheSameRejectedDishOnceAndOnlyTheMatchingCustomerPays()
        {
            ReadyIrrelevantOrder(); DishItem dish = FinalDish(); Place(dish);
            var originalDishId = dish.State.InstanceId;
            var orderIds = new HashSet<Guid>();
            var feedback = _service.gameObject.AddComponent<OrderFeedback>(); Set(feedback, "_service", _service);
            for (int index = 0; index < 3; index++)
            {
                if (index > 0)
                {
                    Assert.That(_service.ForceNextOrder(index == 2 ? 0 : 1), Is.True);
                    _service.Advance(10); _service.Advance(100); _service.Advance(3); _service.Advance(100); _service.Advance(1);
                    Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
                }
                Assert.That(orderIds.Add(_service.Visit.Order.InstanceId), Is.True);
                _delivery.Poll();
                OrderResult result = _service.LastResult;
                Assert.That(result.Accepted, Is.EqualTo(index == 2));
                Assert.That(result.Evaluation.DeliveredDish.InstanceId, Is.EqualTo(originalDishId));
                Assert.That(feedback.ResultRevision, Is.EqualTo(index + 1));
                Assert.That(feedback.LastDeliveryMessage, Does.Contain(index == 2 ? "DELIVERY ACCEPTED" : "DELIVERY REJECTED"));
                Assert.That(feedback.LastDeliveryMessage, Does.Contain(index == 2 ? "Paid" : "Ordered: Cheese plate"));
                for (int repeat = 0; repeat < 20; repeat++) _delivery.Poll();
                Assert.That(_service.LastResult, Is.SameAs(result));
                Assert.That(feedback.ResultRevision, Is.EqualTo(index + 1));
                Assert.That(_service.Ledger.Transactions.Count(transaction => transaction.Category == LedgerCategory.Sales), Is.EqualTo(index == 2 ? 1 : 0));
            }
            Assert.That(_carrier.Dish, Is.SameAs(dish)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
        }

        [Test]
        public void IndividualFoodBoxesDraftDishesAndFastOrAirborneFinalDishesAreIgnored()
        {
            Ready(); Food(_beef, _pad.transform.position + Vector3.up * 0.3f);
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
        [TestCase(100, false, 45, true, true)]
        public void RuntimeEvaluationKeepsSafetySeparateAndIncompleteCustomDishReceivesPartialPayment(float freshness, bool contaminated, double dose, bool custom, bool accepted)
        {
            Ready(); DishItem dish = FinalDish(freshness: freshness, contaminated: contaminated, dose: dose, custom: custom); Place(dish); _delivery.Poll();
            Assert.That(_service.LastResult.Accepted, Is.EqualTo(accepted));
            Assert.That(_service.LastResult.Evaluation.DeliveredDish.ContainsContamination, Is.EqualTo(contaminated));
            Assert.That(_service.LastResult.Evaluation.DeliveredDish.MinimumFreshnessPercent, Is.EqualTo(freshness));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(custom ? 350 : 500));
            Assert.That(OrderFeedback.ResultText(_service.LastResult), Does.Contain("Correct order: " + (!custom ? "YES" : "NO")));
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
            DishItem dish = FinalDish(); Assert.That(_service.TryDeliver(dish), Is.False);
            Place(dish); _delivery.Poll(); Assert.That(first.Order.IsCompleted, Is.True);
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
        [UnityTest]
        public IEnumerator DeliveryFromKitchenFacingCustomerCarriesOriginalsThroughPhysics() =>
            DeliverWithoutPlayerDependency(new Vector3(0, 0, 1.8f), 180f);

        [UnityTest]
        public IEnumerator DeliveryFromLeftLookingSidewaysCarriesOriginalsThroughPhysics() =>
            DeliverWithoutPlayerDependency(new Vector3(-1.8f, 0, 0), 270f);

        [UnityTest]
        public IEnumerator DeliveryFromRightLookingAwayCarriesOriginalsThroughPhysics() =>
            DeliverWithoutPlayerDependency(new Vector3(1.8f, 0, 0), 90f);

        [UnityTest]
        public IEnumerator DeliveryWithoutLookingAtCustomerCarriesOriginalsThroughPhysics() =>
            DeliverWithoutPlayerDependency(new Vector3(0, 0, 1.8f), 0f);

        [UnityTest] public IEnumerator DishWithPlateFromFrontCarriesSameUtensilToExit() =>
            DeliverWithoutPlayerDependency(new Vector3(0, 0, 1.8f), 180f, true);
        [UnityTest] public IEnumerator DishWithPlateFromLeftCarriesSameUtensilToExit() =>
            DeliverWithoutPlayerDependency(new Vector3(-1.8f, 0, 0), 270f, true);
        [UnityTest] public IEnumerator DishWithPlateFromRightCarriesSameUtensilToExit() =>
            DeliverWithoutPlayerDependency(new Vector3(1.8f, 0, 0), 90f, true);
        [UnityTest] public IEnumerator DishWithPlateWithoutLookingAtCustomerCarriesSameUtensilToExit() =>
            DeliverWithoutPlayerDependency(new Vector3(0, 0, 1.8f), 0f, true);

        [UnityTest] public IEnumerator FarPlayerCannotAffectDishDeliveryOrExit() =>
            DeliverWithoutPlayerDependency(new Vector3(40, 0, 40), 90f, leaveImmediately: true);
        [UnityTest] public IEnumerator FarPlayerCannotAffectPlateDeliveryOrExit() =>
            DeliverWithoutPlayerDependency(new Vector3(40, 0, 40), 90f, true, true);
        [UnityTest] public IEnumerator PlayerBehindCustomerCannotAffectDishDeliveryOrExit() =>
            DeliverWithoutPlayerDependency(new Vector3(0, 0, -6), 180f, leaveImmediately: true);
        [UnityTest] public IEnumerator PlayerBehindCustomerCannotAffectPlateDeliveryOrExit() =>
            DeliverWithoutPlayerDependency(new Vector3(0, 0, -6), 180f, true, true);
        [UnityTest] public IEnumerator PlayerLeavingDeliveryImmediatelyAfterReleaseCannotCancelHandoff() =>
            DeliverWithoutPlayerDependency(new Vector3(-20, 0, 10), 270f, leaveImmediately: true);
        [UnityTest] public IEnumerator PlateHandoffSurvivesPlayerLeavingAndScaledCustomer() =>
            DeliverWithoutPlayerDependency(new Vector3(-20, 0, 10), 270f, true, true, true);

        [Test]
        public void DirectServiceApiCannotBypassPadOrItsPlacementRetryPolicy()
        {
            ReadyIrrelevantOrder(); DishItem dish = FreeDish(false);
            Assert.That(_service.TryDeliver(dish), Is.False, "A workbench is not a delivery pad.");
            Assert.That(_service.LastResult, Is.Null); Assert.That(_service.Ledger.BalanceCents, Is.Zero);
            PlaceAt(dish, new Vector2(2, 0)); Assert.That(_service.TryDeliver(dish), Is.False);
            Place(dish); Assert.That(_service.TryDeliver(dish), Is.True);
            Assert.That(_service.LastResult.Accepted, Is.False); var result = _service.LastResult;
            Assert.That(_service.TryDeliver(dish), Is.False); Assert.That(_service.LastResult, Is.SameAs(result));
            Assert.That(dish.GetComponent<Pickup>().enabled, Is.True); Assert.That(dish.State.IsSold, Is.False);
        }

        [TestCase(false)] [TestCase(true)]
        public void MovingTheGreenPadWithoutMovingTheOldTriggerKeepsItTheOnlySpatialReference(bool withPlate)
        {
            Ready(0); DishItem dish = FreeDish(withPlate);
            _pad.transform.position += new Vector3(3, 0, 2);
            _pad.transform.rotation = Quaternion.Euler(0, 40, 0);
            PlaceAt(dish, Vector2.zero); _delivery.Poll();
            Assert.That(_service.LastResult.Accepted, Is.True); Assert.That(_carrier.Dish, Is.SameAs(dish));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
        }

        [Test]
        public void UnconfirmedHamburgerOnTheGreenPadDeliversWithoutFinalization()
        {
            Ready(0); Vector3 at = _pad.bounds.center; float top = _pad.bounds.max.y;
            FoodItem bottom = Food(_bun, new Vector3(at.x, top + .11f, at.z));
            FoodItem patty = Food(_beef, new Vector3(at.x, top + .28f, at.z));
            FoodItem upper = Food(_bun, new Vector3(at.x, top + .45f, at.z));
            _delivery.Poll(); _delivery.Poll();
            Assert.That(_service.LastResult.Accepted, Is.True); Assert.That(_carrier.Dish, Is.Null);
            Assert.That(_carrier.Foods, Is.EquivalentTo(new[] { bottom, patty, upper }));
            Assert.That(_service.LastResult.Evaluation.CorrectOrder, Is.True);
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
        }

        [Test]
        public void SupportedDishShowsTheActualReasonWhileItCannotBeDelivered()
        {
            Ready(); DishItem dish = FinalDish(); Place(dish);
            Rigidbody body = dish.GetComponent<Rigidbody>(); body.linearVelocity = Vector3.right;
            _delivery.Poll(); Assert.That(_delivery.PlacementMessage, Does.Contain("settle"));
            body.linearVelocity = Vector3.zero; body.position += Vector3.up * .2f; Physics.SyncTransforms();
            _delivery.Poll(); Assert.That(_delivery.PlacementMessage, Does.Contain("rest the dish"));
            Assert.That(_service.LastResult, Is.Null); Assert.That(_service.Ledger.BalanceCents, Is.Zero);
            Place(dish); _delivery.Poll();
            Assert.That(_service.LastResult.Accepted, Is.True); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
        }

        private DishItem FreeDish(bool withPlate)
        {
            float baseHeight = .9f;
            if (withPlate)
            {
                GameObject utensil = Create("Plate", _origin + Vector3.up * .9225f);
                utensil.AddComponent<BoxCollider>().size = new Vector3(.6f, .045f, .6f);
                utensil.AddComponent<PlateItem>(); baseHeight += .045f;
            }
            Food(_bun, _origin + Vector3.up * (baseHeight + .11f));
            Food(_beef, _origin + Vector3.up * (baseHeight + .28f));
            FoodItem top = Food(_bun, _origin + Vector3.up * (baseHeight + .45f));
            var assembly = Create("Physical Assembly", _origin).AddComponent<PhysicalDishAssembly>();
            Set(assembly, "_simulation", _simulation); Set(assembly, "_definitions", new[] { _hamburger, _cheeseburger });
            Physics.SyncTransforms();
            Assert.That(assembly.TryFinalize(top, out DishItem dish), Is.True); _objects.Add(dish.gameObject);
            Assert.That(dish.State.DisplayName, Is.EqualTo("Hamburger")); Assert.That(dish.Plate != null, Is.EqualTo(withPlate));
            return dish;
        }

        private IEnumerator DeliverWithoutPlayerDependency(Vector3 playerOffset, float yaw, bool withPlate = false,
            bool leaveImmediately = false, bool scaledCustomer = false)
        {
            Set(_service, "_advanceAutomatically", false);
            if (scaledCustomer) _customer.transform.localScale = Vector3.one * 1.2f;
            Ready(); DishItem dish = FreeDish(withPlate);
            Vector3 originalWorldScale = dish.transform.lossyScale;
            PlateItem plate = dish.Plate; Guid plateId = plate != null ? plate.InstanceId : Guid.Empty;
            Assert.That(dish.State.InstanceId.Value, Is.Not.EqualTo(plateId), "Plate is never the order identity.");
            Vector3 plateLocalPosition = plate != null ? dish.transform.InverseTransformPoint(plate.transform.position) : Vector3.zero;
            FoodItem[] ingredients = dish.GetComponentsInChildren<FoodItem>();
            Vector3[] localPositions = ingredients.Select(food => dish.transform.InverseTransformPoint(food.transform.position)).ToArray();
            Quaternion[] localRotations = ingredients.Select(food => Quaternion.Inverse(dish.transform.rotation) * food.transform.rotation).ToArray();
            FoodState[] states = ingredients.Select(food => food.State).ToArray();
            foreach (FoodItem food in ingredients) food.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
            Vector3 pickupOffset = leaveImmediately ? new Vector3(0, 0, 1.8f) : playerOffset;
            GameObject actor = Create("Player", _pad.transform.position + pickupOffset - Vector3.up * _pad.transform.position.y);
            Transform view = Create("Camera", actor.transform.position + Vector3.up * 1.65f).transform;
            view.SetParent(actor.transform, true); view.rotation = Quaternion.Euler(0, yaw, 0);
            if (yaw == 0f)
                Assert.That(Vector3.Dot(view.forward, (_customer.transform.position - view.position).normalized), Is.LessThan(0f),
                    "The customer is behind the camera throughout pickup/release and detection.");
            PhysicalCarry carry = actor.AddComponent<PhysicalCarry>(); Set(carry, "_viewTransform", view); Set(carry, "_actorRoot", actor.transform);
            Place(dish);
            Assert.That(carry.TryPickUp(dish.GetComponent<Pickup>()), Is.True);
            _delivery.Poll(); Assert.That(_service.LastResult, Is.Null, "Holding a dish never submits it.");
            carry.ReleaseFromMouse();
            if (leaveImmediately)
            {
                actor.transform.position = _pad.transform.position + playerOffset;
                view.rotation = Quaternion.Euler(0, yaw + 180, 0);
                carry.enabled = false;
            }
            for (int step = 0; step < 50 && _service.LastResult == null; step++) yield return new WaitForFixedUpdate();
            Assert.That(_service.LastResult?.Accepted, Is.True, "Delivery depends on the released Dish and zone, including looking away.");
            var visit = _service.Visit;
            Assert.That(_carrier.Dish, Is.SameAs(dish));
            Assert.That(Vector3.Distance(dish.transform.lossyScale, originalWorldScale), Is.LessThan(.001f));
            // Move and turn the player throughout result/exit, also processing native physics/render frames.
            for (int step = 0; step < 300 && visit.Stage != CustomerStage.Finished; step++)
            {
                actor.transform.position += new Vector3(.02f, 0, .01f); view.Rotate(17f, 41f, 0f);
                _service.Advance(.05);
                yield return new WaitForFixedUpdate(); yield return null;
                if (visit.Stage == CustomerStage.Finished) break;
                Assert.That(_carrier.Dish, Is.SameAs(dish)); Assert.That(dish.IsIntact, Is.True);
                Assert.That(dish.transform.IsChildOf(_customer.transform), Is.True);
                if (withPlate)
                {
                    Assert.That(dish.Plate, Is.SameAs(plate)); Assert.That(plate.InstanceId, Is.EqualTo(plateId));
                    Assert.That(Vector3.Distance(plate.transform.position, dish.transform.TransformPoint(plateLocalPosition)), Is.LessThan(.002f));
                    Assert.That(Vector3.Distance(plate.GetComponent<Rigidbody>().position, plate.transform.position), Is.LessThan(.002f));
                    Assert.That(carry.TryPickUp(plate.GetComponent<Pickup>()), Is.False);
                }
                Assert.That(Quaternion.Angle(dish.transform.localRotation, Quaternion.identity), Is.LessThan(.1f));
                Assert.That(Vector3.Distance(dish.GetComponent<Rigidbody>().position, dish.transform.position), Is.LessThan(.002f));
                Assert.That(Vector3.Distance(dish.transform.localPosition,
                    Vector3.up * (dish.GetComponent<BoxCollider>().size.y * .5f - dish.GetComponent<BoxCollider>().center.y)), Is.LessThan(.001f));
                for (int i = 0; i < ingredients.Length; i++)
                {
                    Assert.That(ingredients[i].State, Is.SameAs(states[i]));
                    Assert.That(Vector3.Distance(ingredients[i].transform.position, dish.transform.TransformPoint(localPositions[i])), Is.LessThan(.002f), "Original ingredient follows the carrier after physics and turns.");
                    Assert.That(Quaternion.Angle(ingredients[i].transform.rotation, dish.transform.rotation * localRotations[i]), Is.LessThan(.1f));
                    Assert.That(Vector3.Distance(ingredients[i].GetComponent<Rigidbody>().position, ingredients[i].transform.position), Is.LessThan(.002f));
                }
                Assert.That(carry.TryPickUp(dish.GetComponent<Pickup>()), Is.False);
                Assert.That(_service.TryDeliver(dish), Is.False); _delivery.Poll();
                Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
            }
            Assert.That(visit.Stage, Is.EqualTo(CustomerStage.Finished));
            yield return null;
            Assert.That(dish == null, Is.True); Assert.That(ingredients.All(food => food == null), Is.True);
            Assert.That(plate == null, Is.True, "Optional utensil is cleaned with the same Dish at Exit.");
            Assert.That(_simulation.Foods, Is.Empty); Assert.That(_carrier.Dish, Is.Null);
            Assert.That(_customer.gameObject.activeSelf, Is.False); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
        }

        [UnityTest]
        public IEnumerator DishAssembledDirectlyOnWorkbenchUsesTheSameDeliveryPaymentAndExitPipeline()
        {
            Ready(0);
            FoodItem bottom = Food(_bun, _origin + Vector3.up * 1.01f);
            FoodItem patty = Food(_beef, _origin + Vector3.up * 1.18f);
            FoodItem top = Food(_bun, _origin + Vector3.up * 1.35f);
            var assembly = Create("Physical Assembly", _origin).AddComponent<PhysicalDishAssembly>();
            Set(assembly, "_simulation", _simulation); Set(assembly, "_definitions", new[] { _hamburger, _cheeseburger });
            Assert.That(assembly.TryFinalize(top, out DishItem dish), Is.True); _objects.Add(dish.gameObject);
            Assert.That(dish.State.DisplayName, Is.EqualTo("Hamburger"));
            FoodState[] originals = { bottom.State, patty.State, top.State };
            Assert.That(dish.State.Components, Is.EqualTo(originals));
            var dishId = dish.State.InstanceId;
            Place(dish); _delivery.Poll(); _delivery.Poll();
            Assert.That(_service.LastResult.Accepted, Is.True); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
            Assert.That(_service.LastResult.Evaluation.DeliveredDish.InstanceId, Is.EqualTo(dishId));
            Assert.That(_service.LastResult.Evaluation.DeliveredDish.Ingredients.Select(food => food.InstanceId),
                Is.EqualTo(originals.Select(food => food.InstanceId)));
            Assert.That(_carrier.Dish, Is.SameAs(dish)); Assert.That(_simulation.Foods.Count, Is.EqualTo(3));
            _service.Advance(10); _service.Advance(100); yield return null;
            Assert.That(dish == null, Is.True); Assert.That(_simulation.Foods, Is.Empty);
            _delivery.Poll(); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500));
        }

        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
