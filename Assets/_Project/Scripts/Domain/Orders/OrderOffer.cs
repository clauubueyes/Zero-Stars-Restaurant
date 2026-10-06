using System;
using ZeroStarRestaurant.Dishes;

namespace ZeroStarRestaurant.Orders
{
    public sealed class OrderOffer
    {
        public DishProfile Dish { get; }
        public int SalePriceCents { get; }
        public OrderOffer(DishProfile dish, int salePriceCents)
        {
            Dish = dish ?? throw new ArgumentNullException(nameof(dish));
            if (salePriceCents <= 0) throw new ArgumentOutOfRangeException(nameof(salePriceCents));
            SalePriceCents = salePriceCents;
        }
    }
}
