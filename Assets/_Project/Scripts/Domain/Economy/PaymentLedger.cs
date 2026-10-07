using System;
using System.Collections.Generic;

namespace ZeroStarRestaurant.Economy
{
    public sealed class PaymentLedger
    {
        private readonly HashSet<Guid> _processedOrders = new HashSet<Guid>();
        private readonly HashSet<Guid> _soldDishes = new HashSet<Guid>();
        private readonly HashSet<Guid> _processedPurchases = new HashSet<Guid>();
        private readonly HashSet<Guid> _purchasedUnits = new HashSet<Guid>();
        private readonly List<LedgerTransaction> _transactions = new List<LedgerTransaction>();
        private readonly List<DailySummary> _summaries = new List<DailySummary>();
        private readonly IReadOnlyList<LedgerTransaction> _transactionView;
        private readonly IReadOnlyList<DailySummary> _summaryView;
        private int _dayTransactionStart;
        private long _openingBalanceCents;
        public int DayNumber { get; private set; } = 1;
        public DailySummary CurrentSummary { get; private set; }
        public bool IsDaySettled => CurrentSummary != null;
        public IReadOnlyList<LedgerTransaction> Transactions => _transactionView;
        public IReadOnlyList<DailySummary> Summaries => _summaryView;
        public long BalanceCents { get; private set; }
        public PaymentLedger(long initialBalanceCents = 0)
        {
            if (initialBalanceCents < 0) throw new ArgumentOutOfRangeException(nameof(initialBalanceCents));
            BalanceCents = initialBalanceCents;
            _openingBalanceCents = initialBalanceCents;
            _transactionView = _transactions.AsReadOnly(); _summaryView = _summaries.AsReadOnly();
        }
        // No knowledge of customers, ingredients, cooking, freshness or recognition rules.
        public bool TrySpend(Guid purchaseId, Guid unitId, int priceCents)
        {
            if (IsDaySettled || purchaseId == Guid.Empty || unitId == Guid.Empty || priceCents <= 0 ||
                BalanceCents < priceCents || _processedPurchases.Contains(purchaseId) ||
                _purchasedUnits.Contains(unitId)) return false;
            _processedPurchases.Add(purchaseId);
            _purchasedUnits.Add(unitId);
            BalanceCents -= priceCents;
            _transactions.Add(new LedgerTransaction(DayNumber, purchaseId, unitId, LedgerCategory.Procurement, priceCents));
            return true;
        }

        public bool TryRecord(Guid orderId, Guid dishId, int salePriceCents, bool accepted)
        {
            if (IsDaySettled || orderId == Guid.Empty || dishId == Guid.Empty || salePriceCents <= 0 ||
                _processedOrders.Contains(orderId) || _soldDishes.Contains(dishId)) return false;
            int payment = accepted ? salePriceCents : 0;
            if (BalanceCents > long.MaxValue - payment) return false;
            _processedOrders.Add(orderId);
            if (accepted) _soldDishes.Add(dishId);
            BalanceCents += payment;
            if (accepted) _transactions.Add(new LedgerTransaction(DayNumber, orderId, dishId, LedgerCategory.Sales, payment));
            return true;
        }

        public bool TrySettleDay(OperatingCostPolicy policy, double consumedKilowattHours, out DailySummary summary)
        {
            summary = CurrentSummary;
            if (IsDaySettled) return true; // The first immutable receipt is authoritative on every retry.
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            long electricity = policy.ElectricityCostCents(consumedKilowattHours);
            var charges = new List<LedgerTransaction>
            {
                new LedgerTransaction(DayNumber, Guid.NewGuid(), Guid.Empty, LedgerCategory.Electricity, electricity, "electricity", "Electricity")
            };
            long total = electricity;
            foreach (DailyFixedCost cost in policy.FixedCosts)
            {
                total = checked(total + cost.AmountCents);
                charges.Add(new LedgerTransaction(DayNumber, Guid.NewGuid(), Guid.Empty, LedgerCategory.FixedCost, cost.AmountCents, cost.Id, cost.Label));
            }
            long closingBalance = checked(BalanceCents - total); // Charges allow debt; procurement still requires funds.
            var evidence = _transactions.GetRange(_dayTransactionStart, _transactions.Count - _dayTransactionStart);
            evidence.AddRange(charges);
            var candidate = new DailySummary(DayNumber, _openingBalanceCents, consumedKilowattHours,
                policy.ElectricityCentsPerKilowattHour, evidence);
            if (candidate.ClosingBalanceCents != closingBalance) throw new InvalidOperationException("Daily accounting must reconcile the ledger.");
            // Complete all arithmetic/validation before committing any balance, receipt or charge.
            _transactions.AddRange(charges); BalanceCents = closingBalance;
            CurrentSummary = candidate; _summaries.Add(candidate); summary = candidate;
            return true;
        }

        public bool TryBeginNextDay(int dayNumber)
        {
            if (!IsDaySettled || DayNumber == int.MaxValue || dayNumber != DayNumber + 1) return false;
            DayNumber = dayNumber; _openingBalanceCents = BalanceCents;
            _dayTransactionStart = _transactions.Count; CurrentSummary = null;
            return true;
        }
    }
}
