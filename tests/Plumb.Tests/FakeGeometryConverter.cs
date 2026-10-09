using Plumb.Core.Geometry;
using Plumb.Geometry;

namespace Plumb.Tests;

/// <summary>
/// Stands in for IfcConvert in pipeline tests so they do not depend on, or wait for, the real converter.
/// </summary>
internal sealed class FakeGeometryConverter : IGeometryConverter
{
    private readonly Func<string, CancellationToken, Task<GeometryState>> _convert;

    private FakeGeometryConverter(Func<string, CancellationToken, Task<GeometryState>> convert)
    {
        _convert = convert;
    }

    public static FakeGeometryConverter Builds() =>
        new((glbPath, _) =>
        {
            File.WriteAllText(glbPath, "glTF");
            return Task.FromResult<GeometryState>(new GeometryState.Built());
        });

    public static FakeGeometryConverter Fails(GeometryError error, string reason) =>
        new((_, _) => Task.FromResult<GeometryState>(new GeometryState.NotBuilt(error, reason)));

    public Task<GeometryState> ConvertAsync(string ifcPath, string glbPath, CancellationToken cancellationToken) =>
        _convert(glbPath, cancellationToken);
}
