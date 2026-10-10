using System.Collections.Immutable;

namespace Plumb.App.Screens.Start.Recent;

/// <summary>
/// Recently opened models, pinned first, then newest first. Every change returns a new list.
/// </summary>
public sealed class RecentModelList
{
    /// <summary>How many unpinned models are kept; pinned ones are never dropped.</summary>
    public const int MaxUnpinned = 50;

    private RecentModelList(ImmutableArray<RecentModel> models) => Models = models;

    public static RecentModelList Empty { get; } = new([]);

    public IReadOnlyList<RecentModel> Models { get; }

    /// <summary>Builds a list from stored entries, merging entries that describe the same model.</summary>
    public static RecentModelList Of(IEnumerable<RecentModel> models) =>
        models.OrderBy(model => model.LastOpened).Aggregate(Empty, (list, model) => list.Record(model));

    /// <summary>Puts <paramref name="opened"/> in place of every entry for the same files; a pinned model stays pinned.</summary>
    public RecentModelList Record(RecentModel opened)
    {
        var replaced = Models.Where(model => model.SharesLocationWith(opened)).ToList();
        var merged = opened with { IsPinned = opened.IsPinned || replaced.Any(model => model.IsPinned) };
        return Arrange(Models.Where(model => !model.SharesLocationWith(opened)).Append(merged));
    }

    public RecentModelList SetPinned(RecentModel model, bool pinned) =>
        Arrange(Models.Select(entry => entry == model ? entry with { IsPinned = pinned } : entry));

    public RecentModelList Remove(RecentModel model) => Arrange(Models.Where(entry => entry != model));

    private static RecentModelList Arrange(IEnumerable<RecentModel> models)
    {
        var newestFirst = models.OrderByDescending(model => model.LastOpened).ToList();
        var pinned = newestFirst.Where(model => model.IsPinned);
        var unpinned = newestFirst.Where(model => !model.IsPinned).Take(MaxUnpinned);
        return new RecentModelList([.. pinned, .. unpinned]);
    }
}
