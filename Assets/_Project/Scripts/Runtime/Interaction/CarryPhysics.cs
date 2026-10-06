using UnityEngine;

namespace ZeroStarRestaurant.Interaction
{
    // Bounded, deterministic calculations; no input, renderer or physics queries.
    public static class CarryPhysics
    {
        public static Vector3 NaturalReleaseVelocity(Vector3 bodyVelocity, Vector3 handVelocity,
            float maximumRestingSpeed, float maximumMovingSpeed)
        {
            // Acquisition correction is not a throw. Movement only preserves already acquired momentum.
            float limit = handVelocity.sqrMagnitude > .25f ? maximumMovingSpeed : maximumRestingSpeed;
            return Vector3.ClampMagnitude(bodyVelocity, Mathf.Max(0f, limit));
        }
        public static Vector3 FollowVelocity(Vector3 current, Vector3 error, float gain,
            float maximumSpeed, float maximumForce, float mass, float deltaTime)
        {
            Vector3 desired = Vector3.ClampMagnitude(error * Mathf.Max(0f, gain), Mathf.Max(0f, maximumSpeed));
            float acceleration = Mathf.Max(0f, maximumForce) / Mathf.Max(0.01f, mass);
            return Vector3.ClampMagnitude(Vector3.MoveTowards(current, desired,
                acceleration * Mathf.Max(0f, deltaTime)), Mathf.Max(0f, maximumSpeed));
        }

        public static Vector3 ReleaseVelocity(Vector3 current, Vector3 direction, float impulse,
            float mass, float maximumDropSpeed, float maximumThrowSpeed)
        {
            Vector3 inherited = Vector3.ClampMagnitude(current, Mathf.Max(0f, maximumDropSpeed));
            Vector3 thrown = direction.normalized * Mathf.Max(0f, impulse) / Mathf.Max(0.01f, mass);
            return Vector3.ClampMagnitude(inherited + thrown, Mathf.Max(0f, maximumThrowSpeed));
        }
    }
}
