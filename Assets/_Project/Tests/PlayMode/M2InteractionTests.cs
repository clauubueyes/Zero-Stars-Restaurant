using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Tests
{
    public sealed class M2InteractionTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private GameObject _actor;
        private Transform _view;
        private PhysicalCarry _carry;
        private InteractionDetector _detector;
        private PlayerInteraction _interaction;
        private readonly Vector3 _origin = new Vector3(1000f, 1.65f, 1000f);

        [SetUp]
        public void SetUp()
        {
            _actor = Create("M2Actor", new Vector3(1000f, 0f, 1000f));
            var capsule = _actor.AddComponent<CharacterController>();
            capsule.height = 1.8f; capsule.radius = 0.3f; capsule.center = Vector3.up * 0.9f;
            _view = Create("M2View", _origin).transform;
            _view.SetParent(_actor.transform, true);
            _carry = _actor.AddComponent<PhysicalCarry>();
            Set(_carry, "_viewTransform", _view); Set(_carry, "_actorRoot", _actor.transform);
            _detector = _actor.AddComponent<InteractionDetector>();
            Set(_detector, "_origin", _view); Set(_detector, "_actorRoot", _actor.transform);
            _interaction = _actor.AddComponent<PlayerInteraction>();
            Set(_interaction, "_detector", _detector); Set(_interaction, "_carry", _carry);
        }

        [TearDown]
        public void TearDown()
        {
            _carry.Drop();
            foreach (GameObject item in _objects)
                if (item != null) Object.DestroyImmediate(item);
            _objects.Clear();
        }

        private GameObject Create(string name, Vector3 position)
        {
            var item = new GameObject(name);
            item.transform.position = position;
            _objects.Add(item);
            return item;
        }

        private Pickup Box(float distance = 1.3f, float mass = 1f)
        {
            GameObject item = Create("TestBox", _origin + Vector3.forward * distance);
            item.transform.localScale = Vector3.one * 0.45f;
            item.AddComponent<BoxCollider>();
            Rigidbody body = item.AddComponent<Rigidbody>(); body.mass = mass;
            Pickup pickup = item.AddComponent<Pickup>();
            Physics.SyncTransforms();
            return pickup;
        }

        private GameObject Wall()
        {
            GameObject wall = Create("TestWall", _origin + Vector3.forward * 2f);
            wall.transform.localScale = new Vector3(10f, 10f, 0.5f);
            wall.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            return wall;
        }

        [Test]
        public void WallAndRangeBlockDetection()
        {
            Pickup box = Box(2.8f);
            Assert.That(_detector.Detect(), Is.EqualTo(box));
            GameObject wall = Wall();
            Assert.That(_detector.Detect(), Is.Null);
            Assert.That(_interaction.TryInteract(), Is.False);
            Object.DestroyImmediate(wall);
            box.transform.position = _origin + Vector3.forward * 4f;
            Physics.SyncTransforms();
            Assert.That(_detector.Detect(), Is.Null);
        }

        [Test]
        public void GenericInteractionDoesNotRequirePickupAndRechecksOcclusion()
        {
            GameObject target = Create("ContractProbe", _origin + Vector3.forward * 2.8f);
            target.AddComponent<BoxCollider>();
            var probe = target.AddComponent<M2TestInteractable>();
            Physics.SyncTransforms();
            Assert.That(_interaction.TryInteract(), Is.True);
            Assert.That(probe.Calls, Is.EqualTo(1));
            Wall();
            Assert.That(_interaction.TryInteract(), Is.False);
            Assert.That(probe.Calls, Is.EqualTo(1));
        }

        [Test]
        public void PickupOwnershipIsExclusiveAndRestoresPhysicsOnDrop()
        {
            Pickup first = Box(); Pickup second = Box(2.5f);
            Rigidbody body = first.Body;
            body.constraints = RigidbodyConstraints.FreezeRotationY;
            body.maxLinearVelocity = 25f;
            Assert.That(_carry.TryPickUp(first), Is.True);
            Assert.That(_carry.TryPickUp(second), Is.False);
            PhysicalCarry other = Create("OtherHolder", _actor.transform.position).AddComponent<PhysicalCarry>();
            Set(other, "_viewTransform", _view); Set(other, "_actorRoot", _actor.transform);
            Assert.That(other.TryPickUp(first), Is.False);
            Assert.That(body.useGravity, Is.False);
            Assert.That(Physics.GetIgnoreCollision(_actor.GetComponent<Collider>(), first.GetComponent<Collider>()), Is.True);
            body.linearVelocity = Vector3.one * 100f;
            _carry.Drop();
            Assert.That(_carry.HasHeldObject, Is.False);
            Assert.That(body.useGravity, Is.True);
            Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints.FreezeRotationY));
            Assert.That(body.maxLinearVelocity, Is.EqualTo(25f));
            Assert.That(body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode.Discrete));
            Assert.That(body.linearVelocity.magnitude, Is.LessThanOrEqualTo(2.001f));
            Assert.That(Physics.GetIgnoreCollision(_actor.GetComponent<Collider>(), first.GetComponent<Collider>()), Is.False);
            Assert.That(_carry.TryPickUp(first), Is.True);
        }

        [Test]
        public void DisabledOrDestroyedPickupReleasesHolderAndAllowsAnotherPickup()
        {
            Pickup first = Box();
            Assert.That(_carry.TryPickUp(first), Is.True);
            first.gameObject.SetActive(false);
            Assert.That(_carry.HasHeldObject, Is.False);
            Assert.That(first.Body.useGravity, Is.True);
            Pickup next = Box();
            Assert.That(_carry.TryPickUp(next), Is.True);
            Object.DestroyImmediate(next.gameObject);
            Assert.That(_carry.HasHeldObject, Is.False);
            first.gameObject.SetActive(true);
            Assert.That(_carry.TryPickUp(first), Is.True);
            _carry.enabled = false;
            Assert.That(_carry.HasHeldObject, Is.False);
            Assert.That(first.Body.useGravity, Is.True);
        }

        [Test]
        public void ThrowUsesMassAndDoesNotTeleport()
        {
            Pickup light = Box(1.3f, 0.5f);
            Vector3 position = light.Body.position;
            Assert.That(_carry.TryPickUp(light), Is.True);
            _carry.Throw();
            Assert.That(light.Body.position, Is.EqualTo(position));
            Assert.That(light.Body.linearVelocity.z, Is.EqualTo(12f).Within(0.001f));
            Object.DestroyImmediate(light.gameObject);
            Pickup heavy = Box(1.3f, 12f);
            Assert.That(_carry.TryPickUp(heavy), Is.True);
            _carry.Throw();
            Assert.That(heavy.Body.linearVelocity.z, Is.EqualTo(0.5f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator HeldBodyFollowsWithoutPassingThroughWall()
        {
            Pickup box = Box(); Wall();
            Assert.That(_carry.TryPickUp(box), Is.True);
            for (int frame = 0; frame < 40; frame++) yield return new WaitForFixedUpdate();
            Assert.That(_carry.HasHeldObject, Is.True);
            Assert.That(box.Body.position.z, Is.GreaterThan(1001.3f).And.LessThan(1001.6f));
            Assert.That(box.GetComponent<Collider>().bounds.max.z, Is.LessThan(1001.77f));
            Assert.That(box.Body.linearVelocity.magnitude, Is.LessThanOrEqualTo(6.01f));
        }

        [Test]
        public void InteractionInputOwnsOnlyItsActionsAndDisablingItDropsTheObject()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            try
            {
                InputActionMap map = asset.AddActionMap("Player");
                map.AddAction("Interact", InputActionType.Button).AddBinding("<Keyboard>/e", groups: "Keyboard&Mouse");
                map.AddAction("Drop", InputActionType.Button).AddBinding("<Keyboard>/g", groups: "Keyboard&Mouse");
                map.AddAction("Throw", InputActionType.Button).AddBinding("<Mouse>/rightButton", groups: "Keyboard&Mouse");
                map.AddAction("Move", InputActionType.Value);
                GameObject inputObject = Create("TestInteractionInput", _actor.transform.position);
                inputObject.SetActive(false);
                InteractionInput input = inputObject.AddComponent<InteractionInput>();
                Set(input, "_inputActions", asset); Set(input, "_interaction", _interaction);
                inputObject.SetActive(true);
                var owned = (InputActionAsset)typeof(InteractionInput).GetField("_ownedActions",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(input);
                Assert.That(asset.enabled, Is.False);
                Assert.That(input.DropBinding, Is.Not.Empty);
                for (int cycle = 0; cycle < 2; cycle++)
                {
                    Assert.That(owned.FindAction("Move").enabled, Is.False);
                    foreach (string action in new[] { "Interact", "Drop", "Throw" })
                        Assert.That(owned.FindAction(action).enabled, Is.True);
                    Pickup box = Box();
                    Assert.That(_carry.TryPickUp(box), Is.True);
                    input.enabled = false;
                    Assert.That(owned.enabled, Is.False);
                    Assert.That(_carry.HasHeldObject, Is.False);
                    Object.DestroyImmediate(box.gameObject);
                    input.enabled = true;
                }
            }
            finally { Object.DestroyImmediate(asset); }
        }

        [UnityTest]
        public IEnumerator HeldBodyFollowsWalkingAndTurningWithoutParenting()
        {
            Pickup box = Box();
            Assert.That(_carry.TryPickUp(box), Is.True);
            for (int frame = 0; frame < 20; frame++)
            {
                _actor.transform.position += new Vector3(0.015f, 0f, 0.02f);
                _view.rotation = Quaternion.Euler(0f, frame * 1.5f, 0f);
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();
                Assert.That(_carry.HasHeldObject, Is.True);
                Assert.That(box.Body.linearVelocity.magnitude, Is.LessThanOrEqualTo(6.01f));
            }
            for (int frame = 0; frame < 40; frame++) yield return new WaitForFixedUpdate();
            Vector3 expected = _view.position + _view.forward * 1.8f;
            Assert.That(Vector3.Distance(box.Body.position, expected), Is.LessThan(0.1f));
            Assert.That(box.transform.parent, Is.Null);
            Assert.That(box.Body.isKinematic, Is.False);
        }

        [UnityTest]
        public IEnumerator UnsafeCarrySpaceDropsAtCurrentPosition()
        {
            Pickup box = Box();
            Assert.That(_carry.TryPickUp(box), Is.True);
            for (int frame = 0; frame < 10; frame++) yield return new WaitForFixedUpdate();
            GameObject obstruction = Create("CameraObstruction", _view.position);
            obstruction.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(_carry.HasHeldObject, Is.False);
            Assert.That(box.Body.useGravity, Is.True);
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
