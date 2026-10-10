using Plumb.App.Shell;
using Plumb.Core.Model;

namespace Plumb.App.Screens.Model.Properties;

public sealed class ElementDetailsViewModel : ViewModelBase
{
    private const string NoValue = "—";

    /// <param name="globalId">The element's GlobalId, copied on click.</param>
    public ElementDetailsViewModel(ElementRecord element, string? storeyName, IEnumerable<PropertyRecord> properties, CopyableTextViewModel globalId)
    {
        Title = string.IsNullOrWhiteSpace(element.Name) ? element.IfcType : element.Name;
        Kicker = element.IfcType.ToUpperInvariant();
        GlobalId = globalId;
        TagSuffix = element.Tag == null ? string.Empty : $" · #{element.Tag}";
        Storey = storeyName;
        Groups = properties
            .GroupBy(p => (p.Pset, p.Source))
            .OrderBy(g => g.Key.Source)
            .ThenBy(g => g.Key.Pset, StringComparer.OrdinalIgnoreCase)
            .Select(g => new PropertyGroupViewModel(
                g.Key.Pset,
                SourceLabel(g.Key.Source),
                g.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Select(ToRow).ToList()))
            .ToList();
    }

    /// <summary>The IFC type in capitals, shown above the title.</summary>
    public string Kicker { get; }

    public string Title { get; }

    public CopyableTextViewModel GlobalId { get; }

    /// <summary>" · #Tag" after the GlobalId, or empty when the element has no tag.</summary>
    public string TagSuffix { get; }

    public string? Storey { get; }

    public IReadOnlyList<PropertyGroupViewModel> Groups { get; }

    public bool HasProperties => Groups.Count > 0;

    private static string SourceLabel(PropertySource source) => source switch
    {
        PropertySource.Instance => "INSTANCE",
        PropertySource.Type => "TYPE",
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown property source."),
    };

    private static PropertyRowViewModel ToRow(PropertyRecord property) =>
        new(property.Name, string.IsNullOrEmpty(property.Value) ? NoValue : property.Value, property.Unit);
}
