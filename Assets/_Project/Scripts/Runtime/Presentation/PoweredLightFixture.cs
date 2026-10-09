using System;
using UnityEngine;
using ZeroStarRestaurant.Utilities;

namespace ZeroStarRestaurant.Presentation
{
    // Presentation only: no meter, power switch, thermal state or simulation clock.
    [DisallowMultipleComponent]
    public sealed class PoweredLightFixture : MonoBehaviour
    {
        [SerializeField] private RestaurantElectricity _supply;
        [SerializeField] private Light _light;
        [SerializeField] private Renderer[] _tubes = Array.Empty<Renderer>();
        [SerializeField, ColorUsage(false, true)] private Color _litEmission = new Color(3.1f, 3.2f, 3.08f);
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _properties;
        private bool? _lastLit;

        public RestaurantElectricity Supply => _supply;
        public Light Light => _light;
        public bool IsLit => isActiveAndEnabled && _supply != null && _supply.IsOn;

        public void Validate()
        {
            if (_supply == null || _light == null || _tubes == null || _tubes.Length == 0)
                throw new ArgumentException("Fixture requires an explicit M12 supply, light and visible tubes.");
            foreach (Renderer tube in _tubes)
                if (tube == null || tube.sharedMaterial == null || !tube.sharedMaterial.HasProperty(EmissionColor))
                    throw new ArgumentException("Every tube needs an emissive material.");
        }

        private void Awake()
        {
            try { Validate(); }
            catch (ArgumentException exception) { Debug.LogError("Invalid powered light: " + exception.Message, this); enabled = false; }
        }
        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();
        private void OnDisable() => Show(false);

        public void Refresh() => Show(IsLit);

        private void Show(bool lit)
        {
            if (_lastLit == lit) return;
            _lastLit = lit;
            if (_light != null) _light.enabled = lit;
            if (_tubes == null) return;
            _properties ??= new MaterialPropertyBlock();
            foreach (Renderer tube in _tubes)
            {
                if (tube == null) continue;
                tube.GetPropertyBlock(_properties);
                _properties.SetColor(EmissionColor, lit ? _litEmission : Color.black);
                tube.SetPropertyBlock(_properties);
            }
        }
    }
}
