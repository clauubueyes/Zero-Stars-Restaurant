using System;
using UnityEngine;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Hygiene
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Pickup))]
    public sealed class CleaningTool : MonoBehaviour
    {
        [SerializeField, Min(0)] private float _cleaningPerSecond = .12f;
        [SerializeField, Min(.01f)] private float _maximumSurfaceDistance = 1.25f;
        public double Clean(CleanableSurface surface, PhysicalCarry carry, RaycastHit hit, double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!isActiveAndEnabled || surface == null || surface.State == null || !surface.AcceptsHit(hit) ||
                carry == null || !carry.HasHeldObject || carry.HeldBody != GetComponent<Pickup>().Body ||
                Vector3.Distance(carry.HeldBody.worldCenterOfMass, hit.point) > _maximumSurfaceDistance) return 0;
            return surface.State.Clean(seconds, _cleaningPerSecond);
        }
    }
}
