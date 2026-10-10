using System;
using System.Collections.Generic;

namespace ZeroStarRestaurant.Customers
{
    // Session history survives Exit, service cancellation and Next Day, like the ledger.
    public sealed class CustomerServiceStatistics
    {
        private readonly HashSet<Guid> _customers = new HashSet<Guid>();
        private readonly HashSet<Guid> _orders = new HashSet<Guid>();
        private readonly List<CustomerConsequence> _history = new List<CustomerConsequence>();
        private readonly List<CustomerConsequence> _healthIncidents = new List<CustomerConsequence>();
        public IReadOnlyList<CustomerConsequence> History { get; }
        public IReadOnlyList<CustomerConsequence> HealthIncidentHistory { get; }
        public long CustomersServed => _history.Count;
        public long Satisfied { get; private set; }
        public long Unhappy { get; private set; }
        public long Complaints { get; private set; }
        public long HealthIncidents => _healthIncidents.Count;
        public int DayNumber { get; private set; } = 1;
        public long DailyCustomersServed { get; private set; }
        public long DailySatisfied { get; private set; }
        public long DailyUnhappy { get; private set; }
        public long DailyComplaints { get; private set; }
        public long DailyHealthIncidents { get; private set; }

        public bool TryBeginNextDay(int dayNumber)
        {
            if (DayNumber == int.MaxValue || dayNumber != DayNumber + 1) return false;
            DayNumber = dayNumber;
            DailyCustomersServed = DailySatisfied = DailyUnhappy = DailyComplaints = DailyHealthIncidents = 0;
            return true;
        }

        public CustomerServiceStatistics()
        { History = _history.AsReadOnly(); HealthIncidentHistory = _healthIncidents.AsReadOnly(); }

        public bool TryRecord(CustomerConsequence consequence)
        {
            if (consequence == null || !consequence.Result.Accepted ||
                _customers.Contains(consequence.CustomerId) || _orders.Contains(consequence.OrderId)) return false;
            _customers.Add(consequence.CustomerId); _orders.Add(consequence.OrderId); _history.Add(consequence);
            DailyCustomersServed++;
            switch (consequence.Reaction)
            {
                case CustomerReaction.Satisfied: Satisfied++; DailySatisfied++; break;
                case CustomerReaction.Unhappy: Unhappy++; DailyUnhappy++; break;
                case CustomerReaction.Complaint: Complaints++; DailyComplaints++; break;
                case CustomerReaction.HealthIncident: _healthIncidents.Add(consequence); DailyHealthIncidents++; break;
            }
            return true;
        }
    }
}
