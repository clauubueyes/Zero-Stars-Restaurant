using System;

namespace ZeroStarRestaurant.Orders
{
    public sealed class OrderState
    {
        public Guid InstanceId { get; }
        public OrderOffer Offer { get; }
        public OrderResult Result { get; private set; }
        public bool IsCompleted => Result != null;
        public OrderState(OrderOffer offer, Guid? instanceId = null)
        {
            Offer = offer ?? throw new ArgumentNullException(nameof(offer));
            InstanceId = instanceId ?? Guid.NewGuid();
            if (InstanceId == Guid.Empty) throw new ArgumentException("Order identity cannot be empty.", nameof(instanceId));
        }
        internal void Complete(OrderResult result) => Result = result;
    }
}
