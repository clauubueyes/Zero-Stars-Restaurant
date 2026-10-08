using UnityEngine;

namespace ZeroStarRestaurant.Hygiene
{
    // Only new decorative meshes are changed. The user's renderer/material is never touched.
    [DisallowMultipleComponent]
    public sealed class DirtSurfaceView : MonoBehaviour
    {
        [SerializeField] private CleanableSurface _surface;
        [SerializeField] private Transform[] _stains;
        private static readonly Vector2[] Offsets = { new Vector2(-.3f, -.2f), new Vector2(.1f, .27f), new Vector2(.32f, -.27f),
            new Vector2(-.25f, .3f), new Vector2(.05f, -.1f), new Vector2(.3f, .15f) };
        public float VisualStrength { get; private set; }
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (_surface == null || _surface.State == null || _stains == null) return;
            VisualStrength = (float)_surface.State.Amount;
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
    }
}
