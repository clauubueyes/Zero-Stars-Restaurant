using UnityEngine;

namespace ZeroStarRestaurant.Food
{
    [CreateAssetMenu(menuName = "Zero Star Restaurant/Food Preservation Settings", fileName = "PreservationSettings")]
    public sealed class FoodPreservationSettings : ScriptableObject
    {
        [SerializeField, Min(-273.15f)] private float _refrigeratedAtCelsius = 5f;
        [SerializeField, Min(-273.15f)] private float _frozenAtCelsius;
        [SerializeField, Range(0f, 1f)] private float _refrigeratedRate = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _frozenRate = 0.001f;

        public FoodPreservationProfile CreateProfile() => new FoodPreservationProfile(
            _refrigeratedAtCelsius, _frozenAtCelsius, _refrigeratedRate, _frozenRate);
    }
}
