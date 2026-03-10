using System.Text.Json;
using System.Text.Json.Serialization;

namespace JulschaVehicleTool.Core.Serialization;

/// <summary>
/// Central JSON serializer options configured with all custom converters
/// needed for Project serialization/deserialization.
/// </summary>
public static class ProjectJsonOptions
{
    private static JsonSerializerOptions? _instance;

    public static JsonSerializerOptions Default => _instance ??= Create();

    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        options.Converters.Add(new Vector3JsonConverter());
        options.Converters.Add(new Vector2JsonConverter());
        options.Converters.Add(new SubHandlingJsonConverter());

        return options;
    }
}
