using System.Globalization;
using System.Text;
using UnityEngine;
using ZeroStarRestaurant.Customers;

namespace ZeroStarRestaurant.Orders
{
    public sealed class OrderFeedback : MonoBehaviour
    {
        [SerializeField] private CustomerServiceLoop _service;
        private GUIStyle _style;
        private Vector2 _scroll;
        public static string Money(long cents) => "€" + (cents / 100m).ToString("0.00", CultureInfo.InvariantCulture);
        public static string ResultText(OrderResult result)
        {
            var evaluation = result.Evaluation; var dish = evaluation.DeliveredDish;
            var text = new StringBuilder("Order Result #" + evaluation.OrderId.ToString("N").Substring(0, 8) + "\n");
            text.AppendLine("Requested: " + evaluation.RequestedDish.DisplayName);
            text.AppendLine("Delivered: " + dish.DisplayName + " #" + dish.InstanceId.ToString("N").Substring(0, 8));
            text.AppendLine("Correct order: " + (evaluation.CorrectOrder ? "YES" : "NO"));
            text.AppendLine(string.Format(CultureInfo.InvariantCulture, "Worst freshness: {0:0.0}%   Mean: {1:0.0}%", dish.MinimumFreshnessPercent, dish.MeanFreshnessPercent));
            text.AppendLine("Spoiled ingredient: " + (dish.ContainsSpoiled ? "YES" : "NO"));
            text.AppendLine("Rotten ingredient: " + (dish.ContainsRotten ? "YES" : "NO"));
            text.AppendLine("Contaminated: " + (dish.ContainsContamination ? "YES" : "NO"));
            foreach (IngredientSnapshot food in dish.Ingredients)
                text.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} #{1}: {2} | {3} | {4:0.0}°C",
                    food.Profile.DisplayName, food.InstanceId.ToString("N").Substring(0, 8),
                    food.CookingStage.HasValue ? food.CookingStage.Value.ToString() : "Not cookable", food.Condition, food.TemperatureCelsius));
            text.AppendLine("Ingredient cost: " + Money(dish.TotalIngredientCostCents));
            text.AppendLine("Payment: " + Money(result.PaymentCents));
            text.AppendLine(result.Accepted ? "Accepted / Sold" : "Rejected / Dish remains available");
            return text.ToString();
        }
        private void OnGUI()
        {
            if (_service == null || _service.Ledger == null) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
                _style.normal.textColor = Color.white;
            }
            float width = Mathf.Min(360f, Screen.width - 24f);
            GUI.Box(new Rect(Screen.width - width - 12f, 12f, width, 120f), GUIContent.none);
            string order = "Balance: " + Money(_service.Ledger.BalanceCents) + "\n";
            if (_service.Visit == null) order += "Next customer in " + _service.NextCustomerSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s";
            else order += "Customer #" + _service.CustomerNumber.ToString("D3") + "   " + _service.Visit.Stage +
                "\nOrder: " + _service.Visit.Order.Offer.Dish.DisplayName + "\nPrice: " + Money(_service.Visit.Order.Offer.SalePriceCents);
            GUI.Label(new Rect(Screen.width - width - 4f, 20f, width - 16f, 104f), order, _style);
            if (_service.LastResult == null) return;
            var rect = new Rect(Screen.width - width - 12f, 144f, width, Mathf.Min(370f, Screen.height - 156f));
            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f));
            _scroll = GUILayout.BeginScrollView(_scroll); GUILayout.Label(ResultText(_service.LastResult), _style);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
    }
}
