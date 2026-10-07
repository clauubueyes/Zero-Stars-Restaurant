using UnityEngine;

namespace ZeroStarRestaurant.Customers
{
    [CreateAssetMenu(menuName = "Zero Star Restaurant/Reputation Settings")]
    public sealed class ReputationSettings : ScriptableObject
    {
        [SerializeField] private int _minimum;
        [SerializeField] private int _maximum = 100;
        [SerializeField] private int _initial = 50;
        [SerializeField] private int _satisfiedChange = 1;
        [SerializeField] private int _unhappyChange = -1;
        [SerializeField] private int _complaintChange = -3;
        [SerializeField] private int _confirmedIncidentChange = -8;
        [SerializeField, Min(0)] private float _delayWorldSeconds = 7200;
        [SerializeField, Range(0, 1)] private float _rawMeatProbability = .35f;
        [SerializeField, Range(0, 1)] private float _spoiledProbability = .55f;
        [SerializeField, Range(0, 1)] private float _contaminatedProbability = .75f;

        public ReputationPolicy CreatePolicy() => new ReputationPolicy(_minimum, _maximum, _initial,
            _satisfiedChange, _unhappyChange, _complaintChange, _confirmedIncidentChange, _delayWorldSeconds,
            _rawMeatProbability, _spoiledProbability, _contaminatedProbability);
    }
}
