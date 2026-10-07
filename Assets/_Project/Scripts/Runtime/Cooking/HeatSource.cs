using UnityEngine;
using ZeroStarRestaurant.Food;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Cooking
{
    // A source describes a local environment; it never advances food time or owns food state.
    public abstract class HeatSource : MonoBehaviour
    {
        [SerializeField, Tooltip("Explicit electrical dependency. Unassigned keeps historical standalone thermal fixtures usable.")]
        private ElectricalAppliance _electricity;
        [SerializeField] private bool _requiresElectricity;
        public ElectricalAppliance Electricity => _electricity;
        public bool RequiresElectricity => _requiresElectricity;
        public virtual bool IsOperational => isActiveAndEnabled &&
            (_electricity != null ? _electricity.IsPowered : !_requiresElectricity);
        public abstract bool TryGetEnvironment(FoodItem food, out ThermalEnvironment environment);
    }
}
