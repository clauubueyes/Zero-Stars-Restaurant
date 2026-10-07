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
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Orders;
using ZeroStarRestaurant.Player;
using ZeroStarRestaurant.Restaurant;

namespace ZeroStarRestaurant.Tests
{
    public sealed partial class M11RestaurantDayTests
    {
        private Scene _scene;
        private RestaurantDayController _day;
        private CustomerServiceLoop _service;
        private FoodSimulation _food;
        private CustomerQueueController Queue => _service.Queue;
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        private static void Set(Component component, string name, float value)
        {
            var data = new SerializedObject(component); data.FindProperty(name).floatValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Manual(Component component)
        {
            var data = new SerializedObject(component); data.FindProperty("_advanceAutomatically").boolValue = false; data.ApplyModifiedPropertiesWithoutUndo();
        }
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _scene = EditorSceneManager.LoadSceneInPlayMode(M1Path, new LoadSceneParameters(LoadSceneMode.Additive)); yield return null;
            Components<FirstPersonController>().Single().enabled = false;
            _day = Components<RestaurantDayController>().Single(); Manual(_day);
            _service = Components<CustomerServiceLoop>().Single(); _service.enabled = false; Manual(_service); _service.enabled = true;
            _food = Components<FoodSimulation>().Single(); _food.enabled = false;
            Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Open)); Assert.That(Queue.Count, Is.Zero);
        }
        private const string M1Path = "Assets/_Project/Scenes/PrototypeRestaurant.unity";
        [UnityTearDown] public IEnumerator TearDown() { if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene); }
        private void CloseClock()
        {
            _day.Advance((_day.State.ClosingMinute * 60 - _day.State.Clock.SecondsOfDay) / 60.0);
            Assert.That(_day.CanAdmitCustomers, Is.False);
        }
        [UnityTest]
        public IEnumerator WorldSpeedPauseAndDayChangesNeverAdvanceOrRecreateFoodOrMoney()
        {
            IngredientPurchaseStation station = Components<IngredientPurchaseStation>().Single();
            Assert.That(station.TryPurchase(1, out FoodItem item), Is.True);
            item.transform.position = new Vector3(-3, 2, 2); item.State.Contaminate(); item.State.SetTemperature(80); Physics.SyncTransforms();
            FoodState state = item.State; var id = state.InstanceId; var ledger = _service.Ledger;
            double clock = _day.State.Clock.SecondsOfDay; Set(_day, "_worldSecondsPerSimulationSecond", 120); _day.Advance(2.5);
            Assert.That(_day.State.Clock.SecondsOfDay, Is.EqualTo(clock + 300)); Assert.That(state.AgeSeconds, Is.Zero);
            _day.PauseClock(); _day.Advance(10000); Assert.That(_day.State.Clock.SecondsOfDay, Is.EqualTo(clock + 300));
            _food.Advance(10); Assert.That(state.AgeSeconds, Is.EqualTo(10)); Assert.That(state.TemperatureCelsius, Is.LessThan(80));
            double temperature = state.TemperatureCelsius, deterioration = state.DeteriorationSeconds;
            _service.Advance(30); Assert.That(Queue.Count, Is.EqualTo(4), "Pausing world time does not pause customers.");
            Assert.That(Queue.Head.State.WaitingSeconds, Is.GreaterThan(0));
            _service.enabled = false; _day.ResumeClock(); Set(_day, "_worldSecondsPerSimulationSecond", 60); CloseClock();
            Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Closed)); Assert.That(_day.StartNextDay(), Is.True);
            Assert.That(_day.State.Clock.DayNumber, Is.EqualTo(2)); Assert.That(item.State, Is.SameAs(state)); Assert.That(state.InstanceId, Is.EqualTo(id));
            Assert.That(state.AgeSeconds, Is.EqualTo(10)); Assert.That(state.TemperatureCelsius, Is.EqualTo(temperature));
            Assert.That(state.DeteriorationSeconds, Is.EqualTo(deterioration)); Assert.That(state.IsContaminated, Is.True);
            Assert.That(_service.Ledger, Is.SameAs(ledger)); Assert.That(ledger.BalanceCents, Is.EqualTo(620));
            _food.Advance(5); Assert.That(state.AgeSeconds, Is.EqualTo(15)); yield return null;
        }
        [UnityTest]
        public IEnumerator ClosingBlocksDirectAndTimedAdmissionWhileAllFourExistingCustomersFinish()
        {
            _service.Advance(30); Assert.That(Queue.Count, Is.EqualTo(4)); var ids = Queue.Customers.Select(customer => customer.State.InstanceId).ToArray();
            CloseClock(); Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Closing)); Assert.That(_day.StartNextDay(), Is.False);
            Assert.That(Queue.TryAdmit(), Is.False); _service.Advance(100); Assert.That(Queue.AdmittedCount, Is.EqualTo(4));
            DishItem custom = null; yield return BuildDish(new[] { 2 }, dish => custom = dish);
            for (int customer = 0; customer < 4; customer++)
            {
                Assert.That(_service.Visit.InstanceId, Is.EqualTo(ids[customer])); Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
                PlaceAndPoll(custom); Assert.That(_service.LastResult.Accepted, Is.False);
                Assert.That(_service.ForceNextOrder(0), Is.True); // Cheese remains irrelevant to each Hamburger in this closing fixture.
                _service.Advance(30);
                Assert.That(Queue.Count, Is.EqualTo(3 - customer)); Assert.That(Queue.AdmittedCount, Is.EqualTo(4)); Assert.That(Queue.TryAdmit(), Is.False);
                custom.transform.position += Vector3.right * 3; Physics.SyncTransforms(); Components<DeliveryZone>().Single().Poll();
            }
            Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Closed)); Assert.That(_service.Visit, Is.Null);
            Assert.That(custom.State.IsSold, Is.False); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(675));
            Assert.That(_day.StartNextDay(), Is.True); _service.Advance(0.55);
            Assert.That(Queue.Count, Is.EqualTo(1)); Assert.That(Queue.Head.State.Number, Is.EqualTo(5));
            Assert.That(ids, Has.No.Member(Queue.Head.State.InstanceId)); Assert.That(_day.State.Clock.DayNumber, Is.EqualTo(2));
            Assert.That(custom.State.IsFinalized, Is.True); yield return null;
        }
        [UnityTest]
        public IEnumerator PaidCustomerCanFinishAfterClosingAndTheNextDayPreservesReceiptAndUnservedFood()
        {
            _service.Advance(0.55); CloseClock(); Assert.That(Queue.Count, Is.EqualTo(1));
            Assert.That(Queue.Head.State.Stage, Is.EqualTo(QueuedCustomerStage.Entering));
            _service.Advance(30); Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
            DishItem hamburger = null; yield return BuildDish(new[] { 0, 1, 0 }, dish => hamburger = dish); PlaceAndPoll(hamburger);
            Assert.That(_service.LastResult.Accepted, Is.True); var receipt = _service.LastResult; var firstVisit = _service.Visit;
            var firstConsequence = firstVisit.Consequence; var statistics = _service.Statistics;
            Assert.That(firstConsequence.Reaction, Is.EqualTo(CustomerReaction.Satisfied));
            Assert.That(statistics.CustomersServed, Is.EqualTo(1));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1350)); Assert.That(_service.TryDeliver(hamburger), Is.False);
            Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Closing)); Assert.That(_day.StartNextDay(), Is.False);
            IngredientPurchaseStation station = Components<IngredientPurchaseStation>().Single(); Assert.That(station.TryPurchase(2, out FoodItem reserve), Is.True);
            reserve.transform.position = new Vector3(-3, 2, 2); Physics.SyncTransforms(); FoodState reserveState = reserve.State;
            _service.Advance(30); Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Closed));
            Assert.That(firstVisit.Stage, Is.EqualTo(CustomerStage.Finished)); Assert.That(Queue.Count, Is.Zero); yield return null;
            Assert.That(hamburger == null, Is.True); Assert.That(_food.Foods, Is.EqualTo(new[] { reserve }));
            Assert.That(_day.StartNextDay(), Is.True); Assert.That(_service.LastResult, Is.SameAs(receipt));
            Assert.That(_service.Statistics, Is.SameAs(statistics)); Assert.That(_service.LastConsequence, Is.SameAs(firstConsequence));
            Assert.That(statistics.History.Single(), Is.SameAs(firstConsequence));
            Assert.That(reserve.State, Is.SameAs(reserveState)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1025));
            _service.Advance(0.55); Assert.That(Queue.Head.State.Number, Is.EqualTo(2)); CloseClock(); _service.Advance(30);
            Assert.That(_service.Visit.Order.Offer.Dish.Id, Is.EqualTo("dish.cheeseburger"));
            DishItem cheese = null; yield return BuildDish(new[] { 0, 1, 2, 0 }, dish => cheese = dish); PlaceAndPoll(cheese);
            Assert.That(_service.LastResult.Accepted, Is.True); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1500));
            _service.Advance(30); Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Closed));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1200), "Both days settle rent once.");
            Assert.That(_day.State.Clock.DayNumber, Is.EqualTo(2)); Assert.That(_food.Foods, Is.EqualTo(new[] { reserve })); yield return null;
            Assert.That(statistics.CustomersServed, Is.EqualTo(2)); Assert.That(statistics.Satisfied, Is.EqualTo(2));
            Assert.That(statistics.Complaints + statistics.HealthIncidents, Is.Zero);
        }
        [UnityTest]
        public IEnumerator EmptyClosingFreezesWorldClockAndDisablingTheDayCannotBypassAdmission()
        {
            CloseClock(); Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Closed));
            double time = _day.State.Clock.SecondsOfDay; _day.Advance(10000); _service.Advance(100);
            Assert.That(_day.State.Clock.SecondsOfDay, Is.EqualTo(time)); Assert.That(Queue.Count, Is.Zero);
            _day.enabled = false; Assert.That(Queue.TryAdmit(), Is.False); _service.Advance(100); Assert.That(Queue.Count, Is.Zero);
            _day.enabled = true; Assert.That(_day.StartNextDay(), Is.True); _service.Advance(0.55); Assert.That(Queue.Count, Is.EqualTo(1)); yield return null;
        }
        private IEnumerator BuildDish(int[] products, System.Action<DishItem> completed)
        {
            BoxCollider support = Components<BoxCollider>().Single(item => item.name == "PrepSupport3");
            PhysicalDishAssembly physical = Components<PhysicalDishAssembly>().Single();
            FoodItem last = null;
            IngredientPurchaseStation station = Components<IngredientPurchaseStation>().Single(); float top = support.bounds.max.y;
            foreach (int product in products)
            {
                Assert.That(station.TryPurchase(product, out FoodItem item), Is.True, station.LastMessage);
                if (product == 1) { item.State.SetTemperature(120); item.State.Advance(45, new ThermalEnvironment(120, allowsCooking: true), 0); }
                float half = item.GetComponent<BoxCollider>().bounds.extents.y;
                item.GetComponent<Rigidbody>().position = new Vector3(support.transform.position.x, top + half + 0.003f, support.transform.position.z);
                top += 2 * half + 0.003f; last = item; Physics.SyncTransforms();
            }
            for (int frame = 0; frame < 40; frame++) yield return new WaitForFixedUpdate();
            Assert.That(physical.TryFinalize(last, out DishItem dish), Is.True); completed(dish);
        }
        private void PlaceAndPoll(DishItem dish)
        {
            BoxCollider pad = Components<BoxCollider>().Single(item => item.name == "DeliveryPad");
            Rigidbody body = dish.GetComponent<Rigidbody>(); body.interpolation = RigidbodyInterpolation.None;
            dish.transform.position = pad.transform.position + Vector3.up; Physics.SyncTransforms();
            dish.transform.position += Vector3.up * (pad.bounds.max.y - dish.GetComponent<BoxCollider>().bounds.min.y);
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; Physics.SyncTransforms(); Components<DeliveryZone>().Single().Poll();
            Assert.That(_service.Visit.Order.IsCompleted, Is.True);
        }
    }
}
#endif
