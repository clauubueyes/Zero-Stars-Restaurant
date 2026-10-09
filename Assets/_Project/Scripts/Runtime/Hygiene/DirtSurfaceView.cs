using UnityEngine;

namespace ZeroStarRestaurant.Hygiene
{
    // Only new decorative meshes are changed. The user's renderer/material is never touched.
    [DisallowMultipleComponent]
    public sealed class DirtSurfaceView : MonoBehaviour
    {
        [SerializeField] private CleanableSurface _surface;
        [SerializeField] private Transform[] _stains;
        [SerializeField] private bool _organicOverlay;
        [SerializeField] private int _visualSeed;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _properties;
        private float _lastAmount = -1, _lastGrease = -1;
        private Matrix4x4 _lastSupport;
        private bool _lastActive;
        private static readonly Vector2[] Offsets = { new Vector2(-.3f, -.2f), new Vector2(.1f, .27f), new Vector2(.32f, -.27f),
            new Vector2(-.25f, .3f), new Vector2(.05f, -.1f), new Vector2(.3f, .15f) };
        public float VisualStrength { get; private set; }
        private void LateUpdate()
        {
            if (_surface == null || _surface.State == null || _surface.Support == null) return;
            float grease = (float)_surface.State.AmountsByKind[DirtKind.Grease];
            if (_lastAmount != (float)_surface.State.Amount || _lastGrease != grease ||
                _lastSupport != _surface.Support.transform.localToWorldMatrix || _lastActive != _surface.isActiveAndEnabled) Refresh();
        }
        public void Refresh()
        {
            if (_surface == null || _surface.State == null || _stains == null) return;
            VisualStrength = (float)_surface.State.Amount;
            _lastAmount = VisualStrength; _lastGrease = (float)_surface.State.AmountsByKind[DirtKind.Grease];
            _lastSupport = _surface.Support.transform.localToWorldMatrix; _lastActive = _surface.isActiveAndEnabled;
            if (_organicOverlay) { RefreshOrganic(); return; }
            Transform support = _surface.Support.transform; Vector3 size = _surface.LocalSize, center = _surface.LocalCenter;
            for (int i = 0; i < _stains.Length; i++)
            {
                Transform stain = _stains[i]; if (stain == null) continue;
                stain.gameObject.SetActive(VisualStrength > .001f && _surface.isActiveAndEnabled);
                Vector2 offset = Offsets[i % Offsets.Length];
                stain.position = support.TransformPoint(center + new Vector3(offset.x * size.x, size.y * .5f, offset.y * size.z)) + support.up * .003f;
                stain.rotation = support.rotation;
                Vector3 scale = support.lossyScale;
                float strength = Mathf.Sqrt(VisualStrength);
                stain.localScale = new Vector3(Mathf.Abs(size.x * scale.x) * .25f * strength, .004f, Mathf.Abs(size.z * scale.z) * .23f * strength);
            }
        }
        private float Noise(int index)
        {
            uint h = unchecked((uint)(_visualSeed + index * 374761393));
            h = (h ^ (h >> 13)) * 1274126177; h ^= h >> 16;
            return (h & 65535) / 65535f;
        }
        private void RefreshOrganic()
        {
            if (_renderers == null || _renderers.Length != _stains.Length)
            {
                _renderers = new Renderer[_stains.Length];
                for (int i = 0; i < _stains.Length; i++) if (_stains[i] != null) _renderers[i] = _stains[i].GetComponent<Renderer>();
                _properties = new MaterialPropertyBlock();
            }
            Transform support = _surface.Support.transform;
            Vector3 size = _surface.LocalSize, center = _surface.LocalCenter;
            Vector3 metric = Vector3.Scale(size, support.lossyScale);
            float grease = VisualStrength > 0 ? _lastGrease / VisualStrength : 0;
            for (int i = 0; i < _stains.Length; i++)
            {
                Transform stain = _stains[i]; if (stain == null) continue;
                float threshold = i == 0 ? .002f : .04f + i * i * .024f;
                float presence = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(threshold, threshold + .29f, VisualStrength));
                stain.gameObject.SetActive(presence > .001f && _surface.isActiveAndEnabled);
                float x = (Noise(i * 11) - .5f) * .48f, z = (Noise(i * 11 + 1) - .5f) * .48f;
                float yaw = Noise(i * 11 + 2) * 360;
                stain.position = support.TransformPoint(center + new Vector3(x * size.x, size.y * .5f, z * size.z)) + support.up * (.0035f + i * .00025f);
                stain.rotation = support.rotation * Quaternion.Euler(0, yaw, 0);
                // A square inscribed in the available rectangle stays within the region at any rotation.
                float span = Mathf.Min(Mathf.Abs(metric.x) * (.5f - Mathf.Abs(x)), Mathf.Abs(metric.z) * (.5f - Mathf.Abs(z))) * 1.4f;
                float growth = .58f + .42f * Mathf.Sqrt(VisualStrength);
                stain.localScale = new Vector3(span * growth, 1, span * growth);
                if (_renderers[i] == null || _renderers[i].sharedMaterial == null) continue;
                Color tint = _renderers[i].sharedMaterial.GetColor("_BaseColor");
                tint.a *= presence * (.76f + .24f * Noise(i * 11 + 3));
                _properties.Clear(); _properties.SetColor("_BaseColor", tint);
                _properties.SetFloat("_Smoothness", Mathf.Clamp01(_renderers[i].sharedMaterial.GetFloat("_Smoothness") + grease * .15f));
                _renderers[i].SetPropertyBlock(_properties);
            }
        }
    }
}
