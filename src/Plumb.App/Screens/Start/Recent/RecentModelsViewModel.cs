using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plumb.App.Platform;
using Plumb.App.Screens.Model;
using Plumb.App.Shell;

namespace Plumb.App.Screens.Start.Recent;

/// <summary>
/// The recent models on the start screen: pinned ones, then the ten most recent, all of them on request or when filtering.
/// </summary>
public sealed partial class RecentModelsViewModel : ViewModelBase
{
    /// <summary>Unpinned models shown before "Show all"; a longer list also gets a filter.</summary>
    public const int ShownByDefault = 10;

    private readonly IRecentModelStore _store;
    private readonly IAsyncRelayCommand<string?> _openPath;
    private readonly IFileRevealer _revealer;
    private readonly TimeProvider _clock;
    private RecentModelList _models;
    private int _unpinnedCount;

    public RecentModelsViewModel(IRecentModelStore store, IAsyncRelayCommand<string?> openPath, IFileRevealer revealer, TimeProvider clock)
    {
        _store = store;
        _openPath = openPath;
        _revealer = revealer;
        _clock = clock;
        _models = store.Load();
        Rebuild();
    }

    [ObservableProperty]
    public partial string? Filter { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<RecentModelItemViewModel> Pinned { get; private set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<RecentModelItemViewModel> Recent { get; private set; } = [];

    [ObservableProperty]
    public partial int HiddenCount { get; private set; }

    [ObservableProperty]
    public partial string? ActionError { get; private set; }

    public bool IsEmpty => _models.Models.Count == 0;

    public bool HasPinned => Pinned.Count > 0;

    public bool HasRecent => Recent.Count > 0;

    public bool CanFilter => _models.Models.Count > ShownByDefault;

    public bool HasHidden => HiddenCount > 0;

    public bool HasNoMatches => IsFiltering && !HasPinned && !HasRecent;

    public string ShowAllText => $"Show all {_unpinnedCount}";

    internal string RevealLabel => _revealer.ActionLabel;

    private bool IsFiltering => !string.IsNullOrWhiteSpace(Filter);

    private bool ShowsAll { get; set; }

    public void Record(LoadedModel loaded) => Update(_models.Record(RecentModel.From(loaded, _clock.GetUtcNow())));

    /// <summary>Checks again which files exist and how long ago each model was opened.</summary>
    public void Refresh()
    {
        ActionError = null;
        Rebuild();
    }

    internal Task OpenAsync(string path) => _openPath.CanExecute(path) ? _openPath.ExecuteAsync(path) : Task.CompletedTask;

    internal void TogglePin(RecentModel model) => Update(_models.SetPinned(model, !model.IsPinned));

    internal void Remove(RecentModel model) => Update(_models.Remove(model));

    internal void Reveal(string path) => ActionError = _revealer.TryReveal(path, out var error) ? null : error;

    partial void OnFilterChanged(string? value) => Rebuild();

    [RelayCommand]
    private void ShowAll()
    {
        ShowsAll = true;
        Rebuild();
    }

    [RelayCommand]
    private void ClearFilter() => Filter = null;

    private void Update(RecentModelList models)
    {
        _models = models;
        _store.Save(models);
        Rebuild();
    }

    private void Rebuild()
    {
        var now = _clock.GetLocalNow();
        var matching = _models.Models.Where(Matches).ToList();
        var unpinned = matching.Where(model => !model.IsPinned).ToList();
        var shown = IsFiltering || ShowsAll ? unpinned : unpinned.Take(ShownByDefault).ToList();

        _unpinnedCount = unpinned.Count;
        Pinned = matching.Where(model => model.IsPinned).Select(model => Item(model, now)).ToList();
        Recent = shown.Select(model => Item(model, now)).ToList();
        HiddenCount = unpinned.Count - shown.Count;

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasPinned));
        OnPropertyChanged(nameof(HasRecent));
        OnPropertyChanged(nameof(CanFilter));
        OnPropertyChanged(nameof(HasHidden));
        OnPropertyChanged(nameof(HasNoMatches));
        OnPropertyChanged(nameof(ShowAllText));
    }

    private bool Matches(RecentModel model) =>
        !IsFiltering
        || model.Name.Contains(Filter!.Trim(), StringComparison.OrdinalIgnoreCase)
        || model.Folder.Contains(Filter!.Trim(), StringComparison.OrdinalIgnoreCase);

    private RecentModelItemViewModel Item(RecentModel model, DateTimeOffset now) =>
        new(model, OpenTarget.For(model), RelativeTime.Format(model.LastOpened, now), this);
}
