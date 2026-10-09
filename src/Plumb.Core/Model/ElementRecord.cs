namespace Plumb.Core.Model
{
    /// <summary>
    /// An IFC object placed in the spatial structure.
    /// </summary>
    /// <param name="GlobalId">IFC GlobalId (22-character base64 GUID).</param>
    /// <param name="IfcType">Express name, e.g. <c>IfcWallStandardCase</c>.</param>
    /// <param name="ParentGlobalId">Spatial parent or aggregating whole; null for the project.</param>
    /// <param name="StoreyGlobalId">
    /// Nearest storey. A storey points to itself; objects above storeys (project, site, building) have null.
    /// </param>
    public sealed record ElementRecord(
        string GlobalId,
        string IfcType,
        string? Name,
        string? ParentGlobalId,
        string? StoreyGlobalId);
}
