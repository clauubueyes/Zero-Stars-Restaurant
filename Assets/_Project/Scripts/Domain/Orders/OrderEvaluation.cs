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
        private OrderEvaluation(OrderState order, DishState dish)
        {
            OrderId = order.InstanceId; RequestedDish = order.Offer.Dish;
            DeliveredDish = new DishSnapshot(dish);
            // Structural identity and quality/safety are separate dimensions.
            CorrectOrder = string.Equals(RequestedDish.Id, DeliveredDish.DefinitionId, StringComparison.Ordinal);
        }
        public static OrderEvaluation Capture(OrderState order, DishState dish)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (dish == null || !dish.IsFinalized || dish.IsDisposed || dish.IsSold || dish.Components.Count == 0)
                throw new ArgumentException("A finalized, available dish is required.", nameof(dish));
            return new OrderEvaluation(order, dish);
        }
    }
}
