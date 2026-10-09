using System.Collections.Generic;

namespace Plumb.Core.Model
{
    /// <summary>
    /// Everything read from one IFC file. Elements are ordered parents-first.
    /// </summary>
    public sealed record IfcModelData(
        string SourceFile,
        string IfcSchema,
        IReadOnlyList<ElementRecord> Elements,
        IReadOnlyList<PropertyRecord> Properties);
}
