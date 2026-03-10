using System.Text.Json;
using System.Text.Json.Serialization;
using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Serialization;

/// <summary>
/// JSON converter for the polymorphic SubHandlingDataBase hierarchy.
/// Uses a "type" discriminator property to determine the concrete type.
/// </summary>
public class SubHandlingJsonConverter : JsonConverter<SubHandlingDataBase>
{
    public override SubHandlingDataBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp))
            throw new JsonException("SubHandlingData missing 'type' discriminator.");

        var typeName = typeProp.GetString();
        var rawJson = root.GetRawText();

        return typeName switch
        {
            "CCarHandlingData" => JsonSerializer.Deserialize<CCarHandlingData>(rawJson, options),
            "CBikeHandlingData" => JsonSerializer.Deserialize<CBikeHandlingData>(rawJson, options),
            "CBoatHandlingData" => JsonSerializer.Deserialize<CBoatHandlingData>(rawJson, options),
            "CFlyingHandlingData" => JsonSerializer.Deserialize<CFlyingHandlingData>(rawJson, options),
            _ => throw new JsonException($"Unknown SubHandlingData type: {typeName}")
        };
    }

    public override void Write(Utf8JsonWriter writer, SubHandlingDataBase value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.TypeName);

        // Serialize the concrete type's properties
        var concreteType = value.GetType();
        using var doc = JsonSerializer.SerializeToDocument(value, concreteType, options);

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            // Skip the TypeName property (we already wrote "type")
            if (prop.Name == "TypeName")
                continue;
            prop.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
