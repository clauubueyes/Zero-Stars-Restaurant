using System;
using UnityEngine;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Orders;

namespace ZeroStarRestaurant.Customers
{
    [DisallowMultipleComponent]
    public sealed class CustomerServiceLoop : MonoBehaviour
    {
        [SerializeField] private CustomerServiceConfiguration _configuration;
        [SerializeField] private CustomerMovement _customer;
        [SerializeField] private CustomerDishCarrier _dishCarrier;
        [SerializeField] private FoodSimulation _foodSimulation;
        [SerializeField, Min(-1), Tooltip("Development: -1 alternates the menu; 0/1 force the next spawned customer's menu slot once.")]
        private int _forceNextOfferIndex = -1;
        private OrderOffer[] _offers;
        private double _remainingSeconds;
        public PaymentLedger Ledger { get; private set; }
        public CustomerVisit Visit { get; private set; }
        public OrderResult LastResult { get; private set; }
        public int CustomerNumber { get; private set; }
        public double NextCustomerSeconds => Visit == null ? Math.Max(0, _remainingSeconds) : 0;

        private void Awake()
        {
            if (_configuration == null || _customer == null || !_customer.HasValidRoute || _dishCarrier == null ||
                !_dishCarrier.HasValidAnchor || _dishCarrier.transform != _customer.transform || _foodSimulation == null)
            { Debug.LogError("Customer service needs configuration, routed customer with dish carrier and the existing food simulation.", this); enabled = false; return; }
            try { _offers = _configuration.CreateOffers(); }
            catch (ArgumentException exception)
            { Debug.LogError("Invalid customer service configuration: " + exception.Message, this); enabled = false; return; }
            Ledger = new PaymentLedger(); _remainingSeconds = _configuration.InitialDelaySeconds; _customer.Hide();
        }
        private void Update() => Advance(Time.deltaTime);
        private void OnDisable()
        {
            RetireSoldDish();
            if (_customer != null) _customer.Hide();
            Visit = null;
            if (_configuration != null) _remainingSeconds = _configuration.InitialDelaySeconds;
        }
        public bool ForceNextOrder(int menuIndex)
        {
            if (_offers == null || menuIndex < 0 || menuIndex >= _offers.Length) return false;
            _forceNextOfferIndex = menuIndex; return true;
        }
        public void Advance(double elapsedSeconds)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (!isActiveAndEnabled || Ledger == null) return;
            if (_customer == null || _dishCarrier == null || !_dishCarrier.HasValidAnchor || _configuration == null)
            { Debug.LogError("Customer service lost its scene references.", this); enabled = false; return; }
            if (Visit == null)
            {
                _remainingSeconds -= elapsedSeconds;
                if (_remainingSeconds <= 0) SpawnNext();
                return;
            }
            switch (Visit.Stage)
            {
                case CustomerStage.Enter:
                    if (_customer.Advance(elapsedSeconds, _configuration.WalkingSpeed))
                    { Visit.Arrive(); _remainingSeconds = _configuration.OrderDisplaySeconds; }
                    break;
                case CustomerStage.Order:
                    _remainingSeconds -= elapsedSeconds;
                    if (_remainingSeconds <= 0) Visit.BeginWaiting();
                    break;
                case CustomerStage.Pay:
                case CustomerStage.Reject:
                    _remainingSeconds -= elapsedSeconds;
                    if (_remainingSeconds <= 0) { Visit.BeginLeaving(); _customer.BeginLeaving(); }
                    break;
                case CustomerStage.Leave:
                    if (_customer.Advance(elapsedSeconds, _configuration.WalkingSpeed))
                    { Visit.Finish(); RetireSoldDish(); _customer.Hide(); Visit = null; _remainingSeconds = _configuration.NextCustomerDelaySeconds; }
                    break;
            }
        }
        private void SpawnNext()
        {
            int menuIndex = CustomerNumber % _offers.Length;
            if (_forceNextOfferIndex >= 0 && _forceNextOfferIndex < _offers.Length) menuIndex = _forceNextOfferIndex;
            else if (_forceNextOfferIndex != -1) Debug.LogWarning("Invalid forced menu slot; using normal menu rotation.", this);
            _forceNextOfferIndex = -1;
            Visit = new CustomerVisit(new OrderState(_offers[menuIndex])); CustomerNumber++;
            _customer.BeginEntering(CustomerNumber);
        }
        public bool TryDeliver(DishItem dish)
        {
            if (!isActiveAndEnabled || Visit == null || Visit.Stage != CustomerStage.Wait || dish == null ||
                !dish.isActiveAndEnabled || dish.State == null || !dish.State.IsFinalized || dish.State.IsSold || !dish.IsIntact) return false;
            Pickup pickup = dish.GetComponent<Pickup>();
            if (pickup == null || !pickup.isActiveAndEnabled || pickup.IsHeld || _dishCarrier == null || !_dishCarrier.CanTake(dish)) return false;
            Visit.Receive(); Visit.BeginEvaluation();
            if (!OrderDelivery.TryComplete(Visit.Order, dish.State, Ledger, out OrderResult result))
            { Visit.ResumeWaiting(); return false; }
            Visit.Resolve(); LastResult = result; _remainingSeconds = _configuration.ResultDisplaySeconds;
            if (result.Accepted)
            {
                // Receipt is historical. The SAME sold dish remains visible with its original living units until exit.
                _dishCarrier.Take(dish);
            }
            return true;
        }

        private void RetireSoldDish()
        {
            DishItem dish = _dishCarrier != null ? _dishCarrier.Dish : null;
            if (_foodSimulation != null)
                _foodSimulation.Unregister(dish != null ? dish.GetComponentsInChildren<FoodItem>(true) : Array.Empty<FoodItem>());
            if (_dishCarrier != null) _dishCarrier.Clear();
        }
    }
}
