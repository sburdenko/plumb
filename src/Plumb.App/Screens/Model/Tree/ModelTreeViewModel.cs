using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plumb.App.Screens.Model.Filter;
using Plumb.App.Shell;
using Plumb.Core.Model;
using Plumb.Core.Search;
using Plumb.Core.Tree;

namespace Plumb.App.Screens.Model.Tree;

/// <summary>
/// The spatial tree of a model with its search box. It shows either the whole tree the way the user left it, or
/// the result of a search, which opens up when it is small and never changes what the user had expanded.
/// </summary>
public sealed partial class ModelTreeViewModel : ViewModelBase
{
    private const int MaxNodesToExpandInAResult = 300;

    private readonly IReadOnlyList<ModelTreeNode> _tree;
    private readonly ElementSearch _search;
    private readonly Action<TreeNodeViewModel?> _selectionChanged;
    private readonly Dictionary<string, bool> _expansion = new(StringComparer.Ordinal);
    private Dictionary<string, TreeNodeViewModel> _storeys = new(StringComparer.Ordinal);
    private TreeMode _mode = TreeMode.Browsing;

    /// <param name="selectionChanged">Told whenever the selected node changes, including to none.</param>
    public ModelTreeViewModel(
        IReadOnlyList<ElementRecord> elements,
        IReadOnlyList<PropertyRecord> properties,
        Action<TreeNodeViewModel?> selectionChanged)
    {
        _tree = ModelTreeBuilder.Build(elements);
        _search = new ElementSearch(elements, properties);
        _selectionChanged = selectionChanged;
        Show(_tree, TreeMode.Browsing);
    }

    private enum TreeMode
    {
        /// <summary>The whole tree; what the user expands is remembered.</summary>
        Browsing,

        /// <summary>The elements a search matched; expanding them is not remembered.</summary>
        SearchResult,
    }

    [ObservableProperty]
    public partial string? Filter { get; set; }

    /// <summary>The result of the filter text, or null while the box is empty.</summary>
    [ObservableProperty]
    public partial FilterStatusViewModel? FilterStatus { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyList<TreeNodeViewModel> Nodes { get; private set; } = [];

    [ObservableProperty]
    public partial TreeNodeViewModel? SelectedNode { get; set; }

    /// <summary>
    /// Selects the element with <paramref name="globalId"/> and opens the rows above it. An element outside the
    /// search result clears the search; null, or an element the tree does not show, clears the selection.
    /// </summary>
    public void SelectElement(string? globalId)
    {
        var path = globalId == null ? null : PathTo(Nodes, globalId);
        if (path == null && globalId != null && _mode == TreeMode.SearchResult)
        {
            Filter = null;
            path = PathTo(Nodes, globalId);
        }

        foreach (var ancestor in path?.SkipLast(1) ?? [])
        {
            ancestor.IsExpanded = true;
        }

        SelectedNode = path?[^1];
    }

    [RelayCommand]
    private void ClearFilter() => Filter = null;

    partial void OnFilterChanged(string? value)
    {
        switch (QueryParser.Parse(value))
        {
            case ParseResult.Invalid invalid:
                FilterStatus = new FilterRejectedViewModel(invalid.Reason);
                break;
            case ParseResult.Blank:
                FilterStatus = null;
                Show(_tree, TreeMode.Browsing);
                break;
            case ParseResult.Parsed parsed:
                var matches = _search.Find(parsed.Query);
                FilterStatus = new FilterMatchedViewModel(matches.Count);
                Show(ModelTreeFilter.Apply(_tree, matches), TreeMode.SearchResult);
                break;
        }
    }

    partial void OnSelectedNodeChanged(TreeNodeViewModel? value)
    {
        MarkActiveStorey();
        _selectionChanged(value);
    }

    private void Show(IReadOnlyList<ModelTreeNode> nodes, TreeMode mode)
    {
        _mode = mode;
        Func<string, ModelTreeNode, bool> isExpanded = mode == TreeMode.SearchResult && CountNodes(nodes) <= MaxNodesToExpandInAResult
            ? (_, _) => true
            : RememberedOrDefault;
        var built = nodes
            .Select(node => TreeNodeViewModel.From(node, parentKey: string.Empty, depth: 0, isExpanded, OnExpansionChanged))
            .ToList();
        _storeys = Flatten(built).Where(n => n.Kind == TreeNodeKind.Storey).ToDictionary(n => n.Key, StringComparer.Ordinal);
        Nodes = built;
        MarkActiveStorey();
    }

    private bool RememberedOrDefault(string key, ModelTreeNode node) =>
        _expansion.TryGetValue(key, out var expanded) ? expanded : ExpandedByDefault(node);

    private void OnExpansionChanged(string key, bool expanded)
    {
        if (_mode == TreeMode.Browsing)
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

    private static IReadOnlyList<TreeNodeViewModel>? PathTo(IEnumerable<TreeNodeViewModel> nodes, string globalId)
    {
        foreach (var node in nodes)
        {
            if (node.Element?.GlobalId == globalId)
            {
                return [node];
            }

            if (PathTo(node.Children, globalId) is { } below)
            {
                return [node, .. below];
            }
        }

        return null;
    }

    private static bool ExpandedByDefault(ModelTreeNode node) =>
        node.Children.OfType<ElementNode>().Any(child => ModelTreeBuilder.IsSpatialLevel(child.Element.IfcType));

    private static IEnumerable<TreeNodeViewModel> Flatten(IEnumerable<TreeNodeViewModel> nodes) =>
        nodes.SelectMany(node => Flatten(node.Children).Prepend(node));

    private static int CountNodes(IReadOnlyList<ModelTreeNode> nodes) =>
        nodes.Sum(node => 1 + CountNodes(node.Children));
}
