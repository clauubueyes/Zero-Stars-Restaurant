using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Orders
{
    // A submission of original objects, without manufacturing a Dish to evaluate loose food.
    public sealed class PhysicalDelivery
    {
        public DishItem Dish { get; }
        public DeliveryContents Contents { get; }
        public IReadOnlyList<FoodItem> Foods { get; }
        public IReadOnlyList<Transform> Roots { get; }

        public PhysicalDelivery(DishItem dish, IReadOnlyList<FoodItem> additionalFoods = null)
        {
            Dish = dish; Contents = new DeliveryContents(dish.State, additionalFoods?.Select(food => food.State));
            var foods = new List<FoodItem>(dish.GetComponentsInChildren<FoodItem>(true));
            var roots = new List<Transform> { dish.transform };
            if (additionalFoods != null)
                foreach (FoodItem food in additionalFoods) { foods.Add(food); roots.Add(food.transform); }
            Foods = foods.AsReadOnly(); Roots = roots.AsReadOnly();
        }

        public PhysicalDelivery(IReadOnlyList<FoodItem> foods, IReadOnlyList<DishProfile> recipes, bool isStack, PlateItem plate = null)
        {
            var originals = new List<FoodItem>(foods); Foods = originals.AsReadOnly();
            Contents = new DeliveryContents(originals.Select(food => food.State), recipes, isStack);
            var roots = originals.Select(food => food.transform).ToList();
            if (plate != null) roots.Add(plate.transform);
            Roots = roots.AsReadOnly();
        }

        public bool IsAvailable
        {
            get
            {
                if (!Contents.IsAvailable || (Dish != null && (!Dish.isActiveAndEnabled || !Dish.IsIntact))) return false;
                foreach (FoodItem food in Foods)
                    if (food == null || !food.isActiveAndEnabled || !Contents.Foods.Contains(food.State)) return false;
                foreach (Transform root in Roots)
                {
                    if (root == null || !root.gameObject.activeInHierarchy) return false;
                    Pickup pickup = root.GetComponent<Pickup>(); Rigidbody body = root.GetComponent<Rigidbody>();
                    if (pickup == null || !pickup.isActiveAndEnabled || pickup.IsHeld || body == null || body.isKinematic ||
                        !root.GetComponentsInChildren<Collider>().Any(c => c.enabled && !c.isTrigger && c.attachedRigidbody == body)) return false;
                }
                return true;
            }
        }
    }
}
