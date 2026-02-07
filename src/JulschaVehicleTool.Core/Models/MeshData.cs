using System.Numerics;

namespace JulschaVehicleTool.Core.Models;

/// <summary>
/// Intermediate mesh representation, independent of any 3D toolkit.
/// Converted from CodeWalker data in Core, then mapped to HelixToolkit in App.
/// </summary>
public class MeshData
{
    public Vector3[] Positions { get; set; } = [];
    public Vector3[] Normals { get; set; } = [];
    public Vector2[] TexCoords { get; set; } = [];
    public int[] Indices { get; set; } = [];
    public int ShaderIndex { get; set; }
    public string? TextureName { get; set; }
}

public class ModelLodData
{
    public string Name { get; set; } = "";
    public List<MeshData> Meshes { get; set; } = new();
}

public class VehicleModelData
{
    public ModelLodData? High { get; set; }
    public ModelLodData? Medium { get; set; }
    public ModelLodData? Low { get; set; }
    public ModelLodData? VeryLow { get; set; }
    public List<TextureInfo> Textures { get; set; } = new();
}

public class TextureInfo
{
    public string Name { get; set; } = "";
    public uint NameHash { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Stride { get; set; }
    public string Format { get; set; } = "";
    public byte[] DdsData { get; set; } = [];
    public byte Levels { get; set; }
}
