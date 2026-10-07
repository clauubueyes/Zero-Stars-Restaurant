using System.Globalization;
using UnityEngine;

namespace ZeroStarRestaurant.Restaurant
{
    public sealed class RestaurantDayFeedback : MonoBehaviour
    {
        [SerializeField] private RestaurantDayController _day;
        public static string ClockText(RestaurantDay day) => string.Format(CultureInfo.InvariantCulture,
            "Day {0} — {1:D2}:{2:D2}", day.Clock.DayNumber, day.Clock.Hour, day.Clock.Minute);
        public string Text => _day == null || _day.State == null ? "" : ClockText(_day.State) + " | " + _day.State.Stage +
            (_day.State.IsPaused ? " | Clock paused" : "") +
            (_day.State.Stage == RestaurantDayStage.Closed ? " | Next Day in Inspector" : "");
    }
}
