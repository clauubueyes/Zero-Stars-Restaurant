using System;
using UnityEngine;
using ZeroStarRestaurant.Cooking;

namespace ZeroStarRestaurant.Utilities
{
    [DisallowMultipleComponent]
    public sealed class ElectricalAppliance : MonoBehaviour
    {
        [SerializeField] private RestaurantElectricity _supply;
        [SerializeField] private HeatSource _thermalSource;
        [SerializeField, Min(0), Tooltip("Nominal continuous load while the thermal appliance operates, including when empty.")]
        private float _ratedWatts = 100;
        public RestaurantElectricity Supply => _supply;
        public HeatSource ThermalSource => _thermalSource;
        public ElectricityMeter Meter { get; private set; }
        public double RatedWatts => Meter == null ? _ratedWatts : Meter.RatedWatts;
        public bool IsPowered => isActiveAndEnabled && _supply != null && _supply.IsOn;
        public bool IsOperating => IsPowered && Meter != null && _thermalSource != null && _thermalSource.IsOperational;

        public void Validate()
        {
            if (_supply == null || _thermalSource == null || _thermalSource.Electricity != this)
                throw new ArgumentException("Appliance needs explicit supply and mutual thermal source references.");
            if (float.IsNaN(_ratedWatts) || float.IsInfinity(_ratedWatts) || _ratedWatts < 0)
                throw new ArgumentException("Rated watts must be finite and nonnegative.");
        }

        private void Awake()
        {
            try { Validate(); Meter = new ElectricityMeter(_ratedWatts); }
            catch (ArgumentException exception) { Debug.LogError("Invalid electrical appliance: " + exception.Message, this); enabled = false; }
        }
    }
}
