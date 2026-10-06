using UnityEngine;
using ZeroStarRestaurant.Cooking;

namespace ZeroStarRestaurant.Food
{
    [CreateAssetMenu(menuName = "Zero Star Restaurant/Food Definition", fileName = "NewFoodDefinition")]
    public sealed class FoodDefinition : ScriptableObject
    {
        [SerializeField] private string _id = "food.new";
        [SerializeField] private string _displayName = "New Food";
        [SerializeField] private FoodCategory _category;
        [SerializeField, Min(0)] private int _referenceCostCents;
        [SerializeField, Min(1f)] private float _freshnessLifetimeSeconds = 600f;
        [SerializeField, Min(0.1f)] private float _thermalResponseSeconds = 60f;
        [SerializeField, Range(0f, 100f)] private float _freshMinimumPercent = 80f;
        [SerializeField, Range(0f, 100f)] private float _spoiledAtPercent = 30f;
        [SerializeField, Range(0f, 100f)] private float _rottenAtPercent = 5f;
        [Header("Cooking capability (independent of freshness)")]
        [SerializeField] private bool _isCookable;
        [SerializeField] private float _minimumCookingTemperatureCelsius = 60f;
        [SerializeField] private float _referenceCookingTemperatureCelsius = 120f;
        [SerializeField, Min(0.1f)] private float _cookedAtEquivalentSeconds = 45f;
        [SerializeField, Min(0.1f)] private float _overcookedAtEquivalentSeconds = 65f;
        [SerializeField, Min(0.1f)] private float _burntAtEquivalentSeconds = 90f;

        public string Id => _id;
        public string DisplayName => _displayName;

        public FoodProfile CreateProfile()
        {
            return new FoodProfile(_id, _displayName, _category, _referenceCostCents,
                _freshnessLifetimeSeconds, _thermalResponseSeconds,
                _freshMinimumPercent, _spoiledAtPercent, _rottenAtPercent,
                _isCookable ? new CookingProfile(_minimumCookingTemperatureCelsius,
                    _referenceCookingTemperatureCelsius, _cookedAtEquivalentSeconds,
                    _overcookedAtEquivalentSeconds, _burntAtEquivalentSeconds) : null);
        }
    }
}
