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
                "Electricity {0} | {1:F0} W | {2:F6} kWh\n", _supply.IsOn ? "ON" : "OFF",
                _supply.CurrentWatts, _supply.State.ConsumedKilowattHours));
            foreach (ElectricalAppliance appliance in _supply.Appliances)
            {
                if (appliance == null || appliance.Meter == null) continue;
                text.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1} | {2:F0} W | {3:F6} kWh\n", appliance.name,
                    appliance.IsOperating ? "ON" : "OFF", appliance.RatedWatts, appliance.Meter.ConsumedKilowattHours);
            }
            GUI.Box(new Rect(Screen.width - 432, Screen.height - 115, 420, 103), text.ToString());
        }
    }
}
