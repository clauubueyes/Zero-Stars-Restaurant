using System.Collections.Generic;

namespace ZeroStarRestaurant.Economy
{
    public sealed class DailySummary
    {
        public int DayNumber { get; }
        public long OpeningBalanceCents { get; }
        public long SalesCents { get; }
        public long PurchasesCents { get; }
        public long ElectricityPaidCents { get; }
        public long ElectricityAccruedCents { get; }
        public long FixedCostsCents { get; }
        public long OperatingNetCents { get; }
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
            ElectricityAccruedCents = new OperatingCostPolicy(electricityCentsPerKilowattHour,
                new DailyFixedCost[0]).ElectricityCostCents(consumedKilowattHours);
            Transactions = new List<LedgerTransaction>(transactions).AsReadOnly();
            foreach (LedgerTransaction transaction in Transactions)
            {
                switch (transaction.Category)
                {
                    case LedgerCategory.Sales: SalesCents = checked(SalesCents + transaction.AmountCents); break;
                    case LedgerCategory.Procurement: PurchasesCents = checked(PurchasesCents + transaction.AmountCents); break;
                    case LedgerCategory.Electricity: ElectricityPaidCents = checked(ElectricityPaidCents + transaction.AmountCents); break;
                    case LedgerCategory.FixedCost: FixedCostsCents = checked(FixedCostsCents + transaction.AmountCents); break;
                }
            }
            OperatingNetCents = checked(checked(SalesCents - PurchasesCents) - FixedCostsCents);
            NetCents = checked(OperatingNetCents - ElectricityPaidCents);
            ClosingBalanceCents = checked(OpeningBalanceCents + NetCents);
        }
    }
}
