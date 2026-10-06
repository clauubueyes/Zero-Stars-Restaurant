using NUnit.Framework;
using UnityEngine;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Tests
{
    public sealed class CarryPhysicsTests
    {
        [Test]
        public void SameThrowImpulseGivesHeavyObjectsLessSpeed()
        {
            Vector3 light = CarryPhysics.ReleaseVelocity(Vector3.zero, Vector3.forward, 6f, 0.5f, 2f, 12f);
            Vector3 heavy = CarryPhysics.ReleaseVelocity(Vector3.zero, Vector3.forward, 6f, 12f, 2f, 12f);
            Assert.That(light.z, Is.EqualTo(12f).Within(0.001f));
            Assert.That(heavy.z, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void DropAndThrowBoundAccumulatedVelocity()
        {
            Vector3 huge = Vector3.one * 10000f;
            Assert.That(CarryPhysics.ReleaseVelocity(huge, Vector3.zero, 0f, 1f, 2f, 12f).magnitude,
                Is.EqualTo(2f).Within(0.001f));
            Assert.That(CarryPhysics.ReleaseVelocity(huge, Vector3.forward, 6f, 0.01f, 2f, 12f).magnitude,
                Is.EqualTo(12f).Within(0.001f));
        }

        [Test]
        public void FollowHasFiniteForceAndMaximumSpeed()
        {
            Vector3 light = CarryPhysics.FollowVelocity(Vector3.zero, Vector3.forward * 100f, 10f, 6f, 100f, 0.5f, 0.02f);
            Vector3 heavy = CarryPhysics.FollowVelocity(Vector3.zero, Vector3.forward * 100f, 10f, 6f, 100f, 12f, 0.02f);
            Assert.That(light.magnitude, Is.LessThanOrEqualTo(6f));
            Assert.That(light.z, Is.GreaterThan(heavy.z));
            Assert.That(heavy.z, Is.EqualTo(100f / 12f * 0.02f).Within(0.001f));
            Assert.That(CarryPhysics.FollowVelocity(Vector3.one * 100f, Vector3.zero, 10f, 6f, 100f, 1f, 0.02f).magnitude,
                Is.LessThanOrEqualTo(6.001f));
        }

        [Test]
        public void ZeroTimeDoesNotAccelerateAndZeroMassDoesNotCreateInfinity()
        {
            Vector3 current = Vector3.right;
            Assert.That(CarryPhysics.FollowVelocity(current, Vector3.up, 10f, 6f, 100f, 1f, 0f), Is.EqualTo(current));
            Vector3 velocity = CarryPhysics.ReleaseVelocity(Vector3.zero, Vector3.forward, 6f, 0f, 2f, 12f);
            Assert.That(float.IsNaN(velocity.z) || float.IsInfinity(velocity.z), Is.False);
            Assert.That(velocity.magnitude, Is.LessThanOrEqualTo(12.001f));
        }
    }
}
