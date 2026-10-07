using System;

namespace ZeroStarRestaurant.Economy
{
    public sealed class ElectricityBillReceipt
    {
        public Guid BillingPeriodId { get; }
        public int PaidOnDayNumber { get; }
        public double ConsumedKilowattHours { get; }
        public int CentsPerKilowattHour { get; }
        public long AmountCents { get; }

        internal ElectricityBillReceipt(Guid periodId, int dayNumber, double kwh, int tariff, long cents)
        {
            BillingPeriodId = periodId; PaidOnDayNumber = dayNumber;
            ConsumedKilowattHours = kwh; CentsPerKilowattHour = tariff; AmountCents = cents;
        }
    }
}
