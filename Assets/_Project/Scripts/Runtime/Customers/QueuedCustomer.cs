using System;
using UnityEngine;

namespace ZeroStarRestaurant.Customers
{
    [DisallowMultipleComponent]
    public sealed class QueuedCustomer : MonoBehaviour
    {
        [SerializeField] private CustomerMovement _movement;
        [SerializeField] private CustomerDishCarrier _dishCarrier;
        [SerializeField, Min(0), Tooltip("Debug patience only. Zero does not abandon or penalize service.")]
        private float _initialPatienceSeconds = 180f;
        public CustomerMovement Movement => _movement;
        public CustomerDishCarrier DishCarrier => _dishCarrier;
        public QueuedCustomerState State { get; private set; }

        public void Validate()
        {
            if (_movement == null || _movement.transform != transform || !_movement.HasValidRoute || _dishCarrier == null ||
                _dishCarrier.transform != transform || !_dishCarrier.HasValidAnchor || float.IsNaN(_initialPatienceSeconds) ||
                float.IsInfinity(_initialPatienceSeconds) || _initialPatienceSeconds < 0)
                throw new ArgumentException("Queued customer needs local movement, dish carrier and finite nonnegative patience.");
        }
        public void Initialize(int number, double? patienceSeconds = null)
        {
            Validate();
            if (State != null) throw new InvalidOperationException("A queued customer's state cannot be reinitialized.");
            State = new QueuedCustomerState(number, patienceSeconds ?? _initialPatienceSeconds);
        }
    }
}
