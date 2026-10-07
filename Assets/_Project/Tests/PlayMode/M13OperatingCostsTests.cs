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
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M13OperatingCostsTests
    {
        private Scene _scene;
        private RestaurantDayController _day;
        private CustomerServiceLoop _service;
        private RestaurantElectricity _supply;
        private RestaurantOperatingCosts _costs;
        private FoodSimulation _food;
        private IngredientPurchaseStation _station;
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        private static void Automatic(Component component, bool value)
        {
            var data = new SerializedObject(component); data.FindProperty("_advanceAutomatically").boolValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _scene = EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/PrototypeRestaurant.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            Components<FirstPersonController>().Single().enabled = false;
            _day = Components<RestaurantDayController>().Single(); Automatic(_day, false);
            _service = Components<CustomerServiceLoop>().Single(); Automatic(_service, false); _service.enabled = false; _service.enabled = true;
            _supply = Components<RestaurantElectricity>().Single(); Automatic(_supply, false);
            _costs = Components<RestaurantOperatingCosts>().Single(); _food = Components<FoodSimulation>().Single(); _food.enabled = false;
            _station = Components<IngredientPurchaseStation>().Single();
            Assert.That(_service.Queue.Count, Is.Zero); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1000));
            Assert.That(_costs.Summary, Is.Null); Assert.That(_supply.State.DailyConsumedKilowattHours, Is.Zero);
        }
        [UnityTearDown] public IEnumerator TearDown() { if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene); }
        private void Close() => _day.Advance((_day.State.ClosingMinute * 60 - _day.State.Clock.SecondsOfDay) / 60.0);

        [Test]
        public void RealKwhSettleExactlyOnceAndClosedProcurementDoesNotCreateObjects()
        {
            _supply.PowerOn(); _supply.Advance(3600);
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1000));
            Close(); DailySummary summary = _costs.Summary;
            Assert.That(summary.ConsumedKilowattHours, Is.EqualTo(2.35).Within(1e-12));
            Assert.That(summary.ElectricityAccruedCents, Is.EqualTo(71)); Assert.That(summary.FixedCostsCents, Is.EqualTo(300));
            Assert.That(summary.NetCents, Is.EqualTo(-300)); Assert.That(summary.ClosingBalanceCents, Is.EqualTo(700));
            Assert.That(summary.Transactions.Count(item => item.CostId == "rent"), Is.EqualTo(1));
            for (int index = 0; index < 5; index++) { _day.RefreshOccupancy(); _day.Advance(1000); Assert.That(_costs.TrySettleClosedDay(), Is.True); }
            _supply.Advance(3600);
            Assert.That(_supply.State.ConsumedKilowattHours, Is.EqualTo(4.7).Within(1e-12));
            Assert.That(_supply.State.DailyConsumedKilowattHours, Is.EqualTo(2.35).Within(1e-12));
            Assert.That(_costs.Summary, Is.SameAs(summary)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(700));
            int objects = Components<Transform>().Length;
            Assert.That(_station.TryPurchase(0, out FoodItem food), Is.False); Assert.That(food, Is.Null);
            Assert.That(_station.TryPurchasePlate(out PlateItem plate), Is.False); Assert.That(plate, Is.Null);
            Assert.That(_station.LastMessage, Does.Contain("Day settled")); Assert.That(Components<Transform>().Length, Is.EqualTo(objects));
            Assert.That(_day.StartNextDay(), Is.True); Assert.That(_day.StartNextDay(), Is.False);
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(700)); Assert.That(_costs.Summary, Is.Null);
            Assert.That(_supply.State.DailyConsumedKilowattHours, Is.Zero);
        }
        [Test]
        public void IndividualSwitchesAndGeneralCutsReduceTheActualElectricityBill()
        {
            _supply.PowerOn(); _supply.Appliances[0].SetOn(false); _supply.Advance(3600);
            Assert.That(_supply.Appliances[0].Meter.DailyConsumedKilowattHours, Is.Zero);
            Assert.That(_supply.State.DailyConsumedKilowattHours, Is.EqualTo(.35).Within(1e-12));
            _supply.PowerOff(); _supply.Advance(3600); _supply.PowerOn();
            _supply.Appliances[2].SetOn(false); _supply.Advance(3600);
            Close(); Assert.That(_costs.Summary.ConsumedKilowattHours, Is.EqualTo(.5).Within(1e-12));
            Assert.That(_costs.Summary.ElectricityAccruedCents, Is.EqualTo(15)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(700));
        }

        [Test]
        public void MultiDayPendingBillSettlesFromInspectorOnceWithoutChangingObjectsDayOrOtherCosts()
        {
            Assert.That(_station.TryPurchase(2, out FoodItem reserve), Is.True);
            reserve.GetComponent<Rigidbody>().isKinematic = true; reserve.transform.position = new Vector3(-3, 2, 2);
            reserve.State.Contaminate(); var state = reserve.State; double age = state.AgeSeconds;
            Assert.That(_station.TryPurchasePlate(out PlateItem plate), Is.True);
            plate.GetComponent<Rigidbody>().isKinematic = true; plate.transform.position = new Vector3(-2, 2, 2);
            Transform[] objects = Components<Transform>();
            _supply.PowerOn(); _supply.Advance(3600); Close();
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(625)); Assert.That(_costs.Summary.ElectricityPaidCents, Is.Zero);
            Assert.That(_day.StartNextDay(), Is.True); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(625));
            Assert.That(_supply.State.PendingKilowattHours, Is.EqualTo(2.35).Within(1e-12));
            _supply.Advance(3600); Close(); DailySummary before = _costs.Summary;
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(325)); Assert.That(_costs.PendingElectricityCents, Is.EqualTo(141));
            Assert.That(Components<ElectricityFeedback>().Single().Text, Does.Contain("€1.41"));
            var dayState = _day.State; int number = dayState.Clock.DayNumber;
            var meters = _supply.Appliances.Select(item => (item.Meter.DailyConsumedKilowattHours, item.Meter.ConsumedKilowattHours)).ToArray();
            _costs.SettleElectricityBill(); // Exactly the Inspector context-menu entry point.
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(184)); Assert.That(_costs.PendingElectricityCents, Is.Zero);
            Assert.That(_service.Ledger.ElectricityBills, Has.Count.EqualTo(1));
            Assert.That(_service.Ledger.ElectricityBills[0].ConsumedKilowattHours, Is.EqualTo(4.7).Within(1e-12));
            Assert.That(_supply.State.DailyConsumedKilowattHours, Is.EqualTo(2.35).Within(1e-12));
            Assert.That(_supply.State.ConsumedKilowattHours, Is.EqualTo(4.7).Within(1e-12));
            Assert.That(_supply.Appliances.Select(item => (item.Meter.DailyConsumedKilowattHours, item.Meter.ConsumedKilowattHours)), Is.EqualTo(meters));
            Assert.That(_costs.Summary.ElectricityPaidCents, Is.EqualTo(141)); Assert.That(_costs.Summary.ElectricityAccruedCents, Is.EqualTo(71));
            Assert.That(_costs.Summary.OperatingNetCents, Is.EqualTo(-300)); Assert.That(_costs.Summary.NetCents, Is.EqualTo(-441));
            Assert.That(_costs.Summary.ClosingBalanceCents, Is.EqualTo(184)); Assert.That(before.ClosingBalanceCents, Is.EqualTo(325));
            Assert.That(Components<OperatingCostsFeedback>().Single().Text, Does.Contain("Electricity bill pending: €0.00"));
            _costs.SettleElectricityBill(); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(184));
            Assert.That(_service.Ledger.ElectricityBills, Has.Count.EqualTo(1));
            Assert.That(_costs.TrySettleClosedDay(), Is.True); Assert.That(_costs.Summary.FixedCostsCents, Is.EqualTo(300));
            Assert.That(_day.State, Is.SameAs(dayState)); Assert.That(_day.State.Clock.DayNumber, Is.EqualTo(number));
            Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Closed));
            Assert.That(Components<Transform>(), Is.EquivalentTo(objects)); Assert.That(reserve.State, Is.SameAs(state));
            Assert.That(state.IsContaminated, Is.True); Assert.That(state.AgeSeconds, Is.EqualTo(age));
            Assert.That(plate.transform.position, Is.EqualTo(new Vector3(-2, 2, 2)));
            _supply.Advance(1800); Assert.That(_costs.PendingElectricityCents, Is.EqualTo(35));
            Assert.That(_costs.TrySettleElectricityBill(out ElectricityBillReceipt second), Is.True);
            Assert.That(second.AmountCents, Is.EqualTo(35)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(149));
            Assert.That(_day.StartNextDay(), Is.True); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(149));
            Assert.That(_supply.State.DailyConsumedKilowattHours, Is.Zero); Assert.That(_supply.State.PendingKilowattHours, Is.Zero);
            Assert.That(_service.Ledger.ElectricityBills, Has.Count.EqualTo(2));
        }

        [Test]
        public void BillPaymentDuringOpenDayAppearsInCashSummaryAndAllowsNegativeBalance()
        {
            _supply.PowerOn(); _supply.Advance(36000);
            Assert.That(_costs.TrySettleElectricityBill(out ElectricityBillReceipt paid), Is.True);
            Assert.That(paid.AmountCents, Is.EqualTo(705)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(295));
            Close(); Assert.That(_costs.Summary.OperatingNetCents, Is.EqualTo(-300));
            Assert.That(_costs.Summary.ElectricityPaidCents, Is.EqualTo(705)); Assert.That(_costs.Summary.ElectricityAccruedCents, Is.EqualTo(705));
            Assert.That(_costs.Summary.NetCents, Is.EqualTo(-1005)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(-5));
            Assert.That(_day.StartNextDay(), Is.True); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(-5));
            Assert.That(_costs.TrySettleElectricityBill(out ElectricityBillReceipt empty), Is.True); Assert.That(empty, Is.Null);
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(-5));
        }
        [UnityTest]
        public IEnumerator TwoPhysicalServiceDaysIncludeClosingSalesAndPreserveLedgerFoodPlateAndSwitches()
        {
            Assert.That(_station.TryPurchase(2, out FoodItem reserve), Is.True);
            reserve.GetComponent<Rigidbody>().isKinematic = true; reserve.transform.position = new Vector3(-3, 2, 2);
            reserve.State.Contaminate(); reserve.State.SetTemperature(80); _food.Advance(10);
            FoodState state = reserve.State; double age = state.AgeSeconds, temperature = state.TemperatureCelsius, deterioration = state.DeteriorationSeconds;
            var reserveId = state.InstanceId; var ledger = _service.Ledger;
            Assert.That(_station.TryPurchasePlate(out PlateItem plate), Is.True);
            plate.GetComponent<Rigidbody>().isKinematic = true; plate.transform.position = new Vector3(-2, 2, 2); var plateId = plate.InstanceId;
            Physics.SyncTransforms();
            _service.Advance(.55); Close();
            Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Closing)); Assert.That(_costs.Summary, Is.Null);
            Assert.That(_costs.TrySettleClosedDay(), Is.False); Assert.That(_day.StartNextDay(), Is.False);
            Assert.That(_service.Queue.TryAdmit(), Is.False);
            _service.Advance(30); Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
            DishItem burger = null; yield return BuildDish(new[] { 0, 1, 0 }, item => burger = item);
            _supply.PowerOn(); _supply.Advance(60); _supply.Appliances[2].SetOn(false); _supply.Advance(60);
            Deliver(burger); var receipt = _service.LastResult;
            Assert.That(receipt.Accepted, Is.True); Assert.That(_costs.Summary, Is.Null);
            _service.Advance(30); DailySummary first = _costs.Summary; yield return null;
            Assert.That(burger == null, Is.True); Assert.That(first.SalesCents, Is.EqualTo(500));
            Assert.That(first.PurchasesCents, Is.EqualTo(225)); Assert.That(first.ElectricityAccruedCents, Is.EqualTo(2));
            Assert.That(first.NetCents, Is.EqualTo(-25)); Assert.That(first.ClosingBalanceCents, Is.EqualTo(975));
            Assert.That(_day.StartNextDay(), Is.True); Assert.That(_day.StartNextDay(), Is.False);
            Assert.That(_service.Ledger, Is.SameAs(ledger)); Assert.That(_service.LastResult, Is.SameAs(receipt));
            Assert.That(_supply.IsOn, Is.True); Assert.That(_supply.Appliances[2].IsOn, Is.False);
            Assert.That(_supply.State.DailyConsumedKilowattHours, Is.Zero);
            Assert.That(_supply.Appliances.All(item => item.Meter.DailyConsumedKilowattHours == 0), Is.True);
            Assert.That(_supply.State.ConsumedKilowattHours, Is.EqualTo(.075).Within(1e-12));
            Assert.That(ledger.BalanceCents, Is.EqualTo(975)); Assert.That(_costs.Summary, Is.Null);
            Assert.That(reserve.State, Is.SameAs(state)); Assert.That(state.InstanceId, Is.EqualTo(reserveId));
            Assert.That(state.AgeSeconds, Is.EqualTo(age)); Assert.That(state.TemperatureCelsius, Is.EqualTo(temperature));
            Assert.That(state.DeteriorationSeconds, Is.EqualTo(deterioration)); Assert.That(state.IsContaminated, Is.True);
            Assert.That(plate.InstanceId, Is.EqualTo(plateId)); Assert.That(plate.transform.position, Is.EqualTo(new Vector3(-2, 2, 2)));
            _service.Advance(.55); Close(); _service.Advance(30);
            DishItem cheese = null; yield return BuildDish(new[] { 0, 1, 2, 0 }, item => cheese = item); Deliver(cheese);
            _supply.Appliances[0].SetOn(false); _supply.Advance(120);
            _service.Advance(30); DailySummary second = _costs.Summary;
            Assert.That(second.DayNumber, Is.EqualTo(2)); Assert.That(second.OpeningBalanceCents, Is.EqualTo(975));
            Assert.That(second.SalesCents, Is.EqualTo(650)); Assert.That(second.PurchasesCents, Is.EqualTo(175));
            Assert.That(second.ElectricityAccruedCents, Is.Zero); Assert.That(second.NetCents, Is.EqualTo(175));
            Assert.That(second.ClosingBalanceCents, Is.EqualTo(1150)); Assert.That(ledger.Summaries, Has.Count.EqualTo(2));
            Assert.That(first.ClosingBalanceCents, Is.EqualTo(975)); Assert.That(_food.Foods, Is.EqualTo(new[] { reserve }));
            Assert.That(OperatingCostsFeedback.SummaryText(second), Does.Contain("Sales: €6.50"));
        }
        [Test]
        public void ConsecutiveEmptyDaysAllowNegativeBalanceAndKeepTheSamePhysicalObjects()
        {
            Assert.That(_station.TryPurchase(0, out FoodItem food), Is.True);
            food.GetComponent<Rigidbody>().isKinematic = true; food.transform.position = new Vector3(-3, 2, 2);
            var state = food.State; var ledger = _service.Ledger; Transform[] objects = Components<Transform>();
            for (int number = 1; number <= 4; number++)
            {
                Close(); Assert.That(_costs.Summary.DayNumber, Is.EqualTo(number));
                Assert.That(_costs.Summary.PurchasesCents, Is.EqualTo(number == 1 ? 35 : 0));
                Assert.That(_costs.Summary.FixedCostsCents, Is.EqualTo(300));
                Assert.That(_day.StartNextDay(), Is.True); Assert.That(ledger.BalanceCents, Is.EqualTo(965 - 300 * number));
                Assert.That(Components<Transform>(), Is.EquivalentTo(objects)); Assert.That(food.State, Is.SameAs(state));
            }
            Assert.That(ledger.BalanceCents, Is.EqualTo(-235)); Assert.That(ledger.Summaries, Has.Count.EqualTo(4));
            Assert.That(IngredientPurchaseStation.FormatCents(ledger.BalanceCents), Is.EqualTo("-€2.35"));
            Assert.That(_station.TryPurchase(1, out _), Is.False);
        }
        [Test]
        public void NextDayCannotBypassDisabledOrMissingRequiredAccounting()
        {
            _costs.enabled = false; Close(); Assert.That(_costs.Summary, Is.Null); Assert.That(_day.StartNextDay(), Is.False);
            _costs.enabled = true; _day.RefreshOccupancy(); Assert.That(_costs.Summary, Is.Not.Null);
            long balance = _service.Ledger.BalanceCents;
            var data = new SerializedObject(_day); data.FindProperty("_operatingCosts").objectReferenceValue = null; data.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(_day.StartNextDay(), Is.False); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(balance));
            data.FindProperty("_operatingCosts").objectReferenceValue = _costs; data.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(_day.StartNextDay(), Is.True); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(balance));
        }
        [UnityTest]
        public IEnumerator AutomaticClosingIncludesTheElectricityStepOfItsFinalFrame()
        {
            _day.State.Advance(28800 - .0001, 0); _supply.PowerOn(); Automatic(_supply, true); Automatic(_day, true);
            yield return null;
            Assert.That(_day.State.Stage, Is.EqualTo(RestaurantDayStage.Closed)); Assert.That(_costs.Summary, Is.Not.Null);
            Assert.That(_costs.Summary.ConsumedKilowattHours, Is.GreaterThan(0));
            Assert.That(_costs.Summary.ConsumedKilowattHours, Is.EqualTo(_supply.State.DailyConsumedKilowattHours));
            Assert.That(_costs.Summary.ElectricityAccruedCents, Is.EqualTo(
                new OperatingCostPolicy(30, new DailyFixedCost[0]).ElectricityCostCents(_supply.State.DailyConsumedKilowattHours)));
        }
        private IEnumerator BuildDish(int[] products, System.Action<DishItem> completed)
        {
            BoxCollider support = Components<BoxCollider>().Single(item => item.name == "PrepSupport3");
            PhysicalDishAssembly physical = Components<PhysicalDishAssembly>().Single(); FoodItem last = null; float top = support.bounds.max.y;
            foreach (int product in products)
            {
                Assert.That(_station.TryPurchase(product, out FoodItem item), Is.True, _station.LastMessage);
                if (product == 1) { item.State.SetTemperature(120); item.State.Advance(45, new ThermalEnvironment(120, allowsCooking: true), 0); }
                float half = item.GetComponent<BoxCollider>().bounds.extents.y;
                item.GetComponent<Rigidbody>().position = new Vector3(support.transform.position.x, top + half + .003f, support.transform.position.z);
                top += 2 * half + .003f; last = item; Physics.SyncTransforms();
            }
            for (int frame = 0; frame < 40; frame++) yield return new WaitForFixedUpdate();
            Assert.That(physical.TryFinalize(last, out DishItem dish), Is.True); completed(dish);
        }
        private void Deliver(DishItem dish)
        {
            BoxCollider pad = Components<BoxCollider>().Single(item => item.name == "DeliveryPad");
            Rigidbody body = dish.GetComponent<Rigidbody>(); body.interpolation = RigidbodyInterpolation.None;
            dish.transform.position = pad.transform.position + Vector3.up; Physics.SyncTransforms();
            dish.transform.position += Vector3.up * (pad.bounds.max.y - dish.GetComponent<BoxCollider>().bounds.min.y);
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; Physics.SyncTransforms();
            Components<DeliveryZone>().Single().Poll(); Assert.That(_service.Visit.Order.IsCompleted, Is.True);
        }
    }
}
#endif
