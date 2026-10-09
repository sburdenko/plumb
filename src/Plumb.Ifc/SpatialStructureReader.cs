using Plumb.Core.Model;
using Xbim.Ifc4.Interfaces;

namespace Plumb.Ifc;

internal readonly record struct SpatialEntry(ElementRecord Record, IIfcObjectDefinition Source);

/// <summary>
/// Walks Project &gt; Site &gt; Building &gt; Storey &gt; Space, the elements contained in each
/// spatial element, and the parts those elements aggregate. Emits entries parents-first.
/// </summary>
internal sealed class SpatialStructureReader
{
    private readonly HashSet<string> _visited = [];
    private readonly List<SpatialEntry> _entries = [];
    private readonly CancellationToken _cancellationToken;

    private SpatialStructureReader(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
    }

    public static IReadOnlyList<SpatialEntry> Read(IIfcProject project, CancellationToken cancellationToken)
    {
        var reader = new SpatialStructureReader(cancellationToken);
        reader.Visit(project, parentGlobalId: null, storeyGlobalId: null);
        return reader._entries;
    }

    private void Visit(IIfcObjectDefinition definition, string? parentGlobalId, string? storeyGlobalId)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        var globalId = definition.GlobalId.ToString();
        if (!_visited.Add(globalId))
        {
            return;
        }

        var ownStorey = definition is IIfcBuildingStorey ? globalId : storeyGlobalId;
        var record = new ElementRecord(
            globalId,
            definition.ExpressType.ExpressName,
            definition.Name?.ToString(),
            parentGlobalId,
            ownStorey);
        _entries.Add(new SpatialEntry(record, definition));

        foreach (var child in ChildrenOf(definition))
        {
            Visit(child, globalId, ownStorey);
        }
    }

    private static IEnumerable<IIfcObjectDefinition> ChildrenOf(IIfcObjectDefinition definition)
    {
        var parts = definition.IsDecomposedBy
            .SelectMany(rel => rel.RelatedObjects)
            .OrderBy(StoreyElevation);

        var contained = definition is IIfcSpatialElement spatial
            ? spatial.ContainsElements.SelectMany(rel => rel.RelatedElements)
            : [];

        return parts.Concat(contained);
    }

    private static double StoreyElevation(IIfcObjectDefinition definition) =>
        definition is IIfcBuildingStorey { Elevation: { } elevation } ? elevation : 0d;
}
