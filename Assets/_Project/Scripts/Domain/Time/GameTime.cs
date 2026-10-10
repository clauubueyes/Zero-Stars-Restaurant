using System;

namespace ZeroStarRestaurant.Restaurant
{
    public sealed class GameTime
    {
        // A service day never rolls over implicitly while customers are still inside.
        public const double LastSecondOfDay = 86399;
        public RestaurantCalendar Calendar { get; } = new RestaurantCalendar();
        public int DayNumber => Calendar.CurrentDay;
        public double SecondsOfDay { get; private set; }
        // Actual world-clock progress only; StartDay does not simulate an overnight gap.
        public double ElapsedWorldSeconds { get; private set; }
        public int Hour => (int)(SecondsOfDay / 3600);
        public int Minute => (int)(SecondsOfDay / 60) % 60;

        internal void SetTimeOfDay(int startingMinute)
        {
            SecondsOfDay = startingMinute * 60.0;
        }
        public void Advance(double worldSeconds)
        {
            RequireElapsed(worldSeconds);
            double next = worldSeconds >= LastSecondOfDay - SecondsOfDay ? LastSecondOfDay : SecondsOfDay + worldSeconds;
            ElapsedWorldSeconds += next - SecondsOfDay;
            SecondsOfDay = next;
        }
        internal static void RequireElapsed(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
        }
    }
}
