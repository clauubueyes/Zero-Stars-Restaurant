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
    public sealed class PhysicalInteractionPolishSceneTests
    {
        private Scene _scene;
        [SetUp] public void SetUp() => _scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
        [TearDown] public void TearDown() => EditorSceneManager.ClosePreviewScene(_scene);
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        [Test]
        public void NoFreeTrayOrStationClaimsAndFreeAssemblyUsesTheSameSimulationAndRecipes()
        {
            Assert.That(Components<AssemblySurface>(), Is.Empty); Assert.That(Components<DishTraySupply>(), Is.Empty);
            Assert.That(Components<PlateItem>(), Is.Empty); Assert.That(Components<DishItem>(), Is.Empty);
            PhysicalDishAssembly physical = Components<PhysicalDishAssembly>().Single();
            var assembly = new SerializedObject(physical);
            Assert.That(assembly.FindProperty("_simulation").objectReferenceValue, Is.SameAs(Components<FoodSimulation>().Single()));
            var definitions = assembly.FindProperty("_definitions"); Assert.That(definitions.arraySize, Is.EqualTo(2));
            Assert.That(new SerializedObject(Components<DishAssemblyInteraction>().Single()).FindProperty("_physicalAssembly").objectReferenceValue, Is.SameAs(physical));
            Assert.That(new SerializedObject(Components<InteractionInput>().Single()).FindProperty("_assembly").objectReferenceValue,
                Is.SameAs(Components<DishAssemblyInteraction>().Single()));
        }
        [Test]
        public void InstallingPolishAgainPreservesSceneObjectsAndGuids()
        {
            Component[] before = Components<Component>();
            var poses = Components<Transform>().ToDictionary(item => item, item => (item.position, item.rotation, item.localScale));
            PhysicalInteractionPolishBuilder.ConfigureScene(_scene); PhysicalInteractionPolishBuilder.ConfigureScene(_scene);
            Assert.That(Components<Component>(), Is.EquivalentTo(before));
            foreach (var pair in poses) Assert.That((pair.Key.position, pair.Key.rotation, pair.Key.localScale), Is.EqualTo(pair.Value), pair.Key.name);
        }
    }
}
