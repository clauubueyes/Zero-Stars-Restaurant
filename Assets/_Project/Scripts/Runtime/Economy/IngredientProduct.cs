using System;
using UnityEngine;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Economy
{
    [CreateAssetMenu(menuName = "Zero Star Restaurant/Ingredient Product", fileName = "IngredientProduct")]
    public sealed class IngredientProduct : ScriptableObject
    {
        [SerializeField] private FoodItem _prefab;
        [SerializeField, Min(1)] private int _priceCents = 35;
        public FoodItem Prefab => _prefab;
        public int PriceCents => _priceCents;

        public void Validate()
        {
            if (_priceCents <= 0 || _prefab == null || _prefab.gameObject.activeSelf || !_prefab.enabled ||
                _prefab.Definition == null || _prefab.GetComponent<Pickup>() == null ||
                !_prefab.GetComponent<Pickup>().enabled || _prefab.transform.parent != null)
                throw new ArgumentException("Products need a positive price and an inactive root FoodItem prefab with definition and Pickup.");
            Rigidbody body = _prefab.GetComponent<Rigidbody>();
            BoxCollider box = _prefab.GetComponent<BoxCollider>();
            if (body == null || body.isKinematic || !body.useGravity || box == null || !box.enabled || box.isTrigger ||
                _prefab.GetComponentsInChildren<FoodItem>(true).Length != 1 ||
                _prefab.GetComponentsInChildren<Collider>(true).Length != 1 ||
                _prefab.transform.localScale.x <= 0 || _prefab.transform.localScale.y <= 0 || _prefab.transform.localScale.z <= 0)
                throw new ArgumentException("Products need one physical food unit with a dynamic body and one solid box collider.");
            _prefab.Definition.CreateProfile();
        }
    }
}
