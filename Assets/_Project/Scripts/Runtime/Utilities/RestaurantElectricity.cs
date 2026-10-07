using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroStarRestaurant.Utilities
{
    [DefaultExecutionOrder(-150)]
    [DisallowMultipleComponent]
    public sealed class RestaurantElectricity : MonoBehaviour
    {
        [SerializeField] private ElectricalAppliance[] _appliances = Array.Empty<ElectricalAppliance>();
        [SerializeField, Tooltip("Development override. A normal new session starts without power.")]
        private bool _initiallyOn;
        [SerializeField] private bool _advanceAutomatically = true;
        public ElectricitySupplyState State { get; private set; }
        public IReadOnlyList<ElectricalAppliance> Appliances => _appliances;
        public bool IsOn => isActiveAndEnabled && State != null && State.IsOn;
        public double CurrentWatts
        {
            get
            {
                double watts = 0;
                var unique = new HashSet<ElectricalAppliance>();
                foreach (ElectricalAppliance appliance in _appliances)
                    if (appliance != null && unique.Add(appliance) && appliance.Supply == this && appliance.IsOperating)
                        watts += appliance.RatedWatts;
                return watts;
            }
        }

        public void Validate()
        {
            if (_appliances == null) throw new ArgumentException("Explicit appliance list is required.");
            foreach (ElectricalAppliance appliance in _appliances)
            {
                if (appliance == null || appliance.Supply != this)
                    throw new ArgumentException("Every appliance must reference this supply.");
                appliance.Validate();
            }
        }

        private void Awake()
        {
            try { Validate(); State = new ElectricitySupplyState(_initiallyOn); }
            catch (ArgumentException exception) { Debug.LogError("Invalid restaurant electricity: " + exception.Message, this); enabled = false; }
        }
        private void Update() { if (_advanceAutomatically) Advance(UnityEngine.Time.deltaTime); }
        public void Advance(double simulationSeconds)
        {
            if (double.IsNaN(simulationSeconds) || double.IsInfinity(simulationSeconds) || simulationSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(simulationSeconds));
            if (!isActiveAndEnabled || State == null) return;
            Validate();
            var meters = new List<ElectricityMeter>();
            foreach (ElectricalAppliance appliance in _appliances)
                if (appliance.IsOperating) meters.Add(appliance.Meter);
            State.Advance(simulationSeconds, meters);
        }
        [ContextMenu("Development: Power ON")]
        public void PowerOn() => SetPower(true);
        [ContextMenu("Development: Power OFF")]
        public void PowerOff() => SetPower(false);
        public bool SetPower(bool isOn)
        {
            if (!isActiveAndEnabled || State == null) return false;
            State.SetOn(isOn); return true;
        }
    }
}
