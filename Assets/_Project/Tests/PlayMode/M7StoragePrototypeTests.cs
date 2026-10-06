#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Player;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M7StoragePrototypeTests
    {
        private Scene _scene;
        private PhysicalCarry _carry;
        private Transform _player, _view;
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _scene = EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/PrototypeRestaurant.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            Components<DevelopmentIngredientSupply>().Single().EnableForDevelopment();
            FirstPersonController controller = Components<FirstPersonController>().Single(); controller.enabled = false;
            _player = controller.transform; _view = Components<Camera>().Single().transform;
            _carry = Components<PhysicalCarry>().Single(); Components<FoodSimulation>().Single().enabled = false;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        { if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene); }

        [UnityTest]
        public IEnumerator ExistingInteractionPhysicallyStoresAndRetrievesTheSameUnitFromFridgeAndFreezer()
        {
            FoodSimulation simulation = Components<FoodSimulation>().Single();
            FoodItem food = Components<FoodItem>().Single(item => item.name == "Raw Beef Patty - Fresh fixture");
            FoodState state = food.State; var id = state.InstanceId; var cooking = state.Cooking;
            state.Contaminate(); double age = state.AgeSeconds;
            PlayerInteraction interaction = Components<PlayerInteraction>().Single();
            _player.position = new Vector3(-4.4f, 0.03f, 2.65f);
            _view.LookAt(food.transform.position); Physics.SyncTransforms();
            Assert.That(interaction.TryInteract(), Is.True);
            yield return Move(new Vector3(-3.6f, 0.03f, 2.65f));
            foreach (string name in new[] { "Fridge", "Freezer" })
            {
                ColdStorage storage = Components<ColdStorage>().Single(item => item.name == name);
                yield return Look(Quaternion.Euler(0, 90, 0));
                yield return Move(new Vector3(-3.6f, 0.03f, storage.transform.position.z));
                yield return Look(Quaternion.Euler(0, -90, 0));
                yield return Move(new Vector3(-4.4f, 0.03f, storage.transform.position.z));
                for (int frame = 0; frame < 30; frame++) yield return new WaitForFixedUpdate();
                Assert.That(storage.TryGetEnvironment(food, out _), Is.True, "Food must enter the open cabinet before dropping.");
                interaction.Drop(); for (int frame = 0; frame < 60; frame++) yield return new WaitForFixedUpdate();
                Assert.That(_carry.HasHeldObject, Is.False); Assert.That(food.GetComponent<Rigidbody>().isKinematic, Is.False);
                Assert.That(storage.TryGetEnvironment(food, out _), Is.True, "Released food rests physically inside " + name + " at " + food.transform.position);
                Collider shelf = storage.GetComponentsInChildren<Collider>().Single(collider => collider.name == "Shelf");
                Assert.That(food.GetComponent<Collider>().bounds.min.y, Is.EqualTo(shelf.bounds.max.y).Within(0.015));
                simulation.Advance(120); age += 120;
                Assert.That(state.TemperatureCelsius, name == "Fridge" ? Is.InRange(4.0, 5.0) : Is.InRange(-18.0, -17.0));
                _view.LookAt(food.transform.position); Physics.SyncTransforms();
                Assert.That(interaction.TryInteract(), Is.True, "The ordinary E interaction retrieves food from " + name);
                Assert.That(_carry.HeldBody, Is.SameAs(food.GetComponent<Rigidbody>()));
                yield return Look(Quaternion.Euler(0, -90, 0));
                yield return Move(new Vector3(-3.6f, 0.03f, storage.transform.position.z));
                Assert.That(storage.TryGetEnvironment(food, out _), Is.False);
                Assert.That(food.State, Is.SameAs(state)); Assert.That(state.InstanceId, Is.EqualTo(id));
                Assert.That(state.Cooking, Is.SameAs(cooking)); Assert.That(cooking.EquivalentSeconds, Is.Zero);
                Assert.That(state.IsContaminated, Is.True); Assert.That(state.AgeSeconds, Is.EqualTo(age));
            }
            double frozen = state.TemperatureCelsius; simulation.Advance(10);
            Assert.That(state.TemperatureCelsius, Is.GreaterThan(frozen).And.LessThan(0));
            Assert.That(state.AgeSeconds, Is.EqualTo(age + 10)); Assert.That(simulation.Foods.Count, Is.EqualTo(18));
            interaction.Drop(); Assert.That(food.State, Is.SameAs(state));
        }

        private IEnumerator Move(Vector3 destination)
        {
            Vector3 start = _player.position;
            for (int frame = 0; frame < 55; frame++)
            { _player.position = Vector3.Lerp(start, destination, (frame + 1f) / 55f); yield return new WaitForFixedUpdate(); Assert.That(_carry.HasHeldObject, Is.True); }
        }
        private IEnumerator Look(Quaternion destination)
        {
            Quaternion start = _view.rotation;
            for (int frame = 0; frame < 40; frame++)
            { _view.rotation = Quaternion.Slerp(start, destination, (frame + 1f) / 40f); yield return new WaitForFixedUpdate(); Assert.That(_carry.HasHeldObject, Is.True); }
        }
    }
}
#endif
