using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plumb.App.Screens.Model.Filter;
using Plumb.App.Screens.Model.Viewport;
using Plumb.App.Shell;
using Plumb.Core.Clashes;
using Plumb.Core.Geometry;
using Plumb.Core.Import;
using Plumb.Core.Model;
using Plumb.Core.Search;

namespace Plumb.App.Screens.Model.Clashes;

/// <summary>
/// Two groups chosen with the tree search's words, a search for where their elements pass through each other,
/// and the clashes found.
/// </summary>
public sealed partial class ClashesViewModel : ViewModelBase
{
    private readonly PackageState.Saved? _searchable;
    private readonly IReadOnlyDictionary<string, ElementRecord> _elementsById;
    private readonly ElementSearch _search;
    private readonly IClashService _service;
    private readonly Action<string> _selectElement;
    private ElementMatches? _first;
    private ElementMatches? _second;

    /// <param name="selectElement">Selects an element in the tree, the properties and the viewer.</param>
    public ClashesViewModel(
        PackageState package,
        IReadOnlyList<ElementRecord> elements,
        ElementSearch search,
        IClashService service,
        Action<string> selectElement)
    {
        _elementsById = elements.ToDictionary(e => e.GlobalId, StringComparer.Ordinal);
        _search = search;
        _service = service;
        _selectElement = selectElement;
        _searchable = package is PackageState.Saved { Geometry: GeometryState.Built } saved ? saved : null;
        State = StateBeforeSearching(package);
    }

    [ObservableProperty]
    public partial string? FirstQuery { get; set; }

    [ObservableProperty]
    public partial string? SecondQuery { get; set; }

    /// <summary>How many elements the first group matches, or why its text is not a query yet; null while empty.</summary>
    [ObservableProperty]
    public partial FilterStatusViewModel? FirstStatus { get; private set; }

    [ObservableProperty]
    public partial FilterStatusViewModel? SecondStatus { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FindCommand))]
    public partial ClashesStateViewModel State { get; private set; }

    partial void OnFirstQueryChanged(string? value)
    {
        (_first, FirstStatus) = Match(value);
        FindCommand.NotifyCanExecuteChanged();
    }

    partial void OnSecondQueryChanged(string? value)
    {
        (_second, SecondStatus) = Match(value);
        FindCommand.NotifyCanExecuteChanged();
    }

    private bool CanFind() =>
        _searchable != null && State is not ClashesSearchingViewModel && _first is { Count: > 0 } && _second is { Count: > 0 };

    [RelayCommand(CanExecute = nameof(CanFind), IncludeCancelCommand = true)]
    private async Task FindAsync(CancellationToken cancellationToken)
    {
        if (_searchable == null || _first == null || _second == null)
        {
            return;
        }

        var searching = new ClashesSearchingViewModel();
        State = searching;
        try
        {
            var result = await _service.FindAsync(_searchable.Path, _first, _second, new Progress<ClashStep>(searching.Show), cancellationToken);
            State = StateFor(result);
        }
        catch (Exception ex)
        {
            State = new ClashesFailedViewModel($"The clash search stopped: {ex.Message}");
        }
    }

    private static ClashesStateViewModel StateBeforeSearching(PackageState package) => package switch
    {
        PackageState.Saved { Geometry: GeometryState.Built } => new ClashesIdleViewModel(),
        PackageState.Saved { Geometry: GeometryState.NotBuilt notBuilt } =>
            new ClashesUnavailableViewModel($"Clashes are found in the 3D geometry, and this model has none. {GeometryWarning.Reason(notBuilt)}"),
        PackageState.NotSaved => new ClashesUnavailableViewModel("Clashes are found in the 3D geometry kept in the package, and the package was not saved."),
        _ => throw new UnreachableException(),
    };

    private ClashesStateViewModel StateFor(ClashResult result) => result switch
    {
        ClashResult.Found { Clashes.Count: 0 } => new ClashesNoneViewModel(),
        ClashResult.Found found => new ClashesFoundViewModel(found.Clashes.Select(RowFor).ToList(), _selectElement),
        ClashResult.Failed failed => new ClashesFailedViewModel(failed.Message),
        ClashResult.Cancelled => new ClashesIdleViewModel(),
        _ => throw new UnreachableException(),
    };

    private ClashRowViewModel RowFor(Clash clash) =>
        new(clash, _elementsById.GetValueOrDefault(clash.FirstId), _elementsById.GetValueOrDefault(clash.SecondId));

    private (ElementMatches?, FilterStatusViewModel?) Match(string? text) => QueryParser.Parse(text) switch
    {
        ParseResult.Parsed parsed when _search.Find(parsed.Query) is var matches => (matches, new FilterMatchedViewModel(matches.Count)),
        ParseResult.Invalid invalid => (null, new FilterRejectedViewModel(invalid.Reason)),
        _ => (null, null),
    };
}
