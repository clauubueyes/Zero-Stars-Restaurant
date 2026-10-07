using System.Globalization;
using System.Text;
using UnityEngine;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Dishes
{
    public sealed class DishInspectionFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerInteraction _interaction;
        [SerializeField] private InteractionDetector _detector;
        [SerializeField] private PhysicalCarry _carry;
        [SerializeField] private DishAssemblyInteraction _assembly;
        public static string RecognitionLabel(DishItem dish, AssemblySurface surface) => "Recognized: " +
            (dish.State.IsFinalized ? dish.State.DisplayName : dish.State.Components.Count == 0 ? "None (empty)" :
                surface?.PreviewDefinition?.DisplayName ?? "Custom Dish");

        public string Text => Describe();

        private string Describe()
        {
            if (_interaction == null || !_interaction.HasControl || _detector == null) return "";
            DishItem dish = _carry != null && _carry.HasHeldObject ? _carry.HeldBody.GetComponent<DishItem>() : null;
            AssemblySurface surface = null;
            if (dish == null)
            {
                Interactable target = _detector.Detect(_carry != null ? _carry.HeldBody : null);
                if (target != null)
                {
                    surface = _assembly != null ? _assembly.FindFocusedSurface() : target.GetComponent<AssemblySurface>();
                    dish = surface != null ? surface.Dish : target.GetComponentInParent<DishItem>();
                }
            }
            if (dish == null || dish.State == null)
            {
                string preview = _assembly != null ? _assembly.PhysicalConfirmationPrompt() : null;
                return preview ?? "";
            }
            DishState state = dish.State;
            string headline = RecognitionLabel(dish, surface);
            if (surface != null && _assembly != null) headline += "\n" + _assembly.ConfirmationPrompt(surface);
            var text = new StringBuilder();
            text.AppendLine(state.IsFinalized ? state.DisplayName + "  #" + state.InstanceId.Value.ToString("N").Substring(0, 8) : "Assembly");
            text.AppendLine(state.Components.Count + " ingredients");
            foreach (var food in state.Components)
                text.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} #{1} | {2} | Freshness {3:0.0}% | {4:0.0}°C",
                    food.Profile.DisplayName, food.InstanceId.ToString("N").Substring(0, 8),
                    food.Cooking != null ? food.Cooking.Stage.ToString() : "Not cookable", food.FreshnessPercent, food.TemperatureCelsius));
            text.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "Worst freshness: {0}   Mean: {1}\nContains contamination: {2}   Spoiled/Rotten: {3}\nIngredient cost: {4} cents",
                state.MinimumFreshnessPercent.HasValue ? state.MinimumFreshnessPercent.Value.ToString("0.0", CultureInfo.InvariantCulture) + "%" : "N/A",
                state.MeanFreshnessPercent.HasValue ? state.MeanFreshnessPercent.Value.ToString("0.0", CultureInfo.InvariantCulture) + "%" : "N/A",
                state.ContainsContamination ? "Yes" : "No",
                state.ContainsSpoiledOrRotten ? "Yes" : "No", state.TotalIngredientCostCents));
            if (!dish.IsIntact) text.AppendLine("Missing physical components");
            return headline + "\n" + text;
        }
    }
}
