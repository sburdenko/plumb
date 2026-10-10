namespace Plumb.App.Screens.Model.Properties;

public sealed record PropertyGroupViewModel(string Name, IReadOnlyList<PropertyRowViewModel> Rows);
