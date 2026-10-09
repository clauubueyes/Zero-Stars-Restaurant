using System;
using System.Linq;
using NUnit.Framework;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;
using ZeroStarRestaurant.Orders;
using ZeroStarRestaurant.Restaurant;

namespace ZeroStarRestaurant.Tests
{
    public sealed class FoodSafetyTests
    {
        private readonly FoodSafetyPolicy _policy = new FoodSafetyPolicy();
        private readonly Guid _surfaceId = Guid.NewGuid();
        private static FoodState Food(bool meat = false, bool contaminated = false) => new FoodState(
            new FoodProfile(meat ? "patty" : "bun", meat ? "Beef Patty" : "Bun", meat ? FoodCategory.Meat : FoodCategory.Bakery,
                35, 600, 60, cooking: meat ? new CookingProfile(60, 120, 45, 65, 90) : null),
            temperatureCelsius: 120, isContaminated: contaminated);
        private void Contact(FoodState food, ContaminationState surface) => _policy.TransferContact(food, surface, _surfaceId, "Prep");

        [Test]
        public void SurfaceStartsSanitarilySafeIndependentlyOfDirt()
        {
            var surface = new ContaminationState(); var dirt = new DirtState(new DirtProfile(), 1);
            Contact(Food(), surface);
            Assert.That(surface.IsContaminated, Is.False); Assert.That(surface.Traces, Is.Empty);
            Assert.That(dirt.Amount, Is.EqualTo(1));
        }

        [TestCase(0)] [TestCase(20)]
        public void RawAndUndercookedMeatEmitPredictableDoseAndOriginalIdentity(double dose)
        {
            var patty = Food(true); patty.Advance(dose, new ThermalEnvironment(120, allowsCooking: true), 0);
            var surface = new ContaminationState(); Contact(patty, surface);
            Assert.That(surface.Intensity, Is.EqualTo(.4)); var trace = surface.Traces[ContaminationKind.RawMeat];
            Assert.That(trace.FoodUnitId, Is.EqualTo(patty.InstanceId)); Assert.That(trace.FoodDefinitionId, Is.EqualTo("patty"));
            Assert.That(trace.FoodCategory, Is.EqualTo(FoodCategory.Meat)); Assert.That(trace.OriginId, Is.EqualTo(patty.InstanceId));
            Assert.That(patty.IsContaminated, Is.False, "Inherent raw hazard does not mark the donor itself as cross-contaminated.");
        }

        [TestCase(45)] [TestCase(65)] [TestCase(90)]
        public void CookingStagesPastUndercookedDoNotCreateAnInherentRawHazard(double dose)
        {
            var food = Food(true); food.Advance(dose, new ThermalEnvironment(120, allowsCooking: true), 0);
            var surface = new ContaminationState(); Contact(food, surface); Assert.That(surface.IsContaminated, Is.False);
        }

        [Test]
        public void ExistingFoodContaminationEmitsEvenFromCookedFoodAndKeepsItsOrigin()
        {
            var food = Food(true, true); food.Advance(45, new ThermalEnvironment(120, allowsCooking: true), 0);
            var surface = new ContaminationState(); Contact(food, surface);
            Assert.That(surface.Intensity, Is.EqualTo(.5)); Assert.That(food.IsContaminated, Is.True);
            Assert.That(surface.Traces.Single().Value.OriginId, Is.EqualTo(food.InstanceId));
        }

        [Test]
        public void PattyPrepBunChainRetainsContaminationAndSourceWithoutItsDonorPresent()
        {
            var surface = new ContaminationState(); var patty = Food(true); var bun = Food();
            Contact(patty, surface); var originalId = patty.InstanceId; patty = null;
            Assert.That(surface.Intensity, Is.EqualTo(.4)); Contact(bun, surface);
            Assert.That(bun.IsContaminated, Is.True); Assert.That(bun.Contamination.Intensity, Is.EqualTo(.2));
            var trace = bun.Contamination.Traces[ContaminationKind.RawMeat];
            Assert.That(trace.OriginId, Is.EqualTo(originalId)); Assert.That(trace.LastSourceId, Is.EqualTo(_surfaceId));
            Assert.That(trace.LastSource, Is.EqualTo("Prep")); Assert.That(trace.FoodDefinitionId, Is.EqualTo("patty"));
            surface.Sanitize(); Assert.That(bun.Contamination.Intensity, Is.EqualTo(.2));
        }

        [Test]
        public void DirtySurfaceCleanFoodAndCleaningDoNotCoupleSanitaryState()
        {
            var surface = new ContaminationState(); var dirt = new DirtState(new DirtProfile(), .8); var bun = Food();
            Contact(bun, surface); Assert.That(bun.IsContaminated, Is.False);
            Contact(Food(true), surface); dirt.Clean(100, 1);
            Assert.That(dirt.Amount, Is.Zero); Assert.That(surface.Intensity, Is.EqualTo(.4));
            dirt.AddDirt(.9, DirtKind.Grease, "Usage"); surface.Sanitize();
            Assert.That(dirt.Amount, Is.EqualTo(.9)); Assert.That(surface.IsContaminated, Is.False);
        }

        [TestCase(.5, .2)] [TestCase(1, 0)] [TestCase(0, .4)]
        public void ExplicitSanitizingReducesOnlySurfaceContamination(double fraction, double expected)
        {
            var surface = new ContaminationState(); Contact(Food(true), surface); surface.Sanitize(fraction);
            Assert.That(surface.Intensity, Is.EqualTo(expected));
            if (expected > 0) Assert.That(surface.Traces.Single().Value.FoodDefinitionId, Is.EqualTo("patty"));
        }

