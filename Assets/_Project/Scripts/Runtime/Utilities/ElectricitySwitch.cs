using UnityEngine;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Utilities
{
    public sealed class ElectricitySwitch : Interactable
    {
        [SerializeField] private RestaurantElectricity _supply;
        [SerializeField] private TextMesh _label;
        public RestaurantElectricity Supply => _supply;
        public override string DisplayName => "Restaurant electricity";
        public override string ActionLabel => _supply != null && _supply.IsOn ? "Power OFF" : "Power ON";
        public override bool CanInteract(InteractionContext context) => isActiveAndEnabled && _supply != null &&
            _supply.isActiveAndEnabled && _supply.State != null && context.Carry != null && !context.Carry.HasHeldObject;
        public override bool TryInteract(InteractionContext context) => CanInteract(context) && _supply.SetPower(!_supply.IsOn);
        private void LateUpdate()
        {
            if (_label != null) _label.text = "ELECTRICITY " + (_supply != null && _supply.IsOn ? "ON" : "OFF") + "\n" + ActionLabel + " [E]";
        }
    }
}
