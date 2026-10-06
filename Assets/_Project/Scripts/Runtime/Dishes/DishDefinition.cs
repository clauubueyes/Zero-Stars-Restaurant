using System;
using UnityEngine;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Dishes
{
    [CreateAssetMenu(menuName = "Zero Star Restaurant/Dish Definition", fileName = "NewDishDefinition")]
    public sealed class DishDefinition : ScriptableObject
    {
        [SerializeField] private string _id = "dish.new";
        [SerializeField] private string _displayName = "New Dish";
        [SerializeField] private FoodDefinition[] _orderedIngredients = Array.Empty<FoodDefinition>();
        [SerializeField] private bool _requiresStack = true;

        public DishProfile CreateProfile()
        {
            var ids = new string[_orderedIngredients.Length];
            for (int index = 0; index < ids.Length; index++)
            {
                if (_orderedIngredients[index] == null) throw new ArgumentException("Dish recognition needs valid food definitions.");
                ids[index] = _orderedIngredients[index].CreateProfile().Id;
            }
            return new DishProfile(_id, _displayName, ids, _requiresStack);
        }
    }
}
