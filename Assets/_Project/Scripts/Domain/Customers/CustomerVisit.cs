using System;
using ZeroStarRestaurant.Orders;

namespace ZeroStarRestaurant.Customers
{
    public sealed class CustomerVisit
    {
        public Guid InstanceId { get; } = Guid.NewGuid();
        public OrderState Order { get; }
        public CustomerStage Stage { get; private set; } = CustomerStage.Enter;
        public CustomerVisit(OrderState order) => Order = order ?? throw new ArgumentNullException(nameof(order));
        private bool Transition(CustomerStage expected, CustomerStage next)
        { if (Stage != expected) return false; Stage = next; return true; }
        public bool Arrive() => Transition(CustomerStage.Enter, CustomerStage.Order);
        public bool BeginWaiting() => Transition(CustomerStage.Order, CustomerStage.Wait);
        public bool Receive() => !Order.IsCompleted && Transition(CustomerStage.Wait, CustomerStage.Receive);
        public bool BeginEvaluation() => Transition(CustomerStage.Receive, CustomerStage.Evaluate);
        public bool Resolve()
        {
            if (Stage != CustomerStage.Evaluate || !Order.IsCompleted) return false;
            Stage = Order.Result.Accepted ? CustomerStage.Pay : CustomerStage.Reject; return true;
        }
        public bool ResumeWaiting()
        {
            if (Order.IsCompleted || (Stage != CustomerStage.Receive && Stage != CustomerStage.Evaluate)) return false;
            Stage = CustomerStage.Wait; return true;
        }
        public bool BeginLeaving() => Stage == CustomerStage.Pay ? Transition(CustomerStage.Pay, CustomerStage.Leave) :
            Transition(CustomerStage.Reject, CustomerStage.Leave);
        public bool Finish() => Transition(CustomerStage.Leave, CustomerStage.Finished);
    }
}
