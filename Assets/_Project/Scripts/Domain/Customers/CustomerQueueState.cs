using System;
using System.Collections.Generic;

namespace ZeroStarRestaurant.Customers
{
    public sealed class CustomerQueueState
    {
        private readonly List<QueuedCustomerState> _customers = new List<QueuedCustomerState>();
        public IReadOnlyList<QueuedCustomerState> Customers { get; }
        public int Capacity { get; }
        public int Count => _customers.Count;
        public bool IsFull => Count == Capacity;
        public QueuedCustomerState Head => Count > 0 ? _customers[0] : null;

        public CustomerQueueState(int capacity)
        {
            if (capacity < 1 || capacity > 4) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity; Customers = _customers.AsReadOnly();
        }
        public bool TryEnqueue(QueuedCustomerState customer)
        {
            if (customer == null || IsFull || customer.QueueIndex >= 0 || customer.Stage != QueuedCustomerStage.Entering) return false;
            foreach (QueuedCustomerState existing in _customers)
                if (existing.InstanceId == customer.InstanceId || existing.Number == customer.Number) return false;
            customer.AssignPosition(Count); _customers.Add(customer); return true;
        }
        public bool TryBeginService(QueuedCustomerState customer) => customer != null && ReferenceEquals(customer, Head) && customer.BeginService();
        public bool TryBeginLeaving(QueuedCustomerState customer) => customer != null && ReferenceEquals(customer, Head) && customer.BeginLeaving();
        public bool TryCompleteExit(QueuedCustomerState customer)
        {
            if (customer == null || !ReferenceEquals(customer, Head) || customer.Stage != QueuedCustomerStage.Leaving) return false;
            customer.Finish(); _customers.RemoveAt(0);
            for (int index = 0; index < Count; index++) _customers[index].AssignPosition(index);
            return true;
        }
        public void Clear()
        {
            foreach (QueuedCustomerState customer in _customers) customer.Finish();
            _customers.Clear();
        }
    }
}
