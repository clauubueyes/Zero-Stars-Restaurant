using System;
using System.Collections.Generic;

namespace ZeroStarRestaurant.Economy
{
    public sealed class DailyFixedCost
    {
        public string Id { get; }
        public string Label { get; }
        public int AmountCents { get; }
        public DailyFixedCost(string id, string label, int amountCents)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(label) || amountCents < 0)
                throw new ArgumentException("Daily costs require an ID, label and nonnegative integer cents.");
            Id = id; Label = label; AmountCents = amountCents;
        }
    }

    public sealed class OperatingCostPolicy
    {
        public int ElectricityCentsPerKilowattHour { get; }
        public IReadOnlyList<DailyFixedCost> FixedCosts { get; }
        public OperatingCostPolicy(int electricityCentsPerKilowattHour, IReadOnlyList<DailyFixedCost> fixedCosts)
        {
            if (electricityCentsPerKilowattHour < 0) throw new ArgumentOutOfRangeException(nameof(electricityCentsPerKilowattHour));
            if (fixedCosts == null) throw new ArgumentNullException(nameof(fixedCosts));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var copy = new List<DailyFixedCost>();
            foreach (DailyFixedCost cost in fixedCosts)
            {
                if (cost == null || !ids.Add(cost.Id)) throw new ArgumentException("Daily cost IDs must be unique.", nameof(fixedCosts));
                copy.Add(cost);
            }
            ElectricityCentsPerKilowattHour = electricityCentsPerKilowattHour;
            FixedCosts = copy.AsReadOnly();
        }

        public long ElectricityCostCents(double consumedKilowattHours)
        {
            if (double.IsNaN(consumedKilowattHours) || double.IsInfinity(consumedKilowattHours) || consumedKilowattHours < 0)
                throw new ArgumentOutOfRangeException(nameof(consumedKilowattHours));
            if (ElectricityCentsPerKilowattHour == 0) return 0;
            // M12 owns the double kWh. Convert once to decimal, then round the final bill once.
            decimal cents = checked((decimal)consumedKilowattHours * ElectricityCentsPerKilowattHour);
            return checked((long)decimal.Round(cents, 0, MidpointRounding.AwayFromZero));
        }
    }
}
