using System;

namespace ZeroStarRestaurant.Customers
{
    public enum QueuedCustomerStage { Entering = 0, Waiting = 1, Advancing = 2, Service = 3, Leaving = 4, Finished = 5 }

    public sealed class QueuedCustomerState
    {
        public Guid InstanceId { get; } = Guid.NewGuid();
        public int Number { get; }
        public double InitialPatienceSeconds { get; }
        public double RemainingPatienceSeconds => Math.Max(0, InitialPatienceSeconds - WaitingSeconds);
        public double WaitingSeconds { get; private set; }
        public bool PatienceExhausted => RemainingPatienceSeconds == 0;
        public int QueueIndex { get; private set; } = -1;
        public QueuedCustomerStage Stage { get; private set; } = QueuedCustomerStage.Entering;

        public QueuedCustomerState(int number, double patienceSeconds)
        {
            if (number <= 0) throw new ArgumentOutOfRangeException(nameof(number));
            if (double.IsNaN(patienceSeconds) || double.IsInfinity(patienceSeconds) || patienceSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(patienceSeconds));
            Number = number; InitialPatienceSeconds = patienceSeconds;
        }

        internal void AssignPosition(int index)
        {
            QueueIndex = index;
            if (Stage == QueuedCustomerStage.Waiting || Stage == QueuedCustomerStage.Advancing)
                Stage = QueuedCustomerStage.Advancing;
        }
        public bool Arrive()
        {
            if (QueueIndex < 0 || (Stage != QueuedCustomerStage.Entering && Stage != QueuedCustomerStage.Advancing)) return false;
            Stage = QueuedCustomerStage.Waiting; return true;
        }
        internal bool BeginService()
        {
            if (QueueIndex != 0 || Stage != QueuedCustomerStage.Waiting) return false;
            Stage = QueuedCustomerStage.Service; return true;
        }
        internal bool BeginLeaving()
        {
            if (Stage != QueuedCustomerStage.Service) return false;
            Stage = QueuedCustomerStage.Leaving; return true;
        }
        internal void Finish() { Stage = QueuedCustomerStage.Finished; QueueIndex = -1; }

        public void AdvancePatience(double elapsedSeconds)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (Stage == QueuedCustomerStage.Waiting || Stage == QueuedCustomerStage.Service)
                WaitingSeconds += elapsedSeconds;
        }
    }
}
