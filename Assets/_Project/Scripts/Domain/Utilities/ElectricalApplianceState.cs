namespace ZeroStarRestaurant.Utilities
{
    // The user's switch setting survives supply cuts and restoration.
    public sealed class ElectricalApplianceState
    {
        public bool IsOn { get; private set; }
        public ElectricalApplianceState(bool initiallyOn = true) => IsOn = initiallyOn;
        public void SetOn(bool isOn) => IsOn = isOn;
        public bool HasPower(bool supplyOn) => supplyOn && IsOn;
    }
}
