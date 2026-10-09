using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plumb.App.Services;
using Plumb.Core.Import;
using Plumb.Core.Model;
using Plumb.Core.Tree;

namespace Plumb.App.ViewModels;

public sealed partial class LoadedViewModel : ViewModelBase
{
    private const int MaxNodesToExpandOnSearch = 300;

    private readonly IReadOnlyList<ModelTreeNode> _tree;
    private readonly ILookup<string, PropertyRecord> _propertiesByElement;
    private readonly IReadOnlyDictionary<string, ElementRecord> _elementsById;

    private readonly IFileRevealer _revealer;
    private readonly PackageState _package;

    public LoadedViewModel(ImportResult.Success result, OpenCommands open, IFileRevealer revealer)
    {
        var model = result.Model;
        _tree = ModelTreeBuilder.Build(model.Elements);
        _propertiesByElement = model.Properties.ToLookup(p => p.GlobalId);
        _elementsById = model.Elements.ToDictionary(e => e.GlobalId);

        FileName = model.SourceFile;
        Summary = string.Format(
            CultureInfo.InvariantCulture,
            "{0} · {1} elements · imported in {2:0.0} s",
            model.IfcSchema,
            model.Elements.Count,
            result.ImportDuration.TotalSeconds);
        _package = result.Package;
        Open = open;
        _revealer = revealer;
        Nodes = ToViewModels(_tree, expandAll: false);
    }

    public string FileName { get; }

    public string Summary { get; }

    /// <summary>The saved package, or null when it could not be saved.</summary>
    public string? PackagePath => (_package as PackageState.Saved)?.Path;

    /// <summary>Why the package could not be saved, or null when it was.</summary>
    public string? SaveWarning => (_package as PackageState.NotSaved)?.Reason;

    public bool CanReveal => _package is PackageState.Saved;

    public OpenCommands Open { get; }

    public string RevealLabel => _revealer.ActionLabel;

    [ObservableProperty]
    public partial string? SearchText { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<TreeNodeViewModel> Nodes { get; private set; }

    [ObservableProperty]
    public partial TreeNodeViewModel? SelectedNode { get; set; }

    [ObservableProperty]
    public partial ElementDetailsViewModel? SelectedElement { get; private set; }

    [ObservableProperty]
    public partial string? RevealError { get; private set; }

    [RelayCommand(CanExecute = nameof(CanReveal))]
    private void Reveal()
    {
        if (_package is PackageState.Saved saved)
        {
            RevealError = _revealer.TryReveal(saved.Path, out var error) ? null : error;
        }
    }

    partial void OnSearchTextChanged(string? value)
    {
        var filtered = ModelTreeFilter.Apply(_tree, value);
        var isSearching = !string.IsNullOrWhiteSpace(value);
        Nodes = ToViewModels(filtered, expandAll: isSearching && CountNodes(filtered) <= MaxNodesToExpandOnSearch);
    }

    partial void OnSelectedNodeChanged(TreeNodeViewModel? value)
    {
        SelectedElement = value?.Element is { } element
            ? new ElementDetailsViewModel(value.Label, element, StoreyNameOf(element), _propertiesByElement[element.GlobalId])
            : null;
    }

    private string? StoreyNameOf(ElementRecord element) =>
        element.StoreyGlobalId != null && _elementsById.TryGetValue(element.StoreyGlobalId, out var storey)
            ? storey.Name
            : null;

    private static IReadOnlyList<TreeNodeViewModel> ToViewModels(IReadOnlyList<ModelTreeNode> nodes, bool expandAll) =>
        nodes.Select(node => TreeNodeViewModel.From(node, expandAll)).ToList();

    private static int CountNodes(IReadOnlyList<ModelTreeNode> nodes) =>
        nodes.Sum(node => 1 + CountNodes(node.Children));
}
