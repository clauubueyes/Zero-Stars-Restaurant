using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroStarRestaurant.Restaurant;

namespace ZeroStarRestaurant.Customers
{
    [DisallowMultipleComponent]
    public sealed class CustomerQueueController : MonoBehaviour
    {
        [SerializeField] private QueuedCustomer _template;
        [SerializeField] private Transform[] _queuePoints = Array.Empty<Transform>();
        [SerializeField] private Transform[] _entrancePath = Array.Empty<Transform>();
        [SerializeField] private Transform _servicePosition;
        [SerializeField] private Transform _customersRoot;
        [SerializeField] private RestaurantDayController _restaurantDay;
        [SerializeField, Range(1, 4)] private int _capacity = 4;
        [SerializeField, Min(0.1f)] private float _arrivalIntervalSeconds = 3f;
        private readonly List<QueuedCustomer> _customers = new List<QueuedCustomer>();
        private IReadOnlyList<QueuedCustomer> _view;
        private CustomerQueueState _state;
        private double _arrivalRemaining;
        private double _initialDelaySeconds;
        public const float MinimumSpacing = 1.25f;
        public QueuedCustomer Template => _template;
        public IReadOnlyList<QueuedCustomer> Customers => _view ?? (_view = _customers.AsReadOnly());
        public QueuedCustomer Head => _customers.Count > 0 ? _customers[0] : null;
        public int Count => _customers.Count;
        public int Capacity => _capacity;
        public int AdmittedCount { get; private set; }
        public bool IsFull => _state != null && _state.IsFull;
        public double NextArrivalSeconds => Math.Max(0, _arrivalRemaining);
        public Transform ServicePosition => _servicePosition;
        public RestaurantDayController DayController => _restaurantDay;
        public bool CanAdmitCustomers => _restaurantDay == null || _restaurantDay.CanAdmitCustomers;
        public bool IsReadyForService => Head != null && Head.State.Stage == QueuedCustomerStage.Waiting &&
            (Head.transform.position - _servicePosition.position).sqrMagnitude < 0.0001f;

        public void Initialize(double initialDelaySeconds)
        {
            if (_state != null) return;
            Validate();
            if (double.IsNaN(initialDelaySeconds) || double.IsInfinity(initialDelaySeconds) || initialDelaySeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(initialDelaySeconds));
            _state = new CustomerQueueState(_capacity); _initialDelaySeconds = initialDelaySeconds; _arrivalRemaining = initialDelaySeconds;
        }
        public void RestartAdmissionDelay()
        {
            if (Count != 0) throw new InvalidOperationException("A new admission window requires an empty restaurant.");
            if (_state != null) _arrivalRemaining = _initialDelaySeconds;
        }
        public void Validate()
        {
            if (_template == null || _template.gameObject.activeSelf || _customersRoot == null || _capacity < 1 || _capacity > 4 ||
                _queuePoints == null || _queuePoints.Length < _capacity || _servicePosition == null || _queuePoints[0] != _servicePosition ||
                _entrancePath == null || _entrancePath.Length == 0 || float.IsNaN(_arrivalIntervalSeconds) ||
                float.IsInfinity(_arrivalIntervalSeconds) || _arrivalIntervalSeconds <= 0)
                throw new ArgumentException("Queue needs an inactive template, ordered points, approach path, root and valid interval/capacity.");
            _template.Validate();
            for (int index = 0; index < _queuePoints.Length; index++)
            {
                if (_queuePoints[index] == null) throw new ArgumentException("Queue points cannot be missing.");
                for (int previous = 0; previous < index; previous++)
                    if (Vector3.Distance(_queuePoints[index].position, _queuePoints[previous].position) < MinimumSpacing - 0.001f)
                        throw new ArgumentException("Queue points must be distinct and have customer clearance.");
            }
            foreach (Transform point in _entrancePath) if (point == null) throw new ArgumentException("Approach points cannot be missing.");
        }
        public bool TryAdmit(double? patienceSeconds = null)
        {
            if (!isActiveAndEnabled || !CanAdmitCustomers || _state == null || _state.IsFull || _template == null || _customersRoot == null) return false;
            Vector3 entry = _template.Movement.EntryPosition;
            foreach (QueuedCustomer customer in _customers)
                if ((customer.transform.position - entry).sqrMagnitude < MinimumSpacing * MinimumSpacing) return false;
            if (patienceSeconds.HasValue && (double.IsNaN(patienceSeconds.Value) || double.IsInfinity(patienceSeconds.Value) || patienceSeconds.Value < 0))
                throw new ArgumentOutOfRangeException(nameof(patienceSeconds));
            QueuedCustomer admitted = Instantiate(_template, _customersRoot);
            try
            {
                admitted.Initialize(AdmittedCount + 1, patienceSeconds);
                admitted.Movement.BeginQueueEntering(admitted.State.Number, _entrancePath, _queuePoints[_state.Count]);
                if (!_state.TryEnqueue(admitted.State)) throw new InvalidOperationException("Queue admission lost its reservation.");
                _customers.Add(admitted); AdmittedCount++;
                _arrivalRemaining = _arrivalIntervalSeconds; return true;
            }
            catch { admitted.gameObject.SetActive(false); Destroy(admitted.gameObject); throw; }
        }

        // CustomerServiceLoop is the only driver; this component has no Update.
        public void Advance(double elapsedSeconds, float speed, bool headWaitingForFood)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0 ||
                float.IsNaN(speed) || float.IsInfinity(speed) || speed <= 0) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (!isActiveAndEnabled || _state == null) return;
            foreach (QueuedCustomer customer in _customers)
            {
                if (customer == null || customer.Movement == null || customer.DishCarrier == null)
                    throw new InvalidOperationException("Queue lost a live customer.");
                QueuedCustomerState state = customer.State;
                if (state.Stage == QueuedCustomerStage.Waiting || (state.Stage == QueuedCustomerStage.Service && headWaitingForFood))
                    state.AdvancePatience(elapsedSeconds);
                if ((state.Stage == QueuedCustomerStage.Entering || state.Stage == QueuedCustomerStage.Advancing) && customer.Movement.Advance(elapsedSeconds, speed))
                {
                    state.Arrive(); customer.transform.rotation = Quaternion.identity;
                }
            }
            if (CanAdmitCustomers)
            {
                _arrivalRemaining = Math.Max(0, _arrivalRemaining - elapsedSeconds);
                if (_arrivalRemaining == 0) TryAdmit();
            }
        }
        public bool TryBeginService() => IsReadyForService && _state.TryBeginService(Head.State);
        public bool BeginHeadLeaving() => Head != null && _state.TryBeginLeaving(Head.State);
        public bool CompleteHeadExit()
        {
            QueuedCustomer retired = Head;
            if (retired == null || !_state.TryCompleteExit(retired.State)) return false;
            _customers.RemoveAt(0); retired.gameObject.SetActive(false); Destroy(retired.gameObject);
            foreach (QueuedCustomer customer in _customers) customer.Movement.RetargetQueuePoint(_queuePoints[customer.State.QueueIndex]);
            if (_restaurantDay != null) _restaurantDay.RefreshOccupancy();
            return true;
        }
        public void CancelAll(double nextDelaySeconds)
        {
            foreach (QueuedCustomer customer in _customers)
                if (customer != null) { customer.gameObject.SetActive(false); Destroy(customer.gameObject); }
            _customers.Clear(); _state?.Clear(); _arrivalRemaining = nextDelaySeconds;
            if (_restaurantDay != null) _restaurantDay.RefreshOccupancy();
        }
        private void OnDestroy() => CancelAll(0);
    }
}
