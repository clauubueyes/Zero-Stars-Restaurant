using System;
using System.Linq;
using NUnit.Framework;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Orders;
using ZeroStarRestaurant.Restaurant;

namespace ZeroStarRestaurant.Tests
{
    public sealed class ReputationTests
    {
        private static readonly DishProfile Hamburger = new DishProfile("hamburger", "Hamburger", new[] { "bun", "patty", "bun" });
        private static CustomerConsequence Serve(double dose = 45, double freshness = 100, bool contaminated = false,
            PaymentLedger ledger = null, bool full = false, Guid? customer = null, OrderState existingOrder = null)
        {
            var patty = new FoodState(new FoodProfile("patty", "Patty", FoodCategory.Meat, 25, 600, 60,
                cooking: new CookingProfile(60, 120, 45, 65, 90)), freshnessPercent: freshness,
                isContaminated: contaminated, temperatureCelsius: 120);
            patty.Advance(dose, new ThermalEnvironment(120, allowsCooking: true), 0);
            var bun = new FoodProfile("bun", "Bun", FoodCategory.Bakery, 25, 600, 60);
            var foods = full ? new[] { new FoodState(bun), patty, new FoodState(bun) } : new[] { patty };
            var order = existingOrder ?? new OrderState(new OrderOffer(Hamburger, 500, new[] { 3, 4, 3 }));
            var visit = new CustomerVisit(order, customer ?? Guid.NewGuid());
            Assert.That(visit.Arrive() && visit.BeginWaiting() && visit.Receive() && visit.BeginEvaluation(), Is.True);
            if (!order.IsCompleted) Assert.That(OrderDelivery.TryComplete(order, new DeliveryContents(foods), ledger ?? new PaymentLedger(), out _), Is.True);
            Assert.That(visit.Resolve(), Is.True); return visit.Consequence;
        }
        private static RestaurantReputationState State(Func<double> random = null, ReputationPolicy policy = null)
            => new RestaurantReputationState(policy ?? new ReputationPolicy(delayWorldSeconds: 60), random ?? (() => 0));

        [TestCase(45, CustomerReaction.Satisfied, ReputationEventKind.GoodService, 51)]
        [TestCase(65, CustomerReaction.Unhappy, ReputationEventKind.UnhappyCustomer, 49)]
        [TestCase(90, CustomerReaction.Complaint, ReputationEventKind.Complaint, 47)]
        public void NormalReactionsChangeReputationOnlyAtVisitCompletion(double dose, CustomerReaction reaction, ReputationEventKind kind, int value)
        {
            var state = State(); var clock = new GameTime(); var consequence = Serve(dose);
            Assert.That(consequence.Reaction, Is.EqualTo(reaction)); Assert.That(state.TryRegisterService(consequence, clock), Is.True);
            Assert.That(state.Value, Is.EqualTo(50)); Assert.That(state.History, Is.Empty);
            Assert.That(state.TryCompleteVisit(consequence, clock), Is.True); Assert.That(state.Value, Is.EqualTo(value));
            Assert.That(state.LastChange.Kind, Is.EqualTo(kind)); Assert.That(state.LastChange.Consequence, Is.SameAs(consequence));
            Assert.That(state.TryCompleteVisit(consequence, clock), Is.False); Assert.That(state.History.Count, Is.EqualTo(1));
        }

