using System;
using UnityEngine;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Restaurant;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Economy
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class RestaurantOperatingCosts : MonoBehaviour
    {
        [SerializeField] private RestaurantDayController _day;
        [SerializeField] private CustomerServiceLoop _service;
        [SerializeField] private RestaurantElectricity _electricity;
        [SerializeField] private OperatingCostSettings _settings;
        private OperatingCostPolicy _policy;
        public RestaurantDayController Day => _day;
        public CustomerServiceLoop Service => _service;
        public RestaurantElectricity Electricity => _electricity;
        public DailySummary Summary => _service != null && _service.Ledger != null ? _service.Ledger.CurrentSummary : null;

        public void Validate()
        {
            if (_day == null || _day.OperatingCosts != this || _service == null || _service.Queue != _day.Queue ||
                _electricity == null || _settings == null)
                throw new ArgumentException("Operating costs need explicit day, existing service ledger, electricity and settings.");
            _settings.CreatePolicy();
        }
        private void Awake()
        {
            try
            {
                Validate(); _policy = _settings.CreatePolicy();
                if (_service.Ledger == null || _electricity.State == null || _day.State == null)
                    throw new ArgumentException("Operating costs must initialize after day, service and electricity.");
                _electricity.BeginDay();
            }
            catch (ArgumentException exception)
            { Debug.LogError("Invalid operating costs: " + exception.Message, this); enabled = false; }
        }
        // RestaurantDayController invokes these boundaries; there is no extra Update/clock.
        public bool TrySettleClosedDay()
        {
            if (!isActiveAndEnabled || _policy == null || _day.State == null ||
                _day.State.Stage != RestaurantDayStage.Closed || _day.Queue.Count != 0 ||
                _service.Ledger.DayNumber != _day.State.Clock.DayNumber) return false;
            _service.Ledger.TrySettleDay(_policy, _electricity.State.DailyConsumedKilowattHours, out _);
            _electricity.CompleteDay();
            return true;
        }
        public bool CanBeginNextDay => isActiveAndEnabled && Summary != null && _service.Ledger.DayNumber < int.MaxValue;
        public void BeginNextDay()
        {
            if (!_service.Ledger.TryBeginNextDay(_day.State.Clock.DayNumber))
                throw new InvalidOperationException("Next day requires the previous day's settled ledger.");
            _electricity.BeginDay();
        }
    }
}
