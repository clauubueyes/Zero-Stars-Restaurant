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
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Customers;
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
        public void CurrentPreparationGrillSupportsAndDeliveryHaveAUsableWorkingRoute()
        {
            BoxCollider prep = Components<BoxCollider>().Single(collider => collider.name == "CookingPrepBench");
            Transform grill = Components<GrillHeatSource>().Single().transform;
            Transform[] supports = Components<Transform>().Where(item => item.name.StartsWith("PrepSupport")).OrderBy(item => item.position.x).ToArray();
            Transform delivery = Components<DeliveryZone>().Single().transform;
            Assert.That(supports, Has.Length.EqualTo(3));
            Vector3 start = new Vector3(prep.bounds.center.x, 0, prep.bounds.max.z + .4f);
            Vector3 atGrill = new Vector3(grill.position.x, 0, Mathf.Max(start.z, grill.position.z - 1.5f));
            Vector3 atAssembly = new Vector3(supports[1].position.x, 0, Mathf.Max(start.z, supports[1].position.z - 1.75f));
            Vector3[] stops = { start, atGrill, new Vector3(atAssembly.x, 0, atGrill.z), atAssembly,
                new Vector3(delivery.position.x, 0, atAssembly.z), new Vector3(delivery.position.x, 0, 2.25f) };
            var probe = new GameObject("Working route probe", typeof(CapsuleCollider));
            try
            {
                CapsuleCollider body = probe.GetComponent<CapsuleCollider>(); body.height = 1.8f; body.radius = .35f; body.center = Vector3.up * .92f;
                Collider[] solids = Components<Collider>().Where(item => item.enabled && item.gameObject.activeInHierarchy && !item.isTrigger &&
                    item.GetComponentInParent<FoodItem>() == null && item.GetComponentInParent<CustomerMovement>() == null &&
                    item.GetComponentInParent<CharacterController>() == null).ToArray();
                for (int index = 1; index < stops.Length; index++)
                {
                    int samples = Mathf.CeilToInt(Vector3.Distance(stops[index - 1], stops[index]) / .05f);
                    for (int sample = 0; sample <= samples; sample++) foreach (Collider obstacle in solids)
                        Assert.That(Physics.ComputePenetration(body, Vector3.Lerp(stops[index - 1], stops[index], samples == 0 ? 0 : sample / (float)samples),
                            Quaternion.identity, obstacle, obstacle.transform.position, obstacle.transform.rotation, out _, out float depth) && depth > .005f,
                            Is.False, "Working route clips " + obstacle.name);
                }
            }
            finally { Object.DestroyImmediate(probe); }
            var blockers = Components<BoxCollider>().Where(collider => new[] { "KitchenDivider", "TableVolume", "LowStep", "TallBlocker" }.Contains(collider.name));
            Bounds aisle = new Bounds(new Vector3(-2f, 1f, 2.05f), new Vector3(8f, 1.8f, 0.5f));
            foreach (BoxCollider blocker in blockers) Assert.That(blocker.bounds.Intersects(aisle), Is.False, blocker.name);
        }
    }
}
