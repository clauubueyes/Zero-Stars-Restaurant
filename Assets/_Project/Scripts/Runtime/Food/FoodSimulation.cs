using System;
using UnityEngine;

namespace ZeroStarRestaurant.Food
{
    public sealed class FoodSimulation : MonoBehaviour
    {
        [SerializeField] private FoodItem[] _foods = Array.Empty<FoodItem>();
        [SerializeField, Min(-273.15f)] private float _ambientTemperatureCelsius = 21f;
        [SerializeField, Min(0f), Tooltip("Development only: scales food time, not player movement or Unity physics.")]
        private float _developmentTimeMultiplier = 1f;

        private void Update() => Advance(Time.deltaTime * (double)Mathf.Max(0f, _developmentTimeMultiplier));

        public void Advance(double elapsedSeconds)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0.0)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            // Explicit scene references; already initialized inactive food still ages.
            // Destroyed objects are skipped, and no state is recreated on activation.
            foreach (FoodItem food in _foods)
                if (food != null && food.State != null)
                    food.State.Advance(elapsedSeconds, _ambientTemperatureCelsius);
        }
    }
}
