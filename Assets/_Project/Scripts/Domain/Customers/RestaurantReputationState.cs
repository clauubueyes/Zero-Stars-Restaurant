using System;
using System.Collections.Generic;
using ZeroStarRestaurant.Restaurant;

namespace ZeroStarRestaurant.Customers
{
    public enum ReputationEventKind { GoodService, UnhappyCustomer, Complaint, HealthRiskPending, HealthIncidentConfirmed, HealthRiskDismissed }

    public sealed class ReputationEvent
    {
        public ReputationEventKind Kind { get; }
        public CustomerConsequence Consequence { get; }
        public Guid CustomerId => Consequence.CustomerId;
        public Guid OrderId => Consequence.OrderId;
        public int Before { get; }
        public int After { get; }
        public int DayNumber { get; }
        public double SecondsOfDay { get; }
        public double ElapsedWorldSeconds { get; }
        public double? Roll { get; }
        public bool Forced { get; }
        internal ReputationEvent(ReputationEventKind kind, CustomerConsequence consequence, int before, int after,
            GameTime clock, double? roll = null, bool forced = false)
        {
            Kind = kind; Consequence = consequence; Before = before; After = after;
            DayNumber = clock.DayNumber; SecondsOfDay = clock.SecondsOfDay; ElapsedWorldSeconds = clock.ElapsedWorldSeconds;
            Roll = roll; Forced = forced;
        }
    }

    public sealed class PendingHealthRisk
    {
        public CustomerConsequence Consequence { get; }
        public Guid CustomerId => Consequence.CustomerId;
        public double Probability { get; }
        public double? DueWorldSeconds { get; internal set; }
        public ReputationEvent Resolution { get; internal set; }
        public bool IsResolved => Resolution != null;
        internal PendingHealthRisk(CustomerConsequence consequence, double probability)
        { Consequence = consequence; Probability = probability; }
    }

    // Session state: no references to a ledger, live food, Unity or another clock.
    public sealed class RestaurantReputationState
    {
        private readonly ReputationPolicy _policy;
        private readonly Func<double> _nextRandom;
        private readonly Dictionary<Guid, CustomerConsequence> _services = new Dictionary<Guid, CustomerConsequence>();
        private readonly HashSet<Guid> _orders = new HashSet<Guid>();
        private readonly HashSet<Guid> _completed = new HashSet<Guid>();
        private readonly List<PendingHealthRisk> _risks = new List<PendingHealthRisk>();
        private readonly List<ReputationEvent> _history = new List<ReputationEvent>();
        public int Value { get; private set; }
        public ReputationPolicy Policy => _policy;
        public IReadOnlyList<PendingHealthRisk> HealthRisks { get; }
        public IReadOnlyList<ReputationEvent> History { get; }
        public ReputationEvent LastChange { get; private set; }
        public ReputationEvent LastResolution { get; private set; }
        public int ResolutionCount { get; private set; }
        public int PendingCount { get { int count = 0; foreach (var risk in _risks) if (!risk.IsResolved) count++; return count; } }

        public RestaurantReputationState(ReputationPolicy policy, Func<double> nextRandom)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _nextRandom = nextRandom ?? throw new ArgumentNullException(nameof(nextRandom)); Value = policy.Initial;
            HealthRisks = _risks.AsReadOnly(); History = _history.AsReadOnly();
        }

        public bool TryRegisterService(CustomerConsequence consequence, GameTime clock)
        {
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            if (consequence == null || !consequence.Result.Accepted || _services.ContainsKey(consequence.CustomerId) || _orders.Contains(consequence.OrderId)) return false;
            _services.Add(consequence.CustomerId, consequence); _orders.Add(consequence.OrderId);
            if (consequence.Reaction == CustomerReaction.HealthIncident)
            {
                _risks.Add(new PendingHealthRisk(consequence, _policy.Probability(consequence.Quality.Causes)));
                Record(ReputationEventKind.HealthRiskPending, consequence, 0, clock);
            }
            return true;
        }

        public bool TryCompleteVisit(CustomerConsequence consequence, GameTime clock)
        {
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            if (consequence == null || !_services.TryGetValue(consequence.CustomerId, out var registered) ||
                !ReferenceEquals(registered, consequence) || !_completed.Add(consequence.CustomerId)) return false;
            switch (consequence.Reaction)
            {
                case CustomerReaction.Satisfied: Record(ReputationEventKind.GoodService, consequence, _policy.SatisfiedChange, clock); break;
                case CustomerReaction.Unhappy: Record(ReputationEventKind.UnhappyCustomer, consequence, _policy.UnhappyChange, clock); break;
                case CustomerReaction.Complaint: Record(ReputationEventKind.Complaint, consequence, _policy.ComplaintChange, clock); break;
                case CustomerReaction.HealthIncident:
                    foreach (var risk in _risks)
                        if (risk.CustomerId == consequence.CustomerId) risk.DueWorldSeconds = clock.ElapsedWorldSeconds + _policy.DelayWorldSeconds;
                    break;
            }
            return true;
        }

        public int ResolveDue(GameTime clock)
        {
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            int resolved = 0;
            foreach (var risk in _risks)
                if (!risk.IsResolved && risk.DueWorldSeconds.HasValue && clock.ElapsedWorldSeconds >= risk.DueWorldSeconds.Value)
                { Resolve(risk, clock, null); resolved++; }
            return resolved;
        }

        // Explicit development bypass; still requires departure, preserves the clock and deduplication.
        public int ResolveDepartedNow(GameTime clock, bool? forceConfirmation = null)
        {
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            int resolved = 0;
            foreach (var risk in _risks)
                if (!risk.IsResolved && risk.DueWorldSeconds.HasValue) { Resolve(risk, clock, forceConfirmation); resolved++; }
            return resolved;
        }

        private void Resolve(PendingHealthRisk risk, GameTime clock, bool? forceConfirmation)
        {
            double? roll = null;
            if (!forceConfirmation.HasValue)
            {
                roll = _nextRandom();
                if (double.IsNaN(roll.Value) || double.IsInfinity(roll.Value) || roll < 0 || roll >= 1)
                    throw new InvalidOperationException("Injected RNG must return a finite value in [0, 1).");
            }
            bool confirmed = forceConfirmation ?? roll.Value < risk.Probability;
            risk.Resolution = Record(confirmed ? ReputationEventKind.HealthIncidentConfirmed : ReputationEventKind.HealthRiskDismissed,
                risk.Consequence, confirmed ? _policy.ConfirmedIncidentChange : 0, clock, roll, forceConfirmation.HasValue);
            LastResolution = risk.Resolution; ResolutionCount++;
        }

        private ReputationEvent Record(ReputationEventKind kind, CustomerConsequence consequence, int delta, GameTime clock,
            double? roll = null, bool forced = false)
        {
            int before = Value;
            Value = (int)Math.Max(_policy.Minimum, Math.Min(_policy.Maximum, (long)Value + delta));
            var entry = new ReputationEvent(kind, consequence, before, Value, clock, roll, forced); _history.Add(entry);
            if (kind == ReputationEventKind.GoodService || kind == ReputationEventKind.UnhappyCustomer ||
                kind == ReputationEventKind.Complaint || kind == ReputationEventKind.HealthIncidentConfirmed) LastChange = entry;
            return entry;
        }
    }
}
