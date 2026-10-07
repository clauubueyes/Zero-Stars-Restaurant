using System;
using ZeroStarRestaurant.Dishes;

namespace ZeroStarRestaurant.Orders
{
    public sealed class OrderEvaluation
    {
        public Guid OrderId { get; }
        public DishProfile RequestedDish { get; }
        public DishSnapshot DeliveredDish { get; }
        public bool CorrectOrder { get; }
        public OrderSatisfaction Satisfaction { get; }
        public int BasePriceCents { get; }
        private OrderEvaluation(OrderState order, DeliveryContents delivery)
        {
            OrderId = order.InstanceId; RequestedDish = order.Offer.Dish;
            DeliveredDish = new DishSnapshot(delivery);
            Satisfaction = new OrderSatisfaction(order.Offer, DeliveredDish);
            BasePriceCents = order.Offer.SalePriceCents;
            // Structural identity and quality/safety are separate dimensions.
            CorrectOrder = Satisfaction.IsExact && string.Equals(RequestedDish.Id, DeliveredDish.DefinitionId, StringComparison.Ordinal);
        }
        public static OrderEvaluation Capture(OrderState order, DishState dish)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (dish == null || !dish.IsFinalized || dish.IsDisposed || dish.IsSold || dish.Components.Count == 0)
                throw new ArgumentException("A finalized, available dish is required.", nameof(dish));
            return Capture(order, new DeliveryContents(dish));
        }
        public static OrderEvaluation Capture(OrderState order, DeliveryContents delivery)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (delivery == null || !delivery.IsAvailable) throw new ArgumentException("Available original units are required.", nameof(delivery));
            return new OrderEvaluation(order, delivery);
        }
    }
}
