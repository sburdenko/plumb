using System.Reflection;

namespace Plumb.Tests;

[TestFixture]
public sealed class ArchitectureTests
{
    private static readonly Assembly Core = typeof(Plumb.Core.Model.ElementRecord).Assembly;
    private static readonly Assembly Ifc = typeof(Plumb.Ifc.XbimSetup).Assembly;
    private static readonly Assembly Geometry = typeof(Plumb.Geometry.IGeometryConverter).Assembly;
    private static readonly Assembly Package = typeof(Plumb.Package.PackageReader).Assembly;
    private static readonly Assembly Import = typeof(Plumb.Import.ImportService).Assembly;
    private static readonly Assembly App = typeof(Plumb.App.App).Assembly;

    [Test]
    public void Core_depends_on_no_other_Plumb_assembly() =>
        AssertNoPlumbReferences(Core);

    [Test]
    public void Ifc_Geometry_and_Package_depend_only_on_Core()
    {
        AssertPlumbReferences(Ifc, "Plumb.Core");
        AssertPlumbReferences(Geometry, "Plumb.Core");
        AssertPlumbReferences(Package, "Plumb.Core");
    }

    [Test]
    public void Import_assembles_the_pipeline_from_the_leaf_assemblies() =>
        AssertPlumbReferences(Import, "Plumb.Core", "Plumb.Ifc", "Plumb.Geometry", "Plumb.Package");

    [Test]
    public void App_talks_only_to_Import_and_Core() =>
        AssertPlumbReferences(App, "Plumb.Import", "Plumb.Core");

    [Test]
    public void Only_Ifc_references_Xbim()
    {
        AssertReferences(Ifc, "Xbim.");
        foreach (var other in new[] { Core, Geometry, Package, Import, App })
            AssertDoesNotReference(other, "Xbim.");
    }

    [Test]
    public void Only_App_references_Avalonia()
    {
        AssertReferences(App, "Avalonia");
        foreach (var other in new[] { Core, Ifc, Geometry, Package, Import })
            AssertDoesNotReference(other, "Avalonia");
    }

    [Test]
    public void Only_Package_references_Sqlite()
    {
        AssertReferences(Package, "Microsoft.Data.Sqlite");
        foreach (var other in new[] { Core, Ifc, Geometry, Import, App })
            AssertDoesNotReference(other, "Microsoft.Data.Sqlite");
    }

    [Test]
    public void Core_names_outcomes_as_states_not_as_boolean_flags()
    {
        var flags = PublicInstanceProperties(Core).Where(property => property.PropertyType == typeof(bool));
        Assert.That(flags.Select(Describe), Is.Empty, "model a yes/no outcome as a state type with a reason instead");
    }

    [Test]
    public void Core_values_cannot_change_after_construction()
    {
        var mutable = PublicInstanceProperties(Core).Where(IsMutable);
        Assert.That(mutable.Select(Describe), Is.Empty, "Core records are read-only; use init or a constructor");
    }

    private static IEnumerable<PropertyInfo> PublicInstanceProperties(Assembly assembly) =>
        assembly.GetExportedTypes().SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));

    private static bool IsMutable(PropertyInfo property) =>
        property.SetMethod is { IsPublic: true } setter
        && !setter.ReturnParameter.GetRequiredCustomModifiers().Any(modifier => modifier.Name == "IsExternalInit");

    private static string Describe(PropertyInfo property) => $"{property.DeclaringType?.Name}.{property.Name}";

    private static void AssertNoPlumbReferences(Assembly assembly) =>
        Assert.That(PlumbReferences(assembly), Is.Empty, $"{assembly.GetName().Name} must not depend on other Plumb assemblies");

    private static void AssertPlumbReferences(Assembly assembly, params string[] allowed) =>
        Assert.That(PlumbReferences(assembly), Is.EquivalentTo(allowed), $"{assembly.GetName().Name} has an unexpected Plumb dependency set");

    private static void AssertReferences(Assembly assembly, string prefix) =>
        Assert.That(References(assembly), Has.Some.StartsWith(prefix), $"{assembly.GetName().Name} should reference {prefix}");

    private static void AssertDoesNotReference(Assembly assembly, string prefix) =>
        Assert.That(References(assembly), Has.None.StartsWith(prefix), $"{assembly.GetName().Name} must not reference {prefix}");

    private static IReadOnlyList<string> PlumbReferences(Assembly assembly) =>
        References(assembly).Where(name => name.StartsWith("Plumb.", StringComparison.Ordinal)).ToList();

    private static IReadOnlyList<string> References(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? "").ToList();
}
