using System.Globalization;
using UnityEngine;

namespace ZeroStarRestaurant.Restaurant
{
    public sealed class RestaurantDayFeedback : MonoBehaviour
    {
        [SerializeField] private RestaurantDayController _day;
        public static string ClockText(RestaurantDay day) => string.Format(CultureInfo.InvariantCulture,
            "Day {0} — {1:D2}:{2:D2}", day.Clock.DayNumber, day.Clock.Hour, day.Clock.Minute);
        private void OnGUI()
        {
            if (_day == null || _day.State == null) return;
            RestaurantDay day = _day.State;
            GUI.Box(new Rect(12, Screen.height - 138, 320, 48), ClockText(day) + "\n" + day.Stage +
                (day.IsPaused ? " | Clock paused" : "") + (day.Stage == RestaurantDayStage.Closed ? " | Start Next Day in Inspector" : ""));
        }
    }
}