        [Test]
        public void CrossContaminationSurvivesCookingTemperatureAndFreshnessChanges()
        {
            var patty = Food(true); var surface = new ContaminationState(); Contact(Food(false, true), surface);
            Contact(patty, surface); var evidence = patty.Contamination.Snapshot();
            patty.Advance(45, new ThermalEnvironment(120, allowsCooking: true), 0);
            Assert.That(patty.Cooking.Stage, Is.EqualTo(CookingStage.Cooked));
            Assert.That(patty.IsContaminated, Is.True); Assert.That(patty.Contamination.Snapshot(), Is.EqualTo(evidence));
        }

        [Test]
        public void RepeatedContactsAndRoundTripsCannotAmplifyOrGrowEvidenceUnboundedly()
        {
            var surface = new ContaminationState(); var patty = Food(true); var bun = Food();
            Contact(patty, surface); Contact(bun, surface); int revision = surface.Revision;
            for (int i = 0; i < 1000; i++) { Contact(patty, surface); Contact(bun, surface); }
            Assert.That(surface.Intensity, Is.EqualTo(.4)); Assert.That(bun.Contamination.Intensity, Is.EqualTo(.2));
            Assert.That(surface.Revision, Is.EqualTo(revision)); Assert.That(surface.Traces.Count, Is.EqualTo(1));
            for (int i = 0; i < 100; i++) Contact(Food(true), surface);
            Assert.That(surface.Traces.Count, Is.EqualTo(1)); Assert.That(surface.Intensity, Is.EqualTo(.4));
        }

        [Test]
        public void IndependentSurfacesDoNotShareMutableContamination()
        {
            var first = new ContaminationState(); var second = new ContaminationState();
            Contact(Food(true), first); var bun = Food(); Contact(bun, second);
            Assert.That(first.IsContaminated, Is.True); Assert.That(second.IsContaminated, Is.False); Assert.That(bun.IsContaminated, Is.False);
        }

        [TestCase(0, 0)] [TestCase(.25, .25)] [TestCase(1, 1)]
        public void FractionsAreConfigurableDeterministically(double fraction, double expected)
        {
            var policy = new FoodSafetyPolicy(1, fraction, fraction); var surface = new ContaminationState();
            policy.TransferContact(Food(true), surface, _surfaceId, "Prep");
            Assert.That(surface.Intensity, Is.EqualTo(expected)); var bun = Food();
            policy.TransferContact(bun, surface, _surfaceId, "Prep");
            Assert.That(bun.Contamination.Intensity, Is.EqualTo(expected * fraction));
        }

        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-.1)] [TestCase(1.1)]
        public void InvalidConfigurationAndSanitizingCannotMutateState(double value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FoodSafetyPolicy(value));
            var surface = new ContaminationState(); Contact(Food(true), surface);
            Assert.Throws<ArgumentOutOfRangeException>(() => surface.Sanitize(value)); Assert.That(surface.Intensity, Is.EqualTo(.4));
        }

        [Test]
        public void DominantOriginForEqualLoadsDoesNotDependOnDonorOrder()
        {
            var first = new ContaminationTrace(ContaminationKind.Development, Guid.NewGuid(), "First", .8);
            var second = new ContaminationTrace(ContaminationKind.Development, Guid.NewGuid(), "Second", .8);
            var a = new ContaminationState(); var b = new ContaminationState();
            a.Receive(first); a.Receive(second); b.Receive(second); b.Receive(first);
            Assert.That(a.Traces.Single().Value.OriginId, Is.EqualTo(b.Traces.Single().Value.OriginId));
        }

        [Test]
        public void NormalM14M15M16PipelinePaysFullAndKeepsImmutableCrossContaminationEvidence()
        {
            var surface = new ContaminationState(); var patty = Food(true); var bun = Food();
            Contact(patty, surface); Contact(bun, surface);
            patty.Advance(45, new ThermalEnvironment(120, allowsCooking: true), 0);
            var order = new OrderState(new OrderOffer(new DishProfile("hamburger", "Hamburger", new[] { "bun", "patty", "bun" }), 500, new[] { 3, 4, 3 }));
            var visit = new CustomerVisit(order); visit.Arrive(); visit.BeginWaiting(); visit.Receive(); visit.BeginEvaluation();
            var ledger = new PaymentLedger();
            Assert.That(OrderDelivery.TryComplete(order, new DeliveryContents(new[] { bun, patty, Food() }), ledger, out _), Is.True);
            visit.Resolve(); Assert.That(order.Result.PaymentCents, Is.EqualTo(500));
            Assert.That(visit.Consequence.Reaction, Is.EqualTo(CustomerReaction.HealthIncident));
            Assert.That(visit.Consequence.Quality.Issues, Is.EqualTo(new[] { FoodQualityIssue.Contaminated }));
            var evidence = visit.Consequence.Quality.Delivery.Ingredients.Single(food => food.InstanceId == bun.InstanceId).Contamination;
            Assert.That(evidence.Single().OriginId, Is.EqualTo(patty.InstanceId));
            var reputation = new RestaurantReputationState(new ReputationPolicy(delayWorldSeconds: 1), () => 0);
            var clock = new GameTime(); reputation.TryRegisterService(visit.Consequence, clock); reputation.TryCompleteVisit(visit.Consequence, clock);
            clock.Advance(2); reputation.ResolveDue(clock);
            Assert.That(reputation.Value, Is.EqualTo(42)); Assert.That(ledger.BalanceCents, Is.EqualTo(500));
            bun.Contamination.Sanitize(); surface.Sanitize();
            Assert.That(evidence.Single().Intensity, Is.EqualTo(.2)); Assert.That(order.Result.Evaluation.DeliveredDish.Ingredients.Any(food => food.IsContaminated), Is.True);
        }
    }
}
