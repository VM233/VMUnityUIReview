using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using VMUnityAutomation.Runtime;

namespace VMUnityAutomation.Editor
{
    public static class VmUiGlyphStrokeCapture
    {
        public static VmUiGlyphStrokeAudit.Glyph Capture(Mesh mesh, int penUvChannel = 0)
        {
            if (mesh == null || mesh.vertexCount is < 1 or > 4096 || penUvChannel is < 0 or > 7 ||
                mesh.subMeshCount is < 1 or > 16)
                throw new ArgumentException("Glyph capture requires one bounded native mesh and its actual pen UV channel.");
            int count = 0;
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                if (mesh.GetTopology(submesh) != MeshTopology.Triangles)
                    throw new ArgumentException(mesh.name + ":glyph pen capture requires native triangles.");
                count = checked(count + (int)mesh.GetIndexCount(submesh));
            }
            if (count > 24576) throw new ArgumentException(mesh.name + ":glyph exceeds24576 native indices.");
            using var snapshots = MeshUtility.AcquireReadOnlyMeshData(mesh);
            var data = snapshots[0];
            using var vertices = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp);
            using var uv = new NativeArray<Vector2>(data.vertexCount, Allocator.Temp);
            using var indices = new NativeArray<int>(count, Allocator.Temp);
            data.GetVertices(vertices);
            if (data.HasVertexAttribute(VertexAttribute.TexCoord0 + penUvChannel)) data.GetUVs(penUvChannel, uv);
            int offset = 0;
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                int length = data.GetSubMesh(submesh).indexCount;
                data.GetIndices(indices.GetSubArray(offset, length), submesh, true); offset += length;
            }
            return VmUiGlyphStrokeAudit.Capture(mesh.name, Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z),
                vertices.ToArray(), uv.ToArray(), indices.ToArray());
        }
    }
}
