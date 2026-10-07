using System;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Orders;

namespace ZeroStarRestaurant.Customers
{
    public enum CustomerReaction { Satisfied, Unhappy, Complaint, HealthIncident }

    public sealed class CustomerConsequence
    {
        public Guid CustomerId { get; }
        public Guid OrderId => Result.Evaluation.OrderId;
        public OrderResult Result { get; }
        public FoodQualityEvaluation Quality { get; }
        public CustomerReaction Reaction { get; }

        internal CustomerConsequence(Guid customerId, OrderResult result)
        {
            CustomerId = customerId; Result = result;
            Quality = new FoodQualityEvaluation(result.Evaluation.DeliveredDish);
            Reaction = Quality.IsGood ? CustomerReaction.Satisfied : CustomerReaction.Unhappy;
            foreach (FoodQualityCause cause in Quality.Causes)
            {
                if (cause.IsHealthHazard) { Reaction = CustomerReaction.HealthIncident; break; }
                if (cause.Issue == FoodQualityIssue.Burnt) Reaction = CustomerReaction.Complaint;
            }
        }
    }
}
