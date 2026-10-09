using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroStarRestaurant.Dishes;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Hygiene
{
    [DisallowMultipleComponent]
    public sealed class SurfaceFoodContact : MonoBehaviour
    {
        [SerializeField] private CleanableSurface _surface;
        [SerializeField] private FoodSimulation _simulation;
        [SerializeField, Min(0)] private float _dirtPerContact = .015f;
        [SerializeField, Min(.001f)] private float _contactTolerance = .035f;
        [SerializeField] private DirtKind _kind = DirtKind.FoodResidue;
        [SerializeField] private bool _pollAutomatically = true;
        private HashSet<Guid> _contacts = new HashSet<Guid>(), _next = new HashSet<Guid>();
        private readonly List<FoodItem> _entered = new List<FoodItem>();
        public int ContactEntryCount { get; private set; }
        public int SafetyTransferCount { get; private set; }
        private void FixedUpdate() { if (_pollAutomatically) Poll(); }
        public void Poll()
        {
            if (!isActiveAndEnabled || _surface == null || _surface.State == null || !_surface.isActiveAndEnabled || _simulation == null) return;
            Physics.SyncTransforms(); _next.Clear(); _entered.Clear();
            foreach (FoodItem food in _simulation.Foods)
            {
                if (food == null || !food.isActiveAndEnabled || food.State == null || food.State.IsSold) continue;
                Pickup pickup = food.GetComponentInParent<DishItem>()?.GetComponent<Pickup>() ?? food.GetComponent<Pickup>();
                if (pickup != null && pickup.IsHeld) continue;
                if (_surface.Support == null || !_surface.Support.enabled || !food.TryGetContactBounds(out Bounds bounds)) continue;
                Vector3 bottom = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                Vector3 supportPoint = _surface.Support.ClosestPoint(bottom);
                if (Vector3.Distance(bottom, supportPoint) > _contactTolerance ||
                    Vector3.Dot(bottom - _surface.Support.bounds.center, _surface.Support.transform.up) < 0 ||
                    !_surface.ContainsPoint(supportPoint) || !_next.Add(food.State.InstanceId)) continue;
                if (!_contacts.Contains(food.State.InstanceId)) _entered.Add(food);
            }
            // All entrants see the surface at the beginning of this poll. Serialized food order
            // cannot cause a newly emitted dose to bounce back in the same event.
            var surfaceExposure = _entered.Count == 0 ? Array.Empty<ContaminationTrace>() : _surface.Contamination.Snapshot();
            _entered.Sort((first, second) => first.State.InstanceId.CompareTo(second.State.InstanceId));
            foreach (var food in _entered)
            {
                ContactEntryCount++;
                _surface.AddDirt(_dirtPerContact, _kind, "Food contact", food.State.InstanceId);
                if (_surface.SafetyPolicy == null) continue;
                _surface.SafetyPolicy.TransferContact(food.State, _surface.Contamination,
                    _surface.State.SurfaceId, _surface.DisplayName,
                    _surface.SafetyPolicy.FoodExposure(food.State), surfaceExposure);
                SafetyTransferCount++;
            }
            var previous = _contacts; _contacts = _next; _next = previous;
        }
    }
}
