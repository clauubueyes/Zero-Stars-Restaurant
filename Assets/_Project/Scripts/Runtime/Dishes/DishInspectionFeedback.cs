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
        private GUIStyle _style;
        private GUIStyle _headlineStyle;
        private Vector2 _scroll;

        public static string RecognitionLabel(DishItem dish, AssemblySurface surface) => "Recognized: " +
            (dish.State.IsFinalized ? dish.State.DisplayName : dish.State.Components.Count == 0 ? "None (empty)" :
                surface?.PreviewDefinition?.DisplayName ?? "Custom Dish");

        private void OnGUI()
        {
            if (_interaction == null || !_interaction.HasControl || _detector == null) return;
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
            if (dish == null || dish.State == null) return;
            if (_style == null)
                _style = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            DishState state = dish.State;
            if (_headlineStyle == null)
            {
                _headlineStyle = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                _headlineStyle.normal.textColor = Color.white;
            }
            // Always visible, even at small resolutions or with many ingredients; never inside the scroll view.
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
            float height = Mathf.Clamp(Screen.height / 2f - 120f, 64f, 250f);
            var rect = new Rect(12f, 12f, Mathf.Min(560f, Screen.width - 24f), height);
            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f));
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label(text.ToString(), _style);
            GUILayout.EndScrollView(); GUILayout.EndArea();
            GUI.Box(new Rect(Screen.width / 2f - 260f, Screen.height / 2f - 100f, 520f, 72f), headline, _headlineStyle);
        }
    }
}
