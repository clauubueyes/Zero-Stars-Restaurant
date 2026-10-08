using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroStarRestaurant.Cooking;

namespace ZeroStarRestaurant.Food
{
    public sealed class FoodSimulation : MonoBehaviour
    {
        [SerializeField] private FoodItem[] _foods = Array.Empty<FoodItem>();
        [SerializeField] private HeatSource[] _heatSources = Array.Empty<HeatSource>();
        [SerializeField, Tooltip("Temperature-based deterioration policy. Unassigned preserves the original M3/M4 rate.")]
        private FoodPreservationSettings _preservationSettings;
        [SerializeField, Min(-273.15f)] private float _ambientTemperatureCelsius = 21f;
        [SerializeField, Min(0f), Tooltip("Development only: scales food time, not player movement or Unity physics.")]
        private float _developmentTimeMultiplier = 1f;
        private readonly HashSet<FoodItem> _advancedFoods = new HashSet<FoodItem>();
        public IReadOnlyList<FoodItem> Foods => _foods;

        private void Update() => Advance(Time.deltaTime * (double)Mathf.Max(0f, _developmentTimeMultiplier));

        public bool Register(FoodItem food)
        {
            if (food == null || food.State == null) return false;
            foreach (FoodItem existing in _foods) if (existing == food) return false;
            var registered = new List<FoodItem>();
            foreach (FoodItem existing in _foods) if (existing != null) registered.Add(existing);
            registered.Add(food);
            _foods = registered.ToArray();
            return true;
        }

        public void Unregister(IReadOnlyList<FoodItem> foods)
        {
            if (foods == null) throw new ArgumentNullException(nameof(foods));
            var removed = new HashSet<FoodItem>(foods);
            _advancedFoods.RemoveWhere(food => food == null || removed.Contains(food));
            var remaining = new List<FoodItem>();
            foreach (FoodItem food in _foods) if (food != null && !removed.Contains(food)) remaining.Add(food);
            _foods = remaining.ToArray();
        }

        public void Advance(double elapsedSeconds)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0.0)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            var ambient = new ThermalEnvironment(_ambientTemperatureCelsius);
            FoodPreservationProfile preservation = _preservationSettings == null ? null : _preservationSettings.CreateProfile();
            Physics.SyncTransforms(); // Also makes manual development advances see the current placement.
            _advancedFoods.Clear();
            // Explicit scene references; already initialized inactive food still ages at ambient.
            // Destroyed objects are skipped, and no state is recreated on activation.
            foreach (FoodItem food in _foods)
            {
                if (food == null || food.State == null || !_advancedFoods.Add(food))
                    continue;
                ThermalEnvironment environment = ambient;
                bool hasSource = false;
                HeatSource selectedSource = null;
                foreach (HeatSource source in _heatSources)
                {
                    if (source == null || !source.isActiveAndEnabled ||
                        !source.TryGetEnvironment(food, out ThermalEnvironment candidate))
                        continue;
                    // One environment and one state advance. Strongest coupling wins;
                    // ties use serialized order, never multiple competing temperature updates.
                    if (!hasSource || candidate.ResponseMultiplier > environment.ResponseMultiplier)
                    {
                        environment = candidate;
                        hasSource = true; selectedSource = source;
                    }
                }
                double previousDose = food.State.Cooking?.EquivalentSeconds ?? 0;
                food.State.Advance(elapsedSeconds, environment, preservation: preservation);
                selectedSource?.RecordCookingUse(food.State.InstanceId, (food.State.Cooking?.EquivalentSeconds ?? 0) - previousDose);
            }
        }
    }
}
