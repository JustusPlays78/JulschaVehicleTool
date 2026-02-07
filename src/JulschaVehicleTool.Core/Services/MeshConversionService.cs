using System.Numerics;
using CodeWalker.GameFiles;
using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Services;

/// <summary>
/// Converts CodeWalker FragDrawable/DrawableBase data into toolkit-independent MeshData.
/// Handles vertex extraction via VertexData component-index API and coordinate conversion.
/// </summary>
public class MeshConversionService
{
    /// <summary>
    /// Extract all LOD levels and textures from a YFT file.
    /// </summary>
    public VehicleModelData ConvertYft(YftFile yft, YtdFile? ytd = null)
    {
        var result = new VehicleModelData();

        var frag = yft.Fragment;
        if (frag?.Drawable == null) return result;

        var drawable = frag.Drawable;
        var models = drawable.DrawableModels;

        if (models != null)
        {
            if (models.High != null)
                result.High = ExtractLod("High", models.High, drawable.ShaderGroup);
            if (models.Med != null)
                result.Medium = ExtractLod("Medium", models.Med, drawable.ShaderGroup);
            if (models.Low != null)
                result.Low = ExtractLod("Low", models.Low, drawable.ShaderGroup);
            if (models.VLow != null)
                result.VeryLow = ExtractLod("VeryLow", models.VLow, drawable.ShaderGroup);
        }

        // Extract textures from YTD
        if (ytd?.TextureDict?.Textures?.data_items != null)
        {
            foreach (var tex in ytd.TextureDict.Textures.data_items)
            {
                if (tex?.Data?.FullData == null) continue;
                result.Textures.Add(new TextureInfo
                {
                    Name = tex.Name ?? "",
                    NameHash = tex.NameHash,
                    Width = tex.Width,
                    Height = tex.Height,
                    Stride = tex.Stride,
                    Format = tex.Format.ToString(),
                    DdsData = tex.Data.FullData,
                    Levels = tex.Levels
                });
            }
        }

        // Also check embedded textures in ShaderGroup
        if (ytd == null && drawable.ShaderGroup?.TextureDictionary?.Textures?.data_items != null)
        {
            foreach (var tex in drawable.ShaderGroup.TextureDictionary.Textures.data_items)
            {
                if (tex?.Data?.FullData == null) continue;
                result.Textures.Add(new TextureInfo
                {
                    Name = tex.Name ?? "",
                    NameHash = tex.NameHash,
                    Width = tex.Width,
                    Height = tex.Height,
                    Stride = tex.Stride,
                    Format = tex.Format.ToString(),
                    DdsData = tex.Data.FullData,
                    Levels = tex.Levels
                });
            }
        }

        return result;
    }

    private ModelLodData ExtractLod(string name, DrawableModel[] models, ShaderGroup? shaderGroup)
    {
        var lod = new ModelLodData { Name = name };

        foreach (var model in models)
        {
            if (model.Geometries == null) continue;

            foreach (var geom in model.Geometries)
            {
                var mesh = ExtractGeometry(geom, shaderGroup);
                if (mesh != null)
                    lod.Meshes.Add(mesh);
            }
        }

        return lod;
    }

    private MeshData? ExtractGeometry(DrawableGeometry geom, ShaderGroup? shaderGroup)
    {
        var vd = geom.VertexData;
        var ib = geom.IndexBuffer;
        if (vd == null || ib?.Indices == null || vd.VertexCount == 0)
            return null;

        var vertCount = vd.VertexCount;
        var positions = new Vector3[vertCount];
        var normals = new Vector3[vertCount];
        var texCoords = new Vector2[vertCount];

        // Component indices: 0=Position, 3=Normal, 6=TexCoord0
        for (int v = 0; v < vertCount; v++)
        {
            // Position (component 0) - always Vector3/Float3
            var pos = vd.GetVector3(v, 0);
            // GTA V Z-up to Y-up: swap Y and Z, negate new Z
            positions[v] = new Vector3(pos.X, pos.Z, -pos.Y);

            // Normal (component 3) - can be RGBA8SNorm or Float3
            try
            {
                var nrm = vd.GetVector3(v, 3);
                normals[v] = new Vector3(nrm.X, nrm.Z, -nrm.Y);
            }
            catch
            {
                normals[v] = Vector3.UnitY;
            }

            // TexCoord0 (component 6) - can be Float2 or Half2
            // GetVector2 returns SharpDX.Vector2, must convert to System.Numerics.Vector2
            try
            {
                var uv = vd.GetVector2(v, 6);
                texCoords[v] = new Vector2(uv.X, uv.Y);
            }
            catch
            {
                texCoords[v] = Vector2.Zero;
            }
        }

        // Convert indices from ushort to int
        var indices = new int[ib.Indices.Length];
        for (int i = 0; i < ib.Indices.Length; i++)
            indices[i] = ib.Indices[i];

        // Resolve texture name from shader
        string? textureName = null;
        if (shaderGroup?.Shaders?.data_items != null && geom.ShaderID < shaderGroup.Shaders.data_items.Length)
        {
            var shader = shaderGroup.Shaders.data_items[geom.ShaderID];
            textureName = ResolveTextureName(shader);
        }

        return new MeshData
        {
            Positions = positions,
            Normals = normals,
            TexCoords = texCoords,
            Indices = indices,
            ShaderIndex = geom.ShaderID,
            TextureName = textureName
        };
    }

    private string? ResolveTextureName(ShaderFX? shader)
    {
        if (shader?.ParametersList?.Parameters == null) return null;

        foreach (var param in shader.ParametersList.Parameters)
        {
            if (param.Data is TextureBase tex)
                return tex.Name;
        }

        return null;
    }
}
