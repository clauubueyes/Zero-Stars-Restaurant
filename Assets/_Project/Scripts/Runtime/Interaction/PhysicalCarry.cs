using System.Collections.Generic;
using UnityEngine;

namespace ZeroStarRestaurant.Interaction
{
    public sealed class PhysicalCarry : MonoBehaviour
    {
        [SerializeField] private Transform _viewTransform;
        [SerializeField] private Transform _actorRoot;
        [SerializeField, Min(0.1f)] private float _holdDistance = 1.8f;
        [SerializeField, Min(0.1f)] private float _maximumTargetError = 1.5f;
        [SerializeField, Min(0.1f)] private float _maximumPickupDistance = 3.5f;
        [SerializeField, Min(0.1f)] private float _followGain = 10f;
        [SerializeField, Min(0.1f)] private float _maximumHoldSpeed = 6f;
        [SerializeField, Min(0.1f)] private float _maximumHoldForce = 100f;
        [SerializeField, Min(0f)] private float _throwImpulse = 6f;
        [SerializeField, Min(0f)] private float _maximumDropSpeed = 2f;
        [SerializeField, Min(0.1f)] private float _maximumThrowSpeed = 12f;
        [SerializeField, Min(0.01f)] private float _clearance = 0.04f;
        [SerializeField] private LayerMask _collisionMask = ~0;

        private Pickup _held;
        private Rigidbody _body;
        private float _radius;
        private Vector3 _boundsOffset;
        private BodySettings _saved;
        private readonly List<CollisionPair> _ignoredPairs = new List<CollisionPair>();
        private Collider[] _objectColliders;

        public bool HasHeldObject
        {
            get
            {
                if (_body != null && (_held == null || !_held.isActiveAndEnabled || _body.isKinematic ||
                    _viewTransform == null || _actorRoot == null))
                    Release(false);
                else if (_body == null && !ReferenceEquals(_held, null))
                    Release(false);
                return _held != null;
            }
        }
        public Rigidbody HeldBody => HasHeldObject ? _body : null;
        public string HeldName => HasHeldObject ? _held.DisplayName : string.Empty;

        public bool TryPickUp(Pickup pickup)
        {
            if (!isActiveAndEnabled || HasHeldObject || pickup == null || !pickup.isActiveAndEnabled ||
                pickup.Body == null || pickup.Body.isKinematic || _viewTransform == null || _actorRoot == null)
                return false;

            _objectColliders = pickup.GetComponentsInChildren<Collider>();
            bool hasBounds = false;
            Bounds bounds = default;
            foreach (Collider collider in _objectColliders)
            {
                if (!collider.enabled || collider.isTrigger || collider.attachedRigidbody != pickup.Body)
                    continue;
                if (!hasBounds) bounds = collider.bounds;
                else bounds.Encapsulate(collider.bounds);
                hasBounds = true;
            }
            if (!hasBounds || (bounds.ClosestPoint(_viewTransform.position) - _viewTransform.position).sqrMagnitude >
                _maximumPickupDistance * _maximumPickupDistance)
                return false;
            float radius = Mathf.Max(0.01f, bounds.extents.magnitude);
            if (!TryGetTarget(pickup.Body, radius, out _) || !pickup.TryClaim(this))
                return false;

            _held = pickup;
            _body = pickup.Body;
            _radius = radius;
            _boundsOffset = bounds.center - _body.position;
            _saved = new BodySettings(_body);
            _body.useGravity = false;
            _body.constraints |= RigidbodyConstraints.FreezeRotation;
            _body.angularVelocity = Vector3.zero;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.solverIterations = Mathf.Max(12, _body.solverIterations);
            _body.maxLinearVelocity = _maximumHoldSpeed;
            _body.WakeUp();
            foreach (Collider owner in _actorRoot.GetComponentsInChildren<Collider>())
                foreach (Collider item in _objectColliders)
                    if (item.attachedRigidbody == _body && !item.isTrigger)
                    {
                        _ignoredPairs.Add(new CollisionPair(owner, item, Physics.GetIgnoreCollision(owner, item)));
                        Physics.IgnoreCollision(owner, item, true);
                    }
            return true;
        }

        public void Drop() => Release(false);
        public void Throw() => Release(true);

        internal void ReleaseIfHeld(Pickup pickup)
        {
            if (_held == pickup)
                Release(false);
        }

