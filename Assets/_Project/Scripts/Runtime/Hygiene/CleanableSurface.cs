using System;
using UnityEngine;

namespace ZeroStarRestaurant.Hygiene
{
    [DisallowMultipleComponent]
    public sealed class CleanableSurface : MonoBehaviour
    {
        [SerializeField] private HygieneSettings _settings;
        [SerializeField, Tooltip("Optional physical contamination policy, independent of dirt.")]
        private FoodSafetySettings _foodSafetySettings;
        [SerializeField] private BoxCollider _support;
        [SerializeField] private string _displayName;
        [SerializeField, Range(0, 1)] private float _initialDirt;
        [SerializeField, Min(0)] private float _dirtPerCookingSecond = .0025f;
        [SerializeField, Tooltip("Optional area in the support's local XZ coordinates. Zero size uses the entire support.")]
        private Vector2 _regionCenter, _regionSize;
        [SerializeField, Range(0, 1)] private float _developmentDirtAmount = .25f;
        [SerializeField, Range(0, 1)] private float _developmentContaminationIntensity = .8f;
        [SerializeField, Range(0, 1)] private float _developmentSanitizeFraction = 1f;
        public DirtState State { get; private set; }
        public ContaminationState Contamination { get; } = new ContaminationState();
        public FoodSafetyPolicy SafetyPolicy { get; private set; }
        public FoodSafetySettings FoodSafetySettings => _foodSafetySettings;
        public BoxCollider Support => _support;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public Vector3 LocalCenter => _support.center + new Vector3(_regionCenter.x, 0, _regionCenter.y);
        public Vector3 LocalSize => _regionSize.x > 0 && _regionSize.y > 0 ? new Vector3(_regionSize.x, _support.size.y, _regionSize.y) : _support.size;
        public void Validate()
        {
            if (_settings == null || _support == null || _support.isTrigger) throw new ArgumentException("Surface requires settings and a solid support.");
            _settings.CreateProfile();
            if (_foodSafetySettings != null) _foodSafetySettings.CreatePolicy();
            if (!Finite(_initialDirt) || _initialDirt < 0 || _initialDirt > 1 || !Finite(_dirtPerCookingSecond) || _dirtPerCookingSecond < 0)
                throw new ArgumentException("Invalid initial dirt or cooking usage rate.");
            if (!Finite(_regionCenter.x) || !Finite(_regionCenter.y) || !Finite(_regionSize.x) || !Finite(_regionSize.y) ||
                _regionSize.x < 0 || _regionSize.y < 0 || (_regionSize.x == 0) != (_regionSize.y == 0)) throw new ArgumentException("Invalid surface region.");
        }
        private void Awake()
        {
            try
            {
                Validate(); State = new DirtState(_settings.CreateProfile(), _initialDirt);
                SafetyPolicy = _foodSafetySettings == null ? null : _foodSafetySettings.CreatePolicy();
            }
            catch (ArgumentException exception) { Debug.LogError("Invalid hygiene surface: " + exception.Message, this); enabled = false; }
        }
        public bool ContainsPoint(Vector3 worldPoint)
        {
            if (!isActiveAndEnabled || _support == null || !_support.enabled || !_support.gameObject.activeInHierarchy) return false;
            Vector3 local = _support.transform.InverseTransformPoint(worldPoint) - LocalCenter;
            Vector3 half = LocalSize * .5f;
            return Mathf.Abs(local.x) <= half.x + .001f && Mathf.Abs(local.z) <= half.z + .001f;
        }
        public bool AcceptsHit(RaycastHit hit) => hit.collider == _support && ContainsPoint(hit.point) && Vector3.Dot(hit.normal, _support.transform.up) > .5f;
        public double AddDirt(double amount, DirtKind kind, string origin, Guid? foodUnitId = null)
            => isActiveAndEnabled && State != null ? State.AddDirt(amount, kind, origin, foodUnitId) : 0;
        public void RecordCookingUse(Guid foodUnitId, double equivalentSeconds)
        {
            if (double.IsNaN(equivalentSeconds) || double.IsInfinity(equivalentSeconds) || equivalentSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(equivalentSeconds));
            AddDirt(Math.Min(1, equivalentSeconds * _dirtPerCookingSecond), DirtKind.Grease, "Cooking", foodUnitId);
        }
        [ContextMenu("Development: Add Dirt")]
        public void DevelopmentAddDirt() => AddDirt(_developmentDirtAmount, DirtKind.GeneralDirt, "Development");
        [ContextMenu("Development: Set Dirt")]
        public void DevelopmentSetDirt() => State?.SetForDevelopment(_developmentDirtAmount);
        [ContextMenu("Development: Clean Surface")]
        public void DevelopmentCleanSurface() => State?.SetForDevelopment(0);
        [ContextMenu("Development: Make Filthy")]
        public void DevelopmentMakeFilthy() => State?.SetForDevelopment(1);
        [ContextMenu("Development: Contaminate Surface")]
        public void DevelopmentContaminateSurface()
        {
            if (State == null) return;
            Contamination.Receive(new ContaminationTrace(ContaminationKind.Development, State.SurfaceId,
                "Development: " + DisplayName, _developmentContaminationIntensity));
        }
        [ContextMenu("Development: Sanitize Surface")]
        public void DevelopmentSanitizeSurface() => Contamination.Sanitize(_developmentSanitizeFraction);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
