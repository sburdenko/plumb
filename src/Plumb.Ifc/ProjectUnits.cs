using System.Globalization;
using Xbim.Ifc4.Interfaces;

namespace Plumb.Ifc;

/// <summary>
/// Resolves the display unit of a measure value from the project's default units,
/// for properties that do not carry an explicit unit, and the length scale for values Plumb stores in metres.
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

    private static readonly IReadOnlyDictionary<IfcSIPrefix, double> PrefixFactors = new Dictionary<IfcSIPrefix, double>
    {
        [IfcSIPrefix.EXA] = 1e18,
        [IfcSIPrefix.PETA] = 1e15,
        [IfcSIPrefix.TERA] = 1e12,
        [IfcSIPrefix.GIGA] = 1e9,
        [IfcSIPrefix.MEGA] = 1e6,
        [IfcSIPrefix.KILO] = 1e3,
        [IfcSIPrefix.HECTO] = 1e2,
        [IfcSIPrefix.DECA] = 1e1,
        [IfcSIPrefix.DECI] = 1e-1,
        [IfcSIPrefix.CENTI] = 1e-2,
        [IfcSIPrefix.MILLI] = 1e-3,
        [IfcSIPrefix.MICRO] = 1e-6,
        [IfcSIPrefix.NANO] = 1e-9,
        [IfcSIPrefix.PICO] = 1e-12,
        [IfcSIPrefix.FEMTO] = 1e-15,
        [IfcSIPrefix.ATTO] = 1e-18,
    };

    private readonly IReadOnlyDictionary<IfcUnitEnum, string> _symbolByUnitType;

    private ProjectUnits(IReadOnlyDictionary<IfcUnitEnum, string> symbolByUnitType, double lengthToMetres)
    {
        _symbolByUnitType = symbolByUnitType;
        LengthToMetres = lengthToMetres;
    }

    /// <summary>What one length unit of the project is in metres: 0.001 for millimetres, 0.3048 for feet.</summary>
    public double LengthToMetres { get; }

    public static ProjectUnits From(IIfcProject project)
    {
        var namedUnits = (project.UnitsInContext?.Units ?? Enumerable.Empty<IIfcUnit>()).OfType<IIfcNamedUnit>().ToList();
        var symbols = namedUnits
            .Where(unit => !string.IsNullOrEmpty(unit.Symbol))
            .GroupBy(unit => unit.UnitType)
            .ToDictionary(group => group.Key, group => group.First().Symbol);
        var length = namedUnits.FirstOrDefault(unit => unit.UnitType == IfcUnitEnum.LENGTHUNIT);

        return new ProjectUnits(symbols, MetresPer(length));
    }

    public string? SymbolFor(IIfcValue? value)
    {
        if (value == null || !UnitTypeByMeasure.TryGetValue(value.GetType().Name, out var unitType))
        {
            return null;
        }

        return _symbolByUnitType.GetValueOrDefault(unitType);
    }

    private static double MetresPer(IIfcUnit? unit) => unit switch
    {
        IIfcSIUnit si => si.Prefix is { } prefix ? PrefixFactors[prefix] : 1d,
        IIfcConversionBasedUnit converted =>
            Convert.ToDouble(converted.ConversionFactor.ValueComponent.Value, CultureInfo.InvariantCulture)
            * MetresPer(converted.ConversionFactor.UnitComponent),
        _ => 1d,
    };

    public static string? SymbolOf(IIfcUnit? unit) => unit switch
    {
        IIfcNamedUnit named => named.Symbol,
        IIfcMonetaryUnit monetary => monetary.Currency.ToString(),
        _ => null,
    };
}
