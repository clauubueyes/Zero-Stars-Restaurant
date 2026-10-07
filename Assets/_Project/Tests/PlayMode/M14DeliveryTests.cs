using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Orders;

namespace ZeroStarRestaurant.Tests
{
    public sealed partial class M6ServiceTests
    {
        private FoodItem[] LooseOnPad(string composition, bool withPlate = false)
        {
            float top = _pad.bounds.max.y;
            if (withPlate)
            {
                GameObject plate = Create("Paid Plate", _pad.bounds.center + Vector3.up * .04f);
                plate.transform.position = new Vector3(_pad.bounds.center.x, top + .02f, _pad.bounds.center.z);
                plate.AddComponent<Rigidbody>(); plate.AddComponent<BoxCollider>().size = new Vector3(.65f, .04f, .65f);
                plate.AddComponent<Pickup>(); plate.AddComponent<PlateItem>(); top += .04f;
            }
            var result = composition.Split(',').Select(id =>
            {
                FoodDefinition definition = id == "bun" ? _bun : id == "patty" ? _beef : _cheese;
                FoodItem food = Food(definition, new Vector3(_pad.bounds.center.x, top + 1, _pad.bounds.center.z));
                BoxCollider box = food.GetComponent<BoxCollider>(); Rigidbody body = food.GetComponent<Rigidbody>();
                food.transform.position += Vector3.up * (top + .004f - box.bounds.min.y); Physics.SyncTransforms();
                body.position = food.transform.position; body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
                top = box.bounds.max.y; return food;
            }).ToArray();
            Physics.SyncTransforms(); return result;
        }

