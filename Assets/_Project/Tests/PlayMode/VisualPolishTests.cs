#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;
using ZeroStarRestaurant.Player;
using ZeroStarRestaurant.Presentation;

namespace ZeroStarRestaurant.Tests
{
    public sealed class VisualPolishTests
    {
        private Scene _scene;
        private T[] All<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        [UnitySetUp] public IEnumerator SetUp()
        {
            _scene = EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/PrototypeRestaurant.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null; All<FoodSimulation>().Single().enabled = false; All<FirstPersonController>().Single().enabled = false;
        }
        [UnityTearDown] public IEnumerator TearDown() { if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene); }
        [TestCase(false)] [TestCase(true)]
        public void CompactConfirmedBurgerPreservesOriginalStateContactGeometryAndDishProxy(bool cheese)
        {
            var station = All<IngredientPurchaseStation>().Single();
            var support = All<CleanableSurface>().Single(s => s.DisplayName == "Assembly 3").Support;
            var foods = new System.Collections.Generic.List<FoodItem>(); float top = support.bounds.max.y + .001f;
            foreach (int product in cheese ? new[] { 0, 1, 2, 0 } : new[] { 0, 1, 0 })
            {
                Assert.That(station.TryPurchase(product, out var food), Is.True);
                var body = food.GetComponent<Rigidbody>(); float half = food.GetComponent<BoxCollider>().bounds.extents.y;
                body.position = new Vector3(support.bounds.center.x, top + half, support.bounds.center.z); food.transform.position = body.position;
                top += half * 2; foods.Add(food); Physics.SyncTransforms();
                if (product == 1) { food.State.SetTemperature(120); food.State.Advance(46, new ThermalEnvironment(120, 1, true), 0); food.State.Contaminate(); }
            }
            Assert.That(All<PhysicalDishAssembly>().Single().TryFinalize(foods.Last(), out var dish), Is.True);
            Assert.That(dish.State.RecognizedDefinition.Id, Is.EqualTo(cheese ? "dish.cheeseburger" : "dish.hamburger"));
            var states = foods.Select(f => f.State).ToArray(); var components = dish.State.Components.ToArray(); var dishId = dish.State.InstanceId;
            var originals = foods.SelectMany(f => f.GetComponents<Component>()).Concat(dish.GetComponents<Component>()).Where(c => !(c is Transform)).ToDictionary(c => c, EditorJsonUtility.ToJson);
            var poses = foods.ToDictionary(f => f, f => (f.transform.localPosition, f.transform.localRotation, f.transform.localScale));
            var ages = states.Select(s => (s.InstanceId, s.AgeSeconds, s.FreshnessPercent, s.TemperatureCelsius, s.Cooking?.EquivalentSeconds, s.IsContaminated)).ToArray();
            var visuals = dish.GetComponentsInChildren<BurgerIngredientVisual>();
            foreach (var visual in visuals) visual.SetPolishEnabled(false);
            Bounds BoundsForVisuals()
            {
                var renderers = foods.SelectMany(f => f.transform.Find("Visual_VP1BC").GetComponentsInChildren<Renderer>()).Where(r => r.enabled).ToArray();
                var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds); return bounds;
            }
            float oldHeight = BoundsForVisuals().size.y;
            for (int i = 0; i < 10; i++) foreach (var visual in visuals) { visual.SetPolishEnabled(true); visual.Refresh(); }
            var polished = BoundsForVisuals();
            Assert.That(polished.size.y, Is.LessThan(oldHeight * .4f));
            Assert.That(polished.size.x, Is.InRange(.35f, .41f));
            Assert.That(polished.min.y, Is.EqualTo(support.bounds.max.y + .001f).Within(.003f));
            foreach (var pair in poses) Assert.That((pair.Key.transform.localPosition, pair.Key.transform.localRotation, pair.Key.transform.localScale), Is.EqualTo(pair.Value));
            foreach (var pair in originals) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value));
            Assert.That(dish.State.InstanceId, Is.EqualTo(dishId)); Assert.That(dish.State.Components, Is.EqualTo(components));
            Assert.That(foods.Select(f => f.State).ToArray(), Is.EqualTo(states));
            Assert.That(states.Select(s => (s.InstanceId, s.AgeSeconds, s.FreshnessPercent, s.TemperatureCelsius, s.Cooking?.EquivalentSeconds, s.IsContaminated)).ToArray(), Is.EqualTo(ages));
            Assert.That(foods[1].State.IsContaminated, Is.True);
            foreach (var visual in visuals) Assert.That(visual.GetComponentsInChildren<Collider>(), Is.Empty);
            foreach (var visual in visuals) visual.SetPolishEnabled(false);
            Assert.That(BoundsForVisuals().size.y, Is.EqualTo(oldHeight).Within(.0001f));
        }
        [Test]
        public void OrganicDirtUsesBoundedExistingRenderersAndFadesWithCleaningWithoutSanitizing()
        {
            var pool = All<Renderer>().Where(r => r.name.StartsWith("Residue ")).ToArray(); Assert.That(pool, Has.Length.EqualTo(42));
            foreach (var surface in All<CleanableSurface>())
            {
                var view = surface.GetComponent<DirtSurfaceView>(); var data = new SerializedObject(view); var stains = data.FindProperty("_stains");
                Assert.That(data.FindProperty("_organicOverlay").boolValue, Is.True); Assert.That(stains.arraySize, Is.EqualTo(6));
                surface.DevelopmentCleanSurface(); surface.DevelopmentContaminateSurface(); var contamination = surface.Contamination.Snapshot();
                surface.AddDirt(.55, DirtKind.Grease, "test original source"); var change = surface.State.LastChange; var id = surface.State.SurfaceId;
                view.Refresh(); Assert.That(surface.State.LastChange, Is.SameAs(change)); Assert.That(surface.State.SurfaceId, Is.EqualTo(id));
                var first = (Transform)stains.GetArrayElementAtIndex(0).objectReferenceValue; var renderer = first.GetComponent<Renderer>();
                Assert.That(first.GetComponent<Collider>(), Is.Null); Assert.That(first.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(24));
                var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block); float dirtyOpacity = block.GetColor("_BaseColor").a;
                surface.State.Clean(3, .12); view.Refresh(); renderer.GetPropertyBlock(block);
                Assert.That(block.GetColor("_BaseColor").a, Is.LessThan(dirtyOpacity));
                Assert.That(surface.Contamination.Snapshot().Select(t => t.Intensity), Is.EqualTo(contamination.Select(t => t.Intensity)));
                surface.State.Clean(10, .12); view.Refresh();
                for (int i = 0; i < stains.arraySize; i++) Assert.That(((Transform)stains.GetArrayElementAtIndex(i).objectReferenceValue).gameObject.activeSelf, Is.False);
                Assert.That(surface.State.Amount, Is.Zero); Assert.That(surface.Contamination.Snapshot().Select(t => t.Intensity), Is.EqualTo(contamination.Select(t => t.Intensity)));
            }
            Assert.That(All<Renderer>().Count(r => r.name.StartsWith("Residue ")), Is.EqualTo(pool.Length));
        }
    }
}
#endif
