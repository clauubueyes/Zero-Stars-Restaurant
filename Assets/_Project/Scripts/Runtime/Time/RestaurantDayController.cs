using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Economy;

namespace ZeroStarRestaurant.Restaurant
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class RestaurantDayController : MonoBehaviour
    {
        [SerializeField] private CustomerQueueController _queue;
        [SerializeField] private RestaurantReputation _reputation;
        public RestaurantReputation Reputation => _reputation;
        [SerializeField] private RestaurantOperatingCosts _operatingCosts;
        [SerializeField] private bool _requiresOperatingCosts;
        [SerializeField, Range(0, 23)] private int _startingHour = 8;
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
        public RestaurantCalendar Calendar => State?.Calendar;
        public EndOfDaySummary Summary { get; private set; }
        private readonly List<EndOfDaySummary> _summaries = new List<EndOfDaySummary>();
        public IReadOnlyList<EndOfDaySummary> Summaries => _summaries.AsReadOnly();
        // Runtime boundaries publish after the corresponding accounting/reset hooks.
        public event Action<int> DayStarted;
        public event Action<int> RestaurantOpened;
        public event Action<int> RestaurantClosing;
        public event Action<int> RestaurantClosed;
        public event Action<int> DayEnded;
        public CustomerQueueController Queue => _queue;
        public RestaurantOperatingCosts OperatingCosts => _operatingCosts;
        public bool CanAdmitCustomers => isActiveAndEnabled && State != null && State.CanAdmitCustomers;

        public void Validate()
        {
            if (_queue == null || _queue.DayController != this) throw new ArgumentException("Day and queue need explicit mutual references.");
            if (_requiresOperatingCosts && _operatingCosts == null) throw new ArgumentException("This day requires operating costs.");
            if (_operatingCosts != null) _operatingCosts.Validate();
            RequireClockField(_startingHour, 23); RequireClockField(_openingHour, 23); RequireClockField(_closingHour, 23);
            RequireClockField(_startingMinute, 59); RequireClockField(_openingMinute, 59); RequireClockField(_closingMinute, 59);
            RequireSpeed(); CreateState();
        }
        private RestaurantDay CreateState() => new RestaurantDay(_startingHour * 60 + _startingMinute,
            _openingHour * 60 + _openingMinute, _closingHour * 60 + _closingMinute);
        private void Awake()
        {
            try { Validate(); State = CreateState(); State.SetPaused(_paused);
                State.RestaurantOpened += number => RestaurantOpened?.Invoke(number);
                State.RestaurantClosing += number => RestaurantClosing?.Invoke(number);
                State.RestaurantClosed += number => RestaurantClosed?.Invoke(number); }
            catch (ArgumentException exception) { Debug.LogError("Invalid restaurant day configuration: " + exception.Message, this); enabled = false; }
        }
        private void Start() { if (_startAutomatically) StartNextDay(); }
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
            _reputation?.ResolveDue();
        }
        public void RefreshOccupancy()
        {
            if (State == null) return;
            State.Advance(0, _queue.Count);
        }
        [ContextMenu("Development: Open Restaurant")]
        public void OpenFromInspector() => ReportDevelopmentResult(OpenRestaurant(), "Open Restaurant");
        public bool OpenRestaurant()
        {
            if (!isActiveAndEnabled || State == null || State.Stage != RestaurantDayStage.Preparation || _queue.Count != 0) return false;
            _queue.RestartAdmissionDelay();
            return State.TryOpenRestaurant();
        }
        [ContextMenu("Development: Force Close (finish existing customers)")]
        public void ForceCloseFromInspector() => ReportDevelopmentResult(ForceClose(), "Force Close");
        public bool ForceClose() => isActiveAndEnabled && State != null && State.TryForceClose(_queue.Count);

        [ContextMenu("Development: End Current Day")]
        public void EndDayFromInspector() => ReportDevelopmentResult(EndCurrentDay(), "End Current Day");
        public bool EndCurrentDay()
        {
            if (!isActiveAndEnabled || State == null || State.Stage != RestaurantDayStage.Closed || _queue.Count != 0 ||
                (_requiresOperatingCosts && _operatingCosts == null)) return false;
            if (_operatingCosts != null)
            {
                // No charge or reset is permitted while a visit is still entering/being served/leaving.
                if (_operatingCosts.Service.Statistics.DayNumber != Calendar.CurrentDay || !_operatingCosts.TrySettleClosedDay()) return false;
                Summary = new EndOfDaySummary(_operatingCosts.Summary, _operatingCosts.Service.Statistics,
                    _reputation?.State, _operatingCosts.PendingElectricityCents);
                _summaries.Add(Summary);
            }
            if (!State.TryEndDay(_queue.Count)) return false;
            DayEnded?.Invoke(Calendar.CurrentDay);
            return true;
        }
        [ContextMenu("Development: Start Next Day")]
        public void StartDayFromInspector() => ReportDevelopmentResult(StartNextDay(), "Start Next Day");
        public bool StartNextDay()
        {
            if (!isActiveAndEnabled || State == null || (_requiresOperatingCosts && _operatingCosts == null) || _queue.Count != 0) return false;
            bool nextDay = State.Stage == RestaurantDayStage.EndOfDay;
            if (nextDay && _operatingCosts != null && (!_operatingCosts.CanBeginNextDay ||
                _operatingCosts.Service.Statistics.DayNumber != Calendar.CurrentDay)) return false;
            if (!State.TryStartDay(_queue.Count)) return false;
            if (nextDay && _operatingCosts != null)
            {
                _operatingCosts.BeginNextDay();
                if (!_operatingCosts.Service.Statistics.TryBeginNextDay(Calendar.CurrentDay))
                    throw new InvalidOperationException("Service statistics must follow the calendar exactly once.");
            }
            Summary = null;
            _queue.RestartAdmissionDelay();
            // Changing the calendar does not advance or resolve M16 deadlines.
            DayStarted?.Invoke(Calendar.CurrentDay);
            return true;
        }
        private void ReportDevelopmentResult(bool applied, string action)
        {
            Debug.Log(action + (applied ? " applied." : " unavailable; finish the current phase and all visits first. Required accounting must be enabled.") +
                " Day " + Calendar?.CurrentDay + " | " + State?.Stage + " | Customers: " + _queue.Count, this);
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
