using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;

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
            OrderEvaluation evaluation = OrderEvaluation.Capture(order, dish);
            int payment = evaluation.CorrectOrder ? order.Offer.SalePriceCents : 0;
            var receipt = new OrderResult(evaluation, payment);
            if (!ledger.TryRecord(order.InstanceId, dish.InstanceId.Value, order.Offer.SalePriceCents, evaluation.CorrectOrder)) return false;
            if (evaluation.CorrectOrder) dish.MarkSold();
            order.Complete(receipt); result = receipt;
            return true;
        }
    }
}
