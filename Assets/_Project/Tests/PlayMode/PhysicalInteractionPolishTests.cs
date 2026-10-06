using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using Object = UnityEngine.Object;

namespace ZeroStarRestaurant.Tests
{
    public sealed class PhysicalInteractionPolishTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private readonly Vector3 _origin = new Vector3(1300, 2, 1300);
        private FoodSimulation _simulation;
        private FoodDefinition _bun, _beef, _cheese;
        private DishDefinition _hamburger, _cheeseburger;
        private PhysicalDishAssembly _assembly;

        [SetUp]
        public void SetUp()
        {
            _simulation = Create("Food Clock").AddComponent<FoodSimulation>(); _simulation.enabled = false;
            _bun = Definition("bun"); _beef = Definition("beef", true); _cheese = Definition("cheese");
            _hamburger = Recipe("Hamburger", _bun, _beef, _bun);
            _cheeseburger = Recipe("Cheeseburger", _bun, _beef, _cheese, _bun);
            _assembly = Create("Free Assembly").AddComponent<PhysicalDishAssembly>();
            Set(_assembly, "_simulation", _simulation); Set(_assembly, "_definitions", new[] { _hamburger, _cheeseburger });
        }

        [TearDown]
        public void TearDown()
        { foreach (Object item in _objects.AsEnumerable().Reverse()) if (item != null) Object.DestroyImmediate(item); _objects.Clear(); }

        private GameObject Create(string name)
        { var item = new GameObject(name); item.transform.position = _origin; _objects.Add(item); return item; }
        private FoodDefinition Definition(string id, bool cookable = false)
        {
            var asset = ScriptableObject.CreateInstance<FoodDefinition>(); _objects.Add(asset);
            Set(asset, "_id", id); Set(asset, "_isCookable", cookable); return asset;
        }
        private DishDefinition Recipe(string name, params FoodDefinition[] ingredients)
        {
            var asset = ScriptableObject.CreateInstance<DishDefinition>(); _objects.Add(asset);
            Set(asset, "_id", "dish." + name); Set(asset, "_displayName", name); Set(asset, "_orderedIngredients", ingredients); return asset;
        }
        private FoodItem Food(FoodDefinition definition, float height, float x = 0)
        {
            var item = Create("Food"); item.SetActive(false); item.transform.position += new Vector3(x, height, 0);
            item.AddComponent<BoxCollider>().size = new Vector3(.4f, .12f, .4f);
            item.AddComponent<Rigidbody>().mass = .15f; item.AddComponent<Pickup>();
            var food = item.AddComponent<FoodItem>(); Set(food, "_definition", definition); item.SetActive(true);
            _simulation.Register(food); Physics.SyncTransforms(); return food;
        }
        private GameObject Support(string name)
        {
            var support = Create(name); support.transform.position += Vector3.down * .05f;
            support.AddComponent<BoxCollider>().size = new Vector3(2, .1f, 2);
            if (name == "Tray") support.AddComponent<Rigidbody>().isKinematic = true;
            Physics.SyncTransforms(); return support;
        }

        [TestCase("Counter", false)] [TestCase("Table", true)] [TestCase("Tray", false)] [TestCase("Grill", true)]
        public void SupportedPhysicalStackRecognizesWithoutAssemblySurfaceAndPreservesLiveState(string support, bool cheese)
        {
            Support(support);
            var foods = new List<FoodItem> { Food(_bun, .06f), Food(_beef, .18f) };
            if (cheese) foods.Add(Food(_cheese, .30f));
            foods.Add(Food(_bun, cheese ? .42f : .30f));
            FoodState[] originals = foods.Select(food => food.State).ToArray();
            originals[1].Contaminate(); originals[1].SetTemperature(47); _simulation.Advance(3);
            double age = originals[1].AgeSeconds, temperature = originals[1].TemperatureCelsius;
            double freshness = originals[1].FreshnessPercent;
            Assert.That(_assembly.PreviewName(foods.Last()), Is.EqualTo(cheese ? "Cheeseburger" : "Hamburger"));
            using (var otherDraft = new DishState()) Assert.That(otherDraft.TryAdd(originals[0]), Is.True, "Preview must release every claim.");
            Assert.That(_assembly.TryFinalize(foods.Last(), out DishItem dish), Is.True); _objects.Add(dish.gameObject);
            Assert.That(dish.State.DisplayName, Is.EqualTo(cheese ? "Cheeseburger" : "Hamburger"));
            Assert.That(dish.State.Components, Is.EqualTo(originals)); Assert.That(dish.IsIntact, Is.True);
            for (int i = 0; i < foods.Count; i++) Assert.That(foods[i].State, Is.SameAs(originals[i]));
            Assert.That(originals[1].AgeSeconds, Is.EqualTo(age)); Assert.That(originals[1].TemperatureCelsius, Is.EqualTo(temperature));
            Assert.That(originals[1].FreshnessPercent, Is.EqualTo(freshness)); Assert.That(originals[1].IsContaminated, Is.True);
            Assert.That(_simulation.Foods, Is.EquivalentTo(foods));
            Assert.That(_assembly.TryFinalize(foods[0], out _), Is.False, "No duplicate aggregate.");
        }

