using System.Globalization;
using System.Text;
using UnityEngine;

namespace ZeroStarRestaurant.Economy
{
    public sealed class OperatingCostsFeedback : MonoBehaviour
    {
        [SerializeField] private RestaurantOperatingCosts _costs;
        private GUIStyle _style;
        private Vector2 _scroll;
        public static string SummaryText(DailySummary summary)
        {
            var text = new StringBuilder("DAY " + summary.DayNumber + " SUMMARY\n");
            text.AppendLine("Opening balance: " + IngredientPurchaseStation.FormatCents(summary.OpeningBalanceCents));
            text.AppendLine("Sales: " + IngredientPurchaseStation.FormatCents(summary.SalesCents));
            text.AppendLine("Purchases: -" + IngredientPurchaseStation.FormatCents(summary.PurchasesCents));
            text.AppendLine("Electricity: -" + IngredientPurchaseStation.FormatCents(summary.ElectricityCents));
            text.AppendLine(string.Format(CultureInfo.InvariantCulture, "  {0:F6} kWh at {1} cents/kWh",
                summary.ConsumedKilowattHours, summary.ElectricityCentsPerKilowattHour));
            foreach (LedgerTransaction transaction in summary.Transactions)
                if (transaction.Category == LedgerCategory.FixedCost)
                    text.AppendLine(transaction.Label + ": -" + IngredientPurchaseStation.FormatCents(transaction.AmountCents));
            text.AppendLine("Net: " + IngredientPurchaseStation.FormatCents(summary.NetCents));
            text.Append("Balance: " + IngredientPurchaseStation.FormatCents(summary.ClosingBalanceCents));
            return text.ToString();
        }
        private void OnGUI()
        {
            DailySummary summary = _costs != null ? _costs.Summary : null;
            if (summary == null) return;
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            float width = Mathf.Min(380, Screen.width - 24), height = Mathf.Min(360, Screen.height - 48);
            GUILayout.BeginArea(new Rect((Screen.width - width) / 2, 24, width, height), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label(SummaryText(summary), _style);
            GUILayout.EndScrollView();
            GUILayout.Label("Next Day: RestaurantDayController in Inspector.");
            GUILayout.EndArea();
        }
    }
}
