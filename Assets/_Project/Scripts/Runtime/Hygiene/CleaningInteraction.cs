using System;
using System.Globalization;
using UnityEngine;
using ZeroStarRestaurant.Interaction;

namespace ZeroStarRestaurant.Hygiene
{
    [DisallowMultipleComponent]
    public sealed class CleaningInteraction : MonoBehaviour
    {
        [SerializeField] private InteractionDetector _detector;
        [SerializeField] private PhysicalCarry _carry;
        [SerializeField] private CleanableSurface[] _surfaces = Array.Empty<CleanableSurface>();
        public CleanableSurface ActiveTarget { get; private set; }
        public bool HasHeldTool => _carry != null && _carry.HasHeldObject && _carry.HeldBody.GetComponent<CleaningTool>() != null;
        private CleanableSurface FindTarget(out RaycastHit hit)
        {
            hit = default;
            if (!isActiveAndEnabled || _detector == null || !_detector.TryDetectHit(out hit, _carry != null ? _carry.HeldBody : null)) return null;
            foreach (CleanableSurface surface in _surfaces) if (surface != null && surface.AcceptsHit(hit)) return surface;
            return null;
        }
        public void Advance(double seconds, bool cleaningPressed, bool hasControl)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            Stop(); if (!isActiveAndEnabled || !hasControl || !cleaningPressed || !HasHeldTool) return;
            CleanableSurface surface = FindTarget(out RaycastHit hit);
            if (surface != null && _carry.HeldBody.GetComponent<CleaningTool>().Clean(surface, _carry, hit, seconds) > 0) ActiveTarget = surface;
        }
        public void Stop() => ActiveTarget = null;
        private void OnDisable() => Stop();
        public string ContextText(string interactBinding)
        {
            if (_carry != null && _carry.HasHeldObject && !HasHeldTool) return "";
            var surface = FindTarget(out _); if (surface == null || surface.State == null) return HasHeldTool ? "Cleaning Tool | Aim at a surface" : "";
            string percent = (surface.State.Amount * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
            return ActiveTarget == surface ? "Cleaning " + surface.DisplayName + "... " + percent :
                surface.DisplayName + " — " + surface.State.Category + " (" + percent + ")" +
                (HasHeldTool ? "\nHold [" + interactBinding + "] to clean" : "");
        }
    }
}
