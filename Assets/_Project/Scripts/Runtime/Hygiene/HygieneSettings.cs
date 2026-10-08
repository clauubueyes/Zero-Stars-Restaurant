using UnityEngine;

namespace ZeroStarRestaurant.Hygiene
{
    [CreateAssetMenu(menuName = "Zero Star Restaurant/Hygiene Settings")]
    public sealed class HygieneSettings : ScriptableObject
    {
        [SerializeField, Range(0, 1)] private float _cleanMaximum = .05f;
        [SerializeField, Range(0, 1)] private float _dirtyMinimum = .35f;
        [SerializeField, Range(0, 1)] private float _filthyMinimum = .75f;
        public DirtProfile CreateProfile() => new DirtProfile(_cleanMaximum, _dirtyMinimum, _filthyMinimum);
    }
}
