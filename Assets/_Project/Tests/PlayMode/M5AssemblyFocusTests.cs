#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;
using ZeroStarRestaurant.Player;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M5AssemblyFocusTests
    {
        private Scene _scene;
        private AssemblySurface _surface;
        private DishAssemblyInteraction _assembly;
        private InteractionDetector _detector;
        private PhysicalCarry _carry;
        private FirstPersonController _player;
        private Transform _view;

        private T[] Components<T>() where T : Component => _scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>()).ToArray();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _scene = EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/PrototypeRestaurant.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            Components<DevelopmentIngredientSupply>().Single().EnableForDevelopment();
            _scene.GetRootGameObjects().Single(root => root.name == "PhysicalTestObjects").SetActive(true);
            _surface = Components<AssemblySurface>().OrderBy(surface => surface.name).First();
            _assembly = Components<DishAssemblyInteraction>().Single();
            _detector = Components<InteractionDetector>().Single();
            _carry = Components<PhysicalCarry>().Single();
            _player = Components<FirstPersonController>().Single(); _player.enabled = false;
            _view = Components<Camera>().Single().transform;
            Components<FoodSimulation>().Single().enabled = false;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        { if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene); }

        private IEnumerator StackHamburger()
        {
            FoodItem[] buns = Components<FoodItem>().Where(food => food.Definition.Id == "food.bun").Take(2).ToArray();
            FoodItem beef = Components<FoodItem>().First(food => food.Definition.Id == "food.raw_beef_patty");
            FoodItem[] stack = { buns[0], beef, buns[1] };
            float height = _surface.Dish.GetComponent<BoxCollider>().bounds.max.y;
            foreach (FoodItem food in stack)
            {
                Rigidbody body = food.GetComponent<Rigidbody>();
                body.rotation = Quaternion.identity;
                float half = food.GetComponent<BoxCollider>().bounds.extents.y;
                body.position = new Vector3(_surface.Dish.transform.position.x, height + half + 0.003f, _surface.Dish.transform.position.z);
                body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
                height += half * 2f + 0.003f;
            }
            Physics.SyncTransforms();
            for (int frame = 0; frame < 35; frame++) yield return new WaitForFixedUpdate();
            _surface.RefreshComposition();
            Assert.That(_surface.PreviewDefinition?.DisplayName, Is.EqualTo("Hamburger"));
            _player.transform.position = _surface.Dish.transform.position + new Vector3(0f, -0.935f, -1.65f);
            _view.LookAt(buns[1].GetComponent<BoxCollider>().bounds.center);
            Physics.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator LookingAtTopBunShowsRecognitionAndFinalizesTheRealSceneDish()
        {
            yield return StackHamburger();
            Interactable firstHit = _detector.Detect();
            Assert.That(firstHit, Is.TypeOf<Pickup>(), "The solid top bun really occludes the tray raycast.");
            Assert.That(firstHit.GetComponent<FoodItem>().Definition.Id, Is.EqualTo("food.bun"));
            Assert.That(_assembly.FindFocusedSurface(), Is.SameAs(_surface));
            Assert.That(_surface.ActionLabel + " " + _surface.DisplayName, Is.EqualTo("Finalize Hamburger"));
            Assert.That(DishInspectionFeedback.RecognitionLabel(_surface.Dish, _surface), Is.EqualTo("Recognized: Hamburger"));
            Assert.That(_assembly.ConfirmationPrompt(_surface), Is.EqualTo("[F] Finalize Hamburger"));
            FoodState[] original = _surface.Dish.State.Components.ToArray();
            Assert.That(_assembly.TryFinalize(), Is.True);
            Physics.SyncTransforms();
            Interactable aggregate = _detector.Detect();
            Assert.That(aggregate, Is.SameAs(_surface.Dish.GetComponent<Pickup>()));
            Assert.That(aggregate.ActionLabel + " " + aggregate.DisplayName, Is.EqualTo("Pick up Hamburger"));
            Assert.That(_surface.Dish.State.Components, Is.EqualTo(original));
            Assert.That(_surface.Dish.GetComponentsInChildren<FoodItem>().All(food => !food.GetComponent<Pickup>().enabled &&
                food.GetComponentsInChildren<Collider>().All(collider => !collider.enabled)), Is.True);
            Assert.That(_assembly.FindFocusedSurface(), Is.Null);
            Assert.That(_assembly.TryFinalize(), Is.False);
            Assert.That(aggregate.TryInteract(new InteractionContext(_player.transform, _carry)), Is.True);
            _carry.Drop();
            yield return new WaitForFixedUpdate(); Physics.SyncTransforms();
            Assert.That(_detector.Detect()?.DisplayName, Is.EqualTo("Hamburger"));
        }

        [UnityTest]
        public IEnumerator ContextNeverSeesThroughWallsUnrelatedObjectsOrBeyondReachAndRejectsOccupiedHands()
        {
            yield return StackHamburger();
            Assert.That(_assembly.FindFocusedSurface(), Is.SameAs(_surface));
            Vector3 start = _view.position;
            var wall = new GameObject("Focus blocker", typeof(BoxCollider));
            SceneManager.MoveGameObjectToScene(wall, _scene);
            wall.transform.position = start + _view.forward * 0.4f;
            wall.transform.rotation = _view.rotation; wall.transform.localScale = new Vector3(0.6f, 0.6f, 0.04f);
            Physics.SyncTransforms();
            Assert.That(_assembly.FindFocusedSurface(), Is.Null);
            Assert.That(_assembly.TryFinalize(), Is.False);
            wall.SetActive(false);
            var unrelated = Components<Pickup>().First(pickup => pickup.GetComponent<FoodItem>() == null);
            unrelated.Body.position = start + _view.forward * 0.65f; Physics.SyncTransforms();
            Assert.That(_detector.Detect(), Is.SameAs(unrelated));
            Assert.That(_assembly.FindFocusedSurface(), Is.Null);
            unrelated.Body.position = start + Vector3.right * 5f; Physics.SyncTransforms();
            _player.transform.position -= _view.forward * 5f; Physics.SyncTransforms();
            Assert.That(_assembly.FindFocusedSurface(), Is.Null);
            Assert.That(_assembly.TryFinalize(), Is.False);
            _player.transform.position += _view.forward * 5f; Physics.SyncTransforms();
            unrelated.Body.position = start + Vector3.left * 0.7f; Physics.SyncTransforms();
            Assert.That(_carry.TryPickUp(unrelated), Is.True);
            Assert.That(_assembly.ConfirmationPrompt(_surface), Does.Contain("held object"));
            Assert.That(_assembly.TryFinalize(), Is.False);
            _carry.Drop();
        }

        [UnityTest]
        public IEnumerator QuickPlacementUsesTheActualFirstHitAndRejectsWallRangeAndDisabledSurface()
        {
            FoodItem food = Components<FoodItem>().First(item => item.Definition.Id == "food.bun");
            _player.transform.position = new Vector3(-4.4f, 0.03f, 2.65f);
            _view.LookAt(food.transform.position); Physics.SyncTransforms();
            Assert.That(_carry.TryPickUp(food.GetComponent<Pickup>()), Is.True);
            _player.transform.position = _surface.Dish.transform.position + new Vector3(0f, -0.935f, -1.65f);
            _view.LookAt(_surface.Dish.transform.position); Physics.SyncTransforms();
            Assert.That(_assembly.FindFocusedSurface(), Is.SameAs(_surface));
            Assert.That(_assembly.ConfirmationPrompt(_surface), Does.Contain("Place Bun on stack"));
            var wall = new GameObject("Snap occlusion", typeof(BoxCollider)); SceneManager.MoveGameObjectToScene(wall, _scene);
            wall.transform.position = _view.position + _view.forward * 0.4f; wall.transform.localScale = Vector3.one * 0.25f;
            Physics.SyncTransforms(); Assert.That(_assembly.TryPlace(), Is.False); Assert.That(_carry.HasHeldObject, Is.True);
            wall.SetActive(false); _player.transform.position += Vector3.back * 5f; Physics.SyncTransforms();
            Assert.That(_assembly.TryPlace(), Is.False); Assert.That(_carry.HasHeldObject, Is.True);
            _player.transform.position += Vector3.forward * 5f; Physics.SyncTransforms();
            _surface.enabled = false; Assert.That(_assembly.TryPlace(), Is.False); _surface.enabled = true;
            Assert.That(_assembly.TryPlace(), Is.True); Assert.That(_carry.HasHeldObject, Is.False);
            Assert.That(_surface.Dish.State.Components.Single(), Is.SameAs(food.State));
            Assert.That(_assembly.TryPlace(), Is.False); Assert.That(_surface.Dish.State.Components.Count, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MousePlaceBindingReceivesIntentButReleasedCursorCannotPlace()
        {
            InputSettings previousSettings = InputSystem.settings;
            InputSettings testSettings = Object.Instantiate(previousSettings);
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate; InputSystem.settings = testSettings;
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            var owned = (InputActionAsset)typeof(DishAssemblyInteraction).GetField("_ownedActions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_assembly);
            try
            {
                InputSystem.EnableDevice(mouse);
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left)); InputSystem.Update();
                Assert.That(owned.FindAction("Player/PlaceIngredient").IsPressed(), Is.True);
                Assert.That(owned.FindAction("Player/Throw").IsPressed(), Is.False);
                typeof(DishAssemblyInteraction).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_assembly, null);
                Assert.That(_surface.Dish.State.Components, Is.Empty);
                InputSystem.QueueStateEvent(mouse, new MouseState()); InputSystem.Update();
                Assert.That(owned.FindAction("Player/PlaceIngredient").IsPressed(), Is.False);
            }
            finally { InputSystem.RemoveDevice(mouse); InputSystem.settings = previousSettings; Object.DestroyImmediate(testSettings); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator KeyboardFinalizeBindingReceivesIntentButCannotConfirmWithoutGameplayControl()
        {
            yield return StackHamburger();
            InputSettings previousSettings = InputSystem.settings;
            InputSettings testSettings = Object.Instantiate(previousSettings);
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            InputSystem.settings = testSettings;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            var owned = (InputActionAsset)typeof(DishAssemblyInteraction).GetField("_ownedActions",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_assembly);
            try
            {
                // Batch Editor has no focused Game view. Enable only our virtual test device and pump its events explicitly.
                InputSystem.EnableDevice(keyboard);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F));
                InputSystem.Update();
                Assert.That(owned.FindAction("Player/FinalizeDish").IsPressed(), Is.True);
                typeof(DishAssemblyInteraction).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_assembly, null);
                Assert.That(_surface.Dish.State.IsFinalized, Is.False, "Cursor/control released: F cannot confirm.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                Assert.That(owned.FindAction("Player/FinalizeDish").IsPressed(), Is.False);
            }
            finally { InputSystem.RemoveDevice(keyboard); InputSystem.settings = previousSettings; Object.DestroyImmediate(testSettings); }
        }

        [UnityTest]
        public IEnumerator FinalizeInputUsesConfiguredBindingAndOwnsItsActionLifecycle()
        {
            yield return null;
            var field = typeof(DishAssemblyInteraction).GetField("_ownedActions", BindingFlags.Instance | BindingFlags.NonPublic);
            var owned = (InputActionAsset)field.GetValue(_assembly);
            var shared = (InputActionAsset)typeof(DishAssemblyInteraction).GetField("_inputActions",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_assembly);
            Assert.That(owned, Is.Not.SameAs(shared));
            // Project-wide actions may already be enabled by Unity; this adapter must preserve that state.
            bool sharedEnabled = shared.enabled;
            Assert.That(_assembly.FinalizeBinding, Is.EqualTo("F"));
            Assert.That(owned.FindAction("Player/Move").enabled, Is.False);
            for (int cycle = 0; cycle < 2; cycle++)
            {
                Assert.That(owned.FindAction("Player/FinalizeDish").enabled, Is.True);
                Assert.That(owned.FindAction("Player/PlaceIngredient").enabled, Is.True);
                _assembly.enabled = false; Assert.That(owned.enabled, Is.False);
                Assert.That(shared.enabled, Is.EqualTo(sharedEnabled));
                _assembly.enabled = true;
            }
        }
    }
}
#endif
