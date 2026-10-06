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
        public void ThreeAssemblyStationsHaveLocalTraySensorDefinitionsAndTheExistingSingleClock()
        {
            AssemblySurface[] surfaces = Components<AssemblySurface>();
            Assert.That(surfaces, Has.Length.EqualTo(3));
            Assert.That(Components<DishItem>(), Has.Length.EqualTo(3));
            foreach (AssemblySurface surface in surfaces)
            {
                var data = new SerializedObject(surface);
                var dish = (DishItem)data.FindProperty("_dish").objectReferenceValue;
                Assert.That(dish.GetComponent<Rigidbody>().isKinematic, Is.True);
                Assert.That(dish.GetComponent<Pickup>(), Is.Null);
                var zone = (BoxCollider)data.FindProperty("_assemblyZone").objectReferenceValue;
                Assert.That(zone.isTrigger, Is.True);
                Assert.That(zone.size.y, Is.GreaterThan(0.7f));
                Assert.That(data.FindProperty("_simulation").objectReferenceValue, Is.EqualTo(Components<FoodSimulation>().Single()));
                Assert.That(data.FindProperty("_definitions").arraySize, Is.EqualTo(2));
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
        }
    }
}
