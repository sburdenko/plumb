using Xbim.Ifc4.Interfaces;

namespace Plumb.Import.Xbim;

/// <summary>
/// Resolves the display unit of a measure value from the project's default units,
/// for properties that do not carry an explicit unit.
/// </summary>
internal sealed class ProjectUnits
{
    private static readonly IReadOnlyDictionary<string, IfcUnitEnum> UnitTypeByMeasure = new Dictionary<string, IfcUnitEnum>
    {
        ["IfcLengthMeasure"] = IfcUnitEnum.LENGTHUNIT,
        ["IfcPositiveLengthMeasure"] = IfcUnitEnum.LENGTHUNIT,
        ["IfcNonNegativeLengthMeasure"] = IfcUnitEnum.LENGTHUNIT,
        ["IfcAreaMeasure"] = IfcUnitEnum.AREAUNIT,
        ["IfcVolumeMeasure"] = IfcUnitEnum.VOLUMEUNIT,
        ["IfcPlaneAngleMeasure"] = IfcUnitEnum.PLANEANGLEUNIT,
        ["IfcPositivePlaneAngleMeasure"] = IfcUnitEnum.PLANEANGLEUNIT,
        ["IfcMassMeasure"] = IfcUnitEnum.MASSUNIT,
        ["IfcTimeMeasure"] = IfcUnitEnum.TIMEUNIT,
        ["IfcThermodynamicTemperatureMeasure"] = IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT,
    };

    private readonly IReadOnlyDictionary<IfcUnitEnum, string> _symbolByUnitType;

    private ProjectUnits(IReadOnlyDictionary<IfcUnitEnum, string> symbolByUnitType)
    {
        _symbolByUnitType = symbolByUnitType;
    }

    public static ProjectUnits From(IIfcProject project)
    {
        var symbols = (project.UnitsInContext?.Units ?? Enumerable.Empty<IIfcUnit>())
            .OfType<IIfcNamedUnit>()
            .Where(unit => !string.IsNullOrEmpty(unit.Symbol))
            .GroupBy(unit => unit.UnitType)
            .ToDictionary(group => group.Key, group => group.First().Symbol);

        return new ProjectUnits(symbols);
    }

    public string? SymbolFor(IIfcValue? value)
    {
        if (value == null || !UnitTypeByMeasure.TryGetValue(value.GetType().Name, out var unitType))
        {
            return null;
        }

        return _symbolByUnitType.GetValueOrDefault(unitType);
    }

    public static string? SymbolOf(IIfcUnit? unit) => unit switch
    {
        IIfcNamedUnit named => named.Symbol,
        IIfcMonetaryUnit monetary => monetary.Currency.ToString(),
        _ => null,
    };
}
