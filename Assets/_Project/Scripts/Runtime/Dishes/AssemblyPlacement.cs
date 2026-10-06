using UnityEngine;

namespace ZeroStarRestaurant.Dishes
{
    // Geometry for the assembly-only snap. No food state, input or recipe rules.
    internal static class AssemblyPlacement
    {
        public static bool TryBounds(Rigidbody body, Quaternion rotation, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Collider collider in body.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || collider.isTrigger || collider.attachedRigidbody != body) continue;
                BoxCollider box = collider as BoxCollider;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                    Vector3 point = box != null
                        ? box.transform.TransformPoint(box.center + Vector3.Scale(box.size * 0.5f, sign))
                        : collider.bounds.center + Vector3.Scale(collider.bounds.extents, sign);
                    Vector3 rotated = rotation * (Quaternion.Inverse(body.transform.rotation) * (point - body.transform.position));
                    if (!found) { bounds = new Bounds(rotated, Vector3.zero); found = true; }
                    else bounds.Encapsulate(rotated);
                }
            }
            return found;
        }

        public static bool Fits(BoxCollider zone, Bounds proposed)
        {
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 sign = new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                Vector3 local = zone.transform.InverseTransformPoint(proposed.center + Vector3.Scale(proposed.extents, sign)) - zone.center;
                if (Mathf.Abs(local.x) > zone.size.x * 0.5f || Mathf.Abs(local.y) > zone.size.y * 0.5f || Mathf.Abs(local.z) > zone.size.z * 0.5f) return false;
            }
            return true;
        }

        public static bool IsClear(Bounds proposed, Rigidbody ignored)
        {
            Vector3 half = Vector3.Max(proposed.extents - Vector3.one * 0.001f, Vector3.one * 0.001f);
            foreach (Collider collider in Physics.OverlapBox(proposed.center, half, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (collider.attachedRigidbody != ignored) return false;
            return true;
        }
    }
}
