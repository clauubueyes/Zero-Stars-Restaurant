using UnityEngine;

namespace ZeroStarRestaurant.Interaction
{
    public sealed class InteractionFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerInteraction _interaction;
        [SerializeField] private InteractionInput _input;
        public bool HasControl => _interaction != null && _input != null && _interaction.HasControl;
        public string Text
        {
            get
            {
                if (!HasControl) return "";
                if (_interaction.HasHeldObject)
                    return _interaction.HeldName + " | " + (_input.AssistedPlacementPrompt ??
                        (_input.IsMouseHolding ? "Release LMB to let go" : "Hold/release LMB or [" + _input.DropBinding + "] Drop"));
                if (_interaction.FocusedTarget == null) return "";
                return _interaction.FocusedTarget is Pickup ? "Hold LMB to grab " + _interaction.FocusedTarget.DisplayName :
                    "[" + _input.InteractBinding + "] " + _interaction.FocusedTarget.PromptLabel;
            }
        }
    }
}
