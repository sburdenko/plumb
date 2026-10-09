namespace Plumb.Core.Model
{
    /// <summary>
    /// A single property or quantity value of an element.
    /// </summary>
    /// <param name="Pset">Name of the property set or quantity set.</param>
    /// <param name="Value">Invariant-culture text; null when the property has no simple value.</param>
    /// <param name="Unit">Unit symbol, e.g. <c>m²</c>; null when unknown or unitless.</param>
    public sealed record PropertyRecord(
        string GlobalId,
        string Pset,
        string Name,
        string? Value,
        string? Unit);
}
