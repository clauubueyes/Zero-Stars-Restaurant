using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Orders;

namespace ZeroStarRestaurant.Tests
{
    public sealed partial class M6ServiceTests
    {
        [TestCase(0, 100, false, "Raw", CustomerReaction.HealthIncident)]
        [TestCase(20, 100, false, "Undercooked", CustomerReaction.HealthIncident)]
        [TestCase(45, 100, false, "Good", CustomerReaction.Satisfied)]
        [TestCase(65, 100, false, "Overcooked", CustomerReaction.Unhappy)]
        [TestCase(90, 100, false, "Burnt", CustomerReaction.Complaint)]
        [TestCase(45, 50, false, "Stale", CustomerReaction.Unhappy)]
        [TestCase(45, 20, false, "Spoiled", CustomerReaction.HealthIncident)]
        [TestCase(45, 0, false, "Rotten", CustomerReaction.HealthIncident)]
        [TestCase(45, 100, true, "Contaminated", CustomerReaction.HealthIncident)]
        [TestCase(90, 0, true, "Rotten + Contaminated + Burnt", CustomerReaction.HealthIncident)]
        public void M15FullDishQualityReactsOnItsOwnVisitWhileM14PaysFullPrice(double dose, float freshness, bool contaminated, string quality, CustomerReaction reaction)
        {
            Ready(); var dish = FinalDish(freshness: freshness, contaminated: contaminated, dose: dose); var states = dish.State.Components.ToArray();
            Place(dish); _delivery.Poll(); var visit = _service.Visit; var consequence = visit.Consequence;
            Assert.That(consequence, Is.SameAs(_service.LastConsequence)); Assert.That(consequence.CustomerId, Is.EqualTo(visit.InstanceId));
            Assert.That(consequence.Reaction, Is.EqualTo(reaction)); Assert.That(_carrier.Dish, Is.SameAs(dish)); Assert.That(dish.State.Components, Is.EqualTo(states));
            Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(500)); Assert.That(_service.Ledger.Transactions.Single().AmountCents, Is.EqualTo(500));
            Assert.That(OrderFeedback.ConsequenceText(consequence), Does.Contain("Quality: " + quality));
            for (int i = 0; i < 10; i++) { _delivery.Poll(); _service.Advance(.05); }
            Assert.That(visit.Resolve(), Is.False); Assert.That(_service.Statistics.TryRecord(consequence), Is.False);
            Assert.That(_service.Statistics.CustomersServed, Is.EqualTo(1)); Assert.That(_service.ResultRevision, Is.EqualTo(1));
            Assert.That(_service.Statistics.HealthIncidents, Is.EqualTo(reaction == CustomerReaction.HealthIncident ? 1 : 0));
            Assert.That(_service.Statistics.Complaints, Is.EqualTo(reaction == CustomerReaction.Complaint ? 1 : 0));
            Assert.That(_service.Ledger.Transactions.Count(t => t.Category == LedgerCategory.Sales), Is.EqualTo(1));
        }

