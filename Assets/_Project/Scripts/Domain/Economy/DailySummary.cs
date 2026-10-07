using System.Collections.Generic;

namespace ZeroStarRestaurant.Economy
{
    public sealed class DailySummary
    {
        public int DayNumber { get; }
        public long OpeningBalanceCents { get; }
        public long SalesCents { get; }
        public long PurchasesCents { get; }
        public long ElectricityCents { get; }
        public long FixedCostsCents { get; }
        public long NetCents { get; }
        public long ClosingBalanceCents { get; }
        public double ConsumedKilowattHours { get; }
        public int ElectricityCentsPerKilowattHour { get; }
        public IReadOnlyList<LedgerTransaction> Transactions { get; }

        internal DailySummary(int dayNumber, long openingBalanceCents, double consumedKilowattHours,
            int electricityCentsPerKilowattHour, List<LedgerTransaction> transactions)
        {
            DayNumber = dayNumber; OpeningBalanceCents = openingBalanceCents;
            ConsumedKilowattHours = consumedKilowattHours; ElectricityCentsPerKilowattHour = electricityCentsPerKilowattHour;
            Transactions = new List<LedgerTransaction>(transactions).AsReadOnly();
            foreach (LedgerTransaction transaction in Transactions)
            {
                switch (transaction.Category)
                {
                    case LedgerCategory.Sales: SalesCents = checked(SalesCents + transaction.AmountCents); break;
                    case LedgerCategory.Procurement: PurchasesCents = checked(PurchasesCents + transaction.AmountCents); break;
                    case LedgerCategory.Electricity: ElectricityCents = checked(ElectricityCents + transaction.AmountCents); break;
                    case LedgerCategory.FixedCost: FixedCostsCents = checked(FixedCostsCents + transaction.AmountCents); break;
                }
            }
            NetCents = checked(checked(SalesCents - PurchasesCents) - checked(ElectricityCents + FixedCostsCents));
            ClosingBalanceCents = checked(OpeningBalanceCents + NetCents);
        }
    }
}
