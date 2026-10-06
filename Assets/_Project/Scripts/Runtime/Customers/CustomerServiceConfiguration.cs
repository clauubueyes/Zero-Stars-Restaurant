using System;
using UnityEngine;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Orders;

namespace ZeroStarRestaurant.Customers
{
    [CreateAssetMenu(menuName = "Zero Star Restaurant/Customer Service Configuration", fileName = "CustomerService")]
    public sealed class CustomerServiceConfiguration : ScriptableObject
    {
        [Serializable]
        public sealed class MenuEntry
        {
            [SerializeField] private DishDefinition _dish;
            [SerializeField, Min(1)] private int _salePriceCents = 500;
            public OrderOffer CreateOffer() => new OrderOffer(_dish != null ? _dish.CreateProfile() :
                throw new ArgumentException("Menu entries need a dish definition."), _salePriceCents);
        }
        [SerializeField] private MenuEntry[] _menu = Array.Empty<MenuEntry>();
        [SerializeField, Min(0.1f)] private float _walkingSpeed = 1.5f;
        [SerializeField, Min(0f)] private float _initialDelaySeconds = 0.5f;
        [SerializeField, Min(0f)] private float _orderDisplaySeconds = 1f;
        [SerializeField, Min(0f)] private float _resultDisplaySeconds = 4f;
        [SerializeField, Min(0f)] private float _nextCustomerDelaySeconds = 3f;
        public float WalkingSpeed => _walkingSpeed;
        public float InitialDelaySeconds => _initialDelaySeconds;
        public float OrderDisplaySeconds => _orderDisplaySeconds;
        public float ResultDisplaySeconds => _resultDisplaySeconds;
        public float NextCustomerDelaySeconds => _nextCustomerDelaySeconds;

        public OrderOffer[] CreateOffers()
        {
            if (_menu == null || _menu.Length == 0) throw new ArgumentException("A non-empty customer menu is required.");
            RequireTime(_walkingSpeed, true); RequireTime(_initialDelaySeconds); RequireTime(_orderDisplaySeconds);
            RequireTime(_resultDisplaySeconds); RequireTime(_nextCustomerDelaySeconds);
            var offers = new OrderOffer[_menu.Length];
            for (int index = 0; index < _menu.Length; index++)
            {
                if (_menu[index] == null) throw new ArgumentException("Menu entries cannot be null.");
                offers[index] = _menu[index].CreateOffer();
                for (int previous = 0; previous < index; previous++)
                    if (offers[previous].Dish.Id == offers[index].Dish.Id) throw new ArgumentException("Menu dish IDs must be distinct.");
            }
            return offers;
        }
        private static void RequireTime(float value, bool positive = false)
        { if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || (positive && value == 0f)) throw new ArgumentException("Invalid service timing."); }
    }
}