        [TestCase(0, 100, false, FoodQualityIssue.Raw, .35)]
        [TestCase(20, 100, false, FoodQualityIssue.Undercooked, .35)]
        [TestCase(45, 20, false, FoodQualityIssue.Spoiled, .55)]
        [TestCase(45, 0, false, FoodQualityIssue.Rotten, .55)]
        [TestCase(45, 100, true, FoodQualityIssue.Contaminated, .75)]
        [TestCase(0, 0, true, FoodQualityIssue.Contaminated, .75)]
        public void HealthRiskUsesRealM15EvidenceAndHighestCauseProbability(double dose, double freshness, bool contaminated, FoodQualityIssue issue, double probability)
        {
            var consequence = Serve(dose, freshness, contaminated); var state = State(); var clock = new GameTime();
            state.TryRegisterService(consequence, clock); var risk = state.HealthRisks.Single();
            Assert.That(risk.Consequence, Is.SameAs(consequence)); Assert.That(risk.Consequence.Quality.Issues, Has.Member(issue));
            Assert.That(risk.Probability, Is.EqualTo(probability)); Assert.That(risk.DueWorldSeconds, Is.Null);
            Assert.That(state.PendingCount, Is.EqualTo(1)); Assert.That(state.Value, Is.EqualTo(50));
            Assert.That(state.History.Single().Kind, Is.EqualTo(ReputationEventKind.HealthRiskPending));
            Assert.That(state.LastChange, Is.Null); Assert.That(state.ResolveDepartedNow(clock, true), Is.Zero);
            clock.Advance(1000); Assert.That(state.ResolveDue(clock), Is.Zero, "Even zero delay needs departure.");
            state.TryCompleteVisit(consequence, clock); Assert.That(risk.DueWorldSeconds, Is.EqualTo(1060));
        }

        [TestCase(0, true)] [TestCase(.349999, true)] [TestCase(.35, false)] [TestCase(.99999, false)]
        public void ResolutionIsDelayedDeterministicAndExactlyOnce(double roll, bool confirmed)
        {
            int calls = 0; var state = State(() => { calls++; return roll; }); var clock = new GameTime(); var c = Serve(0);
            state.TryRegisterService(c, clock); state.TryCompleteVisit(c, clock);
            clock.Advance(59.999); Assert.That(state.ResolveDue(clock), Is.Zero); Assert.That(calls, Is.Zero);
            clock.Advance(.0011); Assert.That(state.ResolveDue(clock), Is.EqualTo(1));
            Assert.That(state.Value, Is.EqualTo(confirmed ? 42 : 50)); Assert.That(state.PendingCount, Is.Zero);
            Assert.That(state.LastResolution.Kind, Is.EqualTo(confirmed ? ReputationEventKind.HealthIncidentConfirmed : ReputationEventKind.HealthRiskDismissed));
            Assert.That(state.LastResolution.Roll, Is.EqualTo(roll)); Assert.That(state.LastResolution.Forced, Is.False);
            Assert.That(state.HealthRisks.Single().Resolution, Is.SameAs(state.LastResolution));
            Assert.That(state.ResolveDue(clock), Is.Zero); Assert.That(state.ResolveDepartedNow(clock, !confirmed), Is.Zero);
            Assert.That(calls, Is.EqualTo(1)); Assert.That(state.ResolutionCount, Is.EqualTo(1)); Assert.That(state.History.Count, Is.EqualTo(2));
        }

