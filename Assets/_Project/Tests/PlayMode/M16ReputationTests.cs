#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Orders;
using ZeroStarRestaurant.Restaurant;

namespace ZeroStarRestaurant.Tests
{
    public sealed partial class M11RestaurantDayTests
    {
        private RestaurantReputation Reputation => Components<RestaurantReputation>().Single();
        private IEnumerator ServeM16Dish(double dose, bool contaminated = false)
        {
            Assert.That(_service.ForceNextOrder(0), Is.True); _service.Advance(.55); _service.Advance(30);
            Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
            DishItem dish = null; yield return BuildDish(new[] { 0, 1, 0 }, item => dish = item);
            var patty = dish.State.Components.Single(f => f.Cooking != null);
            if (dose > 45) patty.Advance(dose - 45, new ThermalEnvironment(120, allowsCooking: true), 0);
            if (contaminated) patty.Contaminate();
            PlaceAndPoll(dish); Assert.That(_service.LastResult.Accepted, Is.True);
        }

        [UnityTest]
        public IEnumerator M16SatisfiedUpdatesOnlyAfterExitInTheActualPreservedScene()
        {
            Reputation.Validate(); yield return ServeM16Dish(45); var consequence = _service.LastConsequence;
            Assert.That(consequence.Reaction, Is.EqualTo(CustomerReaction.Satisfied)); Assert.That(Reputation.State.Value, Is.EqualTo(50));
            _service.Advance(30); Assert.That(Reputation.State.Value, Is.EqualTo(51));
            Assert.That(Reputation.State.LastChange.Consequence, Is.SameAs(consequence));
            Assert.That(_service.Statistics.Satisfied, Is.EqualTo(1)); yield return null;
        }

