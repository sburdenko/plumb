using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plumb.App.Services;
using Plumb.Core.Geometry;
using Plumb.Core.Import;
using Plumb.Core.Model;
using Plumb.Core.Package;
using Plumb.Core.Tree;
using Plumb.Geometry;

namespace Plumb.App.ViewModels;

/// <summary>
/// The loaded model: spatial tree with filter, selected element, viewport state and status bar.
/// </summary>
public sealed partial class LoadedViewModel : ViewModelBase
{
    private const int MaxNodesToExpandOnFilter = 300;

    private readonly IReadOnlyList<ModelTreeNode> _tree;
    private readonly ILookup<string, PropertyRecord> _propertiesByElement;
    private readonly IReadOnlyDictionary<string, ElementRecord> _elementsById;
    private readonly Dictionary<string, bool> _expansion = new(StringComparer.Ordinal);
    private readonly IFileRevealer _revealer;
    private readonly IViewerLauncher _viewer;
    private readonly PackageState _package;
    private Dictionary<string, TreeNodeViewModel> _storeys = new(StringComparer.Ordinal);
    private bool _rememberExpansion = true;

    public LoadedViewModel(LoadedModel loaded, OpenCommands open, IFileRevealer revealer, IViewerLauncher viewer)
    {
        var model = loaded.Result.Model;
        _tree = ModelTreeBuilder.Build(model.Elements);
        _propertiesByElement = model.Properties.ToLookup(p => p.GlobalId);
        _elementsById = model.Elements.ToDictionary(e => e.GlobalId);
        _package = loaded.Result.Package;
        _revealer = revealer;
        _viewer = viewer;
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
        Nodes = BuildNodes(_tree, expandAll: false);
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

    public string RevealLabel => _revealer.ActionLabel;

    [ObservableProperty]
    public partial string? Filter { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<TreeNodeViewModel> Nodes { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedName), nameof(HasSelection))]
    public partial TreeNodeViewModel? SelectedNode { get; set; }

    [ObservableProperty]
    public partial ElementDetailsViewModel? SelectedElement { get; private set; }

    public string? SelectedName => SelectedNode?.Element != null ? SelectedNode.Label : null;

    public bool HasSelection => SelectedName != null;

    [ObservableProperty]
    public partial string? ActionError { get; private set; }

    [RelayCommand(CanExecute = nameof(CanOpenIn3D))]
    private void OpenIn3D()
    {
        if (_package is PackageState.Saved saved)
        {
            ActionError = _viewer.TryOpen(saved.Path, out var error) ? null : error;
        }
    }

    [RelayCommand(CanExecute = nameof(CanReveal))]
    private void Reveal()
    {
        if (_package is PackageState.Saved saved)
        {
            ActionError = _revealer.TryReveal(saved.Path, out var error) ? null : error;
        }
    }

    [RelayCommand]
    private void ClearFilter() => Filter = null;

    partial void OnFilterChanged(string? value)
    {
        var filtering = !string.IsNullOrWhiteSpace(value);
        var nodes = ModelTreeFilter.Apply(_tree, value);
        Nodes = BuildNodes(nodes, expandAll: filtering && CountNodes(nodes) <= MaxNodesToExpandOnFilter, remember: !filtering);
        MarkActiveStorey();
    }

    partial void OnSelectedNodeChanged(TreeNodeViewModel? value)
    {
        SelectedElement = value?.Element is { } element
            ? new ElementDetailsViewModel(element, StoreyNameOf(element), _propertiesByElement[element.GlobalId])
            : null;
        MarkActiveStorey();
    }

    private IReadOnlyList<TreeNodeViewModel> BuildNodes(IReadOnlyList<ModelTreeNode> nodes, bool expandAll, bool remember = true)
    {
        _rememberExpansion = false;
        var built = nodes.Select(node => TreeNodeViewModel.From(
                node,
                parentKey: string.Empty,
                depth: 0,
                (key, source) => expandAll || (_expansion.TryGetValue(key, out var expanded) ? expanded : ExpandedByDefault(source)),
                OnExpansionChanged))
            .ToList();
        _rememberExpansion = remember;

        _storeys = Flatten(built).Where(n => n.Kind == TreeNodeKind.Storey).ToDictionary(n => n.Key, StringComparer.Ordinal);
        return built;
    }

    private void OnExpansionChanged(string key, bool expanded)
    {
        if (_rememberExpansion)
        {
            _expansion[key] = expanded;
        }
    }

    private void MarkActiveStorey()
    {
        var storeyId = SelectedNode?.Element?.StoreyGlobalId;
        foreach (var storey in _storeys.Values)
        {
            storey.IsActiveStorey = storey.Key == storeyId;
        }
    }

    private string? StoreyNameOf(ElementRecord element) =>
        element.StoreyGlobalId != null && _elementsById.TryGetValue(element.StoreyGlobalId, out var storey)
            ? storey.Name
            : null;

    private ViewportViewModel ViewportFor(PackageState package, string? sourcePath) => package switch
    {
        PackageState.Saved { Geometry: GeometryState.Built } saved =>
            new ViewportReadyViewModel(GlbInfo.CountNodes(Path.Combine(saved.Path, PackageLayout.GeometryFile)), OpenIn3DCommand),
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

    private static bool ExpandedByDefault(ModelTreeNode node) =>
        node.Children.OfType<ElementNode>().Any(child => ModelTreeBuilder.IsSpatialLevel(child.Element.IfcType));

    private static IEnumerable<TreeNodeViewModel> Flatten(IEnumerable<TreeNodeViewModel> nodes) =>
        nodes.SelectMany(node => Flatten(node.Children).Prepend(node));

    private static int CountNodes(IReadOnlyList<ModelTreeNode> nodes) =>
        nodes.Sum(node => 1 + CountNodes(node.Children));
}
