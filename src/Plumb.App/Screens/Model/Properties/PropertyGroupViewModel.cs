namespace Plumb.App.Screens.Model.Properties;

/// <param name="Source">"INSTANCE" or "TYPE": whether the values are set on the element or inherited from its type.</param>
public sealed record PropertyGroupViewModel(string Name, string Source, IReadOnlyList<PropertyRowViewModel> Rows);
