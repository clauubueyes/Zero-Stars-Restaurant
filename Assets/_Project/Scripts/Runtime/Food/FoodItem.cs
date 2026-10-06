using System;
using UnityEngine;

namespace ZeroStarRestaurant.Food
{
    [DisallowMultipleComponent]
    public sealed class FoodItem : MonoBehaviour
    {
        [SerializeField] private FoodDefinition _definition;
        [SerializeField, Min(0f)] private float _initialAgeSeconds;
        [SerializeField, Range(0f, 100f)] private float _initialFreshnessPercent = 100f;
        [SerializeField, Min(-273.15f)] private float _initialTemperatureCelsius = 21f;
        [SerializeField] private bool _initiallyContaminated;

        public FoodDefinition Definition => _definition;
        public FoodState State { get; private set; }

        private void Awake()
        {
            TryInitialize();
        }

        // Allows an inactive purchase prefab to prepare its own state before payment/activation.
        // Repeated calls preserve the same state; they never restore freshness or identity.
        public bool TryInitialize()
        {
            if (State != null) return true;
            if (_definition == null)
            {
                Debug.LogError("FoodItem needs a FoodDefinition assigned before activation.", this);
                enabled = false;
                return false;
            }
            try
            {
                State = new FoodState(_definition.CreateProfile(), _initialAgeSeconds,
                    _initialFreshnessPercent, _initialTemperatureCelsius, _initiallyContaminated);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError("Invalid food configuration: " + exception.Message, this);
                enabled = false;
                return false;
            }
            return true;
        }
    }
}
