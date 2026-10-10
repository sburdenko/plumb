using Plumb.Core.Model;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;

namespace Plumb.Ifc;

/// <summary>
/// Flattens instance and type property sets and element quantities into <see cref="PropertyRecord"/>s.
/// Instance values win over type values with the same set and name.
/// </summary>
internal sealed class PropertyExtractor
{
    private const string ListSeparator = "; ";

    private readonly ProjectUnits _units;

    public PropertyExtractor(ProjectUnits units)
    {
        _units = units;
    }

    public IEnumerable<PropertyRecord> Extract(IIfcObjectDefinition definition)
    {
        if (definition is not IIfcObject ifcObject)
        {
            return [];
        }

        var globalId = ifcObject.GlobalId.ToString();
        var instanceRecords = ifcObject.IsDefinedBy
            .SelectMany(rel => Expand(rel.RelatingPropertyDefinition))
            .SelectMany(set => ReadSet(globalId, set));
        var typeRecords = ifcObject.IsTypedBy
            .SelectMany(rel => rel.RelatingType.HasPropertySets)
            .SelectMany(set => ReadSet(globalId, set))
            .Select(record => record with { Source = PropertySource.Type });

        return instanceRecords
            .Concat(typeRecords)
            .DistinctBy(record => (record.Pset, record.Name));
    }

    private static IEnumerable<IIfcPropertySetDefinition> Expand(IIfcPropertySetDefinitionSelect? select) => select switch
    {
        IIfcPropertySetDefinition single => [single],
        IfcPropertySetDefinitionSet set => set.PropertySetDefinitions,
        _ => [],
    };

    private IEnumerable<PropertyRecord> ReadSet(string globalId, IIfcPropertySetDefinition set)
    {
        var setName = set.Name?.ToString() ?? set.ExpressType.ExpressName;

        return set switch
        {
            IIfcPropertySet properties => properties.HasProperties
                .Select(property => ReadProperty(globalId, setName, property)),
            IIfcElementQuantity quantities => quantities.Quantities
                .Select(quantity => ReadQuantity(globalId, setName, quantity)),
            _ => [],
        };
    }

    private PropertyRecord ReadProperty(string globalId, string setName, IIfcProperty property)
    {
        var name = property.Name.ToString();

        return property switch
        {
            IIfcPropertySingleValue single => Record(globalId, setName, name, single.NominalValue, single.Unit),
            IIfcPropertyEnumeratedValue enumerated => new PropertyRecord(
                globalId, setName, name, JoinValues(enumerated.EnumerationValues), null),
            IIfcPropertyListValue list => new PropertyRecord(
                globalId, setName, name, JoinValues(list.ListValues), UnitOf(list.ListValues.FirstOrDefault(), list.Unit)),
            IIfcPropertyBoundedValue bounded => new PropertyRecord(
                globalId, setName, name, FormatRange(bounded), UnitOf(bounded.LowerBoundValue ?? bounded.UpperBoundValue, bounded.Unit)),
            _ => new PropertyRecord(globalId, setName, name, null, null),
        };
    }

    private PropertyRecord ReadQuantity(string globalId, string setName, IIfcPhysicalQuantity quantity)
    {
        var name = quantity.Name.ToString();

        return quantity switch
        {
            IIfcQuantityLength length => Record(globalId, setName, name, length.LengthValue, length.Unit),
            IIfcQuantityArea area => Record(globalId, setName, name, area.AreaValue, area.Unit),
            IIfcQuantityVolume volume => Record(globalId, setName, name, volume.VolumeValue, volume.Unit),
            IIfcQuantityWeight weight => Record(globalId, setName, name, weight.WeightValue, weight.Unit),
            IIfcQuantityTime time => Record(globalId, setName, name, time.TimeValue, time.Unit),
            IIfcQuantityCount count => Record(globalId, setName, name, count.CountValue, count.Unit),
            _ => new PropertyRecord(globalId, setName, name, null, null),
        };
    }

    private PropertyRecord Record(string globalId, string setName, string name, IIfcValue? value, IIfcUnit? unit) =>
        new(globalId, setName, name, IfcValueFormatter.Format(value), UnitOf(value, unit));

    private string? UnitOf(IIfcValue? value, IIfcUnit? explicitUnit) =>
        explicitUnit != null ? ProjectUnits.SymbolOf(explicitUnit) : _units.SymbolFor(value);

    private static string JoinValues(IEnumerable<IIfcValue> values) =>
        string.Join(ListSeparator, values.Select(IfcValueFormatter.Format).OfType<string>());

    private static string? FormatRange(IIfcPropertyBoundedValue bounded)
    {
        var lower = IfcValueFormatter.Format(bounded.LowerBoundValue);
        var upper = IfcValueFormatter.Format(bounded.UpperBoundValue);
        return lower == null && upper == null ? null : $"{lower ?? "…"} – {upper ?? "…"}";
    }
}