        [Test]
        public void SeparateStacksAirborneFoodAndForeignOwnershipAreNotSilentlyCombined()
        {
            Support("Table"); FoodItem first = Food(_cheese, .06f), separate = Food(_bun, .06f, .7f);
            Assert.That(_assembly.FindStack(first), Is.EqualTo(new[] { first }));
            FoodItem airborne = Food(_beef, 1);
            Assert.That(_assembly.PreviewName(airborne), Is.Null);
            using (var foreign = new DishState())
            {
                Assert.That(foreign.TryAdd(first.State), Is.True);
                Assert.That(_assembly.TryFinalize(first, out _), Is.False);
                Assert.That(first.GetComponent<Pickup>().enabled, Is.True);
            }
            Assert.That(_assembly.TryFinalize(first, out DishItem custom), Is.True); _objects.Add(custom.gameObject);
            Assert.That(custom.State.DisplayName, Is.EqualTo("Custom Dish"));
            Assert.That(custom.State.Components, Is.EqualTo(new[] { first.State }));
            Assert.That(separate.transform.parent, Is.Null);
        }

        [Test]
        public void GrillHeatsOnlyContactingOriginalIngredientsBeforeAndAfterFinalizingAndMoving()
        {
            Support("Grill"); FoodItem beef = Food(_beef, .06f), upper = Food(_bun, .18f);
            var source = Create("Grill Heat"); var zone = source.AddComponent<BoxCollider>(); zone.isTrigger = true;
            zone.center = Vector3.up * .04f; zone.size = new Vector3(1, .08f, 1);
            var grill = source.AddComponent<GrillHeatSource>(); Set(grill, "_effectiveZone", zone);
            Set(_simulation, "_heatSources", new HeatSource[] { grill });
            Assert.That(grill.TryGetEnvironment(beef, out _), Is.True);
            Assert.That(grill.TryGetEnvironment(upper, out _), Is.False);
            Assert.That(_assembly.TryFinalize(upper, out DishItem dish), Is.True); _objects.Add(dish.gameObject);
            Assert.That(beef.GetComponent<Collider>().enabled, Is.False);
            Assert.That(grill.TryGetEnvironment(beef, out _), Is.True);
            Assert.That(grill.TryGetEnvironment(upper, out _), Is.False, "The proxy must not heat every ingredient.");
            _simulation.Advance(30);
            Assert.That(beef.State.TemperatureCelsius, Is.GreaterThan(21)); Assert.That(beef.State.Cooking.EquivalentSeconds, Is.GreaterThan(0));
            Assert.That(upper.State.TemperatureCelsius, Is.EqualTo(21).Within(.001));
            Assert.That(beef.State.AgeSeconds, Is.EqualTo(30)); Assert.That(upper.State.AgeSeconds, Is.EqualTo(30));
            double hot = beef.State.TemperatureCelsius, cooked = beef.State.Cooking.EquivalentSeconds;
            dish.transform.position += Vector3.right * 3; Physics.SyncTransforms();
            Assert.That(grill.TryGetEnvironment(beef, out _), Is.False);
            _simulation.Advance(10); Assert.That(beef.State.TemperatureCelsius, Is.LessThan(hot));
            Assert.That(beef.State.Cooking.EquivalentSeconds, Is.EqualTo(cooked));
            Assert.That(beef.State.AgeSeconds, Is.EqualTo(40));
        }

