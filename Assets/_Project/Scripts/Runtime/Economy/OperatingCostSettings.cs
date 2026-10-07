using System;
using UnityEngine;

namespace ZeroStarRestaurant.Economy
{
    [CreateAssetMenu(menuName = "Zero Star Restaurant/Operating Costs", fileName = "OperatingCostSettings")]
    public sealed class OperatingCostSettings : ScriptableObject
    {
        [Serializable]
        private sealed class FixedCostEntry
        {
            [SerializeField] public string Id = "rent";
            [SerializeField] public string Label = "Rent";
            [SerializeField, Min(0)] public int AmountCents = 300;
        }
        [SerializeField, Min(0)] private int _electricityCentsPerKilowattHour = 30;
        [SerializeField] private FixedCostEntry[] _fixedCosts = { new FixedCostEntry() };

        public OperatingCostPolicy CreatePolicy()
        {
            if (_fixedCosts == null) throw new ArgumentException("Daily fixed costs must be configured.");
            var costs = new DailyFixedCost[_fixedCosts.Length];
            for (int index = 0; index < costs.Length; index++)
            {
                FixedCostEntry entry = _fixedCosts[index];
                if (entry == null) throw new ArgumentException("Daily fixed costs cannot contain null entries.");
                costs[index] = new DailyFixedCost(entry.Id, entry.Label, entry.AmountCents);
            }
            return new OperatingCostPolicy(_electricityCentsPerKilowattHour, costs);
        }
    }
}
