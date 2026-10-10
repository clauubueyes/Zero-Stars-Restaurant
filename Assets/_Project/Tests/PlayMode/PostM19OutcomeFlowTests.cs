#if UNITY_EDITOR
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
using ZeroStarRestaurant.Restaurant;

namespace ZeroStarRestaurant.Tests
{
    public sealed partial class M19MultiDayRestaurantTests
    {
        [UnityTest]
        public IEnumerator PostM19DaysABCHaveExclusiveOutcomesAtExitStableSummariesAndPersistentLifetimeEvidence()
        {
            var days = new[] {
                new[] { CustomerReaction.Satisfied, CustomerReaction.Unhappy, CustomerReaction.Complaint, CustomerReaction.HealthIncident },
                new[] { CustomerReaction.Satisfied, CustomerReaction.Satisfied, CustomerReaction.Satisfied },
                new[] { CustomerReaction.HealthIncident, CustomerReaction.HealthIncident, CustomerReaction.HealthIncident }
            };
            var expected = new[] { new long[] { 1, 1, 1, 1 }, new long[] { 3, 0, 0, 0 }, new long[] { 0, 0, 0, 3 } };
            var stats = _service.Statistics; var reputation = _service.Reputation.State;
            var summaries = new EndOfDaySummary[3]; long lifetime = 0;
            for (int d = 0; d < days.Length; d++)
            {
                Assert.That(_day.OpenRestaurant(), Is.True);
                int admissionSteps = 0;
                while (_service.Queue.Count < days[d].Length && ++admissionSteps < 2000) _service.Advance(.05);
                Assert.That(_service.Queue.Count, Is.EqualTo(days[d].Length));
                Assert.That(_day.ForceClose(), Is.True); _service.Advance(30);
                foreach (var reaction in days[d])
                {
                    Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
                    var visit = _service.Visit; var head = _service.Queue.Head;
                    var food = Buy(1, new Vector3(-3, 2, 2));
                    food.State.SetTemperature(120);
                    double dose = reaction == CustomerReaction.Complaint ? 90 : reaction == CustomerReaction.Unhappy ? 65 : 45;
                    food.State.Advance(dose, new ThermalEnvironment(120, allowsCooking: true), 0);
                    if (reaction == CustomerReaction.HealthIncident) food.State.Contaminate();
                    OnSupport(food, _service.DeliveryZone.Support); _service.DeliveryZone.Poll();
                    Assert.That(visit.Consequence.Reaction, Is.EqualTo(reaction));
                    Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(200), "M14 partial payment is independent of quality.");
                    for (int poll = 0; poll < 5; poll++) _service.DeliveryZone.Poll();
                    Assert.That(_day.EndCurrentDay(), Is.False, "An accepted visit still has to finish Exit.");
                    _service.Advance(30); lifetime++;
                    Assert.That(visit.Stage, Is.EqualTo(CustomerStage.Finished)); Assert.That(visit.Finish(), Is.False);
                    Assert.That(_service.Queue.Customers.Contains(head), Is.False);
                    Assert.That(stats.TryRecord(visit.Consequence), Is.False);
                    Assert.That(reputation.TryCompleteVisit(visit.Consequence, _day.State.Clock), Is.False);
                    Assert.That(stats.CustomersServed, Is.EqualTo(lifetime));
                    Assert.That(stats.DailySatisfied + stats.DailyUnhappy + stats.DailyComplaints + stats.DailyHealthIncidents,
                        Is.EqualTo(stats.DailyCustomersServed));
                    yield return null;
                }
                Assert.That(_service.Queue.Count, Is.Zero); Assert.That(_service.Visit, Is.Null);
                Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Closed));
                Assert.That(_day.EndCurrentDay(), Is.True); Assert.That(_day.EndCurrentDay(), Is.False);
                var summary = summaries[d] = _day.Summary;
                Assert.That(summary.CustomersServed, Is.EqualTo(days[d].Length));
                Assert.That(new[] { summary.Satisfied, summary.Unhappy, summary.Complaints, summary.HealthIncidents }, Is.EqualTo(expected[d]));
                string text = All<OperatingCostsFeedback>().Single().Text;
                Assert.That(text, Does.Contain("Outcomes:").And.Contain("  Health incidents (food hazards): " + expected[d][3]));
                var history = stats.History.ToArray();
                if (d == 0 || d == 2)
                {
                    Assert.That(reputation.ResolveDepartedNow(_day.State.Clock, d == 0), Is.EqualTo(d == 0 ? 1 : 3));
                    Assert.That(reputation.ResolveDepartedNow(_day.State.Clock, d == 0), Is.Zero);
                    Assert.That(stats.History, Is.EqualTo(history));
                    Assert.That(All<OperatingCostsFeedback>().Single().Text, Is.EqualTo(text), "EndOfDay retains the captured M16 evidence too.");
                }
                _service.Advance(100); _day.Advance(10000);
                Assert.That(stats.DailyCustomersServed, Is.EqualTo(days[d].Length), "End Day never resets daily counters.");
                Assert.That(_day.StartNextDay(), Is.True); Assert.That(_day.StartNextDay(), Is.False);
                Assert.That(stats.DailyCustomersServed, Is.Zero);
                Assert.That(new[] { stats.DailySatisfied, stats.DailyUnhappy, stats.DailyComplaints, stats.DailyHealthIncidents }, Is.EqualTo(new long[4]));
                Assert.That(stats.History, Is.EqualTo(history)); Assert.That(All<OperatingCostsFeedback>().Single().Text, Is.Empty);
                Assert.That(summary.CustomersServed, Is.EqualTo(days[d].Length));
                Assert.That(new[] { summary.Satisfied, summary.Unhappy, summary.Complaints, summary.HealthIncidents }, Is.EqualTo(expected[d]));
            }
            Assert.That(stats.CustomersServed, Is.EqualTo(10));
            Assert.That(new[] { stats.Satisfied, stats.Unhappy, stats.Complaints, stats.HealthIncidents }, Is.EqualTo(new long[] { 4, 1, 1, 4 }));
            Assert.That(stats.History.Select(c => c.CustomerId).Distinct().Count(), Is.EqualTo(10));
            Assert.That(stats.History.Select(c => c.OrderId).Distinct().Count(), Is.EqualTo(10));
            Assert.That(reputation.History.Count(e => e.Kind == ReputationEventKind.HealthIncidentConfirmed), Is.EqualTo(1));
            Assert.That(reputation.History.Count(e => e.Kind == ReputationEventKind.HealthRiskDismissed), Is.EqualTo(3));
            Assert.That(_day.Summaries, Is.EqualTo(summaries));
        }
    }
}
#endif
