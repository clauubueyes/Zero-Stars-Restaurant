using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ZeroStarRestaurant.Presentation
{
    [DisallowMultipleComponent]
    public sealed class RestaurantAtmosphere : MonoBehaviour
    {
        [SerializeField] private RestaurantAtmosphereSettings _settings;
        [SerializeField] private GameObject _visuals;
        [SerializeField] private Volume _volume;
        [SerializeField] private Camera _camera;
        [SerializeField] private UniversalAdditionalCameraData _cameraData;
        [SerializeField] private Light _referenceLight;
        [SerializeField, Tooltip("Development comparison only. Does not change M12 power or gameplay.")]
        private bool _atmosphereEnabled = true;
        [SerializeField, HideInInspector] private ReferenceLighting _reference;
        private bool _applied;
        private bool _lastEnabled;

        [Serializable]
        private struct ReferenceLighting
        {
            public AmbientMode AmbientMode;
            public Color Sky, Equator, Ground, Background;
            public float AmbientIntensity, ReflectionIntensity;
            public bool Fog, LightEnabled, PostProcessing;
        }

        public bool AtmosphereEnabled => _atmosphereEnabled;
        public GameObject Visuals => _visuals;
        public Volume Volume => _volume;

        public void Validate()
        {
            if (_settings == null || _visuals == null || _volume == null || _volume.sharedProfile == null ||
                _camera == null || _cameraData == null || _referenceLight == null || _visuals == gameObject)
                throw new ArgumentException("Atmosphere requires explicit scene visuals, profile, camera and reference lighting.");
        }

        // Called once by the incremental installer, before modifying any existing lighting.
        public void InitializeForEditor(RestaurantAtmosphereSettings settings, GameObject visuals,
            Volume volume, Camera camera, Light referenceLight)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Install atmosphere outside Play.");
            _settings = settings; _visuals = visuals; _volume = volume; _camera = camera;
            _cameraData = camera.GetUniversalAdditionalCameraData(); _referenceLight = referenceLight;
            _reference = new ReferenceLighting
            {
                AmbientMode = RenderSettings.ambientMode, Sky = RenderSettings.ambientSkyColor,
                Equator = RenderSettings.ambientEquatorColor, Ground = RenderSettings.ambientGroundColor,
                AmbientIntensity = RenderSettings.ambientIntensity, ReflectionIntensity = RenderSettings.reflectionIntensity,
                Fog = RenderSettings.fog, Background = camera.backgroundColor,
                LightEnabled = referenceLight.enabled, PostProcessing = _cameraData.renderPostProcessing
            };
            Validate(); Apply();
        }

        private void Awake()
        {
            try { Validate(); }
            catch (ArgumentException exception) { Debug.LogError("Invalid restaurant atmosphere: " + exception.Message, this); enabled = false; }
        }
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            SceneManager.activeSceneChanged += ActiveSceneChanged;
            if (_settings != null) Apply();
        }
        private void Update() { if (_lastEnabled != _atmosphereEnabled) Apply(); }
        private void ActiveSceneChanged(Scene previous, Scene current)
        { if (current == gameObject.scene) Apply(); }
        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= ActiveSceneChanged;
            if (_applied) RestoreReference();
        }

        [ContextMenu("Development: Atmosphere ON")]
        public void AtmosphereOn() => SetAtmosphere(true);
        [ContextMenu("Development: Atmosphere OFF")]
        public void AtmosphereOff() => SetAtmosphere(false);
        public void SetAtmosphere(bool enabled)
        {
            _atmosphereEnabled = enabled;
            if (isActiveAndEnabled) Apply();
        }

        public void Apply()
        {
            Validate(); _lastEnabled = _atmosphereEnabled; _applied = true;
            if (!_atmosphereEnabled) { RestoreReference(); return; }
            _visuals.SetActive(true); _volume.enabled = true;
            _referenceLight.enabled = false; _cameraData.renderPostProcessing = true;
            // RenderSettings belongs to the active scene. Never alter another additive scene.
            if (SceneManager.GetActiveScene() == gameObject.scene) _settings.ApplyEnvironment(_camera);
        }

        private void RestoreReference()
        {
            if (_visuals != null) _visuals.SetActive(false);
            if (_volume != null) _volume.enabled = false;
            if (_referenceLight != null) _referenceLight.enabled = _reference.LightEnabled;
            if (_cameraData != null) _cameraData.renderPostProcessing = _reference.PostProcessing;
            if (_camera != null) _camera.backgroundColor = _reference.Background;
            if (SceneManager.GetActiveScene() != gameObject.scene) return;
            RenderSettings.ambientMode = _reference.AmbientMode;
            RenderSettings.ambientSkyColor = _reference.Sky;
            RenderSettings.ambientEquatorColor = _reference.Equator;
            RenderSettings.ambientGroundColor = _reference.Ground;
            RenderSettings.ambientIntensity = _reference.AmbientIntensity;
            RenderSettings.reflectionIntensity = _reference.ReflectionIntensity;
            RenderSettings.fog = _reference.Fog;
        }
    }
}
