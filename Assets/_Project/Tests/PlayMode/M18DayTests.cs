#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;
using ZeroStarRestaurant.Orders;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Tests
{
    public sealed partial class M11RestaurantDayTests
    {
        private static void PlaceM18Food(FoodItem food, CleanableSurface surface, float topOffset = 0)
        {
            Physics.SyncTransforms(); var bounds = surface.Support.bounds;
            var body = food.GetComponent<Rigidbody>(); body.interpolation = RigidbodyInterpolation.None;
            body.position = new Vector3(bounds.center.x, bounds.max.y + food.GetComponent<Collider>().bounds.extents.y + .003f + topOffset, bounds.center.z);
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; Physics.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator M18RawPattyPrepBunCookedHamburgerPassPaysAndProducesNormalDelayedRisk()
        {
            var prep = Components<CleanableSurface>().Single(surface => surface.DisplayName == "Prep / Assembly worktop");
            var contact = prep.GetComponent<SurfaceFoodContact>(); var station = Components<IngredientPurchaseStation>().Single();
            Assert.That(prep.Contamination.IsContaminated, Is.False); Assert.That(prep.State.Amount, Is.Zero);
            Assert.That(station.TryPurchase(0, out FoodItem bun), Is.True);
            Assert.That(bun.State.IsContaminated, Is.False); bun.GetComponent<Rigidbody>().position += Vector3.up * 3;
            Assert.That(station.TryPurchase(1, out FoodItem patty), Is.True); PlaceM18Food(patty, prep);
            for (int frame = 0; frame < 10; frame++) yield return new WaitForFixedUpdate();
            Assert.That(prep.Contamination.Intensity, Is.EqualTo(.4).Within(1e-7));
            patty.GetComponent<Rigidbody>().position += Vector3.up * 3; contact.Poll();
            PlaceM18Food(bun, prep); for (int frame = 0; frame < 10; frame++) yield return new WaitForFixedUpdate();
            Assert.That(bun.State.Contamination.Intensity, Is.EqualTo(.2).Within(1e-7));
            bun.GetComponent<Rigidbody>().position += Vector3.up * 3; contact.Poll();
            var evidence = bun.State.Contamination.Snapshot().Single(); Assert.That(evidence.OriginId, Is.EqualTo(patty.State.InstanceId));
            Assert.That(evidence.LastSourceId, Is.EqualTo(prep.State.SurfaceId));

            // Actual Grill with the existing single FoodSimulation driver.
            var grill = Components<CleanableSurface>().Single(surface => surface.DisplayName == "Grill");
            Components<RestaurantElectricity>().Single().PowerOn(); PlaceM18Food(patty, grill);
            grill.GetComponent<SurfaceFoodContact>().Poll(); _food.Advance(45);
            Assert.That(patty.State.Cooking.Stage, Is.EqualTo(CookingStage.Cooked));
            Assert.That(patty.State.IsContaminated, Is.False, "First raw contact emits without reflecting its new dose back.");
            patty.GetComponent<Rigidbody>().position += Vector3.up * 3; grill.GetComponent<SurfaceFoodContact>().Poll();

            var assembly = Components<CleanableSurface>().Single(surface => surface.DisplayName == "Assembly 3");
            PlaceM18Food(bun, assembly); PlaceM18Food(patty, assembly, .12f);
            Assert.That(station.TryPurchase(0, out FoodItem upperBun), Is.True); PlaceM18Food(upperBun, assembly, .24f);
            for (int frame = 0; frame < 40; frame++) yield return new WaitForFixedUpdate();
            Assert.That(Components<PhysicalDishAssembly>().Single().TryFinalize(upperBun, out DishItem dish), Is.True);
            Assert.That(_service.ForceNextOrder(0), Is.True); _service.Advance(.55); _service.Advance(30);
            var player = Components<ZeroStarRestaurant.Player.FirstPersonController>().Single();
            player.transform.position += Vector3.right * 20; player.transform.rotation = Quaternion.Euler(0, 180, 0);
            PlaceAndPoll(dish); var result = _service.LastResult; var consequence = _service.LastConsequence;
            Assert.That(result.PaymentCents, Is.EqualTo(500));
            Assert.That(consequence.Reaction, Is.EqualTo(CustomerReaction.HealthIncident));
            Assert.That(consequence.Quality.Issues, Is.EqualTo(new[] { FoodQualityIssue.Contaminated }));
            var bunSnapshot = consequence.Quality.Delivery.Ingredients.Single(food => food.InstanceId == bun.State.InstanceId);
            Assert.That(bunSnapshot.Contamination.Single().OriginId, Is.EqualTo(patty.State.InstanceId));
            Assert.That(Reputation.State.PendingCount, Is.EqualTo(1)); _service.Advance(30); yield return null;
            Assert.That(dish == null, Is.True); long balance = _service.Ledger.BalanceCents;
            _day.Advance(120.01);
            Assert.That(Reputation.State.ResolutionCount, Is.EqualTo(1)); Assert.That(Reputation.State.LastResolution.Forced, Is.False);
            Assert.That(Reputation.State.HealthRisks.Single().Probability, Is.EqualTo(.75));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(balance)); Assert.That(_service.LastResult, Is.SameAs(result));
            Assert.That(prep.Contamination.IsContaminated, Is.True);
        }

        [UnityTest]
        public IEnumerator M18NextDayDisableAndDirtCleaningPreserveSurfaceContaminationAndFoodEvidence()
        {
            var surfaces = Components<CleanableSurface>(); var first = surfaces[0]; var second = surfaces[1];
            first.DevelopmentMakeFilthy(); first.DevelopmentContaminateSurface();
            var sanitaryState = first.Contamination; var trace = sanitaryState.Snapshot().Single();
            first.DevelopmentCleanSurface(); first.enabled = false; first.enabled = true;
            CloseClock(); Assert.That(_day.StartNextDay(), Is.True); yield return null;
            Assert.That(first.Contamination, Is.SameAs(sanitaryState)); Assert.That(first.Contamination.Snapshot().Single(), Is.SameAs(trace));
            Assert.That(first.State.Amount, Is.Zero); Assert.That(second.Contamination.IsContaminated, Is.False);
            first.DevelopmentMakeFilthy(); first.DevelopmentSanitizeSurface();
            Assert.That(first.State.Amount, Is.EqualTo(1)); Assert.That(first.Contamination.IsContaminated, Is.False);
        }

        [UnityTest]
        public IEnumerator M18DirtySanitizedPrepKeepsBunSafeThroughRealRestingContact()
        {
            var surface = Components<CleanableSurface>().Single(item => item.DisplayName == "Prep / Assembly worktop");
            surface.DevelopmentMakeFilthy(); surface.DevelopmentSanitizeSurface();
            Assert.That(Components<IngredientPurchaseStation>().Single().TryPurchase(0, out FoodItem bun), Is.True);
            PlaceM18Food(bun, surface); for (int frame = 0; frame < 20; frame++) yield return new WaitForFixedUpdate();
            Assert.That(bun.State.IsContaminated, Is.False); Assert.That(surface.Contamination.IsContaminated, Is.False);
        }
    }
}
#endif
