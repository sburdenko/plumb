
namespace Plumb.App.Screens.Start.Recent;

/// <summary>Keeps the recent models between runs.</summary>
public interface IRecentModelStore
{
    /// <summary>Returns the stored models, or none when nothing usable is stored.</summary>
    RecentModelList Load();

    /// <summary>Stores the models. Failures are logged, never thrown: losing the list must not stop the app.</summary>
    void Save(RecentModelList models);
}
