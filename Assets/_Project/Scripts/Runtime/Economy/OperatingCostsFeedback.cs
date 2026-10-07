using System.Globalization;
using System.Text;
using UnityEngine;

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
        public string Text => _costs == null || _costs.Summary == null ? "" : SummaryText(_costs.Summary) +
            "\nElectricity bill pending: " + IngredientPurchaseStation.FormatCents(_costs.PendingElectricityCents) +
            "\nNext Day: RestaurantDayController in Inspector.";
    }
}
