using System;
using System.Collections.Generic;

namespace ZeroStarRestaurant.Utilities
{
    // Session energy evidence, independent of food, world time, prices and payment.
    public sealed class ElectricitySupplyState
    {
        public bool IsOn { get; private set; }
        public double ConsumedKilowattHours { get; private set; }
        public double DailyConsumedKilowattHours { get; private set; }
        public double PendingKilowattHours { get; private set; }
        public Guid BillingPeriodId { get; private set; } = Guid.NewGuid();
        private bool _recordDaily = true;

        public ElectricitySupplyState(bool initiallyOn = false) => IsOn = initiallyOn;
        public void SetOn(bool isOn) => IsOn = isOn;
        public void BeginDay() { DailyConsumedKilowattHours = 0; _recordDaily = true; }
        public void CompleteDay() => _recordDaily = false;

        public bool TryCompleteBillingPeriod(Guid periodId)
        {
            if (periodId != BillingPeriodId || PendingKilowattHours == 0) return false;
            PendingKilowattHours = 0; BillingPeriodId = Guid.NewGuid();
            return true;
        }

        public void Advance(double elapsedSeconds, IReadOnlyList<ElectricityMeter> operatingMeters)
        {
            ElectricityMeter.RequireNonnegativeFinite(elapsedSeconds, nameof(elapsedSeconds));
            if (operatingMeters == null) throw new ArgumentNullException(nameof(operatingMeters));
            var unique = new HashSet<ElectricityMeter>();
            double total = 0;
            foreach (ElectricityMeter meter in operatingMeters)
            {
                if (meter == null) throw new ArgumentException("Operating meters cannot contain null.", nameof(operatingMeters));
                if (!unique.Add(meter) || !IsOn) continue;
                double energy = meter.ConsumptionFor(elapsedSeconds);
                meter.ValidateRecord(energy);
                total += energy;
            }
            ElectricityMeter.RequireNonnegativeFinite(ConsumedKilowattHours + total, nameof(elapsedSeconds));
            ElectricityMeter.RequireNonnegativeFinite(PendingKilowattHours + total, nameof(elapsedSeconds));
            if (_recordDaily) ElectricityMeter.RequireNonnegativeFinite(DailyConsumedKilowattHours + total, nameof(elapsedSeconds));
            // Validate the complete step before mutating any meter or total.
            if (!IsOn) return;
            foreach (ElectricityMeter meter in unique) meter.Record(meter.ConsumptionFor(elapsedSeconds));
            ConsumedKilowattHours += total;
            // Period evidence includes consumption while the closed daily summary is being viewed.
            PendingKilowattHours += total;
            if (_recordDaily) DailyConsumedKilowattHours += total;
        }
    }
}
