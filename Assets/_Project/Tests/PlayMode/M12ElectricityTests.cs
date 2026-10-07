#if UNITY_EDITOR
using System;
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
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Player;
using ZeroStarRestaurant.Restaurant;
using ZeroStarRestaurant.Utilities;
using Object = UnityEngine.Object;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M12ElectricityTests
    {
        private Scene _scene;
        private RestaurantElectricity _supply;
        private FoodSimulation _simulation;
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        private static void Set(UnityEngine.Object target, string field, bool value)
        { var data = new SerializedObject(target); data.FindProperty(field).boolValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _scene = EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/PrototypeRestaurant.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            _supply = Components<RestaurantElectricity>().Single(); Set(_supply, "_advanceAutomatically", false);
            _simulation = Components<FoodSimulation>().Single(); _simulation.enabled = false;
            Components<FirstPersonController>().Single().enabled = false;
            Set(Components<CustomerServiceLoop>().Single(), "_advanceAutomatically", false);
            Set(Components<RestaurantDayController>().Single(), "_advanceAutomatically", false);
            Components<DevelopmentIngredientSupply>().Single().EnableForDevelopment();
            Assert.That(_supply.IsOn, Is.False); Assert.That(_supply.State.ConsumedKilowattHours, Is.Zero);
        }
        [UnityTearDown] public IEnumerator TearDown() { if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene); }

        private FoodItem FoodAt(HeatSource source)
        {
            FoodItem food = Components<FoodItem>().Single(item => item.name == "Raw Beef Patty - Fresh fixture");
            string field = source is GrillHeatSource ? "_effectiveZone" : "_interior";
            var zone = (BoxCollider)new SerializedObject(source).FindProperty(field).objectReferenceValue;
            food.GetComponent<Rigidbody>().isKinematic = true;
            food.transform.position = zone.transform.TransformPoint(zone.center); Physics.SyncTransforms(); return food;
        }

        [Test]
        public void GrillCutStopsHeatAndCookingAndRestorationResumesOriginalState()
        {
            GrillHeatSource grill = Components<GrillHeatSource>().Single(); FoodItem food = FoodAt(grill);
            FoodState state = food.State; Guid id = state.InstanceId; state.Contaminate();
            _simulation.Advance(10); Assert.That(state.TemperatureCelsius, Is.EqualTo(21));
            Assert.That(state.Cooking.EquivalentSeconds, Is.Zero);
            _supply.PowerOn(); Assert.That(grill.TryGetEnvironment(food, out _), Is.True);
            _simulation.Advance(30); double hot = state.TemperatureCelsius, dose = state.Cooking.EquivalentSeconds;
            Assert.That(hot, Is.GreaterThan(100)); Assert.That(dose, Is.GreaterThan(0));
            _supply.PowerOff(); Assert.That(grill.TryGetEnvironment(food, out _), Is.False);
            _simulation.Advance(10); Assert.That(state.TemperatureCelsius, Is.LessThan(hot).And.GreaterThan(21));
            Assert.That(state.Cooking.EquivalentSeconds, Is.EqualTo(dose));
            double cooling = state.TemperatureCelsius; _supply.PowerOn(); _simulation.Advance(10);
            Assert.That(state.TemperatureCelsius, Is.GreaterThan(cooling)); Assert.That(state.Cooking.EquivalentSeconds, Is.GreaterThan(dose));
            Assert.That(food.State, Is.SameAs(state)); Assert.That(state.InstanceId, Is.EqualTo(id));
            Assert.That(state.IsContaminated, Is.True); Assert.That(state.AgeSeconds, Is.EqualTo(60));
            Assert.That(_supply.State.ConsumedKilowattHours, Is.Zero, "Food development advances never meter electricity.");
        }

        [TestCase("Fridge", 4)] [TestCase("Freezer", -18)]
        public void ColdStorageCutWarmsGraduallyAndRestorationCoolsWithoutRestoringFreshness(string name, double target)
        {
            ColdStorage storage = Components<ColdStorage>().Single(item => item.name == name); FoodItem food = FoodAt(storage);
            FoodState state = food.State; Guid id = state.InstanceId; state.Contaminate();
            _simulation.Advance(10); Assert.That(state.TemperatureCelsius, Is.EqualTo(21));
            _supply.PowerOn(); _simulation.Advance(240);
            double cold = state.TemperatureCelsius, freshness = state.FreshnessPercent;
            Assert.That(cold, Is.EqualTo(target).Within(.02));
            _supply.PowerOff(); Assert.That(storage.TryGetEnvironment(food, out _), Is.False);
            Assert.That(state.TemperatureCelsius, Is.EqualTo(cold), "Switching power must not teleport temperature.");
            _simulation.Advance(30); Assert.That(state.TemperatureCelsius, Is.GreaterThan(cold).And.LessThan(21));
            double warm = state.TemperatureCelsius; Assert.That(state.FreshnessPercent, Is.LessThan(freshness));
            _supply.PowerOn(); Assert.That(state.TemperatureCelsius, Is.EqualTo(warm)); _simulation.Advance(120);
            Assert.That(state.TemperatureCelsius, Is.LessThan(warm)); Assert.That(state.FreshnessPercent, Is.LessThan(freshness));
            Assert.That(food.State, Is.SameAs(state)); Assert.That(state.InstanceId, Is.EqualTo(id));
            Assert.That(state.IsContaminated, Is.True); Assert.That(state.AgeSeconds, Is.EqualTo(400));
            Assert.That(state.Cooking.EquivalentSeconds, Is.Zero);
        }

        [Test]
        public void GeneralSwitchUsesExistingEInteractionAndCutsAllThreeAppliancesImmediately()
        {
            ElectricitySwitch button = Components<ElectricitySwitch>().Single();
            Transform player = Components<FirstPersonController>().Single().transform;
            player.position = new Vector3(.2f, .05f, 4.4f); Components<Camera>().Single().transform.LookAt(button.transform.position);
            Physics.SyncTransforms(); PlayerInteraction interaction = Components<PlayerInteraction>().Single();
            Assert.That(interaction.TryInteract(), Is.True); Assert.That(_supply.IsOn, Is.True);
            Assert.That(_supply.Appliances.All(item => item.IsOperating), Is.True);
            _supply.Advance(60); double energy = _supply.State.ConsumedKilowattHours;
            Assert.That(interaction.TryInteract(), Is.True); Assert.That(_supply.IsOn, Is.False);
            Assert.That(_supply.Appliances.All(item => !item.IsOperating), Is.True);
            _supply.Advance(3600); Assert.That(_supply.State.ConsumedKilowattHours, Is.EqualTo(energy));
            Assert.That(interaction.TryInteract(), Is.True); _supply.Advance(60);
            Assert.That(_supply.State.ConsumedKilowattHours, Is.EqualTo(energy * 2).Within(1e-12));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        public void AllIndividualCombinationsControlOnlyTheirOwnLoadAndSurviveGeneralCuts(int mask)
        {
            ElectricalAppliance[] appliances = _supply.Appliances.ToArray();
            for (int index = 0; index < appliances.Length; index++)
                Assert.That(appliances[index].SetOn((mask & (1 << index)) != 0), Is.True);
            double expectedWatts = appliances.Where(item => item.IsOn).Sum(item => item.RatedWatts);
            _supply.Advance(3600); Assert.That(_supply.CurrentWatts, Is.Zero);
            _supply.PowerOn(); _supply.Advance(3600);
            Assert.That(_supply.CurrentWatts, Is.EqualTo(expectedWatts));
            Assert.That(_supply.State.ConsumedKilowattHours, Is.EqualTo(expectedWatts / 1000).Within(1e-12));
            for (int cut = 0; cut < 2; cut++)
            {
                _supply.PowerOff(); _supply.Advance(3600);
                Assert.That(_supply.CurrentWatts, Is.Zero);
                Assert.That(appliances.All(item => !item.IsOperating), Is.True);
                _supply.PowerOn();
                for (int index = 0; index < appliances.Length; index++)
                {
                    bool selectedOn = (mask & (1 << index)) != 0;
                    Assert.That(appliances[index].IsOn, Is.EqualTo(selectedOn));
                    Assert.That(appliances[index].IsOperating, Is.EqualTo(selectedOn));
                    Assert.That(appliances[index].ThermalSource.IsOperational, Is.EqualTo(selectedOn));
                }
                _supply.Advance(3600);
            }
            foreach (ElectricalAppliance item in appliances)
                Assert.That(item.Meter.ConsumedKilowattHours, Is.EqualTo(item.IsOn ? 3 * item.RatedWatts / 1000 : 0).Within(1e-12));
            Assert.That(_supply.State.ConsumedKilowattHours, Is.EqualTo(3 * expectedWatts / 1000).Within(1e-12));
        }

        [TestCase("Grill")] [TestCase("Fridge")] [TestCase("Freezer")]
        public void IndividualEInteractionShowsTheNextActionAndWorksDuringACut(string name)
        {
            ApplianceSwitch control = Components<ApplianceSwitch>().Single(item => item.DisplayName == name);
            Transform source = control.Appliance.ThermalSource.transform;
            Transform player = Components<FirstPersonController>().Single().transform;
            player.position = source.position - source.forward * 2; player.position = new Vector3(player.position.x, .05f, player.position.z);
            Vector3 target = source.TransformPoint(name == "Grill" ? Vector3.up * .5f : new Vector3(0, 1.4f, .65f));
            Components<Camera>().Single().transform.LookAt(target); Physics.SyncTransforms();
            Assert.That(Components<InteractionDetector>().Single().Detect(), Is.SameAs(control));
            PlayerInteraction interaction = Components<PlayerInteraction>().Single();
            string binding = Components<InteractionInput>().Single().InteractBinding;
            Assert.That("[" + binding + "] " + control.PromptLabel, Is.EqualTo("[E] Turn " + name + " Off"));
            Assert.That(interaction.TryGrabPhysical(), Is.False, "Mouse hold does not toggle appliance switches.");
            Assert.That(control.Appliance.IsOn, Is.True);
            Assert.That(interaction.TryInteract(), Is.True); Assert.That(control.Appliance.IsOn, Is.False);
            Assert.That(control.ActionLabel, Is.EqualTo("Turn " + name + " On"));
            _supply.PowerOn(); Assert.That(control.Appliance.IsOperating, Is.False);
            Assert.That(interaction.TryInteract(), Is.True); Assert.That(control.Appliance.IsOperating, Is.True);
            _supply.PowerOff(); Assert.That(control.Appliance.IsOn, Is.True); Assert.That(control.Appliance.IsOperating, Is.False);
            Assert.That(control.ActionLabel, Is.EqualTo("Turn " + name + " Off"));
            Assert.That(interaction.TryInteract(), Is.True); Assert.That(control.Appliance.IsOn, Is.False);
            _supply.PowerOn(); Assert.That(control.Appliance.IsOperating, Is.False);
            Assert.That(_supply.Appliances.Where(item => item != control.Appliance).All(item => item.IsOn), Is.True);
        }

        [TestCase("GrillStation")] [TestCase("Fridge")] [TestCase("Freezer")]
        public void IndividualOffRemovesThermalEnvironmentAndConsumptionWithoutReplacingFoodOrMeter(string name)
        {
            HeatSource source = Components<HeatSource>().Single(item => item.name == name);
            FoodItem food = FoodAt(source); FoodState state = food.State; state.Contaminate();
            ElectricalAppliance appliance = source.Electricity; ElectricityMeter meter = appliance.Meter;
            _supply.PowerOn(); _simulation.Advance(30); _supply.Advance(60);
            double temperature = state.TemperatureCelsius, dose = state.Cooking.EquivalentSeconds;
            double energy = meter.ConsumedKilowattHours;
            appliance.SetOn(false); Assert.That(source.TryGetEnvironment(food, out _), Is.False);
            _supply.Advance(3600); _simulation.Advance(10);
            Assert.That(Math.Abs(state.TemperatureCelsius - 21), Is.LessThan(Math.Abs(temperature - 21)));
            Assert.That(state.Cooking.EquivalentSeconds, Is.EqualTo(dose));
            Assert.That(meter.ConsumedKilowattHours, Is.EqualTo(energy));
            Assert.That(_supply.CurrentWatts, Is.EqualTo(2350 - appliance.RatedWatts));
            appliance.SetOn(true); Assert.That(source.TryGetEnvironment(food, out _), Is.True);
            _supply.Advance(60); Assert.That(meter.ConsumedKilowattHours, Is.EqualTo(energy * 2).Within(1e-12));
            Assert.That(appliance.Meter, Is.SameAs(meter)); Assert.That(food.State, Is.SameAs(state));
            Assert.That(state.IsContaminated, Is.True); Assert.That(state.AgeSeconds, Is.EqualTo(40));
        }

        [Test]
        public void DisableAndNextDayPreserveMixedIndividualSettingsAndExistingMeters()
        {
            ElectricalAppliance grill = _supply.Appliances[0]; grill.SetOn(false);
            _supply.PowerOn(); _supply.Advance(3600); ElectricityMeter meter = grill.Meter;
            grill.enabled = false; grill.enabled = true;
            Assert.That(grill.IsOn, Is.False); Assert.That(grill.Meter, Is.SameAs(meter));
            RestaurantDayController day = Components<RestaurantDayController>().Single();
            day.State.Advance(28800, 0); Assert.That(day.StartNextDay(), Is.True);
            Assert.That(grill.IsOn, Is.False); Assert.That(_supply.Appliances.Skip(1).All(item => item.IsOn), Is.True);
            _supply.PowerOff(); _supply.PowerOn(); _supply.Advance(3600);
            Assert.That(_supply.State.ConsumedKilowattHours, Is.EqualTo(.7).Within(1e-12));
            Assert.That(meter.ConsumedKilowattHours, Is.Zero);
        }

        [Test]
        public void MeteringSurvivesPowerCyclesDisableAndNextDayAndOnlySettlementChangesMoney()
        {
            FoodItem food = FoodAt(Components<ColdStorage>().First()); double age = food.State.AgeSeconds;
            var ledger = Components<CustomerServiceLoop>().Single().Ledger; long balance = ledger.BalanceCents;
            _supply.PowerOn(); _supply.Advance(3600);
            Assert.That(_supply.CurrentWatts, Is.EqualTo(2350));
            Assert.That(_supply.State.ConsumedKilowattHours, Is.EqualTo(2.35).Within(1e-12));
            foreach (ElectricalAppliance item in _supply.Appliances)
                Assert.That(item.Meter.ConsumedKilowattHours, Is.EqualTo(item.RatedWatts / 1000).Within(1e-12));
            Assert.That(food.State.AgeSeconds, Is.EqualTo(age)); Assert.That(food.State.TemperatureCelsius, Is.EqualTo(21));
            Assert.That(ledger.BalanceCents, Is.EqualTo(balance), "Consumption alone never charges money.");
            RestaurantDayController day = Components<RestaurantDayController>().Single();
            day.Advance(60); day.PauseClock(); day.Advance(100); day.ResumeClock();
            day.State.Advance(28800, 0); Assert.That(day.StartNextDay(), Is.True);
            Assert.That(_supply.IsOn, Is.True); Assert.That(_supply.State.ConsumedKilowattHours, Is.EqualTo(2.35).Within(1e-12));
            _supply.enabled = false; _supply.Advance(3600); Assert.That(_supply.CurrentWatts, Is.Zero);
            _supply.enabled = true; Assert.That(_supply.IsOn, Is.True);
            _supply.PowerOff(); _supply.Advance(3600); _supply.PowerOn(); _supply.Advance(3600);
            Assert.That(_supply.State.ConsumedKilowattHours, Is.EqualTo(4.7).Within(1e-12));
            Assert.That(ledger.BalanceCents, Is.EqualTo(balance - 371)); Assert.That(food.State.AgeSeconds, Is.EqualTo(age));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void InactiveApplianceSourceZoneOrZeroTransferNeitherHeatsNorConsumesAndCanResume(int mode)
        {
            GrillHeatSource grill = Components<GrillHeatSource>().Single(); FoodItem food = FoodAt(grill); ElectricalAppliance appliance = grill.Electricity;
            var zone = (BoxCollider)new SerializedObject(grill).FindProperty("_effectiveZone").objectReferenceValue;
            _supply.PowerOn();
            if (mode == 0) appliance.enabled = false;
            else if (mode == 1) grill.enabled = false;
            else if (mode == 2) zone.enabled = false;
            else if (mode == 3) zone.gameObject.SetActive(false);
            else if (mode == 4) grill.gameObject.SetActive(false);
            else { var data = new SerializedObject(grill); data.FindProperty("_transferMultiplier").floatValue = 0; data.ApplyModifiedPropertiesWithoutUndo(); }
            _supply.Advance(3600); _simulation.Advance(10);
            Assert.That(grill.TryGetEnvironment(food, out _), Is.False); Assert.That(appliance.Meter.ConsumedKilowattHours, Is.Zero);
            Assert.That(_supply.State.ConsumedKilowattHours, Is.EqualTo(.35).Within(1e-12));
            Assert.That(food.State.TemperatureCelsius, Is.EqualTo(21));
            appliance.enabled = true; grill.enabled = true; zone.enabled = true; zone.gameObject.SetActive(true); grill.gameObject.SetActive(true);
            var restore = new SerializedObject(grill); restore.FindProperty("_transferMultiplier").floatValue = 4; restore.ApplyModifiedPropertiesWithoutUndo();
            _supply.Advance(3600); Assert.That(appliance.Meter.ConsumedKilowattHours, Is.EqualTo(2).Within(1e-12));
            Assert.That(grill.TryGetEnvironment(food, out _), Is.True);
        }

        [Test]
        public void DuplicateApplianceReferencesDoNotDoubleMeterAndDestroyedRequiredDependencyCannotSupplyHeat()
        {
            _supply.PowerOn(); ElectricalAppliance appliance = _supply.Appliances[0];
            var data = new SerializedObject(_supply); var list = data.FindProperty("_appliances"); list.arraySize = 4;
            list.GetArrayElementAtIndex(3).objectReferenceValue = appliance; data.ApplyModifiedPropertiesWithoutUndo();
            _supply.Advance(3600); Assert.That(_supply.CurrentWatts, Is.EqualTo(2350));
            Assert.That(_supply.State.ConsumedKilowattHours, Is.EqualTo(2.35).Within(1e-12));
            HeatSource source = appliance.ThermalSource; Object.DestroyImmediate(appliance);
            Assert.That(source.IsOperational, Is.False);
        }

        [UnityTest]
        public IEnumerator AutomaticDriverMetersOnlyPoweredTime()
        {
            Set(_supply, "_advanceAutomatically", true); _supply.PowerOn();
            yield return new WaitForSeconds(.1f);
            Assert.That(_supply.State.ConsumedKilowattHours, Is.GreaterThan(0));
            Assert.That(_supply.Appliances.All(item => item.Meter.ConsumedKilowattHours > 0), Is.True);
            _supply.PowerOff(); double energy = _supply.State.ConsumedKilowattHours;
            yield return new WaitForSeconds(.1f);
            Assert.That(_supply.State.ConsumedKilowattHours, Is.EqualTo(energy));
            _supply.PowerOn(); yield return new WaitForSeconds(.1f);
            Assert.That(_supply.State.ConsumedKilowattHours, Is.GreaterThan(energy));
        }

        [TestCase("GrillStation")] [TestCase("Fridge")] [TestCase("Freezer")]
        public void FinalizedDishOriginalIngredientStillRespondsToCutsAndRestoration(string name)
        {
            HeatSource source = Components<HeatSource>().Single(item => item.name == name); FoodItem food = FoodAt(source);
            FoodState state = food.State; food.GetComponent<Rigidbody>().isKinematic = false;
            var aggregate = new GameObject("M12 finalized dish"); SceneManager.MoveGameObjectToScene(aggregate, _scene);
            aggregate.transform.position = food.transform.position;
            DishItem dish = aggregate.AddComponent<DishItem>();
            Assert.That(dish.State.TryAdd(state), Is.True);
            Assert.That(dish.FinalizeAssembly(new[] { food }, Array.Empty<DishProfile>()), Is.True);
            Assert.That(food.GetComponent<Collider>().enabled, Is.False);
            _supply.PowerOn(); Assert.That(source.TryGetEnvironment(food, out _), Is.True);
            _simulation.Advance(30); double temperature = state.TemperatureCelsius;
            _supply.PowerOff(); Assert.That(source.TryGetEnvironment(food, out _), Is.False);
            _simulation.Advance(10);
            Assert.That(Math.Abs(state.TemperatureCelsius - 21), Is.LessThan(Math.Abs(temperature - 21)));
            _supply.PowerOn(); Assert.That(source.TryGetEnvironment(food, out _), Is.True);
            Assert.That(dish.State.Components.Single(), Is.SameAs(state)); Assert.That(food.State, Is.SameAs(state));
            Assert.That(state.AgeSeconds, Is.EqualTo(40));
        }
    }
}
#endif
