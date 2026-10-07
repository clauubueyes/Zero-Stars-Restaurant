using System;
using System.Collections.Generic;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Orders;

namespace ZeroStarRestaurant.Food
{
    public enum FoodQualityIssue { Stale, Raw, Undercooked, Overcooked, Burnt, Spoiled, Rotten, Contaminated }

    public sealed class FoodQualityCause
    {
        public IngredientSnapshot Ingredient { get; }
        public FoodQualityIssue Issue { get; }
        public bool IsHealthHazard { get; }
        internal FoodQualityCause(IngredientSnapshot ingredient, FoodQualityIssue issue)
        {
            Ingredient = ingredient; Issue = issue;
            IsHealthHazard = issue == FoodQualityIssue.Spoiled || issue == FoodQualityIssue.Rotten ||
                issue == FoodQualityIssue.Contaminated ||
                ((issue == FoodQualityIssue.Raw || issue == FoodQualityIssue.Undercooked) && ingredient.Profile.Category == FoodCategory.Meat);
        }
    }

    // Reads the immutable delivery evidence, never advances or repairs the live food.
    public sealed class FoodQualityEvaluation
    {
        public DishSnapshot Delivery { get; }
        public IReadOnlyList<FoodQualityCause> Causes { get; }
        public IReadOnlyList<FoodQualityIssue> Issues { get; }
        public bool IsGood => Causes.Count == 0;

        public FoodQualityEvaluation(DishSnapshot delivery)
        {
            Delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
            var causes = new List<FoodQualityCause>(); var issues = new List<FoodQualityIssue>();
            foreach (IngredientSnapshot ingredient in delivery.Ingredients)
            {
                switch (ingredient.Condition)
                {
                    case FoodCondition.Acceptable: Add(ingredient, FoodQualityIssue.Stale, causes, issues); break;
                    case FoodCondition.Spoiled: Add(ingredient, FoodQualityIssue.Spoiled, causes, issues); break;
                    case FoodCondition.Rotten: Add(ingredient, FoodQualityIssue.Rotten, causes, issues); break;
                }
                if (ingredient.IsContaminated) Add(ingredient, FoodQualityIssue.Contaminated, causes, issues);
                switch (ingredient.CookingStage)
                {
                    case CookingStage.Raw: Add(ingredient, FoodQualityIssue.Raw, causes, issues); break;
                    case CookingStage.Undercooked: Add(ingredient, FoodQualityIssue.Undercooked, causes, issues); break;
                    case CookingStage.Overcooked: Add(ingredient, FoodQualityIssue.Overcooked, causes, issues); break;
                    case CookingStage.Burnt: Add(ingredient, FoodQualityIssue.Burnt, causes, issues); break;
                }
            }
            Causes = causes.AsReadOnly(); Issues = issues.AsReadOnly();
        }

        private static void Add(IngredientSnapshot ingredient, FoodQualityIssue issue,
            List<FoodQualityCause> causes, List<FoodQualityIssue> issues)
        {
            causes.Add(new FoodQualityCause(ingredient, issue));
            if (!issues.Contains(issue)) issues.Add(issue);
        }
    }
}
