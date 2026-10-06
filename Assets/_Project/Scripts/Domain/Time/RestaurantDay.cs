using System;

namespace ZeroStarRestaurant.Restaurant
{
    public enum RestaurantDayStage { Ready = 0, BeforeOpen = 1, Open = 2, Closing = 3, Closed = 4 }

    public sealed class RestaurantDay
    {
        public GameTime Clock { get; } = new GameTime();
        public int StartingMinute { get; }
        public int OpeningMinute { get; }
        public int ClosingMinute { get; }
        public RestaurantDayStage Stage { get; private set; } = RestaurantDayStage.Ready;
        public bool IsPaused { get; private set; }
        public bool CanAdmitCustomers => Stage == RestaurantDayStage.Open;

        public RestaurantDay(int startingMinute, int openingMinute, int closingMinute)
        {
            if (startingMinute < 0 || closingMinute >= 1440 || startingMinute > openingMinute || openingMinute >= closingMinute)
                throw new ArgumentException("Day needs 0 <= start <= opening < closing < 1440; overnight schedules are unsupported.");
            StartingMinute = startingMinute; OpeningMinute = openingMinute; ClosingMinute = closingMinute;
            Clock.StartDay(1, startingMinute);
        }
        public bool TryStartDay(int customersInside)
        {
            RequireOccupancy(customersInside);
            if (customersInside != 0 || (Stage != RestaurantDayStage.Ready && Stage != RestaurantDayStage.Closed) ||
                (Stage == RestaurantDayStage.Closed && Clock.DayNumber == int.MaxValue)) return false;
            int number = Stage == RestaurantDayStage.Closed ? Clock.DayNumber + 1 : Clock.DayNumber;
            Clock.StartDay(number, StartingMinute);
            Stage = StartingMinute == OpeningMinute ? RestaurantDayStage.Open : RestaurantDayStage.BeforeOpen;
            return true;
        }
        public void SetPaused(bool paused) => IsPaused = paused;
        public void Advance(double worldSeconds, int customersInside)
        {
            GameTime.RequireElapsed(worldSeconds); RequireOccupancy(customersInside);
            if (Stage == RestaurantDayStage.Ready || Stage == RestaurantDayStage.Closed) return;
            if (!IsPaused)
            {
                // With nobody inside, the day ends exactly at closing, even for a large step.
                if (customersInside == 0) worldSeconds = Math.Min(worldSeconds, Math.Max(0, ClosingMinute * 60.0 - Clock.SecondsOfDay));
                Clock.Advance(worldSeconds);
            }
            if (Clock.SecondsOfDay >= ClosingMinute * 60.0) Stage = RestaurantDayStage.Closing;
            else if (Clock.SecondsOfDay >= OpeningMinute * 60.0) Stage = RestaurantDayStage.Open;
            if (Stage == RestaurantDayStage.Closing && customersInside == 0) Stage = RestaurantDayStage.Closed;
        }
        private static void RequireOccupancy(int customersInside)
        { if (customersInside < 0) throw new ArgumentOutOfRangeException(nameof(customersInside)); }
    }
}
