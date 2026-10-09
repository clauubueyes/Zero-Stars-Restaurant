#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Customers;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Economy;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Orders;
using ZeroStarRestaurant.Player;
using ZeroStarRestaurant.Presentation;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Tests
{
    public sealed class FoodVisualPlacementRegressionTests
    {
        private Scene _scene;
        private SimulationMode _mode;
        private IngredientPurchaseStation _station;
        private PhysicalCarry _carry;
        private PhysicalDishAssembly _assembly;
        private FoodSimulation _simulation;
        private Camera _camera;
        private Transform _actor;
        private T[] All<T>() where T : Component => _scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        [UnitySetUp] public IEnumerator SetUp()
        {
            _scene = EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/PrototypeRestaurant.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            _station = All<IngredientPurchaseStation>().Single(); _carry = All<PhysicalCarry>().Single(); _assembly = All<PhysicalDishAssembly>().Single();
            _simulation = All<FoodSimulation>().Single(); _simulation.enabled = false;
            _actor = All<FirstPersonController>().Single().transform; _actor.GetComponent<FirstPersonController>().enabled = false; _camera = All<Camera>().Single();
            All<RestaurantElectricity>().Single().PowerOn();
            var service = new SerializedObject(All<CustomerServiceLoop>().Single()); service.FindProperty("_advanceAutomatically").boolValue = false; service.ApplyModifiedPropertiesWithoutUndo();
            _mode = Physics.simulationMode; Physics.simulationMode = SimulationMode.Script;
        }
        [UnityTearDown] public IEnumerator TearDown()
        { _carry?.Drop(); Physics.simulationMode = _mode; if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene); }
        private void Refresh() { foreach (var v in All<BurgerIngredientVisual>()) v.Refresh(); }
        private void Settle()
        { for (int i = 0; i < 45; i++) { Physics.SyncTransforms(); Physics.Simulate(.02f); } Refresh(); }
        private void Eye(Vector3 center)
        { _actor.position += new Vector3(center.x, 1.65f, center.z + .9f) - _camera.transform.position; _camera.transform.LookAt(center); Physics.SyncTransforms(); }
        private FoodItem Buy(int product)
        { Assert.That(_station.TryPurchase(product, out var food), Is.True, _station.LastMessage); return food; }
        private static Bounds Visual(FoodItem food)
        {
            var renderers = food.transform.Find("Visual_VP1BC").GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            Bounds bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds); return bounds;
        }
        private void Place(FoodItem food, Collider destination)
        {
            Eye(food.GetComponent<BoxCollider>().bounds.center); var state = food.State;
            Assert.That(_carry.TryPickUp(food.GetComponent<Pickup>()), Is.True, "Original proxy must remain pickable.");
            Vector3 center = destination.bounds.center;
            food.GetComponent<Rigidbody>().position = new Vector3(center.x, destination.bounds.max.y + .5f, center.z);
            food.transform.position = food.GetComponent<Rigidbody>().position; Physics.SyncTransforms();
            Assert.That(destination.Raycast(new Ray(new Vector3(center.x, destination.bounds.max.y + 1, center.z), Vector3.down), out var hit, 2), Is.True);
            Assert.That(_assembly.TryPlaceHeld(_carry, hit, 1), Is.True, "Existing collider-based assist must remain valid.");
            Settle(); Assert.That(food.State, Is.SameAs(state)); Assert.That(_carry.HasHeldObject, Is.False);
        }
        private Collider Support(string kind)
        {
            var prep = All<CleanableSurface>().Single(s => s.DisplayName == "Assembly 3").Support;
            if (kind != "Plate" && kind != "PattyPlate") return kind == "Grill" ? All<GrillHeatSource>().Single().CleanableSurface.Support : prep;
            Assert.That(_station.TryPurchasePlate(out var plate), Is.True);
            var box = plate.GetComponent<BoxCollider>(); var body = plate.GetComponent<Rigidbody>();
            body.position = new Vector3(prep.bounds.center.x, prep.bounds.max.y + box.bounds.extents.y + .004f, prep.bounds.center.z);
            plate.transform.position = body.position; Settle(); return box;
        }
        [TestCase("Plate", false)] [TestCase("Plate", true)]
        [TestCase("Prep", false)] [TestCase("Prep", true)]
        [TestCase("Partial", false)] [TestCase("PattyPlate", false)] [TestCase("Grill", true)]
        public void EveryPartialLayerRestsVisuallyAndFDoesNotMoveItsPresentation(string kind, bool cheese)
        {
            Collider support = Support(kind); var units = new List<FoodItem>();
            int[] sequence = kind == "Partial" ? new[] { 0, 1 } : kind == "PattyPlate" ? new[] { 1 } : cheese ? new[] { 0, 1, 2, 0 } : new[] { 0, 1, 0 };
            foreach (int product in sequence)
            {
                FoodItem food = Buy(product); Place(food, support); units.Add(food); support = food.GetComponent<BoxCollider>();
                Assert.That(_assembly.FindStack(food), Is.EquivalentTo(units), "Visual compaction cannot change physical membership.");
                for (int i = 1; i < units.Count; i++)
                {
                    float gap = Visual(units[i]).min.y - Visual(units[i - 1]).max.y;
                    Assert.That(gap, Is.InRange(-.002f, .002f), "Visible layers must touch at every pre-F step.");
                    Assert.That(units[i].GetComponent<BoxCollider>().bounds.min.y, Is.GreaterThanOrEqualTo(units[i - 1].GetComponent<BoxCollider>().bounds.max.y - .003f));
                }
            }
            var originalStates = units.Select(u => u.State).ToArray(); var originalIds = originalStates.Select(s => s.InstanceId).ToArray();
            if (units.Count > 1) units[1].State.Contaminate();
            var physical = units.SelectMany(u => u.GetComponents<Component>()).Where(c => !(c is Transform)).ToDictionary(c => c, EditorJsonUtility.ToJson);
            var poses = units.ToDictionary(u => u, u => EditorJsonUtility.ToJson(u.transform));
            for (int i = 0; i < 5; i++) Refresh();
            foreach (var p in physical) Assert.That(EditorJsonUtility.ToJson(p.Key), Is.EqualTo(p.Value));
            foreach (var p in poses) Assert.That(EditorJsonUtility.ToJson(p.Key.transform), Is.EqualTo(p.Value));
            var before = units.Select(Visual).ToArray();
            Assert.That(_assembly.TryFinalize(units.Last(), out var dish), Is.True); Refresh();
            Assert.That(dish.State.Components, Is.EqualTo(originalStates)); Assert.That(units.Select(u => u.State.InstanceId), Is.EqualTo(originalIds));
            Assert.That(units.Select(u => u.State), Is.EqualTo(originalStates));
            if (units.Count > 1) Assert.That(units[1].State.IsContaminated, Is.True);
            for (int i = 0; i < units.Count; i++)
            {
                Assert.That(Vector3.Distance(before[i].center, Visual(units[i]).center), Is.LessThan(.002f), "F must not compact/move the presentation again.");
                Assert.That(Vector3.Distance(before[i].size, Visual(units[i]).size), Is.LessThan(.002f));
            }
            string recipe = kind == "Partial" || kind == "PattyPlate" ? null : cheese ? "dish.cheeseburger" : "dish.hamburger";
            Assert.That(dish.State.RecognizedDefinition?.Id, Is.EqualTo(recipe));
            var shells = units.Select(u => u.transform.Find("Visual_VP1BC")).ToArray(); var local = shells.Select(t => t.localPosition).ToArray();
            dish.transform.rotation = Quaternion.Euler(28, 57, 13); Refresh();
            for (int i = 0; i < shells.Length; i++) Assert.That(Vector3.Distance(shells[i].localPosition, local[i]), Is.LessThan(.0001f), "Carried/rotated dish retains its visual support frame.");
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void RepeatedPurchasesRemainVisibleAboveCollectOverlayAndPickable(int product)
        {
            var marker = All<Transform>().Single(t => t.name == "OutputMarker").Find("Visual_VP1BC/Cladding").GetComponent<Renderer>();
            var ids = new HashSet<System.Guid>();
            for (int n = 0; n < 2; n++)
            {
                var food = Buy(product); var state = food.State; Refresh();
                Assert.That(Visual(food).min.y, Is.GreaterThan(marker.bounds.max.y), "Visible immediately on purchase.");
                Settle();
                Assert.That(Visual(food).min.y - marker.bounds.max.y, Is.InRange(.0005f, .003f), "Thin shell must rest above the nonphysical collect overlay.");
                Assert.That(ids.Add(state.InstanceId), Is.True); Assert.That(food.GetComponent<BoxCollider>().size, Is.EqualTo(Vector3.one));
                var proxy = EditorJsonUtility.ToJson(food.GetComponent<BoxCollider>()); var physicalPose = EditorJsonUtility.ToJson(food.transform);
                for (int i = 0; i < 5; i++) Refresh();
                Assert.That(EditorJsonUtility.ToJson(food.GetComponent<BoxCollider>()), Is.EqualTo(proxy)); Assert.That(EditorJsonUtility.ToJson(food.transform), Is.EqualTo(physicalPose));
                Eye(food.GetComponent<BoxCollider>().bounds.center); Assert.That(_carry.TryPickUp(food.GetComponent<Pickup>()), Is.True);
                Refresh(); Assert.That(_carry.HeldBody, Is.SameAs(food.GetComponent<Rigidbody>())); Assert.That(food.State, Is.SameAs(state));
                _carry.Drop(); food.gameObject.SetActive(false); Physics.SyncTransforms();
            }
        }
        [Test]
        public void MouseSelectionOfAVisibleCompactLayerPicksItsOriginalFood()
        {
            var support = Support("Prep"); var bottom = Buy(0); Place(bottom, support);
            var patty = Buy(1); Place(patty, bottom.GetComponent<BoxCollider>());
            var cheese = Buy(2); Place(cheese, patty.GetComponent<BoxCollider>()); Refresh();
            var detector = All<InteractionDetector>().Single();
            Vector3 VisibleFront(FoodItem unit) { Bounds b = Visual(unit); return b.center + Vector3.back * b.extents.z * .98f; }
            void ViewFront(Vector3 point)
            {
                _actor.position += new Vector3(point.x, 1.65f, point.z - 1.5f) - _camera.transform.position;
                _camera.transform.LookAt(point); Physics.SyncTransforms();
            }
            foreach (var unit in new[] { cheese, patty, bottom })
            {
                ViewFront(VisibleFront(unit));
                Assert.That(detector.Detect(), Is.SameAs(unit.GetComponent<Pickup>()), "Pick the visible food, rather than another layer's empty proxy volume.");
            }
            Vector3 pickPoint = VisibleFront(patty); ViewFront(pickPoint);
            var obstruction = new GameObject("Temporary selection occluder", typeof(BoxCollider));
            SceneManager.MoveGameObjectToScene(obstruction, _scene);
            obstruction.transform.position = Vector3.Lerp(_camera.transform.position, pickPoint, .5f);
            obstruction.GetComponent<BoxCollider>().size = Vector3.one * .15f; Physics.SyncTransforms();
            Assert.That(detector.Detect(), Is.Null, "Visible support must not select food through solid geometry.");
            Object.DestroyImmediate(obstruction); Physics.SyncTransforms();
            Assert.That(detector.Detect(), Is.SameAs(patty.GetComponent<Pickup>()),
                "Eye=" + _camera.transform.position + " target=" + pickPoint + " hits=" + string.Join(", ", Physics.RaycastAll(new Ray(_camera.transform.position, _camera.transform.forward), 3).OrderBy(h => h.distance).Select(h => h.collider.name + "@" + h.distance)));
            Assert.That(All<PlayerInteraction>().Single().TryGrabPhysical(), Is.True);
            Assert.That(_carry.HeldBody, Is.SameAs(patty.GetComponent<Rigidbody>()));
            Assert.That(_assembly.FindStack(cheese), Is.Empty, "Removing the real physical link still disconnects the stack.");
        }
        [Test]
        public void ChangingPresentationThicknessMovesOnlyVisualSupportForTheNextLayer()
        {
            var prep = Support("Prep"); var bottom = Buy(0); Place(bottom, prep);
            var patty = Buy(1); Place(patty, bottom.GetComponent<BoxCollider>());
            var cheese = Buy(2); Place(cheese, patty.GetComponent<BoxCollider>());
            var units = new[] { bottom, patty, cheese };
            var poses = units.ToDictionary(u => u, u => EditorJsonUtility.ToJson(u.transform));
            var proxies = units.ToDictionary(u => u, u => EditorJsonUtility.ToJson(u.GetComponent<BoxCollider>()));
            float oldTop = Visual(cheese).max.y;
            var data = new SerializedObject(patty.transform.Find("Visual_VP1BC").GetComponent<BurgerIngredientVisual>());
            data.FindProperty("_heightScale").floatValue = .14f; data.ApplyModifiedPropertiesWithoutUndo(); Refresh();
            Assert.That(Visual(cheese).max.y, Is.LessThan(oldTop - .01f));
            Assert.That(Visual(cheese).min.y - Visual(patty).max.y, Is.InRange(-.002f, .002f));
            Assert.That(Visual(patty).min.y - Visual(bottom).max.y, Is.InRange(-.002f, .002f));
            foreach (var p in poses) Assert.That(EditorJsonUtility.ToJson(p.Key.transform), Is.EqualTo(p.Value));
            foreach (var p in proxies) Assert.That(EditorJsonUtility.ToJson(p.Key.GetComponent<BoxCollider>()), Is.EqualTo(p.Value));
            Assert.That(_assembly.FindStack(cheese), Is.EquivalentTo(units));
        }
        [Test]
        public void GrillThermalGeometryAndM18ContaminationSurviveVisualSupportAndF()
        {
            var grill = All<GrillHeatSource>().Single(); grill.CleanableSurface.DevelopmentContaminateSurface();
            FoodItem patty = Buy(1); Place(patty, grill.CleanableSurface.Support);
            var contact = All<SurfaceFoodContact>().Single(c => new SerializedObject(c).FindProperty("_surface").objectReferenceValue == grill.CleanableSurface);
            contact.Poll(); Assert.That(patty.State.IsContaminated, Is.True);
            var beforeHeat = grill.TryGetEnvironment(patty, out _); Assert.That(beforeHeat, Is.True);
            _simulation.Advance(30); Assert.That(patty.State.Cooking.EquivalentSeconds, Is.GreaterThan(0));
            FoodItem bun = Buy(0); Place(bun, patty.GetComponent<BoxCollider>());
            bool bunHeated = grill.TryGetEnvironment(bun, out _); double dose = patty.State.Cooking.EquivalentSeconds, temperature = patty.State.TemperatureCelsius;
            var traces = patty.State.Contamination.Snapshot().Select(t => t.Intensity).ToArray();
            var original = patty.State; Refresh();
            Assert.That(_assembly.TryFinalize(bun, out _), Is.True); Refresh(); contact.Poll();
            Assert.That(patty.State, Is.SameAs(original)); Assert.That(patty.State.Cooking.EquivalentSeconds, Is.EqualTo(dose));
            Assert.That(patty.State.TemperatureCelsius, Is.EqualTo(temperature)); Assert.That(patty.State.Contamination.Snapshot().Select(t => t.Intensity), Is.EqualTo(traces));
            Assert.That(grill.TryGetEnvironment(patty, out _), Is.EqualTo(beforeHeat)); Assert.That(grill.TryGetEnvironment(bun, out _), Is.EqualTo(bunHeated));
            _simulation.Advance(10); Assert.That(patty.State.Cooking.EquivalentSeconds, Is.GreaterThan(dose)); Assert.That(patty.State.IsContaminated, Is.True);
        }
        [Test]
        public void CompactOriginalBurgerStillDeliversThroughPassAndPaysOnce()
        {
            var units = new List<FoodItem>(); Collider support = Support("Plate");
            foreach (int product in new[] { 0, 1, 0 }) { var food = Buy(product); Place(food, support); units.Add(food); support = food.GetComponent<BoxCollider>(); }
            Assert.That(_assembly.TryFinalize(units.Last(), out var dish), Is.True); Refresh();
            var states = units.Select(u => u.State).ToArray(); var ids = states.Select(s => s.InstanceId).ToArray();
            var service = All<CustomerServiceLoop>().Single(); service.ForceNextOrder(0);
            for (int i = 0; i < 1000 && service.Visit?.Stage != CustomerStage.Wait; i++) service.Advance(.1);
            Assert.That(service.Visit?.Stage, Is.EqualTo(CustomerStage.Wait));
            var delivery = All<DeliveryZone>().Single(); var box = dish.GetComponent<BoxCollider>(); var body = dish.GetComponent<Rigidbody>();
            Vector3 delta = new Vector3(delivery.Support.bounds.center.x - box.bounds.center.x, delivery.Support.bounds.max.y + .004f - box.bounds.min.y, delivery.Support.bounds.center.z - box.bounds.center.z);
            body.position += delta; dish.transform.position = body.position; body.linearVelocity = Vector3.zero; Physics.SyncTransforms(); body.Sleep(); Refresh();
            long balance = service.Ledger.BalanceCents; delivery.Poll(); delivery.Poll();
            Assert.That(service.LastResult?.Accepted, Is.True, delivery.PlacementMessage); Assert.That(service.LastResult.PaymentCents, Is.EqualTo(500));
            Assert.That(service.Ledger.BalanceCents, Is.EqualTo(balance + 500)); Assert.That(service.ResultRevision, Is.EqualTo(1));
            Assert.That(dish.State.Components, Is.EqualTo(states)); Assert.That(units.Select(u => u.State.InstanceId), Is.EqualTo(ids)); Assert.That(dish.State.IsSold, Is.True);
            Assert.That(service.LastResult.Evaluation.DeliveredDish.Ingredients.Select(i => i.InstanceId), Is.EqualTo(ids));
        }
    }
}
#endif
