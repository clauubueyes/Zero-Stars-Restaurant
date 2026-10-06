using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Food;

namespace ZeroStarRestaurant.Tests
{
    public sealed class PhysicalInteractionPolishSceneTests
    {
        private Scene _scene;
        [SetUp] public void SetUp() => _scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
        [TearDown] public void TearDown() => EditorSceneManager.ClosePreviewScene(_scene);
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        [Test]
        public void EveryStationHasACleanSupplyAndFreeAssemblyUsesTheSameSimulationAndRecipes()
        {
            AssemblySurface[] surfaces = Components<AssemblySurface>(); Assert.That(surfaces, Has.Length.EqualTo(3));
            foreach (AssemblySurface surface in surfaces)
            {
                DishTraySupply supply = surface.GetComponent<DishTraySupply>(); Assert.That(supply, Is.Not.Null);
                var data = new SerializedObject(supply);
                Assert.That(data.FindProperty("_surface").objectReferenceValue, Is.SameAs(surface));
                var prefab = (DishItem)data.FindProperty("_emptyTrayPrefab").objectReferenceValue;
                Assert.That(AssetDatabase.GetAssetPath(prefab), Is.EqualTo(PhysicalInteractionPolishBuilder.TrayPrefabPath));
                Assert.That(prefab.gameObject.activeSelf, Is.False); Assert.That(prefab.GetComponentsInChildren<FoodItem>(true), Is.Empty);
            }
            PhysicalDishAssembly physical = Components<PhysicalDishAssembly>().Single();
            var assembly = new SerializedObject(physical);
            Assert.That(assembly.FindProperty("_simulation").objectReferenceValue, Is.SameAs(Components<FoodSimulation>().Single()));
            var definitions = assembly.FindProperty("_definitions"); Assert.That(definitions.arraySize, Is.EqualTo(2));
            Assert.That(new SerializedObject(Components<DishAssemblyInteraction>().Single()).FindProperty("_physicalAssembly").objectReferenceValue, Is.SameAs(physical));
        }
        [Test]
        public void InstallingPolishAgainPreservesSceneObjectsAndGuids()
        {
            Component[] before = Components<Component>();
            PhysicalInteractionPolishBuilder.ConfigureScene(_scene); PhysicalInteractionPolishBuilder.ConfigureScene(_scene);
            Assert.That(Components<Component>(), Is.EquivalentTo(before));
        }
    }
}
