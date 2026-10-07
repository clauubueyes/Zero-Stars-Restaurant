using System;
using UnityEngine;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Economy
{
    [CreateAssetMenu(menuName = "Zero Star Restaurant/Plate Product", fileName = "Plate")]
    public sealed class PlateProduct : ScriptableObject
    {
        [SerializeField] private PlateItem _prefab;
        [SerializeField, Min(1)] private int _priceCents = 50;
        public PlateItem Prefab => _prefab;
        public int PriceCents => _priceCents;
        public void Validate()
        {
            if (_priceCents <= 0 || _prefab == null || !_prefab.enabled || _prefab.gameObject.activeSelf ||
                _prefab.transform.parent != null || _prefab.GetComponent<Pickup>() == null || !_prefab.GetComponent<Pickup>().enabled ||
                _prefab.GetComponentsInChildren<FoodItem>(true).Length != 0 ||
                _prefab.GetComponentsInChildren<DishItem>(true).Length != 0 ||
                _prefab.GetComponentsInChildren<PlateItem>(true).Length != 1)
                throw new ArgumentException("Plate needs a positive price and an inactive physical utensil prefab, without food or Dish.");
            Rigidbody body = _prefab.GetComponent<Rigidbody>(); BoxCollider box = _prefab.GetComponent<BoxCollider>();
            Vector3 scale = _prefab.transform.localScale;
            if (body == null || body.isKinematic || !body.useGravity || box == null || !box.enabled || box.isTrigger ||
                _prefab.GetComponentsInChildren<Collider>(true).Length != 1 || scale.x <= 0 || scale.y <= 0 || scale.z <= 0)
                throw new ArgumentException("Plate needs a dynamic body and one solid box collider.");
        }
    }
}
