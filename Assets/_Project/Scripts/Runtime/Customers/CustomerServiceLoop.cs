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
        [SerializeField] private RestaurantReputation _reputation;
        public RestaurantReputation Reputation => _reputation;
        [SerializeField] private CustomerMovement _customer;
        [SerializeField] private CustomerDishCarrier _dishCarrier;
        [SerializeField] private FoodSimulation _foodSimulation;
        [SerializeField] private CustomerQueueController _queue;
        [SerializeField] private DeliveryZone _deliveryZone;
        [SerializeField, Tooltip("Development/testing: turn off to advance the same service clock explicitly with Advance.")]
        private bool _advanceAutomatically = true;
        [SerializeField, Min(0), Tooltip("Development seed money only. Final gameplay starts at zero; first supply remains a design debt.")]
        private int _developmentInitialBalanceCents;
        [SerializeField, Min(-1), Tooltip("Development: -1 alternates the menu; 0/1 force the next served customer's menu slot once.")]
        private int _forceNextOfferIndex = -1;
        private OrderOffer[] _offers;
        private DishProfile[] _deliveryRecipes = Array.Empty<DishProfile>();
        private double _remainingSeconds;
        private double _queueElapsedSeconds;
        private const double QueueStepSeconds = 0.05;
        public CustomerQueueController Queue => _queue;
        public DeliveryZone DeliveryZone => _deliveryZone;
        public FoodSimulation FoodSimulation => _foodSimulation;
        public System.Collections.Generic.IReadOnlyList<DishProfile> DeliveryRecipes => _deliveryRecipes;
        public CustomerMovement ActiveCustomer => Visit != null ? _customer : null;
        public CustomerDishCarrier ActiveDishCarrier => Visit != null ? _dishCarrier : null;
        public PaymentLedger Ledger { get; private set; }
        public CustomerVisit Visit { get; private set; }
        public OrderResult LastResult { get; private set; }
        public CustomerConsequence LastConsequence { get; private set; }
        public CustomerServiceStatistics Statistics { get; } = new CustomerServiceStatistics();
        public int ResultRevision { get; private set; }
        public int LastResultCustomerNumber { get; private set; }
        public int CustomerNumber { get; private set; }
        public double NextCustomerSeconds => _queue != null ? _queue.NextArrivalSeconds : Visit == null ? Math.Max(0, _remainingSeconds) : 0;

        private void Awake()
        {
            if (_configuration == null || _customer == null || !_customer.HasValidRoute || _dishCarrier == null ||
                !_dishCarrier.HasValidAnchor || _dishCarrier.transform != _customer.transform || _foodSimulation == null)
            { Debug.LogError("Customer service needs configuration, routed customer with dish carrier and the existing food simulation.", this); enabled = false; return; }
            try
            {
                _offers = _configuration.CreateOffers();
                _deliveryRecipes = new DishProfile[_offers.Length];
                for (int i = 0; i < _offers.Length; i++) _deliveryRecipes[i] = _offers[i].Dish;
                if (_queue != null) _queue.Initialize(_configuration.InitialDelaySeconds);
            }
            catch (ArgumentException exception)
            { Debug.LogError("Invalid customer service configuration: " + exception.Message, this); enabled = false; return; }
            if (_developmentInitialBalanceCents < 0)
            { Debug.LogError("Development starting balance cannot be negative.", this); enabled = false; return; }
            Ledger = new PaymentLedger(_developmentInitialBalanceCents); _remainingSeconds = _configuration.InitialDelaySeconds; _customer.Hide();
        }
        private void Update() { if (_advanceAutomatically) Advance(Time.deltaTime); }
        private void OnDisable()
        {
            // Consumed hazards still resolve if development cancels the remaining walk.
            if (Visit?.Consequence?.Reaction == CustomerReaction.HealthIncident) _reputation?.CompleteVisit(Visit.Consequence);
            RetireSoldDish();
            if (_customer != null) _customer.Hide();
            if (_queue != null)
            {
                _queue.CancelAll(_configuration != null ? _configuration.InitialDelaySeconds : 0);
                RestoreTemplateReferences();
            }
            _queueElapsedSeconds = 0;
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
            if (_queue != null)
            {
                if (!_queue.isActiveAndEnabled) return;
                _queueElapsedSeconds += elapsedSeconds;
                while (_queueElapsedSeconds + 0.000000001 >= QueueStepSeconds)
                {
                    _queueElapsedSeconds = Math.Max(0, _queueElapsedSeconds - QueueStepSeconds);
                    try { AdvanceQueueStep(QueueStepSeconds); }
                    catch (InvalidOperationException exception)
                    { Debug.LogError("Customer queue failed: " + exception.Message, this); enabled = false; return; }
                }
                return;
            }
            if (Visit == null)
            {
                _remainingSeconds -= elapsedSeconds;
                if (_remainingSeconds <= 0) SpawnNext();
                return;
            }
            AdvanceVisit(elapsedSeconds);
        }
        private void AdvanceQueueStep(double elapsedSeconds)
        {
            _queue.Advance(elapsedSeconds, _configuration.WalkingSpeed,
                Visit != null && (Visit.Stage == CustomerStage.Order || Visit.Stage == CustomerStage.Wait));
            if (Visit == null)
            {
                if (_queue.TryBeginService())
                {
                    _customer = _queue.Head.Movement; _dishCarrier = _queue.Head.DishCarrier;
                    SpawnNext(_queue.Head.State.InstanceId);
                }
                return;
            }
            AdvanceVisit(elapsedSeconds);
        }
        private void AdvanceVisit(double elapsedSeconds)
        {
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
                    if (_remainingSeconds <= 0)
                    {
                        Visit.BeginLeaving(); _customer.BeginLeaving();
                        if (_queue != null && !_queue.BeginHeadLeaving()) throw new InvalidOperationException("Only the serviced queue head can leave.");
                    }
                    break;
                case CustomerStage.Leave:
                    if (_customer.Advance(elapsedSeconds, _configuration.WalkingSpeed))
                    {
                        Visit.Finish(); _reputation?.CompleteVisit(Visit.Consequence); RetireSoldDish(); _customer.Hide(); Visit = null;
                        if (_queue != null)
                        {
                            if (!_queue.CompleteHeadExit()) throw new InvalidOperationException("Exit must release the serviced queue head once.");
                            RestoreTemplateReferences();
                        }
                        _remainingSeconds = _configuration.NextCustomerDelaySeconds;
                    }
                    break;
            }
            // Transfer ownership stays with the active customer through every result/exit step.
            _dishCarrier.SynchronizePose();
        }
        private void SpawnNext(Guid? queuedCustomerId = null)
        {
            int menuIndex = queuedCustomerId.HasValue ? (_queue.Head.State.Number - 1) % _offers.Length : CustomerNumber % _offers.Length;
            if (_forceNextOfferIndex >= 0 && _forceNextOfferIndex < _offers.Length) menuIndex = _forceNextOfferIndex;
            else if (_forceNextOfferIndex != -1) Debug.LogWarning("Invalid forced menu slot; using normal menu rotation.", this);
            _forceNextOfferIndex = -1;
            var order = new OrderState(_offers[menuIndex]);
            Visit = queuedCustomerId.HasValue ? new CustomerVisit(order, queuedCustomerId.Value) : new CustomerVisit(order);
            if (queuedCustomerId.HasValue)
            {
                CustomerNumber = _queue.Head.State.Number; Visit.Arrive(); _remainingSeconds = _configuration.OrderDisplaySeconds;
            }
            else { CustomerNumber++; _customer.BeginEntering(CustomerNumber); }
        }
        // Compatibility entry point: every submission still has to be on the same pad.
        public bool TryDeliver(DishItem dish) => _deliveryZone != null && _deliveryZone.TryDeliver(dish);
        public bool TryDeliver(FoodItem food) => _deliveryZone != null && _deliveryZone.TryDeliver(food);

        internal bool TryAcceptDelivery(DeliveryZone source, DishItem dish)
            => dish != null && TryAcceptDelivery(source, new PhysicalDelivery(dish));

        internal bool TryAcceptDelivery(DeliveryZone source, PhysicalDelivery delivery)
        {
            if (source == null || source != _deliveryZone || !source.isActiveAndEnabled ||
                !isActiveAndEnabled || Visit == null || Visit.Stage != CustomerStage.Wait || delivery == null || !delivery.IsAvailable) return false;
            if (_queue != null && (!_queue.isActiveAndEnabled || _queue.Head == null || _queue.Head.State.Stage != QueuedCustomerStage.Service ||
                _queue.Head.State.InstanceId != Visit.InstanceId || _queue.Head.Movement != _customer ||
                (_customer.transform.position - _queue.ServicePosition.position).sqrMagnitude > 0.0001f)) return false;
            if (_dishCarrier == null || !_dishCarrier.CanTake(delivery)) return false;
            Visit.Receive(); Visit.BeginEvaluation();
            if (!_dishCarrier.TryReceive(delivery, Visit.Order, Ledger, out OrderResult result))
            { Visit.ResumeWaiting(); return false; }
            Visit.Resolve(); LastResult = result; LastResultCustomerNumber = CustomerNumber; ResultRevision++;
            LastConsequence = Visit.Consequence;
            if (LastConsequence != null) Statistics.TryRecord(LastConsequence);
            _reputation?.RegisterService(LastConsequence);
            _remainingSeconds = _configuration.ResultDisplaySeconds;
            return true;
        }

        private void RetireSoldDish()
        {
            if (_foodSimulation != null)
                _foodSimulation.Unregister(_dishCarrier != null ? _dishCarrier.Foods : Array.Empty<FoodItem>());
            if (_dishCarrier != null) _dishCarrier.Clear();
        }
        private void RestoreTemplateReferences()
        {
            if (_queue.Template == null) return;
            _customer = _queue.Template.Movement; _dishCarrier = _queue.Template.DishCarrier;
        }
    }
}
