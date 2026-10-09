using System.Text.Json;
using System.Text.Json.Serialization;
using Plumb.Core.Geometry;

namespace Plumb.Package;

/// <summary>
/// Reads geometry error codes written by newer versions as <see cref="GeometryError.Unknown"/>,
/// so a new code does not make the whole package unreadable.
/// </summary>
internal sealed class GeometryErrorConverter : JsonConverter<GeometryError>
{
    public override GeometryError Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && Enum.TryParse<GeometryError>(reader.GetString(), ignoreCase: false, out var error)
            ? error
            : GeometryError.Unknown;

    public override void Write(Utf8JsonWriter writer, GeometryError value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
