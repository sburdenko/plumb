using Plumb.Core.Model;

namespace Plumb.App.ViewModels;

public sealed record PropertyRowViewModel(string Name, string Value);

public sealed record PropertyGroupViewModel(string Name, IReadOnlyList<PropertyRowViewModel> Rows);

public sealed class ElementDetailsViewModel : ViewModelBase
{
    private const string NoValue = "—";

    public ElementDetailsViewModel(string name, ElementRecord element, string? storeyName, IEnumerable<PropertyRecord> properties)
    {
        Name = name;
        IfcType = element.IfcType;
        GlobalId = element.GlobalId;
        Storey = storeyName ?? NoValue;
        Groups = properties
            .GroupBy(p => p.Pset)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new PropertyGroupViewModel(
                g.Key,
                g.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Select(ToRow).ToList()))
            .ToList();
    }

    public string Name { get; }

    public string IfcType { get; }

    public string GlobalId { get; }

    public string Storey { get; }

    public IReadOnlyList<PropertyGroupViewModel> Groups { get; }

    public bool HasProperties => Groups.Count > 0;

    private static PropertyRowViewModel ToRow(PropertyRecord property)
    {
        var value = string.IsNullOrEmpty(property.Value) ? NoValue : property.Value;
        return new PropertyRowViewModel(property.Name, property.Unit == null ? value : $"{value} {property.Unit}");
    }
}
