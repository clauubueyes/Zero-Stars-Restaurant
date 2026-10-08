using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ZeroStarRestaurant.Cooking;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Hygiene;
using ZeroStarRestaurant.Interaction;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace ZeroStarRestaurant.Tests
{
    public sealed class M17HygieneTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private readonly Vector3 _origin = new Vector3(1200, 0, 1200);
        private HygieneSettings _settings;
        private FoodDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<HygieneSettings>(); _objects.Add(_settings);
            _definition = ScriptableObject.CreateInstance<FoodDefinition>(); _objects.Add(_definition);
            Set(_definition, "_isCookable", true);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--) if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        private GameObject Create(string name, Vector3 position)
        {
            var item = new GameObject(name); item.transform.position = position; _objects.Add(item); return item;
        }
        private CleanableSurface Surface(Vector3? position = null)
        {
            var support = Create("Existing support", position ?? (_origin - Vector3.up * .1f)).AddComponent<BoxCollider>();
            support.size = new Vector3(2.5f, .2f, 1.5f);
            var owner = Create("Hygiene surface", support.transform.position); owner.SetActive(false);
            var surface = owner.AddComponent<CleanableSurface>(); Set(surface, "_settings", _settings); Set(surface, "_support", support);
            Set(surface, "_displayName", "Test worktop"); owner.SetActive(true); Physics.SyncTransforms(); return surface;
        }
        private FoodItem Food(Vector3? position = null)
        {
            var owner = Create("Patty", position ?? (_origin + Vector3.up * .06f)); owner.SetActive(false);
            owner.AddComponent<BoxCollider>().size = new Vector3(.4f, .12f, .4f);
            owner.AddComponent<Rigidbody>().useGravity = false; owner.AddComponent<Pickup>();
            var food = owner.AddComponent<FoodItem>(); Set(food, "_definition", _definition);
            owner.SetActive(true); Physics.SyncTransforms(); return food;
        }
        private FoodSimulation Simulation(params FoodItem[] foods)
        {
            var clock = Create("Food clock", _origin).AddComponent<FoodSimulation>(); clock.enabled = false;
            Set(clock, "_foods", foods); return clock;
        }
        private GrillHeatSource Grill(CleanableSurface surface)
        {
            var grill = surface.Support.gameObject.AddComponent<GrillHeatSource>();
            var zone = Create("Thermal zone", _origin + Vector3.up * .1f).AddComponent<BoxCollider>();
            zone.size = new Vector3(2.4f, .22f, 1.4f); zone.isTrigger = true;
            Set(grill, "_effectiveZone", zone); Set(grill, "_cleanableSurface", surface); return grill;
        }

        [Test]
        public void RealCookingAddsProportionalGreaseOnceAndRetainsFoodIdentity()
        {
            var surface = Surface(); var grill = Grill(surface); var food = Food(); var driver = Simulation(food, food);
            Set(driver, "_heatSources", new HeatSource[] { grill, grill }); var state = food.State;
            driver.Advance(45);
            Assert.That(surface.State.Amount, Is.EqualTo(state.Cooking.EquivalentSeconds * .0025f).Within(1e-9));
            Assert.That(surface.State.Amount, Is.GreaterThan(0).And.LessThan(1));
            Assert.That(surface.State.LastChange.FoodUnitId, Is.EqualTo(state.InstanceId));
            Assert.That(surface.State.LastChange.Kind, Is.EqualTo(DirtKind.Grease));
            Assert.That(state.Cooking.Stage, Is.EqualTo(CookingStage.Cooked)); Assert.That(state.AgeSeconds, Is.EqualTo(45));
            double dirt = surface.State.Amount, dose = state.Cooking.EquivalentSeconds;
            food.GetComponent<Rigidbody>().position = _origin + Vector3.right * 4; Physics.SyncTransforms(); driver.Advance(10);
            Assert.That(surface.State.Amount, Is.EqualTo(dirt)); Assert.That(state.Cooking.EquivalentSeconds, Is.EqualTo(dose));
            food.GetComponent<Rigidbody>().position = _origin + Vector3.up * .06f; Physics.SyncTransforms(); driver.Advance(10);
            Assert.That(surface.State.Amount, Is.GreaterThan(dirt)); Assert.That(food.State, Is.SameAs(state));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void NoCookingMeansNoCookingDirt(int mode)
        {
            var surface = Surface(); var grill = Grill(surface);
            if (mode == 0) grill.enabled = false;
            if (mode == 1) Set(grill, "_transferMultiplier", 0f);
            if (mode == 2) Set(_definition, "_isCookable", false);
            var food = Food(mode == 3 ? _origin + Vector3.right * 4 : (Vector3?)null);
            var driver = Simulation(food); Set(driver, "_heatSources", new HeatSource[] { grill }); driver.Advance(100);
            Assert.That(surface.State.Amount, Is.Zero); Assert.That(food.State.IsContaminated, Is.False);
        }

        [Test]
        public void FoodContactAddsSmallResidueOnEntryRatherThanEveryPoll()
        {
            var surface = Surface(); var food = Food(); var driver = Simulation(food, food);
            var contacts = surface.gameObject.AddComponent<SurfaceFoodContact>();
            Set(contacts, "_surface", surface); Set(contacts, "_simulation", driver); Set(contacts, "_pollAutomatically", false);
            contacts.Poll(); Assert.That(surface.State.Amount, Is.EqualTo(.015f).Within(1e-9));
            for (int i = 0; i < 100; i++) contacts.Poll(); Assert.That(surface.State.Amount, Is.EqualTo(.015f).Within(1e-9));
            food.GetComponent<Rigidbody>().position += Vector3.up; contacts.Poll();
            food.GetComponent<Rigidbody>().position -= Vector3.up; contacts.Poll();
            Assert.That(surface.State.Amount, Is.EqualTo(.03f).Within(1e-9));
            Assert.That(surface.State.LastChange.FoodUnitId, Is.EqualTo(food.State.InstanceId));
            Assert.That(surface.State.AmountsByKind[DirtKind.FoodResidue], Is.EqualTo(surface.State.Amount));
            Assert.That(food.State.AgeSeconds, Is.Zero); Assert.That(food.State.IsContaminated, Is.False);
        }

        private (PhysicalCarry carry, CleaningInteraction cleaning, CleaningTool tool, Transform view) Cleaner(CleanableSurface[] surfaces)
        {
            var actor = Create("Player", _origin + Vector3.back); var view = Create("View", _origin + new Vector3(0, 1.5f, -1)).transform;
            view.SetParent(actor.transform, true); view.LookAt(_origin);
            var carry = actor.AddComponent<PhysicalCarry>(); Set(carry, "_viewTransform", view); Set(carry, "_actorRoot", actor.transform);
            var detector = actor.AddComponent<InteractionDetector>(); Set(detector, "_origin", view); Set(detector, "_actorRoot", actor.transform);
            var cleaning = actor.AddComponent<CleaningInteraction>(); Set(cleaning, "_carry", carry); Set(cleaning, "_detector", detector); Set(cleaning, "_surfaces", surfaces);
            var owner = Create("Tool", _origin + Vector3.up * .3f); owner.AddComponent<BoxCollider>().size = Vector3.one * .2f;
            owner.AddComponent<Rigidbody>(); var pickup = owner.AddComponent<Pickup>(); var tool = owner.AddComponent<CleaningTool>();
            Physics.SyncTransforms(); Assert.That(carry.TryPickUp(pickup), Is.True); return (carry, cleaning, tool, view);
        }

        [Test]
        public void HeldToolCleansOnlyAimedSurfaceProgressivelyAndCanSwitchTargets()
        {
            var first = Surface(); var second = Surface(_origin + new Vector3(3, -.1f, 0));
            first.DevelopmentMakeFilthy(); second.DevelopmentMakeFilthy(); var cleaner = Cleaner(new[] { first, second });
            cleaner.cleaning.Advance(0, true, true); Assert.That(first.State.Amount, Is.EqualTo(1));
            cleaner.cleaning.Advance(2, true, true); Assert.That(first.State.Amount, Is.EqualTo(.76).Within(1e-7));
            Assert.That(second.State.Amount, Is.EqualTo(1)); Assert.That(cleaner.cleaning.ActiveTarget, Is.SameAs(first));
            Assert.That(cleaner.cleaning.ContextText("E"), Does.StartWith("Cleaning Test worktop... 76%"));
            cleaner.view.position += Vector3.right * 3; cleaner.view.LookAt(_origin + Vector3.right * 3);
            cleaner.tool.GetComponent<Rigidbody>().position += Vector3.right * 3; Physics.SyncTransforms();
            cleaner.cleaning.Advance(1, true, true); Assert.That(second.State.Amount, Is.EqualTo(.88).Within(1e-7));
            Assert.That(first.State.Amount, Is.EqualTo(.76).Within(1e-7)); Assert.That(cleaner.cleaning.ActiveTarget, Is.SameAs(second));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void CleaningStopsWithoutHeldToolActionControlRangeOrVisibleTarget(int mode)
        {
            var surface = Surface(); surface.DevelopmentMakeFilthy(); var cleaner = Cleaner(new[] { surface });
            cleaner.cleaning.Advance(1, true, true); double before = surface.State.Amount;
            if (mode == 0) cleaner.carry.Drop();
            if (mode == 3) cleaner.tool.GetComponent<Rigidbody>().position += Vector3.right * 2;
            if (mode == 4)
            {
                var wall = Create("Solid obstruction", Vector3.Lerp(cleaner.view.position, _origin, .5f)).AddComponent<BoxCollider>();
                wall.size = new Vector3(2, .1f, 2);
            }
            if (mode == 5) surface.enabled = false;
            Physics.SyncTransforms(); cleaner.cleaning.Advance(1, mode != 1, mode != 2);
            Assert.That(surface.State.Amount, Is.EqualTo(before)); Assert.That(cleaner.cleaning.ActiveTarget, Is.Null);
        }

        [Test]
        public void FloorRegionsShareGeometryButKeepIndependentDirtAndTargets()
        {
            var first = Surface(); first.Support.size = new Vector3(6, .2f, 2);
            Set(first, "_regionCenter", new Vector2(-1.5f, 0)); Set(first, "_regionSize", new Vector2(2, 2));
            var owner = Create("Second floor region", _origin); owner.SetActive(false);
            var second = owner.AddComponent<CleanableSurface>(); Set(second, "_settings", _settings); Set(second, "_support", first.Support);
            Set(second, "_regionCenter", new Vector2(1.5f, 0)); Set(second, "_regionSize", new Vector2(2, 2)); owner.SetActive(true);
            first.AddDirt(.6, DirtKind.GeneralDirt, "Work area event"); Assert.That(second.State.Amount, Is.Zero);
            Assert.That(first.ContainsPoint(_origin + Vector3.left * 1.5f), Is.True);
            Assert.That(second.ContainsPoint(_origin + Vector3.left * 1.5f), Is.False);
            Assert.That(first.Support, Is.SameAs(second.Support)); Assert.That(first.State.SurfaceId, Is.Not.EqualTo(second.State.SurfaceId));
        }

        [Test]
        public void DecorativeViewFollowsDirtWithoutChangingStateOrExistingMaterial()
        {
            var surface = Surface(); var supportRenderer = surface.Support.gameObject.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); _objects.Add(material);
            supportRenderer.sharedMaterial = material;
            var stain = Create("New decorative mesh", _origin).transform; var view = surface.gameObject.AddComponent<DirtSurfaceView>();
            Set(view, "_surface", surface); Set(view, "_stains", new[] { stain }); view.Refresh(); Assert.That(stain.gameObject.activeSelf, Is.False);
            surface.AddDirt(.25, DirtKind.Grease, "Cooking"); view.Refresh(); float width = stain.localScale.x;
            surface.AddDirt(.5, DirtKind.GeneralDirt, "Development"); view.Refresh();
            Assert.That(stain.localScale.x, Is.GreaterThan(width)); Assert.That(view.VisualStrength, Is.EqualTo(.75));
            stain.localScale = Vector3.zero; stain.gameObject.SetActive(false); view.Refresh();
            Assert.That(surface.State.Amount, Is.EqualTo(.75)); Assert.That(stain.localScale.x, Is.GreaterThan(width));
            Assert.That(supportRenderer.sharedMaterial, Is.SameAs(material)); Assert.That(stain.GetComponent<Collider>(), Is.Null);
            surface.DevelopmentCleanSurface(); view.Refresh(); Assert.That(stain.gameObject.activeSelf, Is.False);
        }

#if UNITY_EDITOR
        [Test]
        public void ExistingInputCleansWhileEIsHeldAndStopsOnReleaseAndDrop()
        {
            var surface = Surface(); surface.DevelopmentMakeFilthy(); var cleaner = Cleaner(new[] { surface });
            var actor = cleaner.carry.gameObject; actor.SetActive(false);
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            var interaction = actor.AddComponent<PlayerInteraction>();
            Set(interaction, "_carry", cleaner.carry); Set(interaction, "_detector", actor.GetComponent<InteractionDetector>());
            var input = actor.AddComponent<InteractionInput>(); Set(input, "_inputActions", asset);
            Set(input, "_interaction", interaction); Set(input, "_cleaning", cleaner.cleaning);
            var previousSettings = InputSystem.settings; var settings = Object.Instantiate(previousSettings);
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings = settings;
            var keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(keyboard);
            var mouse = InputSystem.AddDevice<Mouse>(); InputSystem.EnableDevice(mouse);
            try
            {
                actor.SetActive(true); Physics.SyncTransforms();
                Assert.That(cleaner.carry.TryPickUp(cleaner.tool.GetComponent<Pickup>()), Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); InputSystem.Update();
                ProcessInput(input); double first = surface.State.Amount;
                Assert.That(first, Is.LessThan(1)); Assert.That(cleaner.cleaning.ActiveTarget, Is.SameAs(surface));
                InputSystem.Update(); ProcessInput(input);
                Assert.That(surface.State.Amount, Is.LessThan(first), "Held E continues after its initial press frame.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                double before = surface.State.Amount; ProcessInput(input);
                Assert.That(surface.State.Amount, Is.EqualTo(before)); Assert.That(cleaner.cleaning.ActiveTarget, Is.Null);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E, Key.G)); InputSystem.Update(); ProcessInput(input);
                Assert.That(cleaner.carry.HasHeldObject, Is.False); Assert.That(surface.State.Amount, Is.EqualTo(before));
            }
            finally
            {
                actor.SetActive(false); InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse);
                InputSystem.settings = previousSettings; Object.DestroyImmediate(settings);
            }
        }
        private static void ProcessInput(InteractionInput input) => typeof(InteractionInput)
            .GetMethod("ProcessInput", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(input, new object[] { true, .5 });
#endif

        private static void Set(object target, string field, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var info = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
                if (info != null) { info.SetValue(target, value); return; }
            }
            Assert.Fail("Missing field " + field);
        }
    }
}
