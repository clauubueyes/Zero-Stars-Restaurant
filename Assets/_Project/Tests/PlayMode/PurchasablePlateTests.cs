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
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Orders;
using ZeroStarRestaurant.Player;

namespace ZeroStarRestaurant.Tests
{
    public sealed class PurchasablePlateTests
    {
        private Scene _scene;
        private IngredientPurchaseStation _station;
        private CustomerServiceLoop _service;
        private FoodSimulation _simulation;
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _scene = EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/PrototypeRestaurant.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            Assert.That(_scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ZeroStarRestaurant.Restaurant.RestaurantDayController>(true)).Single().OpenRestaurant(), Is.True);
            Components<FirstPersonController>().Single().enabled = false;
            _station = Components<IngredientPurchaseStation>().Single(); _service = Components<CustomerServiceLoop>().Single();
            _simulation = Components<FoodSimulation>().Single(); _simulation.enabled = false;
            var serviceData = new SerializedObject(_service); serviceData.FindProperty("_advanceAutomatically").boolValue = false;
            serviceData.ApplyModifiedPropertiesWithoutUndo();
        }
        [UnityTearDown] public IEnumerator TearDown() { if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene); }

        [UnityTest]
        public IEnumerator EachPaidPlateIsUniqueAndRemovedPlatesNeverRespawn()
        {
            Assert.That(Components<PlateItem>(), Is.Empty); Assert.That(Components<DishItem>(), Is.Empty);
            var ids = new System.Collections.Generic.HashSet<Guid>();
            for (int purchase = 0; purchase < 6; purchase++)
            {
                Assert.That(_station.TryPurchasePlate(out PlateItem plate), Is.True, _station.LastMessage);
                Assert.That(ids.Add(plate.InstanceId), Is.True); Assert.That(plate.gameObject.scene, Is.EqualTo(_scene));
                Assert.That(plate.GetComponent<Pickup>().isActiveAndEnabled, Is.True); Assert.That(plate.GetComponent<Rigidbody>().isKinematic, Is.False);
                Guid id = plate.InstanceId; plate.Initialize(); Assert.That(plate.InstanceId, Is.EqualTo(id));
                plate.GetComponent<Rigidbody>().position = new Vector3(1100 + purchase, 2, 1100); Physics.SyncTransforms();
            }
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(700)); Assert.That(_simulation.Foods, Is.Empty);
            foreach (PlateItem plate in Components<PlateItem>()) UnityEngine.Object.Destroy(plate.gameObject);
            for (int frame = 0; frame < 60; frame++) yield return new WaitForFixedUpdate();
            Assert.That(Components<PlateItem>(), Is.Empty); Assert.That(Components<DishItem>(), Is.Empty);
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(700));
        }

        [UnityTest]
        public IEnumerator OccupiedOutputInsufficientFundsAndDisabledStationNeverCreateOrChargePlate()
        {
            Assert.That(_station.TryPurchase(0, out FoodItem food), Is.True);
            Assert.That(_station.TryPurchasePlate(out _), Is.False); Assert.That(_station.LastMessage, Does.Contain("occupied"));
            food.GetComponent<Rigidbody>().position = new Vector3(1100, 2, 1100); Physics.SyncTransforms();
            Assert.That(_station.TryPurchasePlate(out PlateItem plate), Is.True);
            Assert.That(_station.TryPurchasePlate(out _), Is.False); Assert.That(_station.TryPurchase(0, out _), Is.False);
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(915));
            plate.GetComponent<Rigidbody>().position = new Vector3(1102, 2, 1100); Physics.SyncTransforms();
            Assert.That(_service.Ledger.TrySpend(Guid.NewGuid(), Guid.NewGuid(), 915), Is.True);
            Assert.That(_station.TryPurchasePlate(out _), Is.False); Assert.That(_station.LastMessage, Does.Contain("Insufficient"));
            _station.enabled = false; Assert.That(_station.TryPurchasePlate(out _), Is.False);
            Assert.That(Components<PlateItem>(), Has.Length.EqualTo(1)); Assert.That(_simulation.Foods.Count, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator GenericEButtonBuysPlateAndNormalPickupCarriesTheSameUtensil()
        {
            IngredientPurchaseButton button = Components<IngredientPurchaseButton>().Single(item => item.name == "BuyPlate");
            Transform player = Components<FirstPersonController>().Single().transform, view = Components<Camera>().Single().transform;
            PhysicalCarry carry = Components<PhysicalCarry>().Single();
            player.position = button.transform.position + new Vector3(0, -button.transform.position.y + .03f, 1.4f);
            view.LookAt(button.transform.position); Physics.SyncTransforms();
            Assert.That(Components<PlayerInteraction>().Single().TryInteract(), Is.True);
            PlateItem plate = Components<PlateItem>().Single(); Guid id = plate.InstanceId;
            view.LookAt(plate.transform.position); Physics.SyncTransforms();
            Assert.That(Components<PlayerInteraction>().Single().TryGrabPhysical(), Is.True);
            Assert.That(carry.HeldBody, Is.SameAs(plate.GetComponent<Rigidbody>()));
            Assert.That(button.TryInteract(new InteractionContext(player, carry)), Is.False);
            carry.Drop(); yield return new WaitForFixedUpdate(); Assert.That(plate.InstanceId, Is.EqualTo(id));
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(950)); Assert.That(_simulation.Foods, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ConfiguredPlatePriceUsesTheExistingLedgerWithoutMutatingProductAsset()
        {
            PlateProduct original = AssetDatabase.LoadAssetAtPath<PlateProduct>("Assets/_Project/ScriptableObjects/Economy/Plate.asset");
            PlateProduct copy = UnityEngine.Object.Instantiate(original);
            var custom = new GameObject("Custom price procurement"); custom.SetActive(false); SceneManager.MoveGameObjectToScene(custom, _scene);
            try
            {
                var price = new SerializedObject(copy); price.FindProperty("_priceCents").intValue = 75; price.ApplyModifiedPropertiesWithoutUndo();
                IngredientPurchaseStation station = custom.AddComponent<IngredientPurchaseStation>();
                var source = new SerializedObject(_station); var data = new SerializedObject(station);
                foreach (string field in new[] { "_service", "_simulation", "_output", "_outputClearance" })
                    data.FindProperty(field).objectReferenceValue = source.FindProperty(field).objectReferenceValue;
                var products = data.FindProperty("_products"); products.arraySize = 3;
                for (int index = 0; index < 3; index++) products.GetArrayElementAtIndex(index).objectReferenceValue = source.FindProperty("_products").GetArrayElementAtIndex(index).objectReferenceValue;
                data.FindProperty("_plateProduct").objectReferenceValue = copy; data.ApplyModifiedPropertiesWithoutUndo(); custom.SetActive(true);
                Assert.That(station.TryPurchasePlate(out _), Is.True); Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(925));
                Assert.That(original.PriceCents, Is.EqualTo(50)); Assert.That(station.PriceCents(3), Is.EqualTo(75));
            }
            finally { UnityEngine.Object.Destroy(custom); UnityEngine.Object.Destroy(copy); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator EmptyPlateOnDeliveryPadNeverBecomesAnOrder()
        {
            _service.Advance(1); _service.Advance(100); _service.Advance(1);
            Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
            Assert.That(_station.TryPurchasePlate(out PlateItem plate), Is.True);
            BoxCollider pad = Components<BoxCollider>().Single(item => item.name == "DeliveryPad");
            plate.GetComponent<Rigidbody>().position = pad.bounds.center + Vector3.up * (pad.bounds.extents.y + .025f); Physics.SyncTransforms();
            for (int frame = 0; frame < 60; frame++) yield return new WaitForFixedUpdate();
            Assert.That(_service.LastResult, Is.Null); Assert.That(_service.ActiveDishCarrier.Dish, Is.Null);
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(950)); Assert.That(plate.Dish, Is.Null);
        }
    }
}
#endif
