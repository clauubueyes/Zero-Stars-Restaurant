using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Orders;

namespace ZeroStarRestaurant.Tests
{
    public sealed class VerticalSlicePolishSceneTests
    {
        private Scene _scene;
        [SetUp] public void SetUp() => _scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
        [TearDown] public void TearDown() => EditorSceneManager.ClosePreviewScene(_scene);
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        [Test]
        public void ContextualPlacementUsesLeftClickWithoutConflictingWithThrowOrFinalize()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            InputAction place = actions.FindAction("Player/PlaceIngredient", true);
            Assert.That(place.type, Is.EqualTo(InputActionType.Button));
            Assert.That(place.bindings.Single().path, Is.EqualTo("<Mouse>/leftButton"));
            Assert.That(actions.FindAction("Player/Throw", true).bindings.Single().path, Is.EqualTo("<Mouse>/rightButton"));
            Assert.That(actions.FindAction("Player/FinalizeDish", true).bindings.Single().path, Is.EqualTo("<Keyboard>/f"));
        }

        [Test]
        public void PreparationGrillAssemblyAndDeliveryAreAdjacentAndTheWorkingAisleHasNoBlockers()
        {
            BoxCollider prep = Components<BoxCollider>().Single(collider => collider.name == "CookingPrepBench");
            Transform grill = Components<GrillHeatSource>().Single().transform;
            AssemblySurface[] surfaces = Components<AssemblySurface>().OrderBy(surface => surface.transform.position.x).ToArray();
            Transform delivery = Components<DeliveryZone>().Single().transform;
            Assert.That(Vector3.Distance(prep.transform.position, grill.position), Is.LessThan(3.5f));
            Assert.That(Vector3.Distance(grill.position, surfaces[0].transform.position), Is.LessThan(2.5f));
            Assert.That(surfaces.All(surface => new Vector2(surface.transform.position.x - delivery.position.x,
                surface.transform.position.z - delivery.position.z).magnitude < 4f), Is.True);
            var blockers = Components<BoxCollider>().Where(collider => new[] { "KitchenDivider", "TableVolume", "LowStep", "TallBlocker" }.Contains(collider.name));
            Bounds aisle = new Bounds(new Vector3(-2f, 1f, 2.05f), new Vector3(8f, 1.8f, 0.5f));
            foreach (BoxCollider blocker in blockers) Assert.That(blocker.bounds.Intersects(aisle), Is.False, blocker.name);
        }
    }
}