        [Test]
        public void TraySupplyReplenishesSixTimesAndWaitsForAClearOutletWithoutCloningFood()
        {
            Support("Counter"); var station = Create("Station"); station.SetActive(false);
            var tray = Create("Tray"); tray.transform.SetParent(station.transform, true); tray.transform.position += Vector3.up * .035f;
            tray.AddComponent<Rigidbody>().isKinematic = true; tray.GetComponent<Rigidbody>().useGravity = false;
            tray.AddComponent<BoxCollider>().size = new Vector3(.7f, .045f, .7f); var first = tray.AddComponent<DishItem>();
            var sensorObject = Create("Assembly Zone"); sensorObject.transform.SetParent(station.transform, true);
            sensorObject.transform.position += Vector3.up * .6f;
            var zone = sensorObject.AddComponent<BoxCollider>(); zone.isTrigger = true; zone.size = new Vector3(.85f, 1.1f, .85f);
            var surface = station.AddComponent<AssemblySurface>(); Set(surface, "_dish", first); Set(surface, "_assemblyZone", zone);
            Set(surface, "_simulation", _simulation); Set(surface, "_definitions", new[] { _hamburger, _cheeseburger });
            var template = Create("Empty Template"); template.SetActive(false); template.transform.position += Vector3.back * 10;
            template.AddComponent<Rigidbody>().isKinematic = true; template.GetComponent<Rigidbody>().useGravity = false;
            template.AddComponent<BoxCollider>().size = new Vector3(.7f, .045f, .7f); var prefab = template.AddComponent<DishItem>();
            var supply = station.AddComponent<DishTraySupply>(); Set(supply, "_surface", surface); Set(supply, "_emptyTrayPrefab", prefab);
            station.SetActive(true); Physics.SyncTransforms();
            var unique = new HashSet<Guid>();
            for (int visit = 0; visit < 6; visit++)
            {
                DishItem previous = surface.Dish;
                Assert.That(supply.TryReplenish(), Is.False, "Unused tray is not duplicated.");
                FoodItem food = Food(_cheese, .13f); surface.RefreshComposition();
                Assert.That(surface.TryInteract(new InteractionContext(null, null)), Is.True);
                Assert.That(unique.Add(previous.State.InstanceId.Value), Is.True);
                Assert.That(supply.TryReplenish(), Is.False, "Finalized dish still occupies its outlet.");
                previous.transform.position += Vector3.right * (5 + visit); Physics.SyncTransforms();
                var blocker = Create("Outlet Blocker"); blocker.transform.position += Vector3.up * .04f;
                blocker.AddComponent<BoxCollider>().size = Vector3.one * .1f; Physics.SyncTransforms();
                Assert.That(supply.TryReplenish(), Is.False); blocker.SetActive(false);
                Assert.That(supply.TryReplenish(), Is.True); _objects.Add(surface.Dish.gameObject);
                Assert.That(surface.Dish, Is.Not.SameAs(previous)); Assert.That(surface.Dish.State.Components, Is.Empty);
                Assert.That(surface.Dish.State.IsFinalized, Is.False); Assert.That(surface.Dish.GetComponent<Rigidbody>().isKinematic, Is.True);
                Assert.That(previous.State.Components.Single(), Is.SameAs(food.State));
                Assert.That(_simulation.Foods.Count, Is.EqualTo(visit + 1), "Only the tray respawns, never ingredients.");
                Assert.That(supply.TryReplenish(), Is.False);
            }
            Object.DestroyImmediate(surface.Dish.gameObject);
            Assert.That(supply.TryReplenish(), Is.True, "A destroyed tray is also recoverable."); _objects.Add(surface.Dish.gameObject);
        }

        [Test]
        public void NaturalReleasePreservesDirectionalMomentumWithoutAnArtificialImpulse()
        {
            Assert.That(CarryPhysics.NaturalReleaseVelocity(Vector3.zero, Vector3.zero, 2, 6), Is.EqualTo(Vector3.zero));
            Assert.That(CarryPhysics.NaturalReleaseVelocity(Vector3.forward * 6, Vector3.zero, 2, 6).magnitude, Is.EqualTo(2));
            Vector3 velocity = new Vector3(3, 1, 2);
            Assert.That(CarryPhysics.NaturalReleaseVelocity(velocity, Vector3.right * 5, 2, 6), Is.EqualTo(velocity));
            Assert.That(CarryPhysics.NaturalReleaseVelocity(Vector3.one * 50, Vector3.forward, 2, 6).magnitude, Is.EqualTo(6).Within(.001));
        }