        [TestCase("patty", 20, CustomerReaction.HealthIncident, 200)]
        [TestCase("patty,bun", 45, CustomerReaction.Satisfied, 350)]
        [TestCase("patty,bun", 90, CustomerReaction.Complaint, 350)]
        [TestCase("bun", 0, CustomerReaction.Satisfied, 150)]
        public void M15LoosePartialFoodUsesTheSameSafetyEvidenceAndPartialPayment(string composition, double dose, CustomerReaction reaction, int payment)
        {
            Ready(); var foods = LooseOnPad(composition);
            foreach (var food in foods.Where(f => f.State.Cooking != null))
            { food.State.SetTemperature(120); food.State.Advance(dose, new ThermalEnvironment(120, allowsCooking: true), 0); }
            _delivery.Poll(); var consequence = _service.Visit.Consequence;
            Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(payment)); Assert.That(consequence.Reaction, Is.EqualTo(reaction));
            Assert.That(consequence.Quality.Delivery.Ingredients.Select(f => f.InstanceId), Is.EqualTo(foods.Select(f => f.State.InstanceId)));
            Assert.That(_carrier.Foods, Is.EquivalentTo(foods)); Assert.That(_service.Statistics.CustomersServed, Is.EqualTo(1));
        }

        [Test]
        public void M15IncompleteFinalDishHasIndependentQualityAndCompleteness()
        {
            Ready(); var dish = FinalDish(custom: true, dose: 65); Place(dish); _delivery.Poll();
            Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(350)); Assert.That(_service.LastResult.Evaluation.Satisfaction.IsComplete, Is.False);
            Assert.That(_service.Visit.Consequence.Reaction, Is.EqualTo(CustomerReaction.Unhappy)); Assert.That(_carrier.Dish, Is.SameAs(dish));
        }

        [Test]
        public void M15RejectedDangerousFoodDoesNotCountOrReactAndLaterVisitOwnsTheReaction()
        {
            Ready(0); var cheese = LooseOnPad("cheese")[0]; cheese.State.Contaminate(); var rejected = _service.Visit;
            _delivery.Poll(); Assert.That(_service.LastResult.Accepted, Is.False); Assert.That(rejected.Consequence, Is.Null);
            Assert.That(_service.LastConsequence, Is.Null); Assert.That(_service.Statistics.CustomersServed, Is.Zero);
            _service.ForceNextOrder(1); _service.Advance(10); _service.Advance(100); _service.Advance(3); _service.Advance(100); _service.Advance(1);
            _delivery.Poll(); _delivery.Poll();
            Assert.That(_service.Visit.InstanceId, Is.Not.EqualTo(rejected.InstanceId)); Assert.That(rejected.Consequence, Is.Null);
            Assert.That(_service.Visit.Consequence.CustomerId, Is.EqualTo(_service.Visit.InstanceId));
            Assert.That(_service.Statistics.HealthIncidents, Is.EqualTo(1)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(150));
        }

        [Test]
        public void M15StatisticsAccumulateAcrossCustomersAndSurviveServiceDisableEnable()
        {
            Ready(0); var first = FinalDish(); Place(first); _delivery.Poll(); var firstConsequence = _service.LastConsequence;
            _service.Advance(10); _service.Advance(100); _service.ForceNextOrder(1);
            _service.Advance(3); _service.Advance(100); _service.Advance(1);
            Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
            var second = FinalDish(cheese: true, dose: 90); Place(second); _delivery.Poll();
            var statistics = _service.Statistics;
            Assert.That(statistics.CustomersServed, Is.EqualTo(2)); Assert.That(statistics.Satisfied, Is.EqualTo(1)); Assert.That(statistics.Complaints, Is.EqualTo(1));
            Assert.That(statistics.History[0], Is.SameAs(firstConsequence)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1150));
            _service.enabled = false; _service.enabled = true;
            Assert.That(_service.Statistics, Is.SameAs(statistics)); Assert.That(statistics.CustomersServed, Is.EqualTo(2));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1150));
        }

        [Test]
        public void M15VisibleFeedbackShowsEveryProblemReactionAndCumulativeStatistics()
        {
            Ready(); var dish = FinalDish(dose: 90, contaminated: true, freshness: 0); Place(dish); _delivery.Poll();
            var feedback = Create("Feedback", _origin).AddComponent<OrderFeedback>(); Set(feedback, "_service", _service);
            Assert.That(feedback.LastDeliveryMessage, Does.Contain("Ordered: Hamburger").And.Contain("Paid €5.00").And.Contain("Burnt").And.Contain("Rotten").And.Contain("Contaminated").And.Contain("Reaction: Health Incident"));
            Assert.That(feedback.Text, Does.Contain("Customers served: 1").And.Contain("Health incidents: 1").And.Contain("health hazard"));
        }

        [UnityTest]
        public IEnumerator M15SameBurntDishAndReactionPersistThroughNativePhysicsUntilExit()
        {
            Ready(); var dish = FinalDish(dose: 90); var original = dish.State.Components.ToArray(); Place(dish); _delivery.Poll();
            var visit = _service.Visit; var consequence = visit.Consequence; _service.Advance(10);
            for (int step = 0; step < 100 && visit.Stage != CustomerStage.Finished; step++)
            {
                _service.Advance(.1); yield return new WaitForFixedUpdate();
                if (visit.Stage == CustomerStage.Finished) break;
                Assert.That(_carrier.Dish, Is.SameAs(dish)); Assert.That(dish.State.Components, Is.EqualTo(original));
                Assert.That(dish.transform.IsChildOf(_customer.transform), Is.True);
                Assert.That(visit.Consequence, Is.SameAs(consequence)); Assert.That(_service.Statistics.Complaints, Is.EqualTo(1));
            }
            Assert.That(visit.Stage, Is.EqualTo(CustomerStage.Finished)); yield return null;
            Assert.That(dish == null, Is.True); Assert.That(_simulation.Foods, Is.Empty);
            Assert.That(_service.LastConsequence, Is.SameAs(consequence)); Assert.That(visit.Consequence, Is.SameAs(consequence));
            Assert.That(consequence.Quality.Delivery.Ingredients.Select(f => f.InstanceId), Is.EqualTo(original.Select(f => f.InstanceId)));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(500)); Assert.That(_service.Statistics.Complaints, Is.EqualTo(1));
        }
    }
}
