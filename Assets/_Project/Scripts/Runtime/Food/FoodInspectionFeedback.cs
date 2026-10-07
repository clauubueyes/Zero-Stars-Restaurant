using System.Globalization;
using UnityEngine;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Food
{
    public sealed class FoodInspectionFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerInteraction _interaction;
        [SerializeField] private PhysicalCarry _carry;
        public string Text
        {
            get
            {
                if (_interaction == null || !_interaction.HasControl) return "";
                FoodItem food = _carry != null && _carry.HasHeldObject ? _carry.HeldBody.GetComponent<FoodItem>() :
                    _interaction.FocusedTarget != null ? _interaction.FocusedTarget.GetComponentInParent<FoodItem>() : null;
                if (food == null || food.State == null) return "";
                FoodState state = food.State;
                return string.Format(CultureInfo.InvariantCulture,
                    "{0} | {1}\nFreshness: {2:0.0}% | {3:0.0}°C\nContaminated: {4} | Cooking: {5}",
                    state.Profile.DisplayName, state.Condition, state.FreshnessPercent, state.TemperatureCelsius,
                    state.IsContaminated ? "Yes" : "No", state.Cooking == null ? "Not cookable" :
                    state.Cooking.Stage + " " + state.Cooking.ProgressPercent.ToString("0.0", CultureInfo.InvariantCulture) + "%");
            }
        }
    }
}
