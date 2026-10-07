using UnityEngine;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Economy
{
    public sealed class IngredientPurchaseButton : Interactable
    {
        [SerializeField] private IngredientPurchaseStation _station;
        [SerializeField] private int _productIndex;
        [SerializeField] private TextMesh _label;
        public override string DisplayName => _station == null ? "Unavailable" : _station.ProductName(_productIndex);
        public override string ActionLabel => "Buy (" + IngredientPurchaseStation.FormatCents(_station == null ? 0 : _station.PriceCents(_productIndex)) + ")";
        private void Start()
        {
            if (_label != null) _label.text = DisplayName + "\n" + ActionLabel + " [E]";
        }
        public override bool CanInteract(InteractionContext context) => isActiveAndEnabled && _station != null &&
            _station.IsAvailable(_productIndex) && context.Carry != null && context.Carry.isActiveAndEnabled && !context.Carry.HasHeldObject;
        public override bool TryInteract(InteractionContext context) => CanInteract(context) && _station.TryPurchase(_productIndex);
    }
}