        [UnityTest]
        public IEnumerator M16UnhappyAndComplaintKeepFullPaymentAndUseTheirOwnExitPenalties()
        {
            yield return ServeM16Dish(65); Assert.That(_service.LastConsequence.Reaction, Is.EqualTo(CustomerReaction.Unhappy));
            Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(500)); _service.Advance(30);
            Assert.That(Reputation.State.Value, Is.EqualTo(49)); yield return null;
            // Cancel remaining unserved queue customers, preserving ledger/reputation/history.
            _service.enabled = false; _service.enabled = true;
            yield return ServeM16Dish(90); Assert.That(_service.LastConsequence.Reaction, Is.EqualTo(CustomerReaction.Complaint));
            Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(500)); _service.Advance(30);
            Assert.That(Reputation.State.Value, Is.EqualTo(46)); Assert.That(_service.Statistics.Complaints, Is.EqualTo(1)); yield return null;
        }

        [UnityTest]
        public IEnumerator M16ContaminatedFullPaymentPersistsAfterExitAndForcedConfirmation()
        {
            yield return ServeM16Dish(45, true); var result = _service.LastResult; var consequence = _service.LastConsequence;
            var state = Reputation.State; var risk = state.HealthRisks.Single();
            Assert.That(result.PaymentCents, Is.EqualTo(500)); Assert.That(state.PendingCount, Is.EqualTo(1));
            Assert.That(state.Value, Is.EqualTo(50)); Assert.That(risk.DueWorldSeconds, Is.Null);
            Reputation.ConfirmDepartedNow(); Assert.That(state.ResolutionCount, Is.Zero, "Cannot resolve before departure.");
            _service.Advance(30); yield return null;
            Assert.That(risk.DueWorldSeconds.HasValue, Is.True); long balance = _service.Ledger.BalanceCents;
            var transactions = _service.Ledger.Transactions.ToArray(); Reputation.ConfirmDepartedNow();
            Assert.That(state.Value, Is.EqualTo(42)); Assert.That(state.PendingCount, Is.Zero); Assert.That(state.ResolutionCount, Is.EqualTo(1));
            Assert.That(state.LastResolution.Consequence, Is.SameAs(consequence));
            Assert.That(Reputation.LastResolutionMessage, Does.Contain("Customer reported illness").And.Contain("Contaminated").And.Contain("50 → 42"));
            Assert.That(Components<OrderFeedback>().Single().Text, Does.Contain("Reputation: 42").And.Contain("Pending health risks: 0"));
            Reputation.ConfirmDepartedNow(); Reputation.ResolveDepartedNow(); _day.Advance(130);
            Assert.That(state.ResolutionCount, Is.EqualTo(1)); Assert.That(_service.LastResult, Is.SameAs(result));
            Assert.That(_service.Ledger.Transactions, Is.EqualTo(transactions)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(balance));
            Assert.That(_service.Statistics.HealthIncidents, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator M16DueResolutionIsDrivenByExistingWorldTimeAndIgnoresPausedTimeAndFoodClock()
        {
            yield return ServeM16Dish(45, true); _service.Advance(30); var state = Reputation.State;
            double elapsed = _day.State.Clock.ElapsedWorldSeconds;
            _day.PauseClock(); _day.Advance(10000); _food.Advance(100000);
            Assert.That(state.ResolutionCount, Is.Zero); Assert.That(_day.State.Clock.ElapsedWorldSeconds, Is.EqualTo(elapsed));
            _day.ResumeClock(); _day.Advance(119); Assert.That(state.ResolutionCount, Is.Zero);
            _day.Advance(1.01); Assert.That(state.ResolutionCount, Is.EqualTo(1)); Assert.That(state.PendingCount, Is.Zero);
            Assert.That(state.LastResolution.Roll.HasValue, Is.True); Assert.That(state.LastResolution.Forced, Is.False);
            Assert.That(state.Value, Is.EqualTo(state.LastResolution.Roll.Value < state.HealthRisks.Single().Probability ? 42 : 50));
            Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(500));
        }

        [UnityTest]
        public IEnumerator M16RiskPendingAtClosingSurvivesNextDayAndComponentDisableEnable()
        {
            _service.ForceNextOrder(0); _service.Advance(.55); CloseClock(); _service.Advance(30);
            DishItem dish = null; yield return BuildDish(new[] { 0, 1, 0 }, item => dish = item);
            dish.State.Components.Single(f => f.Cooking != null).Contaminate(); PlaceAndPoll(dish);
            var state = Reputation.State; var risk = state.HealthRisks.Single(); _service.Advance(30); yield return null;
            Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Closed)); Assert.That(state.PendingCount, Is.EqualTo(1));
            double elapsed = _day.State.Clock.ElapsedWorldSeconds;
            Reputation.enabled = false; Reputation.enabled = true; Assert.That(Reputation.State, Is.SameAs(state));
            Assert.That(_day.EndCurrentDay(), Is.True); Assert.That(_day.StartNextDay(), Is.True); Assert.That(_day.OpenRestaurant(), Is.True); Assert.That(_day.State.Clock.DayNumber, Is.EqualTo(2));
            Assert.That(_day.State.Clock.ElapsedWorldSeconds, Is.EqualTo(elapsed)); Assert.That(state.ResolutionCount, Is.Zero);
            _day.Advance(120); Assert.That(state.ResolutionCount, Is.EqualTo(1)); Assert.That(risk.Resolution.DayNumber, Is.EqualTo(2));
            Assert.That(_service.Statistics.HealthIncidents, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator M16DevelopmentDismissalPreservesMoneyAndConsumedRiskSurvivesServiceCancellation()
        {
            yield return ServeM16Dish(45, true); var state = Reputation.State;
            _service.enabled = false; _service.enabled = true; Assert.That(state.HealthRisks.Single().DueWorldSeconds.HasValue, Is.True);
            long balance = _service.Ledger.BalanceCents; Reputation.DismissDepartedNow();
            Assert.That(state.Value, Is.EqualTo(50)); Assert.That(state.LastResolution.Kind, Is.EqualTo(ReputationEventKind.HealthRiskDismissed));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(balance)); Assert.That(_service.Statistics.HealthIncidents, Is.EqualTo(1));
            Assert.That(state.ResolutionCount, Is.EqualTo(1)); yield return null;
        }
    }
}
#endif
