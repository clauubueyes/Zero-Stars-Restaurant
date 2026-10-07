using System;

namespace ZeroStarRestaurant.Economy
{
    public enum LedgerCategory { Sales = 0, Procurement = 1, Electricity = 2, FixedCost = 3 }

    // Immutable monetary evidence; object lifetime and definition prices cannot change it.
    public sealed class LedgerTransaction
    {
        public int DayNumber { get; }
        public Guid TransactionId { get; }
        public Guid ObjectId { get; }
        public LedgerCategory Category { get; }
        public string CostId { get; }
        public string Label { get; }
        public long AmountCents { get; }

        internal LedgerTransaction(int dayNumber, Guid transactionId, Guid objectId,
            LedgerCategory category, long amountCents, string costId = "", string label = "")
        {
            DayNumber = dayNumber; TransactionId = transactionId; ObjectId = objectId;
            Category = category; AmountCents = amountCents; CostId = costId; Label = label;
        }
    }
}