        [Test]
        public void MultipleCasesHaveIndependentDueTimesOutcomesAndEvidence()
        {
            var rolls = new[] { .1, .9 }; int calls = 0; var state = State(() => rolls[calls++]); var clock = new GameTime();
            var first = Serve(0); var second = Serve(contaminated: true);
            state.TryRegisterService(first, clock); state.TryCompleteVisit(first, clock); clock.Advance(30);
            state.TryRegisterService(second, clock); state.TryCompleteVisit(second, clock); clock.Advance(30);
            Assert.That(state.ResolveDue(clock), Is.EqualTo(1)); Assert.That(state.PendingCount, Is.EqualTo(1)); Assert.That(state.Value, Is.EqualTo(42));
            clock.Advance(30); Assert.That(state.ResolveDue(clock), Is.EqualTo(1)); Assert.That(state.Value, Is.EqualTo(42));
            Assert.That(state.HealthRisks[0].Resolution.CustomerId, Is.EqualTo(first.CustomerId));
            Assert.That(state.HealthRisks[1].Resolution.CustomerId, Is.EqualTo(second.CustomerId)); Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void PendingRisksSurviveDayChangeWithoutCountingAnOvernightGapOrPausedTime()
        {
            var day = new RestaurantDay(540, 540, 541); day.TryStartDay(0); var state = State(); var c = Serve(0);
            day.Advance(30, 0); state.TryRegisterService(c, day.Clock); state.TryCompleteVisit(c, day.Clock);
            day.Advance(30, 0); Assert.That(day.Stage, Is.EqualTo(RestaurantDayStage.Closed));
            Assert.That(state.ResolveDue(day.Clock), Is.Zero); day.Advance(100000, 0);
            Assert.That(day.Clock.ElapsedWorldSeconds, Is.EqualTo(60)); Assert.That(day.TryStartDay(0), Is.True);
            Assert.That(state.ResolveDue(day.Clock), Is.Zero); Assert.That(day.Clock.ElapsedWorldSeconds, Is.EqualTo(60));
            day.SetPaused(true); day.Advance(10000, 0); Assert.That(state.ResolveDue(day.Clock), Is.Zero);
            day.SetPaused(false); day.Advance(30, 0); Assert.That(state.ResolveDue(day.Clock), Is.EqualTo(1));
            Assert.That(state.LastResolution.DayNumber, Is.EqualTo(2)); Assert.That(state.LastResolution.ElapsedWorldSeconds, Is.EqualTo(90));
        }

        [TestCase(true)] [TestCase(false)]
        public void DevelopmentForcingBypassesDelayWithoutAdvancingClockOrUsingRng(bool confirmed)
        {
            var state = State(() => throw new Exception("Forced outcomes must not consume RNG")); var clock = new GameTime(); var c = Serve(0);
            state.TryRegisterService(c, clock); state.TryCompleteVisit(c, clock);
            Assert.That(state.ResolveDepartedNow(clock, confirmed), Is.EqualTo(1)); Assert.That(clock.ElapsedWorldSeconds, Is.Zero);
            Assert.That(state.LastResolution.Forced, Is.True); Assert.That(state.LastResolution.Roll, Is.Null);
            Assert.That(state.Value, Is.EqualTo(confirmed ? 42 : 50));
        }

        [TestCase(0, false)] [TestCase(1, true)]
        public void ProbabilityExtremesAreCertain(double probability, bool confirmed)
        {
            var state = State(() => .5, new ReputationPolicy(delayWorldSeconds: 0, rawMeatProbability: probability));
            var clock = new GameTime(); var c = Serve(0); state.TryRegisterService(c, clock); state.TryCompleteVisit(c, clock);
            Assert.That(state.ResolveDue(clock), Is.EqualTo(1)); Assert.That(state.Value, Is.EqualTo(confirmed ? 42 : 50));
        }

        [Test]
        public void CustomRangeAndExtremeDeltasClampWithoutOverflowAndRetainAttempts()
        {
            var state = State(policy: new ReputationPolicy(-10, 10, 0, int.MaxValue, int.MinValue, int.MinValue, int.MinValue)); var clock = new GameTime();
            foreach (var c in new[] { Serve(), Serve(), Serve(65), Serve(90) })
            { state.TryRegisterService(c, clock); state.TryCompleteVisit(c, clock); }
            Assert.That(state.History.Select(e => e.After), Is.EqualTo(new[] { 10, 10, -10, -10 }));
            Assert.That(state.History.Select(e => e.Before), Is.EqualTo(new[] { 0, 10, 10, -10 }));
        }

        [Test]
        public void SeededSequencesRepeatForIdenticalEvidenceAndOrder()
        {
            var r1 = new Random(1600); var r2 = new Random(1600); var a = State(r1.NextDouble); var b = State(r2.NextDouble); var clock = new GameTime();
            for (int i = 0; i < 20; i++)
            { var c = Serve(0); a.TryRegisterService(c, clock); b.TryRegisterService(c, clock); a.TryCompleteVisit(c, clock); b.TryCompleteVisit(c, clock); }
            clock.Advance(60); a.ResolveDue(clock); b.ResolveDue(clock);
            Assert.That(a.HealthRisks.Select(r => r.Resolution.Roll), Is.EqualTo(b.HealthRisks.Select(r => r.Resolution.Roll)));
            Assert.That(a.HealthRisks.Select(r => r.Resolution.Kind), Is.EqualTo(b.HealthRisks.Select(r => r.Resolution.Kind))); Assert.That(a.Value, Is.EqualTo(b.Value));
        }

        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-.1)] [TestCase(1)]
        public void InvalidInjectedRandomDoesNotResolveOrPenalize(double roll)
        {
            var state = State(() => roll); var clock = new GameTime(); var c = Serve(0);
            state.TryRegisterService(c, clock); state.TryCompleteVisit(c, clock); clock.Advance(60);
            Assert.Throws<InvalidOperationException>(() => state.ResolveDue(clock)); Assert.That(state.PendingCount, Is.EqualTo(1));
            Assert.That(state.Value, Is.EqualTo(50)); Assert.That(state.ResolutionCount, Is.Zero);
        }

