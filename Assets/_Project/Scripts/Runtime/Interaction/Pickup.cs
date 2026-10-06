using UnityEngine;

namespace ZeroStarRestaurant.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Pickup : Interactable
    {
        private Rigidbody _body;
        private PhysicalCarry _holder;
        [SerializeField, Range(-90f, 80f)] private float _minimumCarryElevationDegrees = -90f;
        public float MinimumCarryElevationDegrees => _minimumCarryElevationDegrees;
        internal void ConfigureCarryElevation(float degrees) => _minimumCarryElevationDegrees = Mathf.Clamp(degrees, -90f, 80f);
        public bool IsHeld => _holder != null;
        public Rigidbody Body => _body != null ? _body : (_body = GetComponent<Rigidbody>());
        public override string ActionLabel => "Pick up";

        public override bool CanInteract(InteractionContext context)
        {
            return isActiveAndEnabled && Body != null && !Body.isKinematic && _holder == null &&
                context.Carry != null && context.Carry.isActiveAndEnabled && !context.Carry.HasHeldObject;
        }

        public override bool TryInteract(InteractionContext context)
        {
            return CanInteract(context) && context.Carry.TryPickUp(this);
        }

        internal bool TryClaim(PhysicalCarry holder)
        {
            if (_holder != null || !isActiveAndEnabled)
                return false;
            _holder = holder;
            return true;
        }

        internal void ReleaseClaim(PhysicalCarry holder)
        {
            if (_holder == holder)
                _holder = null;
        }

        private void OnDisable()
        {
            if (_holder != null)
                _holder.ReleaseIfHeld(this);
        }
    }
}
