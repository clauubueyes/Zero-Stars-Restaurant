using System.Globalization;
using System.Text;
using UnityEngine;
using ZeroStarRestaurant.Restaurant;

namespace ZeroStarRestaurant.Economy
{
    public sealed class OperatingCostsFeedback : MonoBehaviour
    {
        [SerializeField] private RestaurantOperatingCosts _costs;
        public static string SummaryText(DailySummary summary)
        {
            var text = new StringBuilder("DAY " + summary.DayNumber + " SUMMARY\n");
            text.AppendLine("Opening balance: " + IngredientPurchaseStation.FormatCents(summary.OpeningBalanceCents));
            text.AppendLine("Sales: " + IngredientPurchaseStation.FormatCents(summary.SalesCents));
            text.AppendLine("Purchases: -" + IngredientPurchaseStation.FormatCents(summary.PurchasesCents));
            text.AppendLine("Fixed daily costs: -" + IngredientPurchaseStation.FormatCents(summary.FixedCostsCents));
            foreach (LedgerTransaction transaction in summary.Transactions)
                if (transaction.Category == LedgerCategory.FixedCost)
                    text.AppendLine("  " + transaction.Label + ": -" + IngredientPurchaseStation.FormatCents(transaction.AmountCents));
            text.AppendLine("Operating net: " + IngredientPurchaseStation.FormatCents(summary.OperatingNetCents));
            text.AppendLine("Electricity bills paid: -" + IngredientPurchaseStation.FormatCents(summary.ElectricityPaidCents));
            text.AppendLine("Cash net: " + IngredientPurchaseStation.FormatCents(summary.NetCents));
            text.AppendLine("Balance: " + IngredientPurchaseStation.FormatCents(summary.ClosingBalanceCents));
            text.AppendLine(string.Format(CultureInfo.InvariantCulture, "Electricity used today: {0:F6} kWh", summary.ConsumedKilowattHours));
            text.Append("Electricity cost accrued today: " + IngredientPurchaseStation.FormatCents(summary.ElectricityAccruedCents));
            return text.ToString();
        }
        public static string EndOfDayText(EndOfDaySummary summary, DailySummary accounting = null)
        {
            accounting = accounting ?? summary.Accounting;
            var text = new StringBuilder("DAY " + summary.DayNumber + " COMPLETE\n");
            text.AppendLine(SummaryText(accounting));
            text.AppendLine("Net cash movement: " + IngredientPurchaseStation.FormatCents(accounting.NetCents));
            text.AppendLine("Customers served: " + summary.CustomersServed);
            text.AppendLine("Outcomes:");
            text.AppendLine("  Satisfied: " + summary.Satisfied);
            text.AppendLine("  Unhappy: " + summary.Unhappy);
            text.AppendLine("  Complaints: " + summary.Complaints);
            text.AppendLine("  Health incidents (food hazards): " + summary.HealthIncidents);
            text.AppendLine("Confirmed health incidents today: " + (summary.ConfirmedHealthIncidents?.ToString() ?? "Unavailable"));
            text.AppendLine("Dismissed health risks today: " + (summary.DismissedHealthRisks?.ToString() ?? "Unavailable"));
            text.AppendLine("Pending health risks: " + (summary.PendingHealthRisks?.ToString() ?? "Unavailable"));
            text.AppendLine("Reputation: " + (summary.Reputation?.ToString() ?? "Unavailable"));
            text.Append("Closing balance: " + IngredientPurchaseStation.FormatCents(accounting.ClosingBalanceCents));
            return text.ToString();
        }
        public string Text => _costs == null || _costs.Day.State?.Stage != RestaurantDayStage.EndOfDay || _costs.Day.Summary == null ? "" :
            EndOfDayText(_costs.Day.Summary, _costs.Summary) +
            "\nPending electricity bill: " + IngredientPurchaseStation.FormatCents(_costs.PendingElectricityCents) +
            "\n[Enter] Start Next Day";
    }
}
