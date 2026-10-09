namespace Plumb.Core.Package
{
    /// <summary>
    /// One entry of <c>elements.json</c>: just enough for a 3D viewer to label a picked object.
    /// </summary>
    /// <param name="Id">IFC GlobalId, also the node name in the geometry file.</param>
    /// <param name="Storey">Storey name, not id; null above storeys.</param>
    public sealed record ElementIndexEntry(string Id, string Type, string? Name, string? Storey);
}
