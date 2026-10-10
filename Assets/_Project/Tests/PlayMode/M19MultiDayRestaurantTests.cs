#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;
using ZeroStarRestaurant.Orders;
using ZeroStarRestaurant.Player;
using ZeroStarRestaurant.Restaurant;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Tests
{
    public sealed partial class M19MultiDayRestaurantTests
    {
        private Scene _scene;
        private RestaurantDayController _day;
        private CustomerServiceLoop _service;
        private RestaurantElectricity _power;
        private FoodSimulation _food;
        private IngredientPurchaseStation _station;
        private T[] All<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        private static void Manual(Component component)
        { var data = new SerializedObject(component); data.FindProperty("_advanceAutomatically").boolValue = false; data.ApplyModifiedPropertiesWithoutUndo(); }
        [UnitySetUp] public IEnumerator SetUp()
        {
            _scene = EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/PrototypeRestaurant.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            All<FirstPersonController>().Single().enabled = false;
            _day = All<RestaurantDayController>().Single(); Manual(_day);
            _service = All<CustomerServiceLoop>().Single(); Manual(_service);
            _power = All<RestaurantElectricity>().Single(); Manual(_power);
            _food = All<FoodSimulation>().Single(); _food.enabled = false;
            _station = All<IngredientPurchaseStation>().Single();
            Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Preparation)); Assert.That(_day.Calendar.CurrentDay, Is.EqualTo(1));
        }
        [UnityTearDown] public IEnumerator TearDown() { if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene); }
        private FoodItem Buy(int product, Vector3 position)
        {
            Assert.That(_station.TryPurchase(product, out FoodItem food), Is.True, _station.LastMessage);
            food.GetComponent<Rigidbody>().isKinematic = true; food.transform.position = position; Physics.SyncTransforms(); return food;
        }
        private void OnSupport(FoodItem food, BoxCollider support)
        {
            var body = food.GetComponent<Rigidbody>(); body.isKinematic = false; body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            food.transform.position = new Vector3(support.bounds.center.x, support.bounds.max.y + food.GetComponent<Collider>().bounds.extents.y + .001f, support.bounds.center.z);
            Physics.SyncTransforms();
        }
        [UnityTest]
        public IEnumerator FourRealDaysPreservePurchasedFoodDishStorageSurfacesPowerDebtAndWorldObjects()
        {
            var storage = All<ColdStorage>(); Assert.That(storage.Length, Is.EqualTo(2)); _power.PowerOn();
            var zones = storage.Select(s => (BoxCollider)new SerializedObject(s).FindProperty("_interior").objectReferenceValue).ToArray();
            var first = Buy(1, zones[0].bounds.center); first.State.Contaminate();
            var second = Buy(0, zones[1].bounds.center);
            var dishFood = Buy(2, Vector3.up * 4);
            var support = All<BoxCollider>().Single(c => c.name == "PrepSupport3"); OnSupport(dishFood, support);
            Assert.That(All<PhysicalDishAssembly>().Single().TryFinalize(dishFood, out DishItem dish), Is.True);
            dish.GetComponent<Rigidbody>().isKinematic = true;
            Assert.That(_station.TryPurchasePlate(out PlateItem plate), Is.True); plate.GetComponent<Rigidbody>().isKinematic = true;
            plate.transform.position = new Vector3(-2, 2, 2); Physics.SyncTransforms();
            var foods = new[] { first, second, dishFood }; var states = foods.Select(f => f.State).ToArray();
            var dishState = dish.State; var plateId = plate.InstanceId;
            var surfaces = All<CleanableSurface>(); surfaces[0].DevelopmentMakeFilthy(); surfaces[0].DevelopmentContaminateSurface();
            var dirt = surfaces.Select(s => s.State).ToArray(); var contamination = surfaces.Select(s => s.Contamination).ToArray();
            var amounts = dirt.Select(d => d.Amount).ToArray(); var traces = contamination.Select(c => c.Snapshot().ToArray()).ToArray();
            _food.Advance(20);
            var foodEvidence = states.Select(s => (s.InstanceId, s.AgeSeconds, s.FreshnessPercent, s.DeteriorationSeconds, s.TemperatureCelsius, s.Cooking?.Stage, s.IsContaminated)).ToArray();
            var poses = foods.Select(f => (f.transform.position, f.transform.rotation)).ToArray();
            _power.Appliances[0].SetOn(false); _power.Advance(3600);
            var ledger = _service.Ledger; var transactions = ledger.Transactions.ToArray(); var objects = All<Transform>();
            var powerState = _power.State; var meters = _power.Appliances.Select(a => a.Meter).ToArray();
            var settings = _power.Appliances.Select(a => a.IsOn).ToArray(); double lifetimeEnergy = powerState.ConsumedKilowattHours;
            var reputation = _service.Reputation.State; int value = reputation.Value;
            int starts = 0, ends = 0;
            _day.DayStarted += number => { starts++; Assert.That(ledger.DayNumber, Is.EqualTo(number)); Assert.That(_service.Statistics.DayNumber, Is.EqualTo(number)); Assert.That(_power.State.DailyConsumedKilowattHours, Is.Zero); };
            _day.DayEnded += number => { ends++; Assert.That(_day.Summary.DayNumber, Is.EqualTo(number)); Assert.That(ledger.IsDaySettled, Is.True); };
            for (int number = 1; number <= 4; number++)
            {
                _day.Advance(100000); _service.Advance(1000); Assert.That(_service.Queue.Count, Is.Zero); Assert.That(_service.Queue.TryAdmit(), Is.False);
                Assert.That(_day.Calendar.CurrentDay, Is.EqualTo(number)); Assert.That(_day.OpenRestaurant(), Is.True);
                Assert.That(_day.ForceClose(), Is.True); Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Closed));
                long beforeEnd = ledger.BalanceCents;
                _day.Advance(100000); Assert.That(_day.StartNextDay(), Is.False); Assert.That(ledger.BalanceCents, Is.EqualTo(beforeEnd));
                Assert.That(_day.EndCurrentDay(), Is.True); Assert.That(_day.EndCurrentDay(), Is.False);
                Assert.That(ledger.BalanceCents, Is.EqualTo(beforeEnd - 300)); Assert.That(_day.Summary.Accounting.ElectricityPaidCents, Is.Zero);
                Assert.That(_day.Summary.Accounting.PurchasesCents, Is.EqualTo(number == 1 ? 190 : 0));
                Assert.That(_day.Summary.Accounting.ConsumedKilowattHours, Is.EqualTo(number == 1 ? lifetimeEnergy : 0));
                Assert.That(_day.Summary.CustomersServed, Is.Zero); Assert.That(_day.Summary.Reputation, Is.EqualTo(value));
                Assert.That(All<Transform>(), Is.EquivalentTo(objects)); Assert.That(storage, Is.EqualTo(All<ColdStorage>()));
                Assert.That(dish.State, Is.SameAs(dishState)); Assert.That(dishState.Components.Single(), Is.SameAs(dishFood.State)); Assert.That(plate.InstanceId, Is.EqualTo(plateId));
                Assert.That(foods.Select(f => f.State), Is.EqualTo(states)); Assert.That(foods.Select(f => (f.transform.position, f.transform.rotation)), Is.EqualTo(poses));
                Assert.That(states.Select(s => (s.InstanceId, s.AgeSeconds, s.FreshnessPercent, s.DeteriorationSeconds, s.TemperatureCelsius, s.Cooking?.Stage, s.IsContaminated)), Is.EqualTo(foodEvidence));
                Assert.That(surfaces.Select(s => s.State), Is.EqualTo(dirt)); Assert.That(surfaces.Select(s => s.Contamination), Is.EqualTo(contamination));
                Assert.That(surfaces.Select(s => s.State.Amount), Is.EqualTo(amounts));
                for (int index = 0; index < surfaces.Length; index++) Assert.That(surfaces[index].Contamination.Snapshot(), Is.EqualTo(traces[index]));
                Assert.That(_service.Reputation.State, Is.SameAs(reputation)); Assert.That(_power.State, Is.SameAs(powerState));
                Assert.That(_power.Appliances.Select(a => a.Meter), Is.EqualTo(meters)); Assert.That(_power.Appliances.Select(a => a.IsOn), Is.EqualTo(settings));
                Assert.That(powerState.PendingKilowattHours, Is.EqualTo(lifetimeEnergy)); Assert.That(powerState.ConsumedKilowattHours, Is.EqualTo(lifetimeEnergy));
                Assert.That(ledger.Transactions.Take(transactions.Length), Is.EqualTo(transactions));
                long balance = ledger.BalanceCents; double elapsed = _day.State.Clock.ElapsedWorldSeconds;
                if (number < 4)
                {
                    Assert.That(_day.StartNextDay(), Is.True); Assert.That(_day.StartNextDay(), Is.False); Assert.That(_day.Calendar.CurrentDay, Is.EqualTo(number + 1));
                    Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Preparation)); Assert.That(ledger.BalanceCents, Is.EqualTo(balance));
                    Assert.That(_day.State.Clock.ElapsedWorldSeconds, Is.EqualTo(elapsed)); Assert.That(_power.State.DailyConsumedKilowattHours, Is.Zero);
                    Assert.That(meters.All(m => m.DailyConsumedKilowattHours == 0), Is.True);
                }
            }
            Assert.That(starts, Is.EqualTo(3)); Assert.That(ends, Is.EqualTo(4)); Assert.That(_day.Summaries.Count, Is.EqualTo(4));
            Assert.That(ledger.BalanceCents, Is.EqualTo(-390)); Assert.That(ledger.ElectricityBills, Is.Empty);
            double age = first.State.AgeSeconds; _food.Advance(1); Assert.That(first.State.AgeSeconds, Is.EqualTo(age + 1));
            yield return null;
        }
        [UnityTest]
        public IEnumerator RealDeliveryFinishesAfterForceCloseAndPendingRiskResolvesNormallyOnDayTwo()
        {
            _service.ForceNextOrder(0); Assert.That(_day.OpenRestaurant(), Is.True); _service.Advance(.55);
            Assert.That(_service.Queue.Count, Is.EqualTo(1)); Assert.That(_day.ForceClose(), Is.True);
            Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Closing)); Assert.That(_day.EndCurrentDay(), Is.False);
            _service.Advance(30); var customer = _service.Visit.InstanceId;
            var patty = Buy(1, Vector3.up * 4); patty.State.SetTemperature(120); patty.State.Advance(45, new ThermalEnvironment(120, allowsCooking: true), 0); patty.State.Contaminate();
            OnSupport(patty, All<BoxCollider>().Single(c => c.name == "DeliveryPad")); All<DeliveryZone>().Single().Poll();
            Assert.That(_service.LastResult.PaymentCents, Is.EqualTo(200)); Assert.That(_service.LastConsequence.Reaction, Is.EqualTo(CustomerReaction.HealthIncident));
            Assert.That(_service.Queue.TryAdmit(), Is.False); _service.Advance(30); yield return null;
            Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Closed)); Assert.That(_service.Queue.AdmittedCount, Is.EqualTo(1));
            var rep = _service.Reputation.State; var risk = rep.HealthRisks.Single(); double deadline = risk.DueWorldSeconds.Value;
            var statistics = _service.Statistics; var history = statistics.History.Single();
            Assert.That(_day.EndCurrentDay(), Is.True); Assert.That(_day.Summary.CustomersServed, Is.EqualTo(1)); Assert.That(_day.Summary.HealthIncidents, Is.EqualTo(1));
            Assert.That(_day.Summary.PendingHealthRisks, Is.EqualTo(1)); Assert.That(_day.Summary.Accounting.SalesCents, Is.EqualTo(200));
            Assert.That(_day.StartNextDay(), Is.True); Assert.That(statistics.DailyCustomersServed, Is.Zero); Assert.That(statistics.CustomersServed, Is.EqualTo(1));
            Assert.That(statistics.HealthIncidentHistory.Single(), Is.SameAs(history)); Assert.That(rep.HealthRisks.Single(), Is.SameAs(risk));
            Assert.That(risk.DueWorldSeconds, Is.EqualTo(deadline)); Assert.That(rep.ResolutionCount, Is.Zero);
            _day.Advance(10000); _service.Advance(1000); Assert.That(rep.ResolutionCount, Is.Zero); Assert.That(_service.Queue.Count, Is.Zero);
            Assert.That(_day.OpenRestaurant(), Is.True);
            _day.Advance((deadline - _day.State.Clock.ElapsedWorldSeconds) / 60 + .001);
            Assert.That(rep.ResolutionCount, Is.EqualTo(1)); Assert.That(rep.LastResolution.DayNumber, Is.EqualTo(2)); Assert.That(rep.LastResolution.Forced, Is.False);
            Assert.That(rep.LastResolution.CustomerId, Is.EqualTo(customer));
            _service.Advance(.55); Assert.That(_service.Queue.Count, Is.EqualTo(1)); Assert.That(_service.Queue.Head.State.Number, Is.EqualTo(2));
            Assert.That(_service.Queue.Head.State.InstanceId, Is.Not.EqualTo(customer));
        }
        [Test]
        public void ClosedAllowsPurchasesAndEnergyUntilExplicitEndAndSummaryUsesRealEvidence()
        {
            Assert.That(_day.OpenRestaurant(), Is.True); Assert.That(_day.ForceClose(), Is.True);
            long balance = _service.Ledger.BalanceCents; var food = Buy(0, Vector3.up * 4);
            _power.PowerOn(); _power.Advance(3600); Assert.That(_day.Summary, Is.Null); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(balance - 35));
            Assert.That(_day.EndCurrentDay(), Is.True); var summary = _day.Summary;
            Assert.That(summary.Accounting.PurchasesCents, Is.EqualTo(35)); Assert.That(summary.Accounting.ConsumedKilowattHours, Is.EqualTo(2.35).Within(1e-12));
            Assert.That(summary.Accounting.ElectricityAccruedCents, Is.EqualTo(71)); Assert.That(summary.Accounting.ElectricityPaidCents, Is.Zero);
            Assert.That(_station.TryPurchase(0, out _), Is.False); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(balance - 335));
            var text = All<OperatingCostsFeedback>().Single().Text;
            foreach (string label in new[] { "DAY 1 COMPLETE", "Sales", "Purchases", "Net cash movement", "Customers served", "Satisfied", "Unhappy", "Complaints", "Health incidents", "Reputation", "Electricity used today", "Electricity cost accrued", "Pending electricity bill", "Closing balance" })
                Assert.That(text, Does.Contain(label));
            _power.Advance(3600); Assert.That(_power.State.DailyConsumedKilowattHours, Is.EqualTo(2.35).Within(1e-12));
            Assert.That(_power.State.PendingKilowattHours, Is.EqualTo(4.7).Within(1e-12));
            Assert.That(_day.StartNextDay(), Is.True); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(balance - 335));
            Assert.That(_day.State.Clock.Hour, Is.EqualTo(8)); Assert.That(food != null, Is.True); Assert.That(_power.State.DailyConsumedKilowattHours, Is.Zero);
            Assert.That(_power.State.PendingKilowattHours, Is.EqualTo(4.7).Within(1e-12));
        }
    }
}
#endif
