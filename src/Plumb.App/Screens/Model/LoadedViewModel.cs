using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plumb.App.Formatting;
using Plumb.App.Platform;
using Plumb.App.Screens.Model.Properties;
using Plumb.App.Screens.Model.Tree;
using Plumb.App.Screens.Model.Viewport;
using Plumb.App.Shell;
using Plumb.Core.Geometry;
using Plumb.Core.Import;
using Plumb.Core.Model;
using Plumb.Core.Package;

namespace Plumb.App.Screens.Model;

/// <summary>
/// The loaded model: header, viewport, status bar and the selected element's properties. The tree and its search
/// box are <see cref="ModelTreeViewModel"/>.
/// </summary>
public sealed partial class LoadedViewModel : ScreenViewModel
{
    private readonly ILookup<string, PropertyRecord> _propertiesByElement;
    private readonly IReadOnlyDictionary<string, ElementRecord> _elementsById;
    private readonly PlatformServices _platform;
    private readonly TimeProvider _clock;
    private readonly PackageState _package;

    public LoadedViewModel(LoadedModel loaded, OpenCommands open, PlatformServices platform, TimeProvider clock)
    {
        var model = loaded.Result.Model;
        _propertiesByElement = model.Properties.ToLookup(p => p.GlobalId);
        _elementsById = model.Elements.ToDictionary(e => e.GlobalId);
        _package = loaded.Result.Package;
        _platform = platform;
        _clock = clock;
        Open = open;

        FileName = Path.GetFileName(Path.TrimEndingDirectorySeparator(loaded.OpenedPath));
        ProjectName = ProjectNameOf(model.Elements);
        Schema = model.IfcSchema;
        ElementCount = model.Elements.Count.ToString("N0", CultureInfo.InvariantCulture);
        LoadLabel = loaded.OpenedPackage ? "Opened in" : "Imported in";
        LoadTime = Durations.Format(loaded.LoadTime);
        Counts = string.Format(CultureInfo.InvariantCulture, "{0:N0} elements · {1:N0} values", model.Elements.Count, model.Properties.Count);
        Viewport = ViewportFor(_package, loaded.FindSourcePath());
        StatusItems = StatusItemsFor(_package);
        Tree = new ModelTreeViewModel(model.Elements, model.Properties, OnSelectionChanged);
    }

    public OpenCommands Open { get; }

    public string FileName { get; }

    /// <summary>The building name, else the project name; null when the model has neither.</summary>
    public string? ProjectName { get; }

    public bool HasProjectName => ProjectName != null;

    public string Schema { get; }

    public string ElementCount { get; }

    public string LoadLabel { get; }

    public string LoadTime { get; }

    public string Counts { get; }

    public ViewportViewModel Viewport { get; }

    public IReadOnlyList<StatusItemViewModel> StatusItems { get; }

    /// <summary>The saved package, or null when it could not be saved.</summary>
    public string? PackagePath => (_package as PackageState.Saved)?.Path;

    /// <summary>What is missing from this model (the saved package or its 3D geometry), or null when nothing is.</summary>
    public string? Warning => _package switch
    {
        PackageState.NotSaved notSaved => notSaved.Reason,
        PackageState.Saved { Geometry: GeometryState.NotBuilt notBuilt } => GeometryWarning.For(notBuilt),
        _ => null,
    };

    public bool CanReveal => _package is PackageState.Saved;

    public bool CanOpenIn3D => _package is PackageState.Saved { Geometry: GeometryState.Built };

    public string RevealLabel => _platform.Revealer.ActionLabel;

    public ModelTreeViewModel Tree { get; }

    [ObservableProperty]
    public partial ElementDetailsViewModel? SelectedElement { get; private set; }

    /// <summary>The selected element's full name for the badge over the 3D area, or null when none is selected.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial string? SelectedName { get; private set; }

    public bool HasSelection => SelectedName != null;

    [ObservableProperty]
    public partial string? ActionError { get; private set; }

    [RelayCommand(CanExecute = nameof(CanOpenIn3D))]
    private void OpenIn3D()
    {
        if (_package is PackageState.Saved saved)
        {
            ActionError = _platform.Viewer.TryOpen(saved.Path, Tree.SelectedNode?.Element?.GlobalId, out var error) ? null : error;
        }
    }

    [RelayCommand(CanExecute = nameof(CanReveal))]
    private void Reveal()
    {
        if (_package is PackageState.Saved saved)
        {
            ActionError = _platform.Revealer.TryReveal(saved.Path, out var error) ? null : error;
        }
    }

    private void OnSelectionChanged(TreeNodeViewModel? node)
    {
        if (_package is PackageState.Saved saved)
        {
            _platform.Viewer.ShowSelection(saved.Path, node?.Element?.GlobalId);
        }

        SelectedName = node?.Element != null ? node.Label : null;
        SelectedElement = node?.Element is { } element
            ? new ElementDetailsViewModel(
                element,
                StoreyNameOf(element),
                _propertiesByElement[element.GlobalId],
                new CopyableTextViewModel(element.GlobalId, _platform.Clipboard, _clock))
            : null;
    }

    private string? StoreyNameOf(ElementRecord element) =>
        element.StoreyGlobalId != null && _elementsById.TryGetValue(element.StoreyGlobalId, out var storey)
            ? storey.Name
            : null;

    private ViewportViewModel ViewportFor(PackageState package, string? sourcePath) => package switch
    {
        PackageState.Saved { Geometry: GeometryState.Built built } =>
            new ViewportReadyViewModel(built.NodeCount, OpenIn3DCommand),
        PackageState.Saved { Geometry: GeometryState.NotBuilt notBuilt } =>
            new ViewportGeometryMissingViewModel(GeometryWarning.Reason(notBuilt), notBuilt.Detail, Open.OpenPath, sourcePath),
        PackageState.NotSaved notSaved => new ViewportNotSavedViewModel(notSaved.Reason),
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    private IReadOnlyList<StatusItemViewModel> StatusItemsFor(PackageState package) => package switch
    {
        PackageState.Saved saved =>
        [
            new StatusItemViewModel("Package saved", IsProblem: false, Reason: null, RevealCommand),
            saved.Geometry is GeometryState.NotBuilt notBuilt
                ? new StatusItemViewModel("Geometry not built", IsProblem: true, GeometryWarning.For(notBuilt), Action: null)
                : new StatusItemViewModel("Geometry built", IsProblem: false, Reason: null, Action: null),
        ],
        PackageState.NotSaved notSaved =>
        [
            new StatusItemViewModel("Package not saved", IsProblem: true, notSaved.Reason, Action: null),
            new StatusItemViewModel("Geometry not built", IsProblem: true, "3D geometry is stored in the package, which was not saved.", Action: null),
        ],
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    private static string? ProjectNameOf(IReadOnlyList<ElementRecord> elements)
    {
        string? NameOf(string type) => elements.FirstOrDefault(e => e.IfcType == type && !string.IsNullOrWhiteSpace(e.Name))?.Name;
        return NameOf("IfcBuilding") ?? NameOf("IfcProject");
    }
}