        private void OnDisable() => Release(false);

        private void FixedUpdate()
        {
            if (!HasHeldObject)
                return;
            if (!TryGetTarget(_body, _radius, out Vector3 target))
            {
                Release(false);
                return;
            }
            Vector3 error = target - (_body.position + _boundsOffset);
            if (error.sqrMagnitude > _maximumTargetError * _maximumTargetError)
            {
                Release(false);
                return;
            }
            _body.linearVelocity = CarryPhysics.FollowVelocity(_body.linearVelocity, error,
                _followGain, _maximumHoldSpeed, _maximumHoldForce, _body.mass, Time.fixedDeltaTime);
        }

        private bool TryGetTarget(Rigidbody item, float radius, out Vector3 target)
        {
            target = _viewTransform.position;
            // Handle overlaps explicitly: sphere sweeps alone do not safely cover their initial volume.
            foreach (Collider obstacle in Physics.OverlapSphere(target, radius, _collisionMask, QueryTriggerInteraction.Ignore))
                if (IsObstacle(obstacle, item))
                    return false;
            float distance = _holdDistance;
            foreach (RaycastHit hit in Physics.SphereCastAll(target, radius, _viewTransform.forward,
                         _holdDistance, _collisionMask, QueryTriggerInteraction.Ignore))
                if (IsObstacle(hit.collider, item))
                    distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - _clearance));
            target += _viewTransform.forward * distance;
            // Keep the destination volume out of the player's capsule, even when looking down.
            foreach (Collider owner in _actorRoot.GetComponentsInChildren<Collider>())
                if (owner.enabled && !owner.isTrigger &&
                    (owner.ClosestPoint(target) - target).sqrMagnitude < (radius + _clearance) * (radius + _clearance))
                    return false;
            return true;
        }

        private bool IsObstacle(Collider collider, Rigidbody item)
        {
            return collider.attachedRigidbody != item && !collider.transform.IsChildOf(_actorRoot);
        }

        private void Release(bool thrown)
        {
            Rigidbody body = _body;
            Pickup pickup = _held;
            _body = null;
            _held = null;
            if (body != null)
            {
                Vector3 velocity = body.linearVelocity;
                _saved.Restore(body);
                if (!body.isKinematic && body.gameObject.activeInHierarchy)
                {
                    body.linearVelocity = CarryPhysics.ReleaseVelocity(velocity,
                        thrown && _viewTransform != null ? _viewTransform.forward : Vector3.zero,
                        thrown ? _throwImpulse : 0f, body.mass, _maximumDropSpeed, _maximumThrowSpeed);
                    body.angularVelocity = Vector3.zero;
                    body.WakeUp();
                }
            }
            foreach (CollisionPair pair in _ignoredPairs)
                if (pair.Owner != null && pair.Item != null)
                    Physics.IgnoreCollision(pair.Owner, pair.Item, pair.WasIgnored);
            _ignoredPairs.Clear();
            if (pickup != null)
                pickup.ReleaseClaim(this);
        }

        private readonly struct CollisionPair
        {
            public readonly Collider Owner;
            public readonly Collider Item;
            public readonly bool WasIgnored;
            public CollisionPair(Collider owner, Collider item, bool wasIgnored)
            { Owner = owner; Item = item; WasIgnored = wasIgnored; }
        }

        private readonly struct BodySettings
        {
            private readonly bool _gravity;
            private readonly RigidbodyConstraints _constraints;
            private readonly RigidbodyInterpolation _interpolation;
            private readonly CollisionDetectionMode _collision;
            private readonly int _solverIterations;
            private readonly float _maximumVelocity;
            public BodySettings(Rigidbody body)
            {
                _gravity = body.useGravity; _constraints = body.constraints;
                _interpolation = body.interpolation; _collision = body.collisionDetectionMode;
                _solverIterations = body.solverIterations; _maximumVelocity = body.maxLinearVelocity;
            }
            public void Restore(Rigidbody body)
            {
                body.useGravity = _gravity; body.constraints = _constraints;
                body.interpolation = _interpolation;
                body.collisionDetectionMode = body.isKinematic && _collision != CollisionDetectionMode.Discrete
                    ? CollisionDetectionMode.ContinuousSpeculative : _collision;
                body.solverIterations = _solverIterations; body.maxLinearVelocity = _maximumVelocity;
            }
        }
    }
}
