using UnityEngine;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Utilities
{
    [DisallowMultipleComponent]
    public sealed class ApplianceSwitch : Interactable
    {
        [SerializeField] private ElectricalAppliance _appliance;
        public ElectricalAppliance Appliance => _appliance;
        public override string DisplayName => _appliance == null ? "Unavailable appliance" : _appliance.DisplayName;
        public override string ActionLabel => "Turn " + DisplayName + (_appliance != null && _appliance.IsOn ? " Off" : " On");
        public override string PromptLabel => ActionLabel;
        public override bool CanInteract(InteractionContext context) => isActiveAndEnabled && _appliance != null &&
            _appliance.isActiveAndEnabled && _appliance.State != null && context.Carry != null && !context.Carry.HasHeldObject;
        public override bool TryInteract(InteractionContext context) => CanInteract(context) && _appliance.SetOn(!_appliance.IsOn);
    }
}
