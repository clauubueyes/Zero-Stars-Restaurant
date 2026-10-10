using System;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Economy;

namespace ZeroStarRestaurant.Restaurant
{
    // Immutable projection of existing accounting, service statistics and reputation evidence.
    public sealed class EndOfDaySummary
    {
        public DailySummary Accounting { get; }
        public int DayNumber => Accounting.DayNumber;
        public long CustomersServed { get; }
        public long Satisfied { get; }
        public long Unhappy { get; }
        public long Complaints { get; }
        // M15 hazard reactions; confirmed illness remains separate M16 evidence.
        public long HealthIncidents { get; }
        public int? Reputation { get; }
        public int? ConfirmedHealthIncidents { get; }
        public int? DismissedHealthRisks { get; }
        public int? PendingHealthRisks { get; }
        public long PendingElectricityCents { get; }

        public EndOfDaySummary(DailySummary accounting, CustomerServiceStatistics statistics,
            RestaurantReputationState reputation, long pendingElectricityCents)
        {
            Accounting = accounting ?? throw new ArgumentNullException(nameof(accounting));
            if (statistics == null || statistics.DayNumber != accounting.DayNumber)
                throw new ArgumentException("Daily service statistics must match the accounting day.");
            CustomersServed = statistics.DailyCustomersServed; Satisfied = statistics.DailySatisfied;
            Unhappy = statistics.DailyUnhappy; Complaints = statistics.DailyComplaints;
            HealthIncidents = statistics.DailyHealthIncidents; PendingElectricityCents = pendingElectricityCents;
            if (reputation == null) return;
            Reputation = reputation.Value; PendingHealthRisks = reputation.PendingCount;
            int confirmed = 0, dismissed = 0;
            foreach (var entry in reputation.History)
            {
                if (entry.DayNumber != DayNumber) continue;
                if (entry.Kind == ReputationEventKind.HealthIncidentConfirmed) confirmed++;
                if (entry.Kind == ReputationEventKind.HealthRiskDismissed) dismissed++;
            }
            ConfirmedHealthIncidents = confirmed; DismissedHealthRisks = dismissed;
        }
    }
}