        [TestCase("bun,patty,bun", 0, 500)]
        [TestCase("bun,patty,cheese,bun", 1, 650)]
        [TestCase("patty,bun", 0, 350)]
        [TestCase("bun,bun", 0, 300)]
        [TestCase("patty", 0, 200)]
        [TestCase("bun", 0, 150)]
        [TestCase("bun,patty,bun,cheese", 0, 500)]
        [TestCase("bun,patty,bun", 1, 500)]
        [TestCase("cheese", 0, 0)]
        [TestCase("cheese", 1, 150)]
        public void M14LooseFoodIsEvaluatedPaidAndTransferredWithoutACompletedDish(string composition, int order, int payment)
        {
            Ready(order); FoodItem[] foods = LooseOnPad(composition); FoodState[] states = foods.Select(f => f.State).ToArray();
            _delivery.Poll(); _delivery.Poll();
            Assert.That(_service.LastResult, Is.Not.Null, _delivery.PlacementMessage);
            Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(payment)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(payment));
            Assert.That(_service.LastResult.Accepted, Is.EqualTo(payment > 0)); Assert.That(_carrier.Dish, Is.Null);
            Assert.That(foods.Select(f => f.State), Is.EqualTo(states));
            Assert.That(_service.LastResult.Evaluation.DeliveredDish.Ingredients.Select(f => f.InstanceId), Is.EqualTo(states.Select(f => f.InstanceId)));
            Assert.That(_service.ResultRevision, Is.EqualTo(1));
            Assert.That(_service.Ledger.Transactions.Count(t => t.Category == LedgerCategory.Sales), Is.EqualTo(payment > 0 ? 1 : 0));
            Assert.That(OrderFeedback.ResultText(_service.LastResult), Does.Contain("Expected:").And.Contain("Received:").And.Contain("Missing:")
                .And.Contain("Extra:").And.Contain("Base price:").And.Contain("Final payment:"));
            if (payment > 0)
            {
                Assert.That(_carrier.Foods, Is.EquivalentTo(foods));
                Assert.That(foods.All(f => f.transform.IsChildOf(_customer.transform) && !f.GetComponent<Pickup>().enabled && f.State.IsSold), Is.True);
                foreach (FoodItem food in foods) Assert.That(_service.TryDeliver(food), Is.False);
            }
            else
            {
                Assert.That(_carrier.Foods, Is.Empty); Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Reject));
                Assert.That(foods.All(f => f.GetComponent<Pickup>().enabled && !f.State.IsSold), Is.True);
            }
        }

        [Test]
        public void M14IncompleteFinalDishPaysAndCarriesItsSameIngredients()
        {
            Ready(); DishItem dish = FinalDish(custom: true); var original = dish.State.Components.ToArray(); Place(dish); _delivery.Poll();
            Assert.That(_service.LastResult.Accepted, Is.True); Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(350));
            Assert.That(_service.LastResult.Evaluation.Satisfaction.Missing, Is.EqualTo(new[] { "food.bun" }));
            Assert.That(_service.LastResult.Evaluation.Satisfaction.Extra, Is.EqualTo(new[] { "food.cheese" }));
            Assert.That(_carrier.Dish, Is.SameAs(dish)); Assert.That(dish.State.Components, Is.EqualTo(original));
        }

        [TestCase(false)] [TestCase(true)]
        public void M14FinalDishAndLooseExtraAreOneSaleAndEveryOriginalLeavesTogether(bool incomplete)
        {
            Ready(); DishItem dish = FinalDish(custom: incomplete); Place(dish);
            FoodItem extra = Food(incomplete ? _bun : _cheese, dish.GetComponent<BoxCollider>().bounds.center + Vector3.up * 2f);
            extra.transform.position += Vector3.up * (dish.GetComponent<BoxCollider>().bounds.max.y + .004f - extra.GetComponent<BoxCollider>().bounds.min.y);
            Physics.SyncTransforms(); FoodState[] originals = dish.State.Components.Concat(new[] { extra.State }).ToArray();
            _delivery.Poll(); _delivery.Poll();
            Assert.That(_service.LastResult, Is.Not.Null, _delivery.PlacementMessage);
            Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(500));
            Assert.That(_service.LastResult.Evaluation.Satisfaction.Missing, Is.Empty);
            Assert.That(_service.LastResult.Evaluation.Satisfaction.Extra, Is.EqualTo(new[] { "food.cheese" }));
            Assert.That(_carrier.Dish, Is.SameAs(dish)); Assert.That(extra.transform.IsChildOf(dish.transform), Is.True);
            Assert.That(_carrier.Foods.Select(f => f.State), Is.EquivalentTo(originals));
            Assert.That(extra.State.IsSold, Is.True); Assert.That(_service.Ledger.Transactions.Single().AmountCents, Is.EqualTo(500));
            _service.Advance(10); _service.Advance(.1);
            Assert.That(extra.transform.IsChildOf(_customer.transform), Is.True);
            _service.Advance(100); Assert.That(_simulation.Foods, Is.Empty); Assert.That(dish.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void M14PartialFoodKeepsSafetyAndTheReceiptAfterExit()
        {
            Ready(); FoodItem patty = LooseOnPad("patty")[0]; FoodState state = patty.State;
            state.Contaminate(); state.SetTemperature(120); state.Advance(900, new ZeroStarRestaurant.Cooking.ThermalEnvironment(120, allowsCooking: true));
            _delivery.Poll(); var receipt = _service.LastResult;
            Assert.That(receipt.PaymentCents, Is.EqualTo(200)); var evidence = receipt.Evaluation.DeliveredDish.Ingredients.Single();
            Assert.That(evidence.IsContaminated, Is.True); Assert.That(evidence.Condition, Is.EqualTo(FoodCondition.Rotten));
            Assert.That(evidence.CookingStage, Is.EqualTo(ZeroStarRestaurant.Cooking.CookingStage.Burnt));
            _simulation.Advance(10); Assert.That(evidence.TemperatureCelsius, Is.EqualTo(120));
            _service.Advance(10); _service.Advance(100);
            Assert.That(_simulation.Foods, Is.Empty); Assert.That(patty.gameObject.activeSelf, Is.False);
            Assert.That(_service.LastResult, Is.SameAs(receipt)); Assert.That(evidence.InstanceId, Is.EqualTo(state.InstanceId));
        }

        [Test]
        public void M14LooseSubmissionCannotBypassPadSpeedSupportSensorOrCarrier()
        {
            Ready(); FoodItem patty = Food(_beef, _origin + Vector3.up);
            Assert.That(_service.TryDeliver(patty), Is.False); Assert.That(_service.Ledger.BalanceCents, Is.Zero);
            patty.transform.position = _pad.bounds.center + Vector3.up * 1f; Physics.SyncTransforms();
            patty.transform.position += Vector3.up * (_pad.bounds.max.y + .004f - patty.GetComponent<BoxCollider>().bounds.min.y);
            Physics.SyncTransforms(); Rigidbody body = patty.GetComponent<Rigidbody>(); body.linearVelocity = Vector3.right;
            _delivery.Poll(); Assert.That(_service.LastResult, Is.Null);
            body.linearVelocity = Vector3.zero; body.position += Vector3.up * .2f; Physics.SyncTransforms();
            _delivery.Poll(); Assert.That(_service.LastResult, Is.Null);
            body.position -= Vector3.up * .2f; patty.transform.position = body.position; Physics.SyncTransforms();
            _delivery.GetComponent<BoxCollider>().enabled = false; Assert.That(_service.TryDeliver(patty), Is.False);
            _delivery.GetComponent<BoxCollider>().enabled = true; _carrier.enabled = false;
            _delivery.Poll(); Assert.That(_service.LastResult, Is.Null); Assert.That(patty.State.IsSold, Is.False);
            _carrier.enabled = true; _delivery.Poll(); Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(200));
        }

        [Test]
        public void M14NearbyFoodIsExcludedAndCancellationRetiresOnlyTheSoldOriginals()
        {
            Ready(); FoodItem patty = LooseOnPad("patty")[0]; FoodItem other = Food(_bun, _origin + Vector3.up);
            _delivery.Poll(); Assert.That(_carrier.Foods, Is.EqualTo(new[] { patty }));
            _service.enabled = false;
            Assert.That(patty.gameObject.activeSelf, Is.False); Assert.That(other.gameObject.activeSelf, Is.True);
            Assert.That(_simulation.Foods, Is.EqualTo(new[] { other })); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(200));
        }

        [Test]
        public void M14RejectedCheeseRemainsForANewRelevantOrderWithoutRepositioning()
        {
            Ready(0); FoodItem cheese = LooseOnPad("cheese")[0]; _delivery.Poll(); Assert.That(_service.LastResult.Accepted, Is.False);
            Assert.That(_service.ForceNextOrder(1), Is.True);
            _service.Advance(10); _service.Advance(100); _service.Advance(3); _service.Advance(100); _service.Advance(1);
            _delivery.Poll(); _delivery.Poll();
            Assert.That(_service.LastResult.Accepted, Is.True); Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(150));
            Assert.That(_carrier.Foods, Is.EqualTo(new[] { cheese })); Assert.That(_service.ResultRevision, Is.EqualTo(2));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(150));
        }

        [UnityTest] public IEnumerator M14PartialOriginalsFollowCustomerUntilExit() => PartialToExit(false, false);
        [UnityTest] public IEnumerator M14PartialOriginalsWithPlateFollowCustomerUntilExit() => PartialToExit(true, false);
        [UnityTest] public IEnumerator M14ScaledCustomerCarriesSamePartialObjectsUntilExit() => PartialToExit(false, true);
        [UnityTest] public IEnumerator M14ScaledCustomerCarriesSamePartialObjectsAndPlateUntilExit() => PartialToExit(true, true);
        private IEnumerator PartialToExit(bool withPlate, bool scaled)
        {
            Ready(); if (scaled) _customer.transform.localScale = Vector3.one * 1.4f;
            FoodItem[] foods = LooseOnPad("patty,bun", withPlate); FoodState[] states = foods.Select(f => f.State).ToArray();
            PlateItem plate = withPlate ? _objects.Select(o => o.GetComponent<PlateItem>()).First(p => p != null) : null;
            var player = Create("Player looking away", _origin + new Vector3(100, 0, 100)); player.transform.rotation = Quaternion.Euler(0, 180, 0);
            _delivery.Poll(); Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(350));
            if (withPlate) Assert.That(plate.transform.IsChildOf(_customer.transform), Is.True);
            var local = foods.Select(f => _customer.transform.InverseTransformPoint(f.transform.position)).ToArray();
            var scales = foods.Select(f => f.transform.lossyScale).ToArray(); var visit = _service.Visit;
            _service.Advance(10);
            for (int step = 0; step < 100 && visit.Stage != CustomerStage.Finished; step++)
            {
                _service.Advance(.1); yield return new WaitForFixedUpdate();
                if (visit.Stage == CustomerStage.Finished) break;
                for (int i = 0; i < foods.Length; i++)
                {
                    Assert.That(foods[i].State, Is.SameAs(states[i]));
                    Assert.That(Vector3.Distance(foods[i].GetComponent<Rigidbody>().position, _customer.transform.TransformPoint(local[i])), Is.LessThan(.002f));
                    Assert.That(Vector3.Distance(foods[i].transform.lossyScale, scales[i]), Is.LessThan(.002f));
                }
                Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(350));
            }
            Assert.That(visit.Stage, Is.EqualTo(CustomerStage.Finished)); yield return null;
            Assert.That(foods.All(f => f == null), Is.True); if (withPlate) Assert.That(plate == null, Is.True);
            Assert.That(_simulation.Foods, Is.Empty); Assert.That(_service.Ledger.Transactions.Single().AmountCents, Is.EqualTo(350));
        }
    }
}
