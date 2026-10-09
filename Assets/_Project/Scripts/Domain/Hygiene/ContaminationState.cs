using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ZeroStarRestaurant.Hygiene
{
    public enum ContaminationKind { RawMeat, ExistingFood, Development }

    // Immutable evidence: original source and the immediate donor, without an unbounded chain.
    public sealed class ContaminationTrace
    {
        public ContaminationKind Kind { get; }
        public Guid OriginId { get; }
        public string Origin { get; }
        public Guid? FoodUnitId { get; }
        public string FoodDefinitionId { get; }
        public Food.FoodCategory? FoodCategory { get; }
        public double Intensity { get; }
        public Guid LastSourceId { get; }
        public string LastSource { get; }

        public ContaminationTrace(ContaminationKind kind, Guid originId, string origin, double intensity,
            Guid? foodUnitId = null, string foodDefinitionId = null, Food.FoodCategory? foodCategory = null,
            Guid? lastSourceId = null, string lastSource = null)
        {
            if (!Enum.IsDefined(typeof(ContaminationKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            ContaminationState.RequireUnit(intensity);
            if (originId == Guid.Empty || foodUnitId == Guid.Empty || lastSourceId == Guid.Empty)
                throw new ArgumentException("Contamination identities cannot be empty.");
            if (string.IsNullOrWhiteSpace(origin) || (lastSource != null && string.IsNullOrWhiteSpace(lastSource)))
                throw new ArgumentException("Contamination needs an origin and donor label.");
            if (foodCategory.HasValue && !Enum.IsDefined(typeof(Food.FoodCategory), foodCategory.Value))
                throw new ArgumentOutOfRangeException(nameof(foodCategory));
            Kind = kind; OriginId = originId; Origin = origin; Intensity = intensity;
            FoodUnitId = foodUnitId; FoodDefinitionId = foodDefinitionId; FoodCategory = foodCategory;
            LastSourceId = lastSourceId ?? originId; LastSource = lastSource ?? origin;
        }

        public ContaminationTrace Transfer(double fraction, Guid donorId, string donor)
        {
            ContaminationState.RequireUnit(fraction);
            return new ContaminationTrace(Kind, OriginId, Origin, Intensity * fraction,
                FoodUnitId, FoodDefinitionId, FoodCategory, donorId, donor);
        }
    }

    // At most one dominant trace per category. Max merge cannot amplify a contact loop.
    public sealed class ContaminationState
    {
        private readonly Dictionary<ContaminationKind, ContaminationTrace> _traces =
            new Dictionary<ContaminationKind, ContaminationTrace>();
        public IReadOnlyDictionary<ContaminationKind, ContaminationTrace> Traces { get; }
        public double Intensity { get; private set; }
        public bool IsContaminated => Intensity > 0;
        public ContaminationTrace LastReceived { get; private set; }
        public int Revision { get; private set; }

        public ContaminationState() => Traces = new ReadOnlyDictionary<ContaminationKind, ContaminationTrace>(_traces);

        public bool Receive(ContaminationTrace trace)
        {
            if (trace == null) throw new ArgumentNullException(nameof(trace));
            if (trace.Intensity == 0) return false;
            LastReceived = trace;
            if (_traces.TryGetValue(trace.Kind, out var previous))
            {
                if (previous.Intensity > trace.Intensity) return false;
                if (previous.Intensity == trace.Intensity && previous.OriginId.CompareTo(trace.OriginId) <= 0) return false;
            }
            _traces[trace.Kind] = trace;
            Intensity = Math.Max(Intensity, trace.Intensity); Revision++; return true;
        }

        public ContaminationTrace[] Snapshot()
        {
            var result = new List<ContaminationTrace>();
            foreach (ContaminationKind kind in Enum.GetValues(typeof(ContaminationKind)))
                if (_traces.TryGetValue(kind, out var trace)) result.Add(trace);
            return result.ToArray();
        }

        // Explicit surface sanitizing; never called by DirtState or cooking.
        public void Sanitize(double removedFraction = 1)
        {
            RequireUnit(removedFraction);
            if (removedFraction == 0 || !IsContaminated) return;
            var previous = Snapshot(); _traces.Clear(); Intensity = 0; LastReceived = null;
            foreach (var trace in previous)
            {
                var retained = trace.Transfer(1 - removedFraction, trace.LastSourceId, trace.LastSource);
                if (retained.Intensity == 0) continue;
                _traces[retained.Kind] = retained; Intensity = Math.Max(Intensity, retained.Intensity);
            }
            Revision++;
        }

        internal static void RequireUnit(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1)
                throw new ArgumentOutOfRangeException(nameof(value), "A finite value in [0, 1] is required.");
        }
    }
}
