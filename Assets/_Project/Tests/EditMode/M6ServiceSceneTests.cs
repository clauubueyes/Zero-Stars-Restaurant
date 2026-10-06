using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Orders;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M6ServiceSceneTests
    {
        private Scene _scene;
        [SetUp] public void SetUp() => _scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
        [TearDown] public void TearDown() => EditorSceneManager.ClosePreviewScene(_scene);
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        [Test]
        public void ServiceUsesOneInactivePrimitiveCustomerExplicitRouteAndTheExistingFoodClock()
        {
            var service = new SerializedObject(Components<CustomerServiceLoop>().Single());
            CustomerMovement customer = Components<CustomerMovement>().Single(); Assert.That(customer.gameObject.activeSelf, Is.False);
            Assert.That(customer.HasValidRoute, Is.True); Assert.That(customer.GetComponent<Rigidbody>().isKinematic, Is.True);
            Assert.That(service.FindProperty("_customer").objectReferenceValue, Is.SameAs(customer));
            CustomerDishCarrier carrier = Components<CustomerDishCarrier>().Single();
            Assert.That(service.FindProperty("_dishCarrier").objectReferenceValue, Is.SameAs(carrier));
            Assert.That(carrier.HasValidAnchor, Is.True);
            Assert.That(service.FindProperty("_foodSimulation").objectReferenceValue, Is.SameAs(Components<FoodSimulation>().Single()));
            var config = (CustomerServiceConfiguration)service.FindProperty("_configuration").objectReferenceValue;
            var offers = config.CreateOffers(); Assert.That(offers.Select(offer => offer.Dish.Id), Is.EqualTo(new[] { "dish.hamburger", "dish.cheeseburger" }));
            Assert.That(offers.Select(offer => offer.SalePriceCents), Is.EqualTo(new[] { 500, 650 }));
            Assert.That(config.NextCustomerDelaySeconds, Is.GreaterThan(0));
            Assert.That(Components<FoodItem>(), Has.Length.EqualTo(18));
        }
        [Test]
        public void DeliveryAndAlwaysVisibleFeedbackReferenceTheSameSessionAndARealCounterSurface()
        {
            CustomerServiceLoop service = Components<CustomerServiceLoop>().Single();
            var delivery = new SerializedObject(Components<DeliveryZone>().Single());
            Assert.That(delivery.FindProperty("_service").objectReferenceValue, Is.SameAs(service));
            var sensor = (BoxCollider)delivery.FindProperty("_zone").objectReferenceValue;
            var support = (BoxCollider)delivery.FindProperty("_support").objectReferenceValue;
            Assert.That(sensor.isTrigger, Is.True); Assert.That(support.isTrigger, Is.False);
            Assert.That(support.bounds.size.x, Is.EqualTo(1.8f).Within(0.001));
            Assert.That(support.bounds.size.z, Is.EqualTo(1f).Within(0.001));
            Assert.That(sensor.bounds.min.x, Is.EqualTo(support.bounds.min.x).Within(0.001));
            Assert.That(sensor.bounds.max.x, Is.EqualTo(support.bounds.max.x).Within(0.001));
            Assert.That(sensor.bounds.min.z, Is.EqualTo(support.bounds.min.z).Within(0.001));
            Assert.That(sensor.bounds.max.z, Is.EqualTo(support.bounds.max.z).Within(0.001));
            Bounds visual = support.GetComponent<Renderer>().bounds;
            Assert.That(visual.size.x, Is.EqualTo(support.bounds.size.x).Within(0.001));
            Assert.That(visual.size.y, Is.EqualTo(support.bounds.size.y).Within(0.001));
            Assert.That(visual.size.z, Is.EqualTo(support.bounds.size.z).Within(0.001));
            Assert.That(delivery.FindProperty("_minimumFootprintOverlap").floatValue, Is.EqualTo(0.2f));
            BoxCollider counter = Components<BoxCollider>().Single(collider => collider.name == "ServiceCounterVolume");
            Assert.That(support.bounds.min.y, Is.EqualTo(counter.bounds.max.y).Within(0.001));
            Assert.That(new SerializedObject(Components<OrderFeedback>().Single()).FindProperty("_service").objectReferenceValue, Is.SameAs(service));
        }
    }
}
