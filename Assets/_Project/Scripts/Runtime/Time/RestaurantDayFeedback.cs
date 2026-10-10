using System.Globalization;
using UnityEngine;

namespace ZeroStarRestaurant.Restaurant
{
    public sealed class RestaurantDayFeedback : MonoBehaviour
    {
        [SerializeField] private RestaurantDayController _day;
        public RestaurantDayController Day => _day;
        public static string ClockText(RestaurantDay day) => string.Format(CultureInfo.InvariantCulture,
            "DAY {0} — {1:D2}:{2:D2}", day.Clock.DayNumber, day.Clock.Hour, day.Clock.Minute);
        public string Text => _day == null || _day.State == null ? "" : ClockText(_day.State) + " | " + _day.State.Stage.ToString().ToUpperInvariant() +
            (_day.State.IsPaused ? " | Clock paused" : "") +
            ActionHint(_day.State.Stage);
        private static string ActionHint(RestaurantDayStage stage) => stage == RestaurantDayStage.Preparation ? " | [Enter] Open Restaurant" :
            stage == RestaurantDayStage.Closed ? " | [Enter] End Day" : stage == RestaurantDayStage.EndOfDay ? " | [Enter] Start Next Day" : "";
    }
}
