using System.Globalization;
using Plumb.App.Screens.Model.Tree;
using Plumb.Core.Clashes;
using Plumb.Core.Model;

namespace Plumb.App.Screens.Model.Clashes;

/// <summary>One clash: the two elements, by name without the Revit family, and how far they pass into each other.</summary>
public sealed class ClashRowViewModel(Clash clash, ElementRecord? first, ElementRecord? second)
{
    public string FirstId => clash.FirstId;

    public string SecondId => clash.SecondId;

    public string Penetration => $"{(clash.Penetration * 100).ToString("0.0", CultureInfo.InvariantCulture)} cm";

    public string FirstName => NameOf(first, clash.FirstId);

    public string FirstType => first?.IfcType ?? string.Empty;

    public string SecondName => NameOf(second, clash.SecondId);

    public string SecondType => second?.IfcType ?? string.Empty;

    /// <summary>Both elements in full with their IFC classes, for the row's tooltip.</summary>
    public string Description => $"{FullName(first, clash.FirstId)}\n× {FullName(second, clash.SecondId)}\n{Penetration} into each other";

    private static string FullName(ElementRecord? element, string globalId) =>
        element == null ? globalId : $"{element.Name ?? element.IfcType} ({element.IfcType})";

    private static string NameOf(ElementRecord? element, string globalId) =>
        element?.Name is { Length: > 0 } name ? ElementLabel.WithoutFamily(name) : element?.IfcType ?? globalId;
}
