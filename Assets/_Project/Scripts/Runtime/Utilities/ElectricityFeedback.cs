using System.Globalization;
using System.Text;
using UnityEngine;
using ZeroStarRestaurant.Economy;

namespace ZeroStarRestaurant.Utilities
{
    public sealed class ElectricityFeedback : MonoBehaviour
    {
        [SerializeField] private RestaurantElectricity _supply;
        [SerializeField] private RestaurantOperatingCosts _costs;
        public string Text
        {
            get
            {
                if (_supply == null || _supply.State == null) return "";
                var text = new StringBuilder(string.Format(CultureInfo.InvariantCulture,
                    "ELECTRICITY {0} | {1:F0} W\nToday: {2:F4} kWh | {3} accrued\nBill pending: {4:F4} kWh | {5}\n",
                    _supply.IsOn ? "ON" : "OFF", _supply.CurrentWatts, _supply.State.DailyConsumedKilowattHours,
                    IngredientPurchaseStation.FormatCents(_costs != null ? _costs.DailyElectricityAccruedCents : 0),
                    _supply.State.PendingKilowattHours, IngredientPurchaseStation.FormatCents(_costs != null ? _costs.PendingElectricityCents : 0)));
                foreach (ElectricalAppliance appliance in _supply.Appliances)
                    if (appliance != null && appliance.Meter != null)
                        text.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1} | {2:F0} W | Today {3:F4} kWh\n",
                            appliance.DisplayName, appliance.IsOperating ? "Running" : "Stopped",
                            appliance.IsOperating ? appliance.RatedWatts : 0, appliance.Meter.DailyConsumedKilowattHours);
                return text.ToString();
            }
        }
    }
}
