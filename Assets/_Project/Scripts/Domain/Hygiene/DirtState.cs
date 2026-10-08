using System;
using System.Collections.Generic;

namespace ZeroStarRestaurant.Hygiene
{
    public enum DirtKind { FoodResidue, Grease, GeneralDirt }
    public enum DirtCategory { Clean, Used, Dirty, Filthy }

    public sealed class DirtProfile
    {
        public double CleanMaximum { get; }
        public double DirtyMinimum { get; }
        public double FilthyMinimum { get; }
        public DirtProfile(double cleanMaximum = .05, double dirtyMinimum = .35, double filthyMinimum = .75)
        {
            if (double.IsNaN(cleanMaximum) || double.IsNaN(dirtyMinimum) || double.IsNaN(filthyMinimum) ||
                cleanMaximum < 0 || cleanMaximum >= dirtyMinimum || dirtyMinimum >= filthyMinimum || filthyMinimum > 1)
                throw new ArgumentException("Dirt thresholds require 0 <= clean < dirty < filthy <= 1.");
            CleanMaximum = cleanMaximum; DirtyMinimum = dirtyMinimum; FilthyMinimum = filthyMinimum;
        }
        public DirtCategory Category(double amount) => amount <= CleanMaximum ? DirtCategory.Clean :
            amount < DirtyMinimum ? DirtCategory.Used : amount < FilthyMinimum ? DirtCategory.Dirty : DirtCategory.Filthy;
    }

    public sealed class DirtChange
    {
        public Guid SurfaceId { get; }
        public double Before { get; }
        public double After { get; }
        public DirtKind? Kind { get; }
        public string Origin { get; }
        public Guid? FoodUnitId { get; }
        internal DirtChange(Guid surfaceId, double before, double after, DirtKind? kind, string origin, Guid? foodUnitId)
        { SurfaceId = surfaceId; Before = before; After = after; Kind = kind; Origin = origin; FoodUnitId = foodUnitId; }
    }

    // One instance per physical surface. Categories and views never replace the continuous state.
    public sealed class DirtState
    {
        private readonly Dictionary<DirtKind, double> _amounts = new Dictionary<DirtKind, double>
        { { DirtKind.FoodResidue, 0 }, { DirtKind.Grease, 0 }, { DirtKind.GeneralDirt, 0 } };
        public Guid SurfaceId { get; } = Guid.NewGuid();
        public DirtProfile Profile { get; }
        public double Amount { get; private set; }
        public DirtCategory Category => Profile.Category(Amount);
        public IReadOnlyDictionary<DirtKind, double> AmountsByKind { get; }
        public DirtChange LastChange { get; private set; }
        public event Action<DirtChange> Changed;

        public DirtState(DirtProfile profile, double initialAmount = 0)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile)); RequireAmount(initialAmount);
            if (initialAmount > 1) throw new ArgumentOutOfRangeException(nameof(initialAmount));
            Amount = initialAmount; _amounts[DirtKind.GeneralDirt] = initialAmount;
            AmountsByKind = new System.Collections.ObjectModel.ReadOnlyDictionary<DirtKind, double>(_amounts);
        }
        public double AddDirt(double amount, DirtKind kind, string origin, Guid? foodUnitId = null)
        {
            RequireAmount(amount);
            if (!_amounts.ContainsKey(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (string.IsNullOrWhiteSpace(origin)) throw new ArgumentException("Dirt needs an origin.", nameof(origin));
            if (foodUnitId == Guid.Empty) throw new ArgumentException("Food identity cannot be empty.", nameof(foodUnitId));
            double before = Amount, applied = Math.Min(amount, 1 - Amount);
            if (applied == 0) return 0;
            Amount += applied; _amounts[kind] += applied; Notify(before, kind, origin, foodUnitId); return applied;
        }
        public double Clean(double seconds, double amountPerSecond)
        {
            RequireAmount(seconds); RequireAmount(amountPerSecond);
            if (seconds == 0 || amountPerSecond == 0 || Amount == 0) return 0;
            double before = Amount;
            double removed = amountPerSecond >= Amount / seconds ? Amount : seconds * amountPerSecond;
            Amount = Math.Max(0, Amount - removed);
            double retained = Amount / before;
            foreach (DirtKind kind in new[] { DirtKind.FoodResidue, DirtKind.Grease, DirtKind.GeneralDirt }) _amounts[kind] *= retained;
            Notify(before, null, "Cleaning", null); return removed;
        }
        // Explicit development operation, never used by the gameplay cleaning path.
        public void SetForDevelopment(double amount)
        {
            RequireAmount(amount); if (amount > 1) throw new ArgumentOutOfRangeException(nameof(amount));
            double before = Amount; Amount = amount;
            _amounts[DirtKind.FoodResidue] = 0; _amounts[DirtKind.Grease] = 0; _amounts[DirtKind.GeneralDirt] = amount;
            Notify(before, DirtKind.GeneralDirt, "Development", null);
        }
        private void Notify(double before, DirtKind? kind, string origin, Guid? foodUnitId)
        { LastChange = new DirtChange(SurfaceId, before, Amount, kind, origin, foodUnitId); Changed?.Invoke(LastChange); }
        private static void RequireAmount(double value)
        { if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); }
    }
}
