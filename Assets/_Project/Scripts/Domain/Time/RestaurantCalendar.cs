using System;

namespace ZeroStarRestaurant.Restaurant
{
    // Session calendar. Only the daily cycle can commit the next day.
    public sealed class RestaurantCalendar
    {
        public int CurrentDay { get; private set; } = 1;
        public int TotalDaysElapsed => CurrentDay - 1;
        public bool HasReachedDay(int dayNumber)
        {
            if (dayNumber < 1) throw new ArgumentOutOfRangeException(nameof(dayNumber));
            return CurrentDay >= dayNumber;
        }
        internal bool TryAdvanceDay()
        {
            if (CurrentDay == int.MaxValue) return false;
            CurrentDay++;
            return true;
        }
    }
}
