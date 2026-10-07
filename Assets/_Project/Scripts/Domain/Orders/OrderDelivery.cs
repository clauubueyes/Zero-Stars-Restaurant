using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using System;
using System.Collections.Generic;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Orders
{
    public static class OrderDelivery
    {
        // One synchronous transaction on the simulation thread; no callbacks or mutable shared assets.
        public static bool TryComplete(OrderState order, DishState dish, PaymentLedger ledger, out OrderResult result)
        {
            result = null;
            if (order == null || order.IsCompleted || ledger == null || dish == null ||
                !dish.IsFinalized || dish.IsDisposed || dish.IsSold || dish.Components.Count == 0) return false;
            return TryComplete(order, new DeliveryContents(dish), ledger, out result);
        }
        public static bool TryComplete(OrderState order, DeliveryContents delivery, PaymentLedger ledger, out OrderResult result)
        {
            result = null;
            if (order == null || order.IsCompleted || ledger == null || delivery == null || !delivery.IsAvailable) return false;
            OrderEvaluation evaluation = OrderEvaluation.Capture(order, delivery);
            int payment = evaluation.Satisfaction.PaymentCents(order.Offer.SalePriceCents);
            var receipt = new OrderResult(evaluation, payment);
            var ids = new List<Guid>(); foreach (FoodState food in delivery.Foods) ids.Add(food.InstanceId);
            if (!ledger.TryRecord(order.InstanceId, delivery.InstanceId, order.Offer.SalePriceCents, payment, receipt.Accepted, ids)) return false;
            if (receipt.Accepted) delivery.MarkSold();
            order.Complete(receipt); result = receipt;
            return true;
        }
    }
}
