using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using ZeroStarRestaurant.Player;

namespace ZeroStarRestaurant.Tests
{
    public sealed class FirstPersonPlayerTests
    {
        private readonly List<Object> _created = new List<Object>();
        private FirstPersonMotor _motor;
        private CharacterController _capsule;

        [SetUp]
        public void SetUp()
        {
            // Far from the loaded scene: native CharacterController collision queries are isolated spatially.
            CreateBox("TestFloor", new Vector3(1000f, -0.25f, 1000f), new Vector3(100f, 0.5f, 100f));
            var player = new GameObject("TestPlayer");
            _created.Add(player);
            player.transform.position = new Vector3(1000f, 2f, 1000f);
            _capsule = player.AddComponent<CharacterController>();
            _capsule.height = 1.8f;
            _capsule.radius = 0.3f;
            _capsule.center = new Vector3(0f, 0.9f, 0f);
            _capsule.skinWidth = 0.03f;
            _capsule.minMoveDistance = 0f;
            _motor = player.AddComponent<FirstPersonMotor>();
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
                if (created != null)
                    Object.DestroyImmediate(created);
            _created.Clear();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void CreateBox(string name, Vector3 position, Vector3 scale)
        {
            var box = new GameObject(name, typeof(BoxCollider));
            _created.Add(box);
            box.transform.position = position;
            box.transform.localScale = scale;
        }

        private void Simulate(int frames, Vector2 movement)
        {
            for (int frame = 0; frame < frames; frame++)
                _motor.Step(movement, false, 1f / 60f);
        }

        [Test]
        public void GravityLandsOnFloorEvenWithoutMovement()
        {
            Simulate(120, Vector2.zero);
            Assert.That(_capsule.isGrounded, Is.True);
            Assert.That(_motor.transform.position.y, Is.EqualTo(0f).Within(0.06f));
        }

        [Test]
        public void DiagonalMovementDoesNotIncreaseWalkingSpeed()
        {
            Simulate(120, Vector2.zero);
            Vector3 before = _motor.transform.position;
            Simulate(60, Vector2.one);
            Vector3 displacement = _motor.transform.position - before;
            Assert.That(new Vector2(displacement.x, displacement.z).magnitude, Is.EqualTo(4f).Within(0.03f));
        }

        [Test]
        public void WalkingCannotPassThroughTallWall()
        {
            CreateBox("TestWall", new Vector3(1000f, 2f, 1002f), new Vector3(10f, 4f, 0.5f));
            Physics.SyncTransforms();
            Simulate(120, Vector2.zero);
            Simulate(120, Vector2.up);
            Assert.That(_motor.transform.position.z, Is.LessThan(1001.8f));
            Assert.That(_motor.transform.position.z, Is.GreaterThan(1001f));
        }

        [Test]
        public void JumpRisesAndReturnsToGroundWithoutAirJump()
        {
            Simulate(120, Vector2.zero);
            _motor.Step(Vector2.zero, true, 1f / 60f);
            float highestY = _motor.transform.position.y;
            // Repeated airborne requests must not reset the upward velocity.
            for (int frame = 0; frame < 35; frame++)
            {
                _motor.Step(Vector2.zero, true, 1f / 60f);
                highestY = Mathf.Max(highestY, _motor.transform.position.y);
            }
            Assert.That(highestY, Is.GreaterThan(0.8f).And.LessThan(1.1f));
            Simulate(120, Vector2.zero);
            Assert.That(_capsule.isGrounded, Is.True);
        }

        [Test]
        public void ReenablingControllerDoesNotEnableSharedAssetOrUnrelatedActions()
        {
            GameObject player = _motor.gameObject;
            player.SetActive(false);
            var cameraObject = new GameObject("TestCamera");
            cameraObject.transform.SetParent(player.transform, false);
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            _created.Add(asset);
            InputActionMap map = asset.AddActionMap("Player");
            map.AddAction("Move", InputActionType.Value);
            map.AddAction("Look", InputActionType.Value);
            map.AddAction("Jump", InputActionType.Button);
            map.AddAction("Interact", InputActionType.Button);
            var controller = player.AddComponent<FirstPersonController>();
            SetField(controller, "_inputActions", asset);
            SetField(controller, "_viewTransform", cameraObject.transform);
            player.SetActive(true);
            var owned = (InputActionAsset)GetField(controller, "_ownedActions");
            for (int cycle = 0; cycle < 2; cycle++)
            {
                Assert.That(owned, Is.Not.SameAs(asset));
                Assert.That(asset.enabled, Is.False);
                Assert.That(owned.FindAction("Player/Move").enabled, Is.True);
                Assert.That(owned.FindAction("Player/Look").enabled, Is.True);
                Assert.That(owned.FindAction("Player/Jump").enabled, Is.True);
                Assert.That(owned.FindAction("Player/Interact").enabled, Is.False);
                controller.enabled = false;
                Assert.That(owned.enabled, Is.False);
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                controller.enabled = true;
            }
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static object GetField(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }
    }
}
