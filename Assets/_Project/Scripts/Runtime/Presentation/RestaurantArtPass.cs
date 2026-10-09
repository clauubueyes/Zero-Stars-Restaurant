using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroStarRestaurant.Presentation
{
    // Development comparison. Only renderers and new visual roots change; physics stays live.
    [DisallowMultipleComponent]
    public sealed class RestaurantArtPass : MonoBehaviour
    {
        [SerializeField] private GameObject[] _shells = Array.Empty<GameObject>();
        [SerializeField] private Renderer[] _originals = Array.Empty<Renderer>();
        [SerializeField] private bool[] _originalEnabled = Array.Empty<bool>();
        [SerializeField] private bool _artEnabled = true;
        [SerializeField] private Volume _exposure;
        private bool _last;
        public bool ArtEnabled => _artEnabled;
        public GameObject[] Shells => _shells;
        public Renderer[] Originals => _originals;
        public void InitializeForEditor(GameObject[] shells, Renderer[] originals, bool[] originalEnabled, Volume exposure)
        {
            _shells = shells; _originals = originals; _originalEnabled = originalEnabled; _exposure = exposure;
            Apply();
        }
        private void OnEnable() => Apply();
        private void Update() { if (_last != _artEnabled) Apply(); }
        private void OnDisable() => Restore();
        [ContextMenu("Development: Restaurant Art ON")] public void ArtOn() => SetArt(true);
        [ContextMenu("Development: Restaurant Art OFF (VP1A comparison)")] public void ArtOff() => SetArt(false);
        public void SetArt(bool value) { _artEnabled = value; Apply(); }
        public void Apply()
        {
            _last = _artEnabled;
            for (int i = 0; i < _originals.Length; i++)
                if (_originals[i] != null) _originals[i].enabled = !_artEnabled && _originalEnabled[i];
            foreach (var shell in _shells) if (shell != null) shell.SetActive(_artEnabled);
            if (_exposure != null) _exposure.enabled = _artEnabled;
        }
        private void Restore()
        {
            for (int i = 0; i < _originals.Length; i++)
                if (_originals[i] != null) _originals[i].enabled = _originalEnabled[i];
            foreach (var shell in _shells) if (shell != null) shell.SetActive(false);
            if (_exposure != null) _exposure.enabled = false;
        }
    }
}
