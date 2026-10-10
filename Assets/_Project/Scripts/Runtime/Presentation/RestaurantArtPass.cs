using System;
using System.Collections.Generic;
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
        private readonly Dictionary<Renderer, Material> _labelOriginals = new Dictionary<Renderer, Material>();
        private readonly Dictionary<Font, Material> _labelMaterials = new Dictionary<Font, Material>();
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
        private void OnDisable()
        {
            Restore();
            Font.textureRebuilt -= RefreshFontTexture;
            foreach (var material in _labelMaterials.Values) Destroy(material);
            _labelMaterials.Clear();
            _labelOriginals.Clear();
        }
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
            if (Application.isPlaying && _artEnabled) ApplyLabelDepth();
            else RestoreLabels();
        }
        private void ApplyLabelDepth()
        {
            // UI/Default is already included in GraphicsSettings. Font materials remain untouched.
            foreach (var shell in _shells)
            {
                if (shell == null) continue;
                foreach (var label in shell.GetComponentsInChildren<TextMesh>(true))
                {
                    var renderer = label.GetComponent<Renderer>();
                    if (renderer == null || label.font == null || renderer.sharedMaterial == null) continue;
                    if (!_labelOriginals.ContainsKey(renderer)) _labelOriginals.Add(renderer, renderer.sharedMaterial);
                    if (!_labelMaterials.TryGetValue(label.font, out var material))
                    {
                        material = new Material(_labelOriginals[renderer])
                        {
                            name = "Depth-tested physical font",
                            shader = Shader.Find("UI/Default")
                        };
                        material.SetInt("unity_GUIZTestMode", (int)CompareFunction.LessEqual);
                        _labelMaterials.Add(label.font, material);
                    }
                    material.mainTexture = label.font.material.mainTexture;
                    renderer.sharedMaterial = material;
                }
            }
            Font.textureRebuilt -= RefreshFontTexture;
            Font.textureRebuilt += RefreshFontTexture;
        }
        private void RefreshFontTexture(Font font)
        {
            if (_labelMaterials.TryGetValue(font, out var material)) material.mainTexture = font.material.mainTexture;
        }
        private void RestoreLabels()
        {
            foreach (var pair in _labelOriginals)
                if (pair.Key != null) pair.Key.sharedMaterial = pair.Value;
        }
        private void Restore()
        {
            RestoreLabels();
            for (int i = 0; i < _originals.Length; i++)
                if (_originals[i] != null) _originals[i].enabled = _originalEnabled[i];
            foreach (var shell in _shells) if (shell != null) shell.SetActive(false);
            if (_exposure != null) _exposure.enabled = false;
        }
    }
}
