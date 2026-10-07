using System;
using UnityEngine;

namespace ZeroStarRestaurant.Food
{
    // Runs before any fixture's FoodItem.Awake. Normal sessions have no free supply.
    [DefaultExecutionOrder(-200)]
    public sealed class DevelopmentIngredientSupply : MonoBehaviour
    {
        [SerializeField] private FoodSimulation _simulation;
        [SerializeField] private FoodItem[] _fixtures = Array.Empty<FoodItem>();
        [SerializeField] private GameObject[] _fixtureSupports = Array.Empty<GameObject>();
        [SerializeField, Tooltip("Development/testing only: enable the old M3-M7 free fixtures.")]
        private bool _enableOnStart;

        private void Awake()
        {
            foreach (GameObject support in _fixtureSupports) if (support != null) support.SetActive(_enableOnStart);
            if (_enableOnStart)
            {
                EnableForDevelopment();
                return;
            }
            if (_simulation != null) _simulation.Unregister(_fixtures);
            foreach (FoodItem food in _fixtures) if (food != null) food.gameObject.SetActive(false);
        }

        [ContextMenu("Development: enable free ingredient fixtures")]
        public void EnableForDevelopment()
        {
            if (!Application.isPlaying || _simulation == null) return;
            foreach (GameObject support in _fixtureSupports) if (support != null) support.SetActive(true);
            foreach (FoodItem food in _fixtures)
            {
                if (food == null) continue;
                food.gameObject.SetActive(true);
                _simulation.Register(food);
            }
        }
    }
}
