using Plumb.Core.Model;

namespace Plumb.App.ViewModels;

/// <param name="Unit">Shown after the value in a muted colour; null when unitless.</param>
public sealed record PropertyRowViewModel(string Key, string Value, string? Unit)
{
    /// <summary>The unit with its leading space, or empty; shown as a second run after the value.</summary>
    public string UnitSuffix => Unit == null ? string.Empty : " " + Unit;

    public string FullValue => Value + UnitSuffix;
}

public sealed record PropertyGroupViewModel(string Name, IReadOnlyList<PropertyRowViewModel> Rows);

public sealed class ElementDetailsViewModel : ViewModelBase
{
    private const string NoValue = "—";

    public ElementDetailsViewModel(ElementRecord element, string? storeyName, IEnumerable<PropertyRecord> properties)
    {
        Title = string.IsNullOrWhiteSpace(element.Name) ? element.IfcType : element.Name;
        Kicker = element.IfcType.ToUpperInvariant();
        GlobalId = element.GlobalId;
        Storey = storeyName;
        Groups = properties
            .GroupBy(p => p.Pset)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new PropertyGroupViewModel(
                g.Key,
                g.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Select(ToRow).ToList()))
            .ToList();
    }

    /// <summary>The IFC type in capitals, shown above the title.</summary>
    public string Kicker { get; }

    public string Title { get; }

    public string GlobalId { get; }

    public string? Storey { get; }

    public IReadOnlyList<PropertyGroupViewModel> Groups { get; }

    public bool HasProperties => Groups.Count > 0;

    private static PropertyRowViewModel ToRow(PropertyRecord property) =>
        new(property.Name, string.IsNullOrEmpty(property.Value) ? NoValue : property.Value, property.Unit);
}
