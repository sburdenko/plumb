using System.Diagnostics;
using System.Globalization;
using Plumb.Core.Clashes;
using Plumb.Core.Package;
using Plumb.Core.Search;
using Plumb.Package;

/// <summary>
/// Times clash detection on a package between two groups chosen with the search box's language, and prints a row
/// for the README followed by the deepest clashes:
///   dotnet run -c Release --project tools/Plumb.Bench -- clashes model.plumb "duct" "pipe"
/// </summary>
internal static class ClashBench
{
    private const float Tolerance = 0.01f;
    private const int Shown = 5;

    public static int Run(string packagePath, string firstQuery, string secondQuery)
    {
        var contents = PackageReader.Read(packagePath, CancellationToken.None);
        var search = new ElementSearch(contents.Model.Elements, contents.Model.Properties);
        if (!TryFind(search, firstQuery, out var firstIds) || !TryFind(search, secondQuery, out var secondIds))
        {
            return 2;
        }

        var clock = Stopwatch.StartNew();
        var shapes = GlbShapes.Read(Path.Combine(packagePath, PackageLayout.GeometryFile));
        var readTime = clock.Elapsed;

        var first = shapes.Where(shape => firstIds.Contains(shape.GlobalId)).ToList();
        var second = shapes.Where(shape => secondIds.Contains(shape.GlobalId)).ToList();
        clock.Restart();
        var clashes = ClashDetector.Find(first, second, Tolerance, CancellationToken.None);
        var findTime = clock.Elapsed;

        var names = contents.Model.Elements.ToDictionary(e => e.GlobalId, e => e.Name ?? e.IfcType);
        Console.WriteLine(string.Join(" | ", [
            "",
            Path.GetFileName(Path.TrimEndingDirectorySeparator(packagePath)),
            $"`{firstQuery}` × `{secondQuery}`",
            string.Create(CultureInfo.InvariantCulture, $"{first.Count:N0} × {second.Count:N0}"),
            shapes.Sum(s => s.Triangles.Count).ToString("N0", CultureInfo.InvariantCulture),
            Milliseconds(readTime),
            Milliseconds(findTime),
            clashes.Count.ToString("N0", CultureInfo.InvariantCulture),
            "",
        ]).Trim());
        foreach (var clash in clashes.Take(Shown))
        {
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  {clash.Penetration * 100:0.0} cm  {names[clash.FirstId]}  ×  {names[clash.SecondId]}"));
        }

        return 0;
    }

    private static bool TryFind(ElementSearch search, string text, out ElementMatches matches)
    {
        if (QueryParser.Parse(text) is ParseResult.Parsed parsed)
        {
            matches = search.Find(parsed.Query);
            return true;
        }

        Console.Error.WriteLine($"Not a search: {text}");
        matches = null!;
        return false;
    }

    private static string Milliseconds(TimeSpan time) => $"{time.TotalMilliseconds.ToString("0", CultureInfo.InvariantCulture)} ms";
}
