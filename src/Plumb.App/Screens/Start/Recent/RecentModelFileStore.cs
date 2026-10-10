using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Plumb.App.Screens.Start.Recent;

/// <summary>
/// Stores the recent models as JSON, by default in <c>Plumb/recent.json</c> under the user's local application data
/// (<c>~/Library/Application Support</c> on macOS, <c>%LOCALAPPDATA%</c> on Windows, <c>~/.local/share</c> on Linux).
/// </summary>
public sealed class RecentModelFileStore(string filePath, ILogger<RecentModelFileStore> logger) : IRecentModelStore
{
    private const int FormatVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify),
        "Plumb",
        "recent.json");

    public RecentModelList Load()
    {
        if (!File.Exists(filePath))
        {
            return RecentModelList.Empty;
        }

        try
        {
            var file = JsonSerializer.Deserialize<RecentFile>(File.ReadAllText(filePath), Options);
            if (file?.Version != FormatVersion)
            {
                logger.LogWarning("Ignoring {Path}: format version {Version} is not {Expected}", filePath, file?.Version, FormatVersion);
                return RecentModelList.Empty;
            }

            return RecentModelList.Of((file.Models ?? []).Select(ToModel).OfType<RecentModel>());
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Cannot read the recent models from {Path}", filePath);
            return RecentModelList.Empty;
        }
    }

    /// <summary>Writes a temporary file next to the target and renames it, so a crash never leaves half a list.</summary>
    public void Save(RecentModelList models)
    {
        var temporary = filePath + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var file = new RecentFile(FormatVersion, models.Models.Select(ToEntry).ToList());
            File.WriteAllText(temporary, JsonSerializer.Serialize(file, Options));
            File.Move(temporary, filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Cannot save the recent models to {Path}", filePath);
            TryDelete(temporary);
        }
    }

    private RecentModel? ToModel(Entry entry)
    {
        var source = IsAbsolute(entry.SourcePath) ? entry.SourcePath : null;
        var package = IsAbsolute(entry.PackagePath) ? entry.PackagePath : null;
        if ((source == null && package == null) || string.IsNullOrWhiteSpace(entry.Name))
        {
            logger.LogWarning("Skipping recent model {Name} ({Source}, {Package}): no absolute path or no name", entry.Name, entry.SourcePath, entry.PackagePath);
            return null;
        }

        return new RecentModel(source, package, entry.Name, entry.Schema ?? string.Empty, entry.ElementCount, entry.LastOpened, entry.IsPinned);
    }

    private static Entry ToEntry(RecentModel model) =>
        new(model.SourcePath, model.PackagePath, model.Name, model.Schema, model.ElementCount, model.LastOpened, model.IsPinned);

    private static bool IsAbsolute(string? path) => !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path);

    private void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not remove {Path}", path);
        }
    }

    private sealed record RecentFile(int Version, List<Entry>? Models);

    private sealed record Entry(
        string? SourcePath,
        string? PackagePath,
        string? Name,
        string? Schema,
        int ElementCount,
        DateTimeOffset LastOpened,
        bool IsPinned);
}
