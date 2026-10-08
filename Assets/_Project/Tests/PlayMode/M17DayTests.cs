#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Hygiene;

namespace ZeroStarRestaurant.Tests
{
    public sealed partial class M11RestaurantDayTests
    {
        [UnityTest]
        public IEnumerator M17NextDayAndComponentDisablePreserveEachSurfaceState()
        {
            var surfaces = Components<CleanableSurface>(); Assert.That(surfaces.Length, Is.EqualTo(7));
            Assert.That(surfaces.Select(surface => surface.State.Amount), Is.All.Zero);
            var grill = surfaces.Single(surface => surface.DisplayName == "Grill");
            var prep = surfaces.Single(surface => surface.DisplayName == "Assembly 3");
            grill.AddDirt(.8, DirtKind.Grease, "Development cooking"); prep.AddDirt(.6, DirtKind.FoodResidue, "Development contact");
            var states = surfaces.Select(surface => surface.State).ToArray();
            var amounts = states.Select(state => state.Amount).ToArray();
            grill.enabled = false; grill.enabled = true; CloseClock(); Assert.That(_day.StartNextDay(), Is.True);
            Assert.That(_day.State.Clock.DayNumber, Is.EqualTo(2));
            yield return null;
            Assert.That(surfaces.Select(surface => surface.State), Is.EqualTo(states));
            Assert.That(surfaces.Select(surface => surface.State.Amount), Is.EqualTo(amounts));
        }

        [UnityTest]
        public IEnumerator M17FilthySurfacesDoNotChangeFullPaymentReactionFoodOrReputation()
        {
            foreach (var surface in Components<CleanableSurface>()) surface.DevelopmentMakeFilthy();
            yield return ServeM16Dish(45);
            Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(500));
            Assert.That(_service.LastConsequence.Reaction, Is.EqualTo(CustomerReaction.Satisfied));
            Assert.That(Reputation.State.Value, Is.EqualTo(50)); Assert.That(Reputation.State.PendingCount, Is.Zero);
            Assert.That(_service.LastConsequence.Quality.Delivery.Ingredients.All(food => !food.IsContaminated), Is.True);
            _service.Advance(30); Assert.That(Reputation.State.Value, Is.EqualTo(51)); yield return null;
        }

        [UnityTest]
        public IEnumerator M17CleaningDoesNotRemoveM15ContaminationOrAlterM16HealthRisk()
        {
            foreach (var surface in Components<CleanableSurface>()) surface.DevelopmentMakeFilthy();
            yield return ServeM16Dish(45, true);
            var risk = Reputation.State.HealthRisks.Single(); var consequence = _service.LastConsequence;
            Assert.That(consequence.Reaction, Is.EqualTo(CustomerReaction.HealthIncident));
            foreach (var surface in Components<CleanableSurface>()) surface.State.Clean(10, .12);
            Assert.That(Reputation.State.HealthRisks.Single(), Is.SameAs(risk));
            Assert.That(_service.LastConsequence, Is.SameAs(consequence)); Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(500));
            _service.Advance(30); yield return null;
            long balance = _service.Ledger.BalanceCents; Reputation.ConfirmDepartedNow();
            Assert.That(Reputation.State.Value, Is.EqualTo(42)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(balance));
            Assert.That(Reputation.State.ResolutionCount, Is.EqualTo(1));
        }
    }
}
#endif
