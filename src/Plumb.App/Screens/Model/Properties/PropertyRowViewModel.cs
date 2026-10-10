namespace Plumb.App.Screens.Model.Properties;

/// <param name="Unit">Shown after the value in a muted colour; null when unitless.</param>
public sealed record PropertyRowViewModel(string Key, string Value, string? Unit)
{
    /// <summary>The unit with its leading space, or empty; shown as a second run after the value.</summary>
    public string UnitSuffix => Unit == null ? string.Empty : " " + Unit;

    public string FullValue => Value + UnitSuffix;
}
