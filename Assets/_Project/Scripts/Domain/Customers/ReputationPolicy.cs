using System;
using System.Collections.Generic;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Customers
{
    public sealed class ReputationPolicy
    {
        public int Minimum { get; }
        public int Maximum { get; }
        public int Initial { get; }
        public int SatisfiedChange { get; }
        public int UnhappyChange { get; }
        public int ComplaintChange { get; }
        public int ConfirmedIncidentChange { get; }
        public double DelayWorldSeconds { get; }
        public double RawMeatProbability { get; }
        public double SpoiledProbability { get; }
        public double ContaminatedProbability { get; }

        public ReputationPolicy(int minimum = 0, int maximum = 100, int initial = 50,
            int satisfiedChange = 1, int unhappyChange = -1, int complaintChange = -3,
            int confirmedIncidentChange = -8, double delayWorldSeconds = 7200,
            double rawMeatProbability = .35, double spoiledProbability = .55, double contaminatedProbability = .75)
        {
            if (minimum >= maximum || initial < minimum || initial > maximum)
                throw new ArgumentException("Reputation needs an ordered range and initial value inside it.");
            if (satisfiedChange < 0 || unhappyChange > 0 || complaintChange > 0 || confirmedIncidentChange > 0)
                throw new ArgumentException("Satisfied improves reputation; negative reactions cannot improve it.");
            if (double.IsNaN(delayWorldSeconds) || double.IsInfinity(delayWorldSeconds) || delayWorldSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(delayWorldSeconds));
            RequireProbability(rawMeatProbability); RequireProbability(spoiledProbability); RequireProbability(contaminatedProbability);
            Minimum = minimum; Maximum = maximum; Initial = initial;
            SatisfiedChange = satisfiedChange; UnhappyChange = unhappyChange; ComplaintChange = complaintChange;
            ConfirmedIncidentChange = confirmedIncidentChange; DelayWorldSeconds = delayWorldSeconds;
            RawMeatProbability = rawMeatProbability; SpoiledProbability = spoiledProbability; ContaminatedProbability = contaminatedProbability;
        }

        // A single case per visit uses its highest evidenced risk, without multiplying repeated ingredients.
        public double Probability(IReadOnlyList<FoodQualityCause> causes)
        {
            double probability = 0;
            foreach (FoodQualityCause cause in causes)
            {
                if (!cause.IsHealthHazard) continue;
                double value = cause.Issue == FoodQualityIssue.Contaminated ? ContaminatedProbability :
                    cause.Issue == FoodQualityIssue.Spoiled || cause.Issue == FoodQualityIssue.Rotten ? SpoiledProbability : RawMeatProbability;
                probability = Math.Max(probability, value);
            }
            return probability;
        }
        private static void RequireProbability(double value)
        { if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value)); }
    }
}
