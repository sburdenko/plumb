using Plumb.Core.Geometry;
using Plumb.Geometry;

namespace Plumb.Tests;

/// <summary>
/// Stands in for IfcConvert in pipeline tests so they do not depend on, or wait for, the real converter.
/// </summary>
internal sealed class FakeGeometryConverter : IGeometryConverter
{
    private readonly Func<string, string, CancellationToken, Task<GeometryState>> _convert;

    private FakeGeometryConverter(Func<string, CancellationToken, Task<GeometryState>> convert)
        : this((_, glbPath, cancellationToken) => convert(glbPath, cancellationToken))
    {
    }

    private FakeGeometryConverter(Func<string, string, CancellationToken, Task<GeometryState>> convert)
    {
        _convert = convert;
    }

    public static FakeGeometryConverter Builds() =>
        new((glbPath, _) =>
        {
            File.WriteAllText(glbPath, "glTF");
            return Task.FromResult<GeometryState>(new GeometryState.Built());
        });

    /// <summary>Builds, and hands the input path to <paramref name="onStart"/> before doing so.</summary>
    public static FakeGeometryConverter BuildsAfter(Action<string> onStart) =>
        new FakeGeometryConverter((ifcPath, glbPath, _) =>
        {
            onStart(ifcPath);
            File.WriteAllText(glbPath, "glTF");
            return Task.FromResult<GeometryState>(new GeometryState.Built());
        });

    /// <summary>Never finishes on its own; completes only when cancelled, and records that it was.</summary>
    public static FakeGeometryConverter WaitsForCancellation(TaskCompletionSource cancelled) =>
        new FakeGeometryConverter(async (_, _, cancellationToken) =>
        {
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            finally
            {
                cancelled.TrySetResult();
            }

            return new GeometryState.Built();
        });

    public static FakeGeometryConverter Fails(GeometryError error, string detail) =>
        new((_, _) => Task.FromResult<GeometryState>(new GeometryState.NotBuilt(error, detail)));

    public Task<GeometryState> ConvertAsync(string ifcPath, string glbPath, CancellationToken cancellationToken) =>
        _convert(ifcPath, glbPath, cancellationToken);
}
