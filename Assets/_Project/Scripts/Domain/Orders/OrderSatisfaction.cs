using System;
using System.Collections.Generic;

namespace ZeroStarRestaurant.Orders
{
    // A multiset comparison, independent of recognition, cooking and safety evidence.
    public sealed class OrderSatisfaction
    {
        public IReadOnlyList<string> Expected { get; }
        public IReadOnlyList<string> Received { get; }
        public IReadOnlyList<string> Missing { get; }
        public IReadOnlyList<string> Extra { get; }
        public long ExpectedWeight { get; }
        public long ReceivedWeight { get; }
        public bool IsComplete => Missing.Count == 0;
        public bool IsExact => IsComplete && Extra.Count == 0;
        public bool IsRelevant => ReceivedWeight > 0;
        public decimal Completeness => (decimal)ReceivedWeight / ExpectedWeight;

        internal OrderSatisfaction(OrderOffer offer, DishSnapshot snapshot)
        {
            Expected = offer.Dish.IngredientDefinitionIds;
            var received = new List<string>();
            foreach (IngredientSnapshot food in snapshot.Ingredients) received.Add(food.Profile.Id);
            Received = received.AsReadOnly(); var unmatched = new List<string>(received); var missing = new List<string>();
            for (int slot = 0; slot < Expected.Count; slot++)
            {
                int weight = offer.IngredientWeights[slot]; ExpectedWeight = checked(ExpectedWeight + weight);
                int index = unmatched.FindIndex(id => string.Equals(id, Expected[slot], StringComparison.Ordinal));
                if (index < 0) missing.Add(Expected[slot]);
                else { unmatched.RemoveAt(index); ReceivedWeight = checked(ReceivedWeight + weight); }
            }
            Missing = missing.AsReadOnly(); Extra = unmatched.AsReadOnly();
        }

        // Integer cents, round down once. Extras earn nothing; all expected units earn the maximum.
        public int PaymentCents(int basePriceCents)
        {
            if (basePriceCents <= 0) throw new ArgumentOutOfRangeException(nameof(basePriceCents));
            return (int)decimal.Floor((decimal)basePriceCents * ReceivedWeight / ExpectedWeight);
        }
    }
}