        [Test]
        public void MovingTheCameraGeneratesReleaseMomentumButAcquisitionAloneDoesNot()
        {
            var actor = Create("Actor"); Transform view = Create("View").transform; view.SetParent(actor.transform, true);
            var carry = actor.AddComponent<PhysicalCarry>(); Set(carry, "_actorRoot", actor.transform); Set(carry, "_viewTransform", view);
            FoodItem food = Food(_bun, 0); Rigidbody body = food.GetComponent<Rigidbody>();
            body.position = _origin + Vector3.forward * 1.3f; Physics.SyncTransforms();
            Assert.That(carry.TryPickUp(food.GetComponent<Pickup>()), Is.True);
            Invoke(carry, "FixedUpdate");
            Assert.That(body.linearVelocity.magnitude, Is.GreaterThan(2), "Acquisition can move quickly toward a still hand.");
            carry.ReleaseFromMouse(); Assert.That(body.linearVelocity.magnitude, Is.EqualTo(2).Within(.001));
            body.position = _origin + Vector3.forward * 1.8f; body.linearVelocity = Vector3.zero; Physics.SyncTransforms();
            Assert.That(carry.TryPickUp(food.GetComponent<Pickup>()), Is.True);
            view.rotation = Quaternion.Euler(0, 15, 0); Invoke(carry, "FixedUpdate");
            Vector3 momentum = body.linearVelocity;
            Assert.That(momentum.x, Is.GreaterThan(2), "Camera motion moves the physical held body.");
            carry.ReleaseFromMouse(); Assert.That(body.linearVelocity, Is.EqualTo(momentum));
            Assert.That(body.useGravity, Is.True); Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints.None));
        }

        [Test]
        public void MouseHoldReleaseOwnsOnlyPhysicalPickupAndRestoresTheBody()
        {
            var actor = Create("Actor"); Transform view = Create("View").transform; view.SetParent(actor.transform, true);
            var carry = actor.AddComponent<PhysicalCarry>(); Set(carry, "_actorRoot", actor.transform); Set(carry, "_viewTransform", view);
            var detector = actor.AddComponent<InteractionDetector>(); Set(detector, "_actorRoot", actor.transform); Set(detector, "_origin", view);
            var interaction = actor.AddComponent<PlayerInteraction>(); Set(interaction, "_detector", detector); Set(interaction, "_carry", carry);
            FoodItem food = Food(_bun, 0); food.transform.position = _origin + Vector3.forward * 1.3f; Physics.SyncTransforms();
            var asset = ScriptableObject.CreateInstance<InputActionAsset>(); _objects.Add(asset);
            var map = asset.AddActionMap("Player"); map.AddAction("Interact", InputActionType.Button).AddBinding("<Keyboard>/e", groups: "Keyboard&Mouse");
            map.AddAction("Drop", InputActionType.Button).AddBinding("<Keyboard>/g", groups: "Keyboard&Mouse");
            map.AddAction("Throw", InputActionType.Button).AddBinding("<Mouse>/rightButton", groups: "Keyboard&Mouse");
            var inputObject = Create("Input"); inputObject.SetActive(false); var input = inputObject.AddComponent<InteractionInput>();
            Set(input, "_inputActions", asset); Set(input, "_interaction", interaction); inputObject.SetActive(true);
            InputSettings previous = InputSystem.settings; InputSettings settings = Object.Instantiate(previous);
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings = settings;
            Mouse mouse = InputSystem.AddDevice<Mouse>(); InputSystem.EnableDevice(mouse);
            try
            {
                MouseState pressed = new MouseState().WithButton(MouseButton.Left);
                InputSystem.QueueStateEvent(mouse, pressed); InputSystem.Update();
                Invoke(input, "UpdateMouseHold", false); Assert.That(carry.HasHeldObject, Is.False, "Recapture click is consumed.");
                InputSystem.QueueStateEvent(mouse, new MouseState()); InputSystem.Update(); Invoke(input, "UpdateMouseHold", true);
                InputSystem.QueueStateEvent(mouse, pressed); InputSystem.Update(); Invoke(input, "UpdateMouseHold", true);
                Assert.That(carry.HeldBody, Is.SameAs(food.GetComponent<Rigidbody>())); Assert.That(input.IsMouseHolding, Is.True);
                InputSystem.Update(); Invoke(input, "UpdateMouseHold", true); Assert.That(carry.HasHeldObject, Is.True);
                carry.HeldBody.linearVelocity = Vector3.zero;
                InputSystem.QueueStateEvent(mouse, new MouseState()); InputSystem.Update(); Invoke(input, "UpdateMouseHold", true);
                Assert.That(carry.HasHeldObject, Is.False); Assert.That(food.GetComponent<Rigidbody>().useGravity, Is.True);
                Assert.That(food.GetComponent<Rigidbody>().linearVelocity, Is.EqualTo(Vector3.zero));
                InputSystem.QueueStateEvent(mouse, pressed); InputSystem.Update(); Invoke(input, "UpdateMouseHold", true);
                carry.HeldBody.linearVelocity = Vector3.forward * 4; Set(carry, "_handVelocity", Vector3.forward * 4);
                InputSystem.QueueStateEvent(mouse, new MouseState()); InputSystem.Update(); Invoke(input, "UpdateMouseHold", true);
                Assert.That(food.GetComponent<Rigidbody>().linearVelocity, Is.EqualTo(Vector3.forward * 4));
                food.gameObject.SetActive(false);
                var contextual = Create("Contextual Target"); contextual.transform.position += Vector3.forward;
                contextual.AddComponent<BoxCollider>(); var target = contextual.AddComponent<M2TestInteractable>(); Physics.SyncTransforms();
                InputSystem.QueueStateEvent(mouse, pressed); InputSystem.Update(); Invoke(input, "UpdateMouseHold", true);
                Assert.That(carry.HasHeldObject, Is.False); Assert.That(target.Calls, Is.Zero);
            }
            finally { InputSystem.RemoveDevice(mouse); InputSystem.settings = previous; Object.DestroyImmediate(settings); }
        }

        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }
}
