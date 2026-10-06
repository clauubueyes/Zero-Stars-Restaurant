using System;
using System.Collections.Generic;

namespace ZeroStarRestaurant.Economy
{
    public sealed class PaymentLedger
    {
        private readonly HashSet<Guid> _processedOrders = new HashSet<Guid>();
        private readonly HashSet<Guid> _soldDishes = new HashSet<Guid>();
        public long BalanceCents { get; private set; }
        public PaymentLedger(long initialBalanceCents = 0)
        {
            if (initialBalanceCents < 0) throw new ArgumentOutOfRangeException(nameof(initialBalanceCents));
            BalanceCents = initialBalanceCents;
        }
        // No knowledge of customers, ingredients, cooking, freshness or recognition rules.
        public bool TryRecord(Guid orderId, Guid dishId, int salePriceCents, bool accepted)
        {
            if (orderId == Guid.Empty || dishId == Guid.Empty || salePriceCents <= 0 ||
                _processedOrders.Contains(orderId) || _soldDishes.Contains(dishId)) return false;
            int payment = accepted ? salePriceCents : 0;
            if (BalanceCents > long.MaxValue - payment) return false;
            _processedOrders.Add(orderId);
            if (accepted) _soldDishes.Add(dishId);
            BalanceCents += payment;
            return true;
        }
    }
}
