using System;
using UnityEngine;
using ZeroStarRestaurant.Customers;

namespace ZeroStarRestaurant.Restaurant
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class RestaurantDayController : MonoBehaviour
    {
        [SerializeField] private CustomerQueueController _queue;
        [SerializeField, Range(0, 23)] private int _startingHour = 9;
        [SerializeField, Range(0, 59)] private int _startingMinute;
        [SerializeField, Range(0, 23)] private int _openingHour = 9;
        [SerializeField, Range(0, 59)] private int _openingMinute;
        [SerializeField, Range(0, 23)] private int _closingHour = 17;
        [SerializeField, Range(0, 59)] private int _closingMinute;
        [SerializeField, Min(0), Tooltip("World seconds per simulation second; never scales food, customers, physics or Unity timeScale.")]
        private float _worldSecondsPerSimulationSecond = 60;
        [SerializeField] private bool _startAutomatically = true;
        [SerializeField] private bool _advanceAutomatically = true;
        [SerializeField, Tooltip("Pauses only the world clock; customers and food keep their existing simulation time.")]
        private bool _paused;
        public RestaurantDay State { get; private set; }
        public CustomerQueueController Queue => _queue;
        public bool CanAdmitCustomers => isActiveAndEnabled && State != null && State.CanAdmitCustomers;

        public void Validate()
        {
            if (_queue == null || _queue.DayController != this) throw new ArgumentException("Day and queue need explicit mutual references.");
            RequireClockField(_startingHour, 23); RequireClockField(_openingHour, 23); RequireClockField(_closingHour, 23);
            RequireClockField(_startingMinute, 59); RequireClockField(_openingMinute, 59); RequireClockField(_closingMinute, 59);
            RequireSpeed(); CreateState();
        }
        private RestaurantDay CreateState() => new RestaurantDay(_startingHour * 60 + _startingMinute,
            _openingHour * 60 + _openingMinute, _closingHour * 60 + _closingMinute);
        private void Awake()
        {
            try { Validate(); State = CreateState(); State.SetPaused(_paused); if (_startAutomatically) StartNextDay(); }
            catch (ArgumentException exception) { Debug.LogError("Invalid restaurant day configuration: " + exception.Message, this); enabled = false; }
        }
        private void Update() { if (_advanceAutomatically) Advance(UnityEngine.Time.deltaTime); }
        public void Advance(double simulationSeconds)
        {
            if (double.IsNaN(simulationSeconds) || double.IsInfinity(simulationSeconds) || simulationSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(simulationSeconds));
            if (!isActiveAndEnabled || State == null) return;
            RequireSpeed();
            double worldSeconds = simulationSeconds * _worldSecondsPerSimulationSecond;
            if (double.IsInfinity(worldSeconds)) worldSeconds = double.MaxValue;
            State.SetPaused(_paused); State.Advance(worldSeconds, _queue.Count);
        }
        public void RefreshOccupancy() { if (State != null) State.Advance(0, _queue.Count); }
        [ContextMenu("Development: Start Day / Next Day")]
        private void StartDayFromInspector() => StartNextDay();
        public bool StartNextDay()
        {
            if (!isActiveAndEnabled || State == null || !State.TryStartDay(_queue.Count)) return false;
            _queue.RestartAdmissionDelay(); return true;
        }
        [ContextMenu("Development: Pause World Clock")]
        public void PauseClock() { _paused = true; State?.SetPaused(true); }
        [ContextMenu("Development: Resume World Clock")]
        public void ResumeClock() { _paused = false; State?.SetPaused(false); }
        private void RequireSpeed()
        {
            if (float.IsNaN(_worldSecondsPerSimulationSecond) || float.IsInfinity(_worldSecondsPerSimulationSecond) || _worldSecondsPerSimulationSecond < 0)
                throw new ArgumentException("World clock speed must be finite and nonnegative.");
        }
        private static void RequireClockField(int value, int maximum)
        { if (value < 0 || value > maximum) throw new ArgumentException("Invalid hour/minute."); }
    }
}
