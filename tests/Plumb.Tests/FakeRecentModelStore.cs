using Plumb.App.Screens.Start.Recent;

namespace Plumb.Tests;

internal sealed class FakeRecentModelStore : IRecentModelStore
{
    public RecentModelList Saved { get; private set; } = RecentModelList.Empty;

    public int SaveCount { get; private set; }

    public RecentModelList Load() => Saved;

    public void Save(RecentModelList models)
    {
        Saved = models;
        SaveCount++;
    }
}
