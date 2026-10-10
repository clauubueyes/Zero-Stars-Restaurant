using System;

namespace ZeroStarRestaurant.Restaurant
{
    // Preserve the original numeric values; Ready is the optional unstarted session.
    public enum RestaurantDayStage { Ready = 0, Preparation = 1, Open = 2, Closing = 3, Closed = 4, EndOfDay = 5 }

    public sealed class RestaurantDay
    {
        public GameTime Clock { get; } = new GameTime();
        public RestaurantCalendar Calendar => Clock.Calendar;
        public int StartingMinute { get; }
        public int OpeningMinute { get; }
        public int ClosingMinute { get; }
        public RestaurantDayStage Stage { get; private set; } = RestaurantDayStage.Ready;
        public bool IsPaused { get; private set; }
        public bool CanAdmitCustomers => Stage == RestaurantDayStage.Open;
        public event Action<int> DayStarted;
        public event Action<int> RestaurantOpened;
        public event Action<int> RestaurantClosing;
        public event Action<int> RestaurantClosed;
        public event Action<int> DayEnded;

        public RestaurantDay(int startingMinute, int openingMinute, int closingMinute)
        {
            if (startingMinute < 0 || closingMinute >= 1440 || startingMinute > openingMinute || openingMinute >= closingMinute)
                throw new ArgumentException("Day needs 0 <= start <= opening < closing < 1440; overnight schedules are unsupported.");
            StartingMinute = startingMinute; OpeningMinute = openingMinute; ClosingMinute = closingMinute;
            Clock.SetTimeOfDay(startingMinute);
        }
        public bool TryStartDay(int customersInside)
        {
            RequireOccupancy(customersInside);
            if (customersInside != 0 || (Stage != RestaurantDayStage.Ready && Stage != RestaurantDayStage.EndOfDay)) return false;
            if (Stage == RestaurantDayStage.EndOfDay && !Calendar.TryAdvanceDay()) return false;
            Clock.SetTimeOfDay(StartingMinute);
            Stage = RestaurantDayStage.Preparation;
            DayStarted?.Invoke(Calendar.CurrentDay);
            return true;
        }
        public bool TryOpenRestaurant()
        {
            if (Stage != RestaurantDayStage.Preparation) return false;
            // Opening selects the service hour without simulating the preparation gap.
            Clock.SetTimeOfDay(OpeningMinute);
            Stage = RestaurantDayStage.Open;
            RestaurantOpened?.Invoke(Calendar.CurrentDay);
            return true;
        }
        public bool TryForceClose(int customersInside)
        {
            RequireOccupancy(customersInside);
            if (Stage != RestaurantDayStage.Open) return false;
            BeginClosing(); FinishClosingIfEmpty(customersInside);
            return true;
        }
        public bool TryEndDay(int customersInside)
        {
            RequireOccupancy(customersInside);
            if (Stage != RestaurantDayStage.Closed || customersInside != 0) return false;
            Stage = RestaurantDayStage.EndOfDay;
            DayEnded?.Invoke(Calendar.CurrentDay);
            return true;
        }
        public void SetPaused(bool paused) => IsPaused = paused;
        public void Advance(double worldSeconds, int customersInside)
        {
            GameTime.RequireElapsed(worldSeconds); RequireOccupancy(customersInside);
            if (Stage != RestaurantDayStage.Open && Stage != RestaurantDayStage.Closing) return;
            if (!IsPaused)
            {
                if (Stage == RestaurantDayStage.Open && customersInside == 0)
                    worldSeconds = Math.Min(worldSeconds, Math.Max(0, ClosingMinute * 60.0 - Clock.SecondsOfDay));
                Clock.Advance(worldSeconds);
            }
            if (Stage == RestaurantDayStage.Open && Clock.SecondsOfDay >= ClosingMinute * 60.0) BeginClosing();
            FinishClosingIfEmpty(customersInside);
        }
        private void BeginClosing()
        { Stage = RestaurantDayStage.Closing; RestaurantClosing?.Invoke(Calendar.CurrentDay); }
        private void FinishClosingIfEmpty(int customersInside)
        {
            if (Stage != RestaurantDayStage.Closing || customersInside != 0) return;
            Stage = RestaurantDayStage.Closed; RestaurantClosed?.Invoke(Calendar.CurrentDay);
        }
        private static void RequireOccupancy(int customersInside)
        { if (customersInside < 0) throw new ArgumentOutOfRangeException(nameof(customersInside)); }
    }
}
