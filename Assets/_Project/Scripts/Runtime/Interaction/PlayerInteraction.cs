using UnityEngine;
using ZeroStarRestaurant.Player;

namespace ZeroStarRestaurant.Interaction
{
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private InteractionDetector _detector;
        [SerializeField] private PhysicalCarry _carry;
        [SerializeField] private FirstPersonController _player;
        public Interactable FocusedTarget { get; private set; }
        public bool HasControl => isActiveAndEnabled && _player != null && _player.HasControl;
        public bool HasHeldObject => _carry != null && _carry.HasHeldObject;
        public string HeldName => _carry != null ? _carry.HeldName : string.Empty;

        private InteractionContext Context => new InteractionContext(transform, _carry);

        private void LateUpdate()
        {
            FocusedTarget = HasControl ? FindAvailableTarget() : null;
        }

        private Interactable FindAvailableTarget()
        {
            if (_detector == null)
                return null;
            Interactable target = _detector.Detect(_carry != null ? _carry.HeldBody : null);
            return target != null && target.CanInteract(Context) ? target : null;
        }

        public bool TryInteract()
        {
            // Re-detect at the moment of intent instead of trusting the previous frame's prompt.
            Interactable target = isActiveAndEnabled ? FindAvailableTarget() : null;
            return target != null && target.TryInteract(Context);
        }

        public bool TryGrabPhysical()
        {
            // Mouse never invokes purchases, finalization or other contextual actions.
            return isActiveAndEnabled && FindAvailableTarget() is Pickup pickup && pickup.TryInteract(Context);
        }

        public void ReleaseFromMouse()
        {
            if (_carry != null) _carry.ReleaseFromMouse();
        }

        public void Drop()
        {
            if (isActiveAndEnabled && _carry != null)
                _carry.Drop();
        }

        public void Throw()
        {
            if (isActiveAndEnabled && _carry != null)
                _carry.Throw();
        }

        private void OnDisable()
        {
            FocusedTarget = null;
            if (_carry != null)
                _carry.Drop();
        }
    }
}
