using System.Globalization;
using System.Text;
using UnityEngine;

namespace ZeroStarRestaurant.Utilities
{
    public sealed class ElectricityFeedback : MonoBehaviour
    {
        [SerializeField] private RestaurantElectricity _supply;
        private void OnGUI()
        {
            if (_supply == null || _supply.State == null) return;
            var text = new StringBuilder(string.Format(CultureInfo.InvariantCulture,
                "Electricity {0} | {1:F0} W\nDay: {2:F6} kWh | Total: {3:F6} kWh\n", _supply.IsOn ? "ON" : "OFF",
                _supply.CurrentWatts, _supply.State.DailyConsumedKilowattHours, _supply.State.ConsumedKilowattHours));
            foreach (ElectricalAppliance appliance in _supply.Appliances)
            {
                if (appliance == null || appliance.Meter == null) continue;
                text.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1} | {2} | {3:F0} W | Day {4:F6} / Total {5:F6} kWh\n", appliance.DisplayName,
                    appliance.IsOn ? "ON" : "OFF", appliance.IsOperating ? "Running" : "Stopped",
                    appliance.RatedWatts, appliance.Meter.DailyConsumedKilowattHours, appliance.Meter.ConsumedKilowattHours);
            }
            GUI.Box(new Rect(Screen.width - 532, Screen.height - 135, 520, 123), text.ToString());
        }
    }
}
