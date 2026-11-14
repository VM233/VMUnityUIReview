using System;
using UnityEngine;

namespace VMUnityAutomation.Runtime
{
    public static class VmUiProjectedMarkerGeometry
    {
        public static Rect ProjectMeshBounds(Camera camera, MeshFilter filter)
        {
            if (camera == null || filter == null || filter.sharedMesh == null)
                throw new ArgumentException("Native mesh projection requires the actual camera, transform and mesh.");
            var bounds = filter.sharedMesh.bounds; var transform = filter.transform;
            var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int index = 0; index < 8; index++)
            {
                var local = new Vector3((index & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (index & 2) == 0 ? bounds.min.y : bounds.max.y, (index & 4) == 0 ? bounds.min.z : bounds.max.z);
                var projected = camera.WorldToScreenPoint(transform.TransformPoint(local));
                var point = new Vector2(projected.x, projected.y);
                minimum = Vector2.Min(minimum, point); maximum = Vector2.Max(maximum, point);
            }
            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }
    }
}
