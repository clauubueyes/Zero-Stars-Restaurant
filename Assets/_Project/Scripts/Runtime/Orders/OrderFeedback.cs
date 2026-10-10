using System.Globalization;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Orders
{
    public sealed class OrderFeedback : MonoBehaviour
    {
        [SerializeField] private CustomerServiceLoop _service;
        public RestaurantReputation Reputation => _service != null ? _service.Reputation : null;
        public int ResultRevision => _service != null ? _service.ResultRevision : 0;
        public string LastDeliveryMessage => _service == null || _service.LastResult == null ? "" :
            "Customer #" + _service.LastResultCustomerNumber.ToString("D3") + " | " + BriefResultText(_service.LastResult) +
            "\n" + ConsequenceText(_service.LastConsequence);
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
        private static string IngredientNames(IEnumerable<string> ids, OrderEvaluation evaluation)
        {
            var names = ids.Select(id => evaluation.DeliveredDish.Ingredients.FirstOrDefault(food => food.Profile.Id == id)?.Profile.DisplayName ??
                IngredientLabel(id));
            return names.Any() ? string.Join(" + ", names) : "None";
        }
        private static string IngredientLabel(string id)
        {
            string name = id.Substring(id.LastIndexOf('.') + 1);
            return char.ToUpperInvariant(name[0]) + name.Substring(1).Replace('_', ' ');
        }
        public static string BriefResultText(OrderResult result)
        {
            var evaluation = result.Evaluation; var satisfaction = evaluation.Satisfaction;
            return (result.Accepted ? "DELIVERY ACCEPTED" : "DELIVERY REJECTED") +
                " | Paid " + Money(result.PaymentCents) + " / " + Money(evaluation.BasePriceCents) +
                "\nOrdered: " + evaluation.RequestedDish.DisplayName +
                "\nMissing: " + IngredientNames(satisfaction.Missing, evaluation) +
                "\nReceived: " + IngredientNames(satisfaction.Received, evaluation);
        }

        public static string ResultText(OrderResult result)
        {
            var evaluation = result.Evaluation; var dish = evaluation.DeliveredDish;
            var text = new StringBuilder("Order Result #" + evaluation.OrderId.ToString("N").Substring(0, 8) + "\n");
            text.AppendLine("Requested: " + evaluation.RequestedDish.DisplayName);
            text.AppendLine("Delivered: " + dish.DisplayName + " #" + dish.InstanceId.ToString("N").Substring(0, 8));
            text.AppendLine("Correct order: " + (evaluation.CorrectOrder ? "YES" : "NO"));
            text.AppendLine("Expected: " + IngredientNames(evaluation.Satisfaction.Expected, evaluation));
            text.AppendLine("Received: " + IngredientNames(evaluation.Satisfaction.Received, evaluation));
            text.AppendLine("Missing: " + IngredientNames(evaluation.Satisfaction.Missing, evaluation));
            text.AppendLine("Extra: " + IngredientNames(evaluation.Satisfaction.Extra, evaluation));
            text.AppendLine("Composition / recipe: " + (dish.DefinitionId ?? "Unrecognized"));
            text.AppendLine("Base price: " + Money(evaluation.BasePriceCents));
            text.AppendLine("Final payment: " + Money(result.PaymentCents));
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
            text.AppendLine(result.Accepted ? "Accepted / Sold" : "Rejected / Food remains available");
            return text.ToString();
        }
        public static string ConsequenceText(CustomerConsequence consequence, bool includeCauses = false)
        {
            if (consequence == null) return "Quality: Not consumed | Reaction: None";
            string issues = consequence.Quality.IsGood ? "Good" : string.Join(" + ", consequence.Quality.Issues);
            var text = new StringBuilder("Quality: " + issues + "\nReaction: " +
                (consequence.Reaction == CustomerReaction.HealthIncident ? "Health Incident" : consequence.Reaction.ToString()));
            if (includeCauses)
                foreach (FoodQualityCause cause in consequence.Quality.Causes)
                    text.AppendFormat(CultureInfo.InvariantCulture, "\n{0} #{1}: {2}{3} | Freshness {4:0.0}% | {5:0.0}°C",
                        cause.Ingredient.Profile.DisplayName, cause.Ingredient.InstanceId.ToString("N").Substring(0, 8), cause.Issue,
                        cause.IsHealthHazard ? " (health hazard)" : "", cause.Ingredient.FreshnessPercent, cause.Ingredient.TemperatureCelsius);
            return text.ToString();
        }
        public static string StatisticsText(CustomerServiceStatistics statistics) =>
            "Customers served: " + statistics.CustomersServed + " | Satisfied: " + statistics.Satisfied +
            " | Unhappy: " + statistics.Unhappy + "\nComplaints: " + statistics.Complaints + " | Health incidents: " + statistics.HealthIncidents;
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
                if (_service.LastResult != null) order += "\nLast delivery: " + LastDeliveryMessage;
                if (Reputation != null) order = Reputation.Text + "\n\n" + order;
                order += "\nToday: " + _service.Statistics.DailyCustomersServed + " served | " + _service.Statistics.DailySatisfied +
                    " satisfied | " + _service.Statistics.DailyUnhappy + " unhappy | " + _service.Statistics.DailyComplaints + " complaints";
                order += "\nSession totals: " + StatisticsText(_service.Statistics);
                if (_service.Queue != null) order += "\n\n" + QueueText(_service.Queue);
                if (_service.LastResult != null) order += "\n" + ResultText(_service.LastResult) + "\n" + ConsequenceText(_service.LastConsequence, true);
                return order;
            }
        }
    }
}
