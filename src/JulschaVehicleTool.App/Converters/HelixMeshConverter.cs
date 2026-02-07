using System.Numerics;
using HelixToolkit;
using JulschaVehicleTool.Core.Models;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;

namespace JulschaVehicleTool.App.Converters;

/// <summary>
/// Converts toolkit-independent MeshData to HelixToolkit MeshGeometry3D.
/// HelixToolkit v3 uses System.Numerics vectors, not SharpDX vectors.
/// </summary>
public static class HelixMeshConverter
{
    public static MeshGeometry3D ToHelixMesh(MeshData mesh)
    {
        var positions = new Vector3Collection(mesh.Positions.Length);
        var normals = new Vector3Collection(mesh.Normals.Length);
        var texCoords = new Vector2Collection(mesh.TexCoords.Length);
        var indices = new IntCollection(mesh.Indices);

        foreach (var p in mesh.Positions)
            positions.Add(new Vector3(p.X, p.Y, p.Z));

        foreach (var n in mesh.Normals)
            normals.Add(new Vector3(n.X, n.Y, n.Z));

        foreach (var uv in mesh.TexCoords)
            texCoords.Add(new Vector2(uv.X, uv.Y));

        return new MeshGeometry3D
        {
            Positions = positions,
            Normals = normals,
            TextureCoordinates = texCoords,
            Indices = indices
        };
    }
}
