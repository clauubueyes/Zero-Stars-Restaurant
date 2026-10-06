using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeroStarRestaurant.Editor;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M3FoodSceneTests
    {
        private Scene _scene;

        [SetUp]
        public void SetUp() => _scene = EditorSceneManager.OpenPreviewScene(M1GreyboxBuilder.ScenePath);

        [TearDown]
        public void TearDown() => EditorSceneManager.ClosePreviewScene(_scene);

        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>()).ToArray();
        // Preserve the original M3 fixtures while later milestones add their own food units.
        private FoodItem[] M3Foods() => _scene.GetRootGameObjects().Single(root => root.name == "FoodTestZone")
            .GetComponentsInChildren<FoodItem>();

        [Test]
        public void FourPhysicalFoodsUseThreeValidDefinitionsAndExplicitSimulationReferences()
        {
            FoodItem[] foods = M3Foods();
            Assert.That(foods, Has.Length.EqualTo(4));
            Assert.That(foods.Select(f => f.Definition).Distinct().Count(), Is.EqualTo(3));
            Assert.That(foods.Select(f => f.Definition.Id).Distinct().Count(), Is.EqualTo(3));
            foreach (FoodItem food in foods)
            {
                FoodProfile profile = food.Definition.CreateProfile();
                Assert.That(profile.ReferenceCostCents, Is.GreaterThan(0));
                Assert.That(profile.FreshnessLifetimeSeconds, Is.GreaterThan(0.0));
                Assert.That(food.GetComponent<Pickup>(), Is.Not.Null);
                Assert.That(food.GetComponent<Rigidbody>().isKinematic, Is.False);
                Assert.That(food.GetComponent<Rigidbody>().useGravity, Is.True);
                Assert.That(food.GetComponent<Collider>().isTrigger, Is.False);
            }
            var simulation = new SerializedObject(Components<FoodSimulation>().Single());
            SerializedProperty references = simulation.FindProperty("_foods");
            Object[] registered = Enumerable.Range(0, references.arraySize)
                .Select(index => references.GetArrayElementAtIndex(index).objectReferenceValue).ToArray();
            Assert.That(registered, Is.EquivalentTo(Components<FoodItem>()));
            foreach (FoodItem food in foods) Assert.That(registered, Does.Contain(food));
            Assert.That(simulation.FindProperty("_developmentTimeMultiplier").floatValue, Is.EqualTo(1f));
        }

        [Test]
        public void BeefUnitsShareDefinitionButHaveDistinctInitialStateFixtures()
        {
            FoodItem[] beef = M3Foods().Where(f => f.Definition.Id == "food.raw_beef_patty").ToArray();
            Assert.That(beef, Has.Length.EqualTo(2));
            Assert.That(beef[0].Definition, Is.SameAs(beef[1].Definition));
            float[] freshness = beef.Select(f => new SerializedObject(f).FindProperty("_initialFreshnessPercent").floatValue).ToArray();
            Assert.That(freshness, Is.EquivalentTo(new[] { 100f, 6f }));
            FoodItem bun = M3Foods().Single(f => f.Definition.Id == "food.bun");
            Assert.That(new SerializedObject(bun).FindProperty("_initialTemperatureCelsius").floatValue, Is.EqualTo(5f));
            FoodItem cheese = M3Foods().Single(f => f.Definition.Id == "food.cheese");
            Assert.That(new SerializedObject(cheese).FindProperty("_initiallyContaminated").boolValue, Is.True);
        }

        [Test]
        public void FoodInspectionIsWiredSeparatelyFromGenericInteraction()
        {
            var feedback = new SerializedObject(Components<FoodInspectionFeedback>().Single());
            Assert.That(feedback.FindProperty("_interaction").objectReferenceValue, Is.EqualTo(Components<PlayerInteraction>().Single()));
            Assert.That(feedback.FindProperty("_carry").objectReferenceValue, Is.EqualTo(Components<PhysicalCarry>().Single()));
            Assert.That(Components<InteractionFeedback>(), Has.Length.EqualTo(1));
        }

        [Test]
        public void SimulationDoesNotMutateDefinitionAndExistingProfilesAreSnapshots()
        {
            var definition = ScriptableObject.CreateInstance<FoodDefinition>();
            try
            {
                string before = EditorJsonUtility.ToJson(definition);
                FoodProfile profile = definition.CreateProfile();
                var first = new FoodState(profile);
                var second = new FoodState(profile);
                first.Advance(300.0, 35.0);
                first.Contaminate();
                Assert.That(EditorJsonUtility.ToJson(definition), Is.EqualTo(before));
                Assert.That(second.AgeSeconds, Is.Zero);
                var authoring = new SerializedObject(definition);
                authoring.FindProperty("_displayName").stringValue = "Next session name";
                authoring.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(first.Profile.DisplayName, Is.EqualTo("New Food"));
                Assert.That(definition.CreateProfile().DisplayName, Is.EqualTo("Next session name"));
            }
            finally { Object.DestroyImmediate(definition); }
        }
    }
}
