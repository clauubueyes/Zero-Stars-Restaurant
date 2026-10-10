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
    public sealed class M19DailyStatisticsTests
    {
        private static CustomerConsequence Serve(PaymentLedger ledger, double cookingDose, bool contaminated = false)
        {
            var profile = new FoodProfile("patty", "Patty", FoodCategory.Meat, 25, 600, 60,
                cooking: new CookingProfile(60, 120, 45, 65, 90));
            var food = new FoodState(profile, temperatureCelsius: 120, isContaminated: contaminated);
            food.Advance(cookingDose, new ThermalEnvironment(120, allowsCooking: true), 0);
            var order = new OrderState(new OrderOffer(new DishProfile("patty.dish", "Patty", new[] { "patty" }), 500));
            var visit = new CustomerVisit(order); visit.Arrive(); visit.BeginWaiting(); visit.Receive(); visit.BeginEvaluation();
            Assert.That(OrderDelivery.TryComplete(order, new DeliveryContents(new[] { food }), ledger, out _), Is.True);
            Assert.That(visit.Resolve(), Is.True); return visit.Consequence;
        }
        [Test]
        public void TwelveHazardousMealsAreTwelveExclusiveHealthOutcomesWithStableEndOfDayText()
        {
            var ledger = new PaymentLedger(); var stats = new CustomerServiceStatistics();
            var reputation = new RestaurantReputationState(new ReputationPolicy(), () => 0);
            var clock = new GameTime();
            for (int i = 0; i < 12; i++)
            {
                var c = Serve(ledger, 90, true); Assert.That(stats.TryRecord(c), Is.True);
                Assert.That(c.Quality.Causes.Count, Is.GreaterThan(1), "Multiple causes still yield one reaction.");
                reputation.TryRegisterService(c, clock); reputation.TryCompleteVisit(c, clock);
            }
            ledger.TrySettleDay(new OperatingCostPolicy(0, Array.Empty<DailyFixedCost>()), 0, out var accounting);
            var snapshot = new EndOfDaySummary(accounting, stats, reputation, 0);
            string text = OperatingCostsFeedback.EndOfDayText(snapshot);
            Assert.That(text.Replace("\r\n", "\n"), Does.Contain("Customers served: 12\nOutcomes:\n  Satisfied: 0\n  Unhappy: 0\n  Complaints: 0\n  Health incidents (food hazards): 12"));
            Assert.That(snapshot.Satisfied + snapshot.Unhappy + snapshot.Complaints + snapshot.HealthIncidents, Is.EqualTo(snapshot.CustomersServed));
            reputation.ResolveDepartedNow(clock, false); stats.TryBeginNextDay(2);
            Assert.That(OperatingCostsFeedback.EndOfDayText(snapshot), Is.EqualTo(text));
            Assert.That(stats.DailyHealthIncidents, Is.Zero); Assert.That(stats.HealthIncidents, Is.EqualTo(12));
        }
        [Test]
        public void DailyCountersResetWhileAllLifetimeEvidenceAndDeduplicationSurviveFourDays()
        {
            var statistics = new CustomerServiceStatistics(); var ledger = new PaymentLedger(1000);
            var policy = new OperatingCostPolicy(30, new[] { new DailyFixedCost("rent", "Rent", 300) });
            var reputation = new RestaurantReputationState(new ReputationPolicy(), () => 0);
            EndOfDaySummary first = null; CustomerConsequence firstConsequence = null;
            for (int number = 1; number <= 4; number++)
            {
                Assert.That(statistics.DailyCustomersServed, Is.Zero); Assert.That(ledger.DayNumber, Is.EqualTo(number));
                Assert.That(ledger.TrySpend(Guid.NewGuid(), Guid.NewGuid(), 100), Is.True);
                var consequences = new[] { Serve(ledger, 45), Serve(ledger, 65), Serve(ledger, 90), Serve(ledger, 45, true) };
                foreach (var c in consequences) Assert.That(statistics.TryRecord(c), Is.True);
                Assert.That(statistics.DailyCustomersServed, Is.EqualTo(4)); Assert.That(statistics.CustomersServed, Is.EqualTo(4 * number));
                Assert.That(new[] { statistics.DailySatisfied, statistics.DailyUnhappy, statistics.DailyComplaints, statistics.DailyHealthIncidents }, Is.EqualTo(new long[] { 1, 1, 1, 1 }));
                ledger.TrySettleDay(policy, number, out var accounting);
                var summary = new EndOfDaySummary(accounting, statistics, reputation, 123);
                Assert.That(summary.Accounting.SalesCents, Is.EqualTo(2000)); Assert.That(summary.Accounting.PurchasesCents, Is.EqualTo(100));
                Assert.That(summary.Accounting.NetCents, Is.EqualTo(1600)); Assert.That(summary.PendingElectricityCents, Is.EqualTo(123));
                if (number == 1) { first = summary; firstConsequence = consequences[0]; }
                if (number == 4) break;
                Assert.That(statistics.TryBeginNextDay(number + 2), Is.False); Assert.That(statistics.TryBeginNextDay(number + 1), Is.True);
                Assert.That(statistics.TryBeginNextDay(number + 1), Is.False); Assert.That(statistics.TryRecord(firstConsequence), Is.False);
                Assert.That(ledger.TryBeginNextDay(number + 1), Is.True);
            }
            Assert.That(statistics.History.Count, Is.EqualTo(16)); Assert.That(statistics.HealthIncidentHistory.Count, Is.EqualTo(4));
            Assert.That(ledger.Summaries.Sum(s => s.SalesCents), Is.EqualTo(8000)); Assert.That(ledger.Summaries.Sum(s => s.PurchasesCents), Is.EqualTo(400));
            Assert.That(ledger.BalanceCents, Is.EqualTo(7400)); Assert.That(first.CustomersServed, Is.EqualTo(4)); Assert.That(first.Accounting.ClosingBalanceCents, Is.EqualTo(2600));
        }
        [Test]
        public void DayTwoRecordsTheResolutionOfADayOneRiskWithoutResettingOrInventingHazardReactions()
        {
            var day = new RestaurantDay(480, 540, 541); day.TryStartDay(0); day.TryOpenRestaurant();
            var ledger = new PaymentLedger(); var stats = new CustomerServiceStatistics();
            var reputation = new RestaurantReputationState(new ReputationPolicy(delayWorldSeconds: 90), () => 0);
            var c = Serve(ledger, 45, true); stats.TryRecord(c); reputation.TryRegisterService(c, day.Clock); reputation.TryCompleteVisit(c, day.Clock);
            var risk = reputation.HealthRisks.Single(); day.Advance(60, 0); day.TryEndDay(0);
            ledger.TrySettleDay(new OperatingCostPolicy(30, Array.Empty<DailyFixedCost>()), 0, out var accounting);
            var first = new EndOfDaySummary(accounting, stats, reputation, 0); day.TryStartDay(0); ledger.TryBeginNextDay(2); stats.TryBeginNextDay(2);
            day.Advance(100000, 0); Assert.That(reputation.ResolveDue(day.Clock), Is.Zero); Assert.That(risk.DueWorldSeconds, Is.EqualTo(90));
            Assert.That(reputation.PendingCount, Is.EqualTo(1)); day.TryOpenRestaurant(); day.Advance(30, 0);
            Assert.That(reputation.ResolveDue(day.Clock), Is.EqualTo(1)); Assert.That(reputation.HealthRisks.Single(), Is.SameAs(risk));
            ledger.TrySettleDay(new OperatingCostPolicy(30, Array.Empty<DailyFixedCost>()), 0, out accounting);
            var second = new EndOfDaySummary(accounting, stats, reputation, 0);
            Assert.That(first.HealthIncidents, Is.EqualTo(1)); Assert.That(first.ConfirmedHealthIncidents, Is.Zero); Assert.That(first.PendingHealthRisks, Is.EqualTo(1));
            Assert.That(second.HealthIncidents, Is.Zero); Assert.That(second.ConfirmedHealthIncidents, Is.EqualTo(1)); Assert.That(second.Reputation, Is.EqualTo(42));
            Assert.That(stats.HealthIncidents, Is.EqualTo(1)); Assert.That(reputation.History, Has.Count.EqualTo(2));
        }
        [Test]
        public void UnavailableReputationIsReportedAsMissingAndMismatchedStatisticsAreRejected()
        {
            var ledger = new PaymentLedger(); ledger.TrySettleDay(new OperatingCostPolicy(0, Array.Empty<DailyFixedCost>()), 0, out var accounting);
            var stats = new CustomerServiceStatistics(); var summary = new EndOfDaySummary(accounting, stats, null, 0);
            Assert.That(summary.Reputation, Is.Null); Assert.That(summary.ConfirmedHealthIncidents, Is.Null);
            stats.TryBeginNextDay(2); Assert.Throws<ArgumentException>(() => new EndOfDaySummary(accounting, stats, null, 0));
        }
    }
}
