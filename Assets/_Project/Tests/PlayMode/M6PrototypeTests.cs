#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Orders;
using ZeroStarRestaurant.Player;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M6PrototypeTests
    {
        private Scene _scene;
        private CustomerServiceLoop _service;
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _scene = EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/PrototypeRestaurant.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            Components<DevelopmentIngredientSupply>().Single().EnableForDevelopment();
            Components<FirstPersonController>().Single().enabled = false;
            Components<FoodSimulation>().Single().enabled = false;
            _service = Components<CustomerServiceLoop>().Single();
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1000));
            _service.Advance(1); _service.Advance(100); _service.Advance(1);
            Assert.That(_service.Visit.Stage, Is.EqualTo(CustomerStage.Wait));
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        { if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene); }

        [UnityTest]
        public IEnumerator RealPrototypeCarriesItsFinalDishToCounterAndPaysOnlyAfterPhysicalDrop() => DeliverThroughNativePhysics(0f);

        [UnityTest]
        public IEnumerator RealPrototypeAcceptsAPartiallyPlacedDishWhoseCenterIsOutsideThePad() => DeliverThroughNativePhysics(1.04f);

        private IEnumerator DeliverThroughNativePhysics(float deliveryX)
        {
            AssemblySurface surface = Components<AssemblySurface>().OrderBy(item => item.name).Last();
            FoodItem[] buns = Components<FoodItem>().Where(food => food.Definition.Id == "food.bun").Take(2).ToArray();
            FoodItem patty = Components<FoodItem>().First(food => food.Definition.Id == "food.raw_beef_patty");
            patty.State.SetTemperature(120); patty.State.Advance(45, new ThermalEnvironment(120, allowsCooking: true), 0);
            var player = Components<FirstPersonController>().Single();
            Transform view = Components<Camera>().Single().transform;
            var carry = Components<PhysicalCarry>().Single();
            var assembly = Components<DishAssemblyInteraction>().Single();
            foreach (FoodItem food in new[] { buns[0], patty, buns[1] })
            {
                player.transform.position = food.transform.position.z < 2.5f
                    ? new Vector3(-4.4f, 0.03f, 2.65f)
                    : new Vector3(food.transform.position.x, 0.03f, food.transform.position.z - 1.5f);
                view.LookAt(food.transform.position); Physics.SyncTransforms();
                Assert.That(carry.TryPickUp(food.GetComponent<Pickup>()), Is.True);
                player.transform.position = new Vector3(surface.Dish.transform.position.x, 0.03f, 1.95f);
                view.LookAt(surface.Dish.transform.position); Physics.SyncTransforms();
                Assert.That(assembly.TryPlace(), Is.True);
                for (int frame = 0; frame < 10; frame++) yield return new WaitForFixedUpdate();
            }
            Physics.SyncTransforms();
            for (int frame = 0; frame < 35; frame++) yield return new WaitForFixedUpdate();
            surface.RefreshComposition(); Assert.That(surface.PreviewDefinition?.DisplayName, Is.EqualTo("Hamburger"));
            Vector3 pickupSpot = new Vector3(surface.Dish.transform.position.x, 0.03f, 1.95f);
            player.transform.position = pickupSpot; view.LookAt(buns[1].transform.position);
            Assert.That(surface.TryInteract(new InteractionContext(player.transform, carry)), Is.True);
            DishItem dish = surface.Dish; var id = dish.State.InstanceId; var pattyId = patty.State.InstanceId;
            Assert.That(carry.TryPickUp(dish.GetComponent<Pickup>()), Is.True);
            Quaternion start = view.rotation;
            for (int frame = 0; frame < 35; frame++)
            { view.rotation = Quaternion.Slerp(start, Quaternion.identity, (frame + 1f) / 35f); yield return new WaitForFixedUpdate(); Assert.That(carry.HasHeldObject, Is.True); }
            for (int frame = 0; frame < 60; frame++)
            { player.transform.position = new Vector3(Mathf.Lerp(pickupSpot.x, deliveryX, (frame + 1f) / 60f), 0.03f, pickupSpot.z); yield return new WaitForFixedUpdate(); Assert.That(carry.HasHeldObject, Is.True); }
            // Turn through the open west side, then approach the unchanged 1.1 m counter.
            for (int frame = 0; frame < 90; frame++)
            { view.rotation = Quaternion.Euler(-5f, -180f * (frame + 1f) / 90f, 0f); yield return new WaitForFixedUpdate(); Assert.That(carry.HasHeldObject, Is.True); }
            for (int frame = 0; frame < 60; frame++)
            { player.transform.position = new Vector3(deliveryX, 0.03f, Mathf.Lerp(pickupSpot.z, 2.25f, (frame + 1f) / 60f)); yield return new WaitForFixedUpdate(); Assert.That(carry.HasHeldObject, Is.True); }
            for (int frame = 0; frame < 35; frame++) yield return new WaitForFixedUpdate();
            Assert.That(_service.LastResult, Is.Null, "A held dish is never sold.");
            if (deliveryX > 0f)
                Assert.That(dish.GetComponent<BoxCollider>().bounds.center.x,
                    Is.GreaterThan(Components<BoxCollider>().Single(collider => collider.name == "DeliveryPad").bounds.max.x),
                    "Regression: the held aggregate's center is outside the green pad before this physical drop.");
            carry.Drop(); for (int frame = 0; frame < 75; frame++) yield return new WaitForFixedUpdate();
            Assert.That(_service.LastResult?.Accepted, Is.True,
                dish == null ? "Dish removed without result" : "Unprocessed dish at " + dish.GetComponent<BoxCollider>().bounds);
            Assert.That(_service.Ledger.BalanceCents, Is.EqualTo(1500));
            Assert.That(_service.LastResult.Evaluation.DeliveredDish.InstanceId, Is.EqualTo(id));
            Assert.That(_service.LastResult.Evaluation.DeliveredDish.Ingredients.Any(food => food.InstanceId == pattyId), Is.True);
            Assert.That(Components<FoodSimulation>().Single().Foods.Count, Is.EqualTo(18));
            Assert.That(_service.ActiveDishCarrier.Dish, Is.SameAs(dish));
            Assert.That(dish.gameObject.activeInHierarchy, Is.True);
            var firstVisit = _service.Visit;
            Assert.That(_service.ForceNextOrder(1), Is.True);
            _service.Advance(10); _service.Advance(100);
            yield return null;
            Assert.That(dish == null, Is.True);
            Assert.That(Components<FoodSimulation>().Single().Foods.Count, Is.EqualTo(15));
            Assert.That(firstVisit.Stage, Is.EqualTo(CustomerStage.Finished));
            Assert.That(_service.Queue.Customers.Count, Is.EqualTo(4));
            _service.Advance(3); _service.Advance(100); _service.Advance(1);
            Assert.That(_service.Visit.Order.InstanceId, Is.Not.EqualTo(firstVisit.Order.InstanceId));
            Assert.That(_service.Visit.Order.Offer.Dish.DisplayName, Is.EqualTo("Cheeseburger"));
            Assert.That(Components<CustomerMovement>().Count(customer => customer.gameObject.activeSelf), Is.EqualTo(4));
        }
    }
}
#endif
