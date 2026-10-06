using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Economy;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M10QueueSceneTests
    {
        private Scene _scene;
        [SetUp] public void SetUp() => _scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
        [TearDown] public void TearDown() => EditorSceneManager.ClosePreviewScene(_scene);
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        [Test]
        public void QueueUsesFourOriginalMarkersAnInactiveLocalTemplateAndTheExistingServiceLedger()
        {
            CustomerQueueController queue = Components<CustomerQueueController>().Single();
            Assert.DoesNotThrow(queue.Validate); Assert.That(queue.Capacity, Is.EqualTo(4));
            CustomerServiceLoop service = Components<CustomerServiceLoop>().Single();
            Assert.That(service.Queue, Is.SameAs(queue));
            Assert.That(Components<QueuedCustomer>(), Has.Length.EqualTo(1));
            Assert.That(queue.Template.gameObject.activeSelf, Is.False); Assert.That(queue.Template.State, Is.Null);
            Assert.That(queue.Template.Movement, Is.SameAs(Components<CustomerMovement>().Single()));
            Assert.That(queue.Template.DishCarrier, Is.SameAs(Components<CustomerDishCarrier>().Single()));
            Transform points = Components<Transform>().Single(item => item.name == "QueuePoints");
            var serialized = new SerializedObject(queue); SerializedProperty slots = serialized.FindProperty("_queuePoints");
            Assert.That(slots.arraySize, Is.EqualTo(4));
            for (int index = 0; index < 4; index++)
                Assert.That(slots.GetArrayElementAtIndex(index).objectReferenceValue, Is.SameAs(points.GetChild(index)));
            Assert.That(queue.ServicePosition, Is.SameAs(points.GetChild(0)));
            Assert.That(new SerializedObject(Components<IngredientPurchaseStation>().Single()).FindProperty("_service").objectReferenceValue, Is.SameAs(service));
            Assert.That(serialized.FindProperty("_customersRoot").objectReferenceValue, Is.Not.Null);
            Assert.That(points.GetComponentsInChildren<MonoBehaviour>(), Is.Empty);
        }

        [Test]
        public void InstallingQueueAgainKeepsOriginalTemplateGeometryAndComponentReferences()
        {
            CustomerQueueController queue = Components<CustomerQueueController>().Single();
            Component[] original = Components<Component>();
            M10CustomerQueueBuilder.ConfigureScene(_scene); M10CustomerQueueBuilder.ConfigureScene(_scene);
            Assert.That(Components<Component>(), Is.EquivalentTo(original));
            Assert.That(Components<CustomerQueueController>().Single(), Is.SameAs(queue));
        }

        [TestCase(0)] [TestCase(5)]
        public void InvalidCapacityIsRejectedBeforeStartingService(int capacity)
        {
            CustomerQueueController queue = Components<CustomerQueueController>().Single();
            var data = new SerializedObject(queue); data.FindProperty("_capacity").intValue = capacity; data.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<System.ArgumentException>(queue.Validate);
        }
    }
}
