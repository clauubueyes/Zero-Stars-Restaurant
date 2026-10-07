using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using Object = UnityEngine.Object;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M8ProcurementSceneTests
    {
        private Scene _scene;
        [SetUp] public void SetUp() => _scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);
        [TearDown] public void TearDown() => EditorSceneManager.ClosePreviewScene(_scene);
        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        [Test]
        public void StationUsesTheExistingServiceAndClockWithThreeExplicitPhysicalProducts()
        {
            var station = new SerializedObject(Components<IngredientPurchaseStation>().Single());
            Assert.That(station.FindProperty("_service").objectReferenceValue, Is.SameAs(Components<CustomerServiceLoop>().Single()));
            Assert.That(station.FindProperty("_simulation").objectReferenceValue, Is.SameAs(Components<FoodSimulation>().Single()));
            Assert.That(station.FindProperty("_output").objectReferenceValue, Is.Not.Null);
            Assert.That(((BoxCollider)station.FindProperty("_outputClearance").objectReferenceValue).isTrigger, Is.True);
            SerializedProperty products = station.FindProperty("_products"); Assert.That(products.arraySize, Is.EqualTo(3));
            string[] expectedIds = { "food.bun", "food.raw_beef_patty", "food.cheese" };
            int[] prices = { 35, 80, 25 };
            for (int index = 0; index < 3; index++)
            {
                IngredientProduct product = (IngredientProduct)products.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.DoesNotThrow(product.Validate); Assert.That(product.PriceCents, Is.EqualTo(prices[index]));
                Assert.That(product.Prefab.Definition.Id, Is.EqualTo(expectedIds[index]));
                Assert.That(PrefabUtility.IsPartOfPrefabAsset(product.Prefab), Is.True);
                Assert.That(product.Prefab.State, Is.Null); Assert.That(product.Prefab.GetComponent<Pickup>(), Is.Not.Null);
                var food = new SerializedObject(product.Prefab);
                Assert.That(food.FindProperty("_initialAgeSeconds").floatValue, Is.Zero);
                Assert.That(food.FindProperty("_initialFreshnessPercent").floatValue, Is.EqualTo(100));
                Assert.That(food.FindProperty("_initiallyContaminated").boolValue, Is.False);
            }
            PlateProduct plate = (PlateProduct)station.FindProperty("_plateProduct").objectReferenceValue;
            Assert.DoesNotThrow(plate.Validate); Assert.That(plate.PriceCents, Is.EqualTo(50));
            Assert.That(plate.Prefab.GetComponent<DishItem>(), Is.Null); Assert.That(plate.Prefab.GetComponent<FoodItem>(), Is.Null);
            IngredientPurchaseButton[] buttons = Components<IngredientPurchaseButton>(); Assert.That(buttons, Has.Length.EqualTo(4));
            Assert.That(buttons.Select(button => new SerializedObject(button).FindProperty("_productIndex").intValue), Is.EquivalentTo(new[] { 0, 1, 2, 3 }));
            foreach (IngredientPurchaseButton button in buttons)
            {
                Assert.That(button.GetComponent<BoxCollider>().isTrigger, Is.False);
                var data = new SerializedObject(button); Assert.That(data.FindProperty("_station").objectReferenceValue, Is.SameAs(station.targetObject));
                TextMesh text = (TextMesh)data.FindProperty("_label").objectReferenceValue;
                Assert.That(text.font, Is.Not.Null); Assert.That(text.text, Does.Contain("[E]"));
            }
        }

        [Test]
        public void AllLegacySupplyIsExplicitlyGatedAndSeedMoneyIsOnlyDevelopmentConfiguration()
        {
            var supply = new SerializedObject(Components<DevelopmentIngredientSupply>().Single());
            Assert.That(supply.FindProperty("_enableOnStart").boolValue, Is.False);
            Assert.That(supply.FindProperty("_simulation").objectReferenceValue, Is.SameAs(Components<FoodSimulation>().Single()));
            SerializedProperty fixtures = supply.FindProperty("_fixtures");
            Assert.That(Enumerable.Range(0, fixtures.arraySize).Select(index => fixtures.GetArrayElementAtIndex(index).objectReferenceValue),
                Is.EquivalentTo(Components<FoodItem>()));
            Assert.That(new SerializedObject(Components<CustomerServiceLoop>().Single()).FindProperty("_developmentInitialBalanceCents").intValue, Is.EqualTo(1000));
            var unconfigured = new GameObject("Unconfigured service"); unconfigured.SetActive(false);
            try
            { Assert.That(new SerializedObject(unconfigured.AddComponent<CustomerServiceLoop>()).FindProperty("_developmentInitialBalanceCents").intValue, Is.Zero); }
            finally { Object.DestroyImmediate(unconfigured); }
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ProductRejectsInvalidPriceWithoutChangingSharedAssets(int price)
        {
            IngredientProduct original = AssetDatabase.LoadAssetAtPath<IngredientProduct>("Assets/_Project/ScriptableObjects/Economy/Bun.asset");
            IngredientProduct copy = Object.Instantiate(original);
            try
            {
                var data = new SerializedObject(copy); data.FindProperty("_priceCents").intValue = price; data.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<ArgumentException>(copy.Validate); Assert.That(original.PriceCents, Is.EqualTo(35));
            }
            finally { Object.DestroyImmediate(copy); }
        }

        [TestCase(0)] [TestCase(-1)]
        public void PlateRejectsInvalidPriceWithoutChangingItsSharedAsset(int price)
        {
            PlateProduct original = AssetDatabase.LoadAssetAtPath<PlateProduct>(PlateProcurementBuilder.ProductPath);
            PlateProduct copy = Object.Instantiate(original);
            try
            {
                var data = new SerializedObject(copy); data.FindProperty("_priceCents").intValue = price; data.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<ArgumentException>(copy.Validate); Assert.That(original.PriceCents, Is.EqualTo(50));
            }
            finally { Object.DestroyImmediate(copy); }
        }

        [Test]
        public void ProductRejectsMissingOrActivePrefabBeforeStartingProcurement()
        {
            var product = ScriptableObject.CreateInstance<IngredientProduct>();
            GameObject unit = null;
            try
            {
                Assert.Throws<ArgumentException>(product.Validate);
                IngredientProduct original = AssetDatabase.LoadAssetAtPath<IngredientProduct>("Assets/_Project/ScriptableObjects/Economy/Bun.asset");
                unit = Object.Instantiate(original.Prefab.gameObject); unit.SetActive(true);
                var data = new SerializedObject(product); data.FindProperty("_prefab").objectReferenceValue = unit.GetComponent<FoodItem>();
                data.ApplyModifiedPropertiesWithoutUndo(); Assert.Throws<ArgumentException>(product.Validate);
            }
            finally { if (unit != null) Object.DestroyImmediate(unit); Object.DestroyImmediate(product); }
        }

        [TestCase(0L, "€0.00")]
        [TestCase(35L, "€0.35")]
        [TestCase(1000L, "€10.00")]
        [TestCase(long.MaxValue, "€92233720368547758.07")]
        public void FeedbackFormatsIntegerCentsWithoutFloatRounding(long cents, string formatted)
            => Assert.That(IngredientPurchaseStation.FormatCents(cents), Is.EqualTo(formatted));
    }
}
