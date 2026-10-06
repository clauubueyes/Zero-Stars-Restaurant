using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Tests
{
    // A contract probe, not a gameplay feature. Only compiled in the test assembly.
    public sealed class M2TestInteractable : Interactable
    {
        public int Calls { get; private set; }
        public override string ActionLabel => "Test";
        public override bool CanInteract(InteractionContext context) => true;
        public override bool TryInteract(InteractionContext context) { Calls++; return true; }
    }
}
