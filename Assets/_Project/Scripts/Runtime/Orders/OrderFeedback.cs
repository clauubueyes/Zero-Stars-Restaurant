using System.Globalization;
using System.Text;
using UnityEngine;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Economy;

namespace ZeroStarRestaurant.Orders
{
    public sealed class OrderFeedback : MonoBehaviour
    {
        [SerializeField] private CustomerServiceLoop _service;
        private GUIStyle _style;
        private Vector2 _scroll;
        public static string Money(long cents) => IngredientPurchaseStation.FormatCents(cents);
        public static string QueueText(CustomerQueueController queue)
        {
            var text = new StringBuilder("QUEUE " + queue.Count + "/" + queue.Capacity + (queue.IsFull ? " (FULL)" : "") + "\n");
            foreach (QueuedCustomer customer in queue.Customers)
            {
                QueuedCustomerState state = customer.State;
                text.AppendFormat(CultureInfo.InvariantCulture, "#{0:D3} Q{1} {2}  [{3}]\nWait {4:0.0}s | Patience {5:0.0}s{6}\n",
                    state.Number, state.QueueIndex + 1, state.Stage, state.InstanceId.ToString("N").Substring(0, 6),
                    state.WaitingSeconds, state.RemainingPatienceSeconds, state.PatienceExhausted ? " (0: debug only)" : "");
            }
            return text.ToString();
        }
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
            if (_service.Visit == null)
                order += _service.Queue != null && _service.Queue.Count > 0 ? "Customer approaching Service Position" :
                    "Next customer in " + _service.NextCustomerSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s";
            else order += "Customer #" + _service.CustomerNumber.ToString("D3") + "   " + _service.Visit.Stage +
                "\nOrder: " + _service.Visit.Order.Offer.Dish.DisplayName + "\nPrice: " + Money(_service.Visit.Order.Offer.SalePriceCents);
            GUI.Label(new Rect(Screen.width - width - 4f, 20f, width - 16f, 104f), order, _style);
            float resultTop = 144f;
            if (_service.Queue != null)
            {
                float height = 30f + 42f * _service.Queue.Count;
                GUI.Box(new Rect(Screen.width - width - 12f, 144f, width, height), GUIContent.none);
                GUI.Label(new Rect(Screen.width - width - 4f, 150f, width - 16f, height - 8f), QueueText(_service.Queue), _style);
                resultTop = 156f + height;
            }
            if (_service.LastResult == null) return;
            if (Screen.height - resultTop < 48f) return;
            var rect = new Rect(Screen.width - width - 12f, resultTop, width, Mathf.Min(370f, Screen.height - resultTop - 12f));
            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f));
            _scroll = GUILayout.BeginScrollView(_scroll); GUILayout.Label(ResultText(_service.LastResult), _style);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
    }
}
