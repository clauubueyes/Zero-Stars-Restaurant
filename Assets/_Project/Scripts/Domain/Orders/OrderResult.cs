namespace ZeroStarRestaurant.Orders
{
    public sealed class OrderResult
    {
        public OrderEvaluation Evaluation { get; }
        public bool Accepted => Evaluation.CorrectOrder;
        public int PaymentCents { get; }
        internal OrderResult(OrderEvaluation evaluation, int paymentCents)
        { Evaluation = evaluation; PaymentCents = paymentCents; }
    }
}
