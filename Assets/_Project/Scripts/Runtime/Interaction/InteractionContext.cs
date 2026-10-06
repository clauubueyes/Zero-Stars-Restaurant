using UnityEngine;

namespace ZeroStarRestaurant.Interaction
{
    public readonly struct InteractionContext
    {
        public Transform Actor { get; }
        public PhysicalCarry Carry { get; }

        public InteractionContext(Transform actor, PhysicalCarry carry)
        {
            Actor = actor;
            Carry = carry;
        }
    }
}