        [Test]
        public void IdentitiesDeduplicateBothCustomersAndOrdersAndNullRejectionsDoNothing()
        {
            var state = State(); var clock = new GameTime(); var c = Serve(0);
            Assert.That(state.TryRegisterService(null, clock), Is.False); Assert.That(state.TryCompleteVisit(c, clock), Is.False);
            Assert.That(state.TryRegisterService(c, clock), Is.True); Assert.That(state.TryRegisterService(c, clock), Is.False);
            Assert.That(state.TryRegisterService(Serve(0, customer: c.CustomerId), clock), Is.False);
            var order = new OrderState(new OrderOffer(Hamburger, 500));
            var duplicateVisit = new CustomerVisit(order);
            Assert.That(duplicateVisit.Arrive() && duplicateVisit.BeginWaiting() && duplicateVisit.Receive() && duplicateVisit.BeginEvaluation(), Is.True);
            var first = Serve(0, existingOrder: order); Assert.That(duplicateVisit.Resolve(), Is.True);
            var duplicate = duplicateVisit.Consequence;
            Assert.That(state.TryRegisterService(first, clock), Is.True); Assert.That(state.TryRegisterService(duplicate, clock), Is.False);
            Assert.That(state.PendingCount, Is.EqualTo(2));
        }

        [TestCase(true, 500)] [TestCase(false, 200)]
        public void PaymentAndM15StatisticsStayIntactThroughConfirmedDelayedConsequence(bool full, int payment)
        {
            var ledger = new PaymentLedger(); var c = Serve(0, ledger: ledger, full: full); var receipt = ledger.Transactions.Single();
            var statistics = new CustomerServiceStatistics(); statistics.TryRecord(c); var state = State(); var clock = new GameTime();
            state.TryRegisterService(c, clock); state.TryCompleteVisit(c, clock); clock.Advance(60); state.ResolveDue(clock);
            Assert.That(c.Reaction, Is.EqualTo(CustomerReaction.HealthIncident)); Assert.That(statistics.HealthIncidents, Is.EqualTo(1));
            Assert.That(statistics.History.Single(), Is.SameAs(c)); Assert.That(c.Result.PaymentCents, Is.EqualTo(payment));
            Assert.That(ledger.BalanceCents, Is.EqualTo(payment)); Assert.That(ledger.Transactions.Single(), Is.SameAs(receipt));
            Assert.That(state.LastResolution.Consequence.Quality.Delivery, Is.SameAs(c.Result.Evaluation.DeliveredDish));
        }

        [Test]
        public void InvalidPolicyIsRejectedBeforeSessionStateExists()
        {
            Assert.Throws<ArgumentException>(() => new ReputationPolicy(100, 0));
            Assert.Throws<ArgumentException>(() => new ReputationPolicy(initial: 101));
            Assert.Throws<ArgumentException>(() => new ReputationPolicy(unhappyChange: 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReputationPolicy(delayWorldSeconds: double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReputationPolicy(rawMeatProbability: 1.1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReputationPolicy(spoiledProbability: -.1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReputationPolicy(contaminatedProbability: double.PositiveInfinity));
        }
    }
}
