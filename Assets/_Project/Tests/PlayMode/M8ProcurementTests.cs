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

namespace ZeroStarRestaurant.Tests
{
    public sealed class M8ProcurementTests
    {
        private Scene _scene;
        private IngredientPurchaseStation _station;
        private CustomerServiceLoop _service;
        private FoodSimulation _simulation;
        private FirstPersonController _player;
        private Transform _view;
        private PhysicalCarry _carry;
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _scene = EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/PrototypeRestaurant.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            _station = Components<IngredientPurchaseStation>().Single(); _service = Components<CustomerServiceLoop>().Single();
            _simulation = Components<FoodSimulation>().Single(); _simulation.enabled = false;
            _player = Components<FirstPersonController>().Single(); _player.enabled = false;
            _view = Components<Camera>().Single().transform; _carry = Components<PhysicalCarry>().Single();
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1000));
            Assert.That(_simulation.Foods, Is.Empty, "Normal gameplay has no free registered fixtures.");
            Assert.That(Components<FoodItem>().All(food => !food.gameObject.activeInHierarchy && food.State == null), Is.True);
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        { if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene); }

        private FoodItem Buy(int index, Vector3 position)
        {
            Assert.That(_station.TryPurchase(index, out FoodItem food), Is.True, _station.LastMessage);
            Assert.That(food.gameObject.scene, Is.EqualTo(_scene));
            food.GetComponent<Rigidbody>().position = position; Physics.SyncTransforms();
            return food;
        }

        [UnityTest]
        public IEnumerator RepeatedPurchasesCreateIndependentPhysicalUnitsAndAdvanceEachOnlyOnce()
        {
            FoodItem first = Buy(0, new Vector3(-5, 1.2f, -3.7f));
            first.State.Contaminate(); _simulation.Advance(10);
            FoodItem second = Buy(0, new Vector3(-3, 1.2f, -3.7f));
            Assert.That(first.Definition, Is.SameAs(second.Definition)); Assert.That(first.State, Is.Not.SameAs(second.State));
            Assert.That(first.State.InstanceId, Is.Not.EqualTo(second.State.InstanceId));
            Assert.That(second.State.IsContaminated, Is.False); Assert.That(second.State.AgeSeconds, Is.Zero);
            Assert.That(second.State.FreshnessPercent, Is.EqualTo(100));
            Assert.That(first.TryInitialize(), Is.True); Assert.That(first.State.AgeSeconds, Is.EqualTo(10));
            Assert.That(_simulation.Register(first), Is.False); Assert.That(_simulation.Register(second), Is.False);
            _simulation.Advance(10);
            Assert.That(first.State.AgeSeconds, Is.EqualTo(20)); Assert.That(second.State.AgeSeconds, Is.EqualTo(10));
            Assert.That(_simulation.Foods.Count, Is.EqualTo(2)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(930));
            first.gameObject.SetActive(false); first.gameObject.SetActive(true);
            Assert.That(first.State.IsContaminated, Is.True); Assert.That(first.State.AgeSeconds, Is.EqualTo(20));
            yield return null;
        }

        [UnityTest]
        public IEnumerator OccupiedOutputAndInsufficientFundsNeverSpawnOrCharge()
        {
            Assert.That(_station.TryPurchase(0, out FoodItem first), Is.True);
            var id = first.State.InstanceId;
            Assert.That(_station.TryPurchase(1, out FoodItem blocked), Is.False); Assert.That(blocked, Is.Null);
            Assert.That(_station.LastMessage, Does.Contain("occupied"));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(965)); Assert.That(_simulation.Foods.Count, Is.EqualTo(1));
            for (int frame = 0; frame < 45; frame++) yield return new WaitForFixedUpdate();
            Assert.That(_station.TryPurchase(2, out _), Is.False, "A settled unit still occupies the output column.");
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(965));
            first.GetComponent<Rigidbody>().position += Vector3.left; Physics.SyncTransforms();
            Assert.That(_service.Ledger.TrySpend(System.Guid.NewGuid(), System.Guid.NewGuid(), 965), Is.True);
            for (int index = 0; index < 3; index++)
            { Assert.That(_station.TryPurchase(index, out FoodItem missing), Is.False); Assert.That(missing, Is.Null); }
            Assert.That(_station.LastMessage, Does.Contain("Insufficient")); Assert.That(_service.Ledger.BalanceCents, Is.Zero);
            Assert.That(_simulation.Foods.Count, Is.EqualTo(1)); Assert.That(first.State.InstanceId, Is.EqualTo(id));
            Assert.That(Components<FoodItem>().Count(food => food.State != null), Is.EqualTo(1));
            Assert.That(_station.TryPurchase(-1, out _), Is.False); Assert.That(_station.TryPurchase(3, out _), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GenericEInteractionSelectsProductBuysAndPicksUpRealOutput()
        {
            IngredientPurchaseButton button = Components<IngredientPurchaseButton>().Single(item => item.name == "BuyRawBeefPatty");
            _player.transform.position = new Vector3(-4.4f, 0.03f, 2.65f);
            _view.LookAt(button.transform.position); Physics.SyncTransforms();
            PlayerInteraction interaction = Components<PlayerInteraction>().Single();
            Assert.That(interaction.TryInteract(), Is.True, "E on the greybox beef product should buy.");
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(920)); FoodItem food = _simulation.Foods.Single();
            Assert.That(food.Definition.Id, Is.EqualTo("food.raw_beef_patty"));
            for (int frame = 0; frame < 45; frame++) yield return new WaitForFixedUpdate();
            _view.LookAt(food.transform.position); Physics.SyncTransforms();
            Assert.That(interaction.TryInteract(), Is.True); Assert.That(_carry.HeldBody, Is.SameAs(food.GetComponent<Rigidbody>()));
            Assert.That(button.CanInteract(new InteractionContext(_player.transform, _carry)), Is.False);
            Assert.That(button.TryInteract(new InteractionContext(_player.transform, _carry)), Is.False);
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(920));
        }

        [UnityTest]
        public IEnumerator PurchasedPattyUsesFridgeFreezerAndGrillWithOriginalIdentityAndState()
        {
            FoodItem food = Buy(1, new Vector3(-4, 1.12f, -3.75f)); FoodState state = food.State;
            var id = state.InstanceId; var cooking = state.Cooking; state.Contaminate();
            double age = 0;
            foreach (string name in new[] { "Fridge", "Freezer" })
            {
                ColdStorage storage = Components<ColdStorage>().Single(item => item.name == name);
                BoxCollider interior = (BoxCollider)new SerializedObject(storage).FindProperty("_interior").objectReferenceValue;
                food.GetComponent<Rigidbody>().position = interior.bounds.center; Physics.SyncTransforms();
                for (int frame = 0; frame < 60; frame++) yield return new WaitForFixedUpdate();
                Assert.That(storage.TryGetEnvironment(food, out _), Is.True);
                _simulation.Advance(240); age += 240;
                Assert.That(state.TemperatureCelsius, name == "Fridge" ? Is.InRange(4, 5) : Is.InRange(-18, -17));
                double before = state.DeteriorationSeconds; _simulation.Advance(60); age += 60;
                Assert.That(state.DeteriorationSeconds - before, Is.EqualTo(name == "Fridge" ? 6 : 0.06).Within(0.001));
            }
            food.transform.position = new Vector3(-3, 1.2f, 0); Physics.SyncTransforms();
            double frozen = state.TemperatureCelsius; _simulation.Advance(20); age += 20;
            Assert.That(state.TemperatureCelsius, Is.GreaterThan(frozen));
            food.transform.position = new Vector3(-2.5f, 1.07f, 3.7f); Physics.SyncTransforms();
            for (int frame = 0; frame < 45; frame++) yield return new WaitForFixedUpdate();
            Assert.That(Components<GrillHeatSource>().Single().TryGetEnvironment(food, out _), Is.True);
            for (int step = 0; step < 50 && cooking.Stage != CookingStage.Cooked; step++)
            { _simulation.Advance(5); age += 5; }
            Assert.That(cooking.Stage, Is.EqualTo(CookingStage.Cooked)); Assert.That(state.AgeSeconds, Is.EqualTo(age));
            Assert.That(food.State, Is.SameAs(state)); Assert.That(state.InstanceId, Is.EqualTo(id));
            Assert.That(state.Cooking, Is.SameAs(cooking)); Assert.That(state.IsContaminated, Is.True);
            Assert.That(_simulation.Foods.Single(), Is.SameAs(food)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(920));
        }

        [UnityTest]
        public IEnumerator PurchasedIngredientsStoreCookAssembleDeliverPayOnceAndFundAnotherPurchase()
        {
            _service.Advance(1); _service.Advance(100); _service.Advance(1);
            AssemblySurface surface = Components<AssemblySurface>().OrderBy(item => item.name).Last();
            FoodItem bottom = Buy(0, new Vector3(-5, 1.12f, -3.75f));
            FoodItem patty = Buy(1, new Vector3(-2.5f, 1.07f, 3.7f));
            FoodItem top = Buy(0, new Vector3(-3, 1.12f, -3.75f));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(850));
            var originalStates = new[] { bottom.State, patty.State, top.State };
            ColdStorage fridge = Components<ColdStorage>().Single(item => item.name == "Fridge");
            BoxCollider interior = (BoxCollider)new SerializedObject(fridge).FindProperty("_interior").objectReferenceValue;
            patty.GetComponent<Rigidbody>().position = interior.bounds.center; Physics.SyncTransforms();
            for (int frame = 0; frame < 60; frame++) yield return new WaitForFixedUpdate();
            Assert.That(fridge.TryGetEnvironment(patty, out _), Is.True);
            _simulation.Advance(120);
            Assert.That(patty.State.TemperatureCelsius, Is.InRange(4, 5));
            Assert.That(patty.State, Is.SameAs(originalStates[1]));
            patty.GetComponent<Rigidbody>().position = Components<GrillHeatSource>().Single().transform.position + Vector3.up * 1.07f;
            Physics.SyncTransforms();
            for (int frame = 0; frame < 45; frame++) yield return new WaitForFixedUpdate();
            for (int step = 0; step < 40 && patty.State.Cooking.Stage != CookingStage.Cooked; step++) _simulation.Advance(5);
            Assert.That(patty.State.Cooking.Stage, Is.EqualTo(CookingStage.Cooked));
            var assembly = Components<DishAssemblyInteraction>().Single();
            foreach (FoodItem food in new[] { bottom, patty, top })
            {
                _player.transform.position = new Vector3(food.transform.position.x, 0.03f, food.transform.position.z - 1.5f);
                _view.LookAt(food.transform.position); Physics.SyncTransforms();
                Assert.That(_carry.TryPickUp(food.GetComponent<Pickup>()), Is.True);
                _player.transform.position = new Vector3(surface.Dish.transform.position.x, 0.03f, 1.95f);
                _view.LookAt(surface.Dish.transform.position); Physics.SyncTransforms();
                Assert.That(assembly.TryPlace(), Is.True);
                for (int frame = 0; frame < 10; frame++) yield return new WaitForFixedUpdate();
            }
            for (int frame = 0; frame < 35; frame++) yield return new WaitForFixedUpdate();
            surface.RefreshComposition(); Assert.That(surface.PreviewDefinition?.DisplayName, Is.EqualTo("Hamburger"));
            Assert.That(surface.TryInteract(new InteractionContext(_player.transform, _carry)), Is.True);
            DishItem dish = surface.Dish; Assert.That(dish.State.Components, Is.EqualTo(originalStates));
            Vector3 pickupSpot = _player.transform.position;
            Assert.That(_carry.TryPickUp(dish.GetComponent<Pickup>()), Is.True);
            Quaternion start = _view.rotation;
            for (int frame = 0; frame < 35; frame++)
            { _view.rotation = Quaternion.Slerp(start, Quaternion.identity, (frame + 1f) / 35f); yield return new WaitForFixedUpdate(); Assert.That(_carry.HasHeldObject, Is.True); }
            for (int frame = 0; frame < 60; frame++)
            { _player.transform.position = new Vector3(Mathf.Lerp(pickupSpot.x, 0, (frame + 1f) / 60f), 0.03f, pickupSpot.z); yield return new WaitForFixedUpdate(); Assert.That(_carry.HasHeldObject, Is.True); }
            for (int frame = 0; frame < 90; frame++)
            { _view.rotation = Quaternion.Euler(-5f, -180f * (frame + 1f) / 90f, 0f); yield return new WaitForFixedUpdate(); Assert.That(_carry.HasHeldObject, Is.True); }
            for (int frame = 0; frame < 60; frame++)
            { _player.transform.position = new Vector3(0, 0.03f, Mathf.Lerp(pickupSpot.z, 2.25f, (frame + 1f) / 60f)); yield return new WaitForFixedUpdate(); Assert.That(_carry.HasHeldObject, Is.True); }
            for (int frame = 0; frame < 35; frame++) yield return new WaitForFixedUpdate();
            Assert.That(_service.LastResult, Is.Null, "A held purchase dish is not sold.");
            _carry.Drop(); for (int frame = 0; frame < 75; frame++) yield return new WaitForFixedUpdate();
            Assert.That(_service.LastResult?.Accepted, Is.True,
                dish == null ? "Dish removed without receipt" : "Unprocessed purchased dish at " + dish.GetComponent<BoxCollider>().bounds);
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1350));
            Assert.That(_service.LastResult.Evaluation.DeliveredDish.Ingredients.Select(item => item.InstanceId),
                Is.EqualTo(originalStates.Select(item => item.InstanceId)));
            Assert.That(_service.ActiveDishCarrier.Dish, Is.SameAs(dish));
            Assert.That(_service.TryDeliver(dish), Is.False); Components<DeliveryZone>().Single().Poll();
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1350));
            _service.Advance(10); _service.Advance(100); yield return null;
            Assert.That(_simulation.Foods, Is.Empty); Assert.That(dish == null, Is.True);
            Buy(2, new Vector3(-4, 1.12f, -3.75f));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1325)); Assert.That(_simulation.Foods.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FailedStatePreparationRollsBackWithoutAChargeOrRegisteredUnit()
        {
            IngredientProduct original = AssetDatabase.LoadAssetAtPath<IngredientProduct>("Assets/_Project/ScriptableObjects/Economy/Bun.asset");
            GameObject prefab = Object.Instantiate(original.Prefab.gameObject);
            IngredientProduct product = Object.Instantiate(original);
            GameObject stationObject = new GameObject("Invalid preparation test station"); stationObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(stationObject, _scene);
            try
            {
                var foodData = new SerializedObject(prefab.GetComponent<FoodItem>());
                foodData.FindProperty("_initialFreshnessPercent").floatValue = 101; foodData.ApplyModifiedPropertiesWithoutUndo();
                var productData = new SerializedObject(product); productData.FindProperty("_prefab").objectReferenceValue = prefab.GetComponent<FoodItem>();
                productData.ApplyModifiedPropertiesWithoutUndo();
                IngredientPurchaseStation station = stationObject.AddComponent<IngredientPurchaseStation>();
                var data = new SerializedObject(station); var originalData = new SerializedObject(_station);
                foreach (string field in new[] { "_service", "_simulation", "_output", "_outputClearance" })
                    data.FindProperty(field).objectReferenceValue = originalData.FindProperty(field).objectReferenceValue;
                data.FindProperty("_products").arraySize = 1;
                data.FindProperty("_products").GetArrayElementAtIndex(0).objectReferenceValue = product;
                data.ApplyModifiedPropertiesWithoutUndo(); stationObject.SetActive(true);
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Invalid food configuration:.*"));
                Assert.That(station.TryPurchase(0, out FoodItem missing), Is.False); Assert.That(missing, Is.Null);
                Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1000)); Assert.That(_simulation.Foods, Is.Empty);
                yield return null;
                Assert.That(Components<FoodItem>().All(food => food.State == null), Is.True);
            }
            finally { Object.DestroyImmediate(stationObject); Object.DestroyImmediate(product); Object.DestroyImmediate(prefab); }
        }

        [UnityTest]
        public IEnumerator DisabledStationOrServiceCannotPurchaseAndReenableDoesNotResetBalance()
        {
            _station.enabled = false; Assert.That(_station.TryPurchase(0, out _), Is.False); Assert.That(_simulation.Foods, Is.Empty);
            _station.enabled = true;
            Buy(0, new Vector3(-5, 1.12f, -3.75f)); var ledger = _service.Ledger;
            _service.enabled = false; Assert.That(_station.TryPurchase(1, out _), Is.False);
            _service.enabled = true; Assert.That(_service.Ledger, Is.SameAs(ledger));
            Assert.That(ledger.BalanceCents, Is.EqualTo(965)); Assert.That(_simulation.Foods.Count, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DevelopmentFixturesRequireExplicitOptInAndNeverDuplicateRegistration()
        {
            DevelopmentIngredientSupply supply = Components<DevelopmentIngredientSupply>().Single();
            GameObject bench = Components<Transform>().Single(item => item.name == "AssemblySupplyBench").gameObject;
            Assert.That(bench.activeSelf, Is.False);
            supply.EnableForDevelopment(); Assert.That(_simulation.Foods.Count, Is.EqualTo(18));
            Assert.That(bench.activeSelf, Is.True);
            Assert.That(_simulation.Foods.All(food => food.State != null && food.gameObject.activeInHierarchy), Is.True);
            var ids = _simulation.Foods.Select(food => food.State.InstanceId).ToArray();
            supply.EnableForDevelopment(); Assert.That(_simulation.Foods.Count, Is.EqualTo(18));
            Assert.That(_simulation.Foods.Select(food => food.State.InstanceId), Is.EqualTo(ids));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1000)); yield return null;
        }

        [UnityTest]
        public IEnumerator SessionRestartRestoresDevelopmentSeedAndCreatesNoPersistentSupplyOrBalance()
        {
            Buy(0, new Vector3(-5, 1.12f, -3.75f)); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(965));
            yield return SceneManager.UnloadSceneAsync(_scene);
            yield return SetUp();
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1000)); Assert.That(_simulation.Foods, Is.Empty);
        }
    }
}
#endif
