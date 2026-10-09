using Plumb.Core.Geometry;

namespace Plumb.Geometry;

public interface IGeometryConverter
{
    /// <summary>
    /// Writes the geometry of <paramref name="ifcPath"/> to <paramref name="glbPath"/>, with nodes named by IFC GlobalId.
    /// Expected failures come back as <see cref="GeometryState.NotBuilt"/>; no partial file is left behind.
    /// </summary>
    /// <exception cref="OperationCanceledException">Cancellation was requested; the conversion was stopped.</exception>
    Task<GeometryState> ConvertAsync(string ifcPath, string glbPath, CancellationToken cancellationToken);
}
