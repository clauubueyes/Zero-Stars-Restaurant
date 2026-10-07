using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M5DishSceneTests
    {
        private Scene _scene;
        [SetUp] public void SetUp() => _scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
        [TearDown] public void TearDown() => EditorSceneManager.ClosePreviewScene(_scene);
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>()).ToArray();

        [Test]
        public void ThreePreparationSupportsAreFixedGeometryWithoutFreePlateOrOrderIdentity()
        {
            Assert.That(Components<AssemblySurface>(), Is.Empty); Assert.That(Components<DishItem>(), Is.Empty);
            Transform[] supports = Components<Transform>().Where(item => item.name.StartsWith("PrepSupport")).ToArray();
            Assert.That(supports, Has.Length.EqualTo(3));
            foreach (Transform support in supports)
            {
                Assert.That(support.GetComponent<Rigidbody>().isKinematic, Is.True);
                Assert.That(support.GetComponent<Pickup>(), Is.Null); Assert.That(support.GetComponent<PlateItem>(), Is.Null);
                Assert.That(support.GetComponent<BoxCollider>().isTrigger, Is.False);
            }
            Assert.That(Components<FoodItem>(), Has.Length.EqualTo(18));
            Assert.That(Components<FoodItem>().Count(food => food.Definition.Id == "food.bun"), Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void RecognitionAssetsUseOrderedStableDefinitionIdsAndFeedbackUsesGenericDetector()
        {
            DishProfile hamburger = AssetDatabase.LoadAssetAtPath<DishDefinition>("Assets/_Project/ScriptableObjects/Dishes/Hamburger.asset").CreateProfile();
            DishProfile cheese = AssetDatabase.LoadAssetAtPath<DishDefinition>("Assets/_Project/ScriptableObjects/Dishes/Cheeseburger.asset").CreateProfile();
            Assert.That(hamburger.IngredientDefinitionIds, Is.EqualTo(new[] { "food.bun", "food.raw_beef_patty", "food.bun" }));
            Assert.That(cheese.IngredientDefinitionIds, Is.EqualTo(new[] { "food.bun", "food.raw_beef_patty", "food.cheese", "food.bun" }));
            Assert.That(hamburger.RequiresStack && cheese.RequiresStack, Is.True);
            var feedback = new SerializedObject(Components<DishInspectionFeedback>().Single());
            Assert.That(feedback.FindProperty("_detector").objectReferenceValue, Is.EqualTo(Components<InteractionDetector>().Single()));
            Assert.That(feedback.FindProperty("_carry").objectReferenceValue, Is.EqualTo(Components<PhysicalCarry>().Single()));
            var assembly = Components<DishAssemblyInteraction>().Single();
            Assert.That(feedback.FindProperty("_assembly").objectReferenceValue, Is.EqualTo(assembly));
            var assemblyData = new SerializedObject(assembly);
            Assert.That(assemblyData.FindProperty("_surfaces").arraySize, Is.Zero);
            var actions = (UnityEngine.InputSystem.InputActionAsset)assemblyData.FindProperty("_inputActions").objectReferenceValue;
            Assert.That(actions.FindAction("Player/FinalizeDish").bindings.Single().path, Is.EqualTo("<Keyboard>/f"));
        }
    }
}
