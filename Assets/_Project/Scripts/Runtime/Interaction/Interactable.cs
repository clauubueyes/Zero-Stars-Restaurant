using UnityEngine;

namespace ZeroStarRestaurant.Interaction
{
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] private string _displayName;
        public virtual string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? gameObject.name : _displayName;
        public abstract string ActionLabel { get; }
        public virtual string PromptLabel => ActionLabel + " " + DisplayName;
        public abstract bool CanInteract(InteractionContext context);
        public abstract bool TryInteract(InteractionContext context);
    }
}
