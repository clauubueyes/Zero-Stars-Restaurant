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
        public string Text
        {
            get
            {
                if (_service == null || _service.Ledger == null) return "";
                string order = _service.Visit == null ? (_service.Queue != null && _service.Queue.Count > 0 ?
                    "Customer approaching Service Position" : "Waiting for customers") :
                    "Customer #" + _service.CustomerNumber.ToString("D3") + " | " + _service.Visit.Stage +
                    "\nOrder: " + _service.Visit.Order.Offer.Dish.DisplayName + " | " + Money(_service.Visit.Order.Offer.SalePriceCents);
                if (_service.DeliveryZone != null) order += "\n" + _service.DeliveryZone.PlacementMessage;
                if (_service.Queue != null) order += "\n\n" + QueueText(_service.Queue);
                if (_service.LastResult != null) order += "\n" + ResultText(_service.LastResult);
                return order;
            }
        }
    }
}
