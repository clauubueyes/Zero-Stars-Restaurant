using System.Globalization;
using UnityEngine;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Food
{
    public sealed class FoodInspectionFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerInteraction _interaction;
        [SerializeField] private PhysicalCarry _carry;
        private GUIStyle _style;

        private void OnGUI()
        {
            if (_interaction == null || !_interaction.HasControl)
                return;
            FoodItem food = null;
            if (_carry != null && _carry.HasHeldObject)
                food = _carry.HeldBody.GetComponent<FoodItem>();
            else if (_interaction.FocusedTarget != null)
                food = _interaction.FocusedTarget.GetComponentInParent<FoodItem>();
            if (food == null || food.State == null)
                return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, fontSize = 16 };
                _style.normal.textColor = Color.white;
            }
            FoodState state = food.State;
            string text = string.Format(CultureInfo.InvariantCulture,
                "{0}\nFreshness: {1:0.0}%\nTemperature: {2:0.0}°C\nCondition: {3}\nAge: {4:0} s    Contaminated: {5}",
                state.Profile.DisplayName, state.FreshnessPercent, state.TemperatureCelsius,
                state.Condition, state.AgeSeconds, state.IsContaminated ? "Yes" : "No");
            text += state.Cooking == null ? "\nCooking: Not cookable" :
                string.Format(CultureInfo.InvariantCulture, "\nCooking: {0}\nCook progress: {1:0.0}%",
                    state.Cooking.Stage, state.Cooking.ProgressPercent);
            GUI.Box(new Rect(Screen.width / 2f - 260f, Screen.height / 2f + 84f, 520f, 160f), text, _style);
        }
    }
}
