using UnityEngine;

namespace ZeroStarRestaurant.Hygiene
{
    [CreateAssetMenu(menuName = "Zero Star Restaurant/Food Safety Settings")]
    public sealed class FoodSafetySettings : ScriptableObject
    {
        [SerializeField, Range(0, 1)] private float _rawMeatIntensity = .8f;
        [SerializeField, Range(0, 1)] private float _foodToSurfaceFraction = .5f;
        [SerializeField, Range(0, 1)] private float _surfaceToFoodFraction = .5f;
        public FoodSafetyPolicy CreatePolicy() => new FoodSafetyPolicy(
            _rawMeatIntensity, _foodToSurfaceFraction, _surfaceToFoodFraction);
    }
}
