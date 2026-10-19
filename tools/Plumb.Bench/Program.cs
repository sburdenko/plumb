using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Plumb.Core.Geometry;
using Plumb.Core.Import;
using Plumb.Core.Package;
using Plumb.Geometry;
using Plumb.Ifc;
using Plumb.Import;

// Imports one IFC file the way the app does, opens the package it wrote, and prints a row for the README.
// One file per run, so the peak memory belongs to that model alone:
//   dotnet run -c Release --project tools/Plumb.Bench -- samples/Duplex.ifc
// With "clashes", times clash detection on a package instead; see ClashBench.

const int OpenRuns = 5;

if (args is ["clashes", var package, var first, var second])
{
    return ClashBench.Run(Path.GetFullPath(package), first, second);
}

if (args is not [var ifcArgument] || !File.Exists(ifcArgument))
{
    Console.Error.WriteLine("Usage: Plumb.Bench <model.ifc>");
    return 2;
}

var ifc = Path.GetFullPath(ifcArgument);
var folder = Path.GetDirectoryName(ifc)!;
XbimSetup.Configure(NullLoggerFactory.Instance);
var service = new ImportService(
    NullLogger<ImportService>.Instance,
    new IfcConvertRunner(IfcConvertRunner.DefaultExecutablePath, IfcConvertRunner.DefaultTimeout, NullLogger<IfcConvertRunner>.Instance));
var noProgress = new Progress<ImportProgress>();

var clock = Stopwatch.StartNew();
var imported = await service.RunAsync(ifc, folder, noProgress, CancellationToken.None);
var importTime = clock.Elapsed;

if (imported is not ImportResult.Success { Package: PackageState.Saved saved } success)
{
    Console.Error.WriteLine($"Import did not produce a package: {imported}");
    return 1;
}

var openTimes = new List<TimeSpan>();
for (var run = 0; run < OpenRuns; run++)
{
    clock.Restart();
    if (await service.OpenPackageAsync(saved.Path, noProgress, CancellationToken.None) is not ImportResult.Success)
    {
        Console.Error.WriteLine("The package written by the import does not open.");
        return 1;
    }

    openTimes.Add(clock.Elapsed);
}

var model = success.Model;
var glb = Path.Combine(saved.Path, PackageLayout.GeometryFile);
var geometry = saved.Geometry switch
{
    GeometryState.Built => $"{Megabytes(new FileInfo(glb).Length)} MB",
    GeometryState.NotBuilt notBuilt => $"not built: {notBuilt.Error} {notBuilt.Detail}".TrimEnd(),
    _ => throw new UnreachableException(),
};

Console.WriteLine(string.Join(" | ", [
    "",
    Path.GetFileName(ifc),
    model.IfcSchema,
    $"{Megabytes(new FileInfo(ifc).Length)} MB",
    model.Elements.Count.ToString("N0", CultureInfo.InvariantCulture),
    model.Properties.Count.ToString("N0", CultureInfo.InvariantCulture),
    Seconds(importTime),
    geometry,
    Milliseconds(openTimes.Order().ElementAt(OpenRuns / 2)),
    PeakMemory.Of(children: false),
    PeakMemory.Of(children: true),
    "",
]).Trim());
return 0;

static string Megabytes(long bytes) => (bytes / 1e6).ToString("0.0", CultureInfo.InvariantCulture);

static string Seconds(TimeSpan time) => $"{time.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture)} s";

static string Milliseconds(TimeSpan time) => $"{time.TotalMilliseconds.ToString("0", CultureInfo.InvariantCulture)} ms";
