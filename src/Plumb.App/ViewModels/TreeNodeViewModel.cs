using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Plumb.Core.Model;
using Plumb.Core.Tree;

namespace Plumb.App.ViewModels;

/// <summary>
/// One row of the spatial tree: what it shows depends on <see cref="Kind"/>.
/// </summary>
public sealed partial class TreeNodeViewModel : ViewModelBase
{
    private const double ElementIndent = 86;
    private static readonly double[] IndentByDepth = [10, 24, 38, 52, 66];

    private readonly Action<string, bool> _expansionChanged;

    private TreeNodeViewModel(
        string key,
        TreeNodeKind kind,
        string title,
        string? subtitle,
        string? meta,
        double indent,
        ElementRecord? element,
        IReadOnlyList<TreeNodeViewModel> children,
        bool isExpanded,
        Action<string, bool> expansionChanged)
    {
        Key = key;
        Kind = kind;
        Title = title;
        Subtitle = subtitle;
        Meta = meta;
        Indent = indent;
        Element = element;
        Children = children;
        _expansionChanged = expansionChanged;
        IsExpanded = isExpanded;
    }

    /// <summary>Stable identity across filtering: the GlobalId, or the parent's id and the IFC type for a group.</summary>
    public string Key { get; }

    public TreeNodeKind Kind { get; }

    public string Title { get; }

    public string? Subtitle { get; }

    /// <summary>The subtitle with its leading gap, or empty; shown as a muted run after the title.</summary>
    public string SubtitleSuffix => Subtitle == null ? string.Empty : "  " + Subtitle;

    /// <summary>Right-aligned number: elements in a storey or a type group.</summary>
    public string? Meta { get; }

    public double Indent { get; }

    /// <summary>The element this node shows, or null for a type group.</summary>
    public ElementRecord? Element { get; }

    public IReadOnlyList<TreeNodeViewModel> Children { get; }

    public bool IsStrong => Kind is TreeNodeKind.Project or TreeNodeKind.Site or TreeNodeKind.Building or TreeNodeKind.Storey;

    /// <summary>Full label for tooltips and the selection badge.</summary>
    public string Label => Element?.Name is { Length: > 0 } name ? name : Title;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>True for the storey that contains the selected element.</summary>
    [ObservableProperty]
    public partial bool IsActiveStorey { get; set; }

    partial void OnIsExpandedChanged(bool value) => _expansionChanged(Key, value);

    /// <param name="isExpanded">Decides the initial expansion of each node from its key.</param>
    /// <param name="expansionChanged">Told whenever the user expands or collapses a node.</param>
    public static TreeNodeViewModel From(
        ModelTreeNode node,
        string parentKey,
        int depth,
        Func<string, ModelTreeNode, bool> isExpanded,
        Action<string, bool> expansionChanged)
    {
        var key = node switch
        {
            ElementNode element => element.Element.GlobalId,
            TypeGroupNode group => $"{parentKey}/{group.IfcType}",
            _ => parentKey,
        };
        var kind = KindOf(node);
        var children = node.Children.Select(child => From(child, key, depth + 1, isExpanded, expansionChanged)).ToList();
        var indent = kind == TreeNodeKind.Element ? ElementIndent : IndentByDepth[Math.Min(depth, IndentByDepth.Length - 1)];

        return new TreeNodeViewModel(
            key,
            kind,
            TitleOf(node, kind),
            SubtitleOf(node, kind),
            MetaOf(node, kind),
            indent,
            (node as ElementNode)?.Element,
            children,
            isExpanded(key, node),
            expansionChanged);
    }

    private static TreeNodeKind KindOf(ModelTreeNode node) => node switch
    {
        TypeGroupNode => TreeNodeKind.TypeGroup,
        ElementNode { Element.IfcType: "IfcProject" } => TreeNodeKind.Project,
        ElementNode { Element.IfcType: "IfcSite" } => TreeNodeKind.Site,
        ElementNode { Element.IfcType: "IfcBuilding" } => TreeNodeKind.Building,
        ElementNode { Element.IfcType: "IfcBuildingStorey" } => TreeNodeKind.Storey,
        _ => TreeNodeKind.Element,
    };

    private static string TitleOf(ModelTreeNode node, TreeNodeKind kind) => (kind, node) switch
    {
        (TreeNodeKind.TypeGroup, TypeGroupNode group) => group.IfcType,
        (TreeNodeKind.Project or TreeNodeKind.Site or TreeNodeKind.Building, ElementNode element) => element.Element.IfcType,
        (TreeNodeKind.Element, ElementNode element) when element.Element.Name is { Length: > 0 } name => ElementLabel.WithoutFamily(name),
        _ => node.Label,
    };

    private static string? SubtitleOf(ModelTreeNode node, TreeNodeKind kind) =>
        kind is TreeNodeKind.Project or TreeNodeKind.Site or TreeNodeKind.Building
        && node is ElementNode { Element.Name: { Length: > 0 } name }
            ? $"\"{name}\""
            : null;

    private static string? MetaOf(ModelTreeNode node, TreeNodeKind kind) => kind switch
    {
        TreeNodeKind.TypeGroup => node.Children.Count.ToString(CultureInfo.InvariantCulture),
        TreeNodeKind.Storey => CountElements(node).ToString(CultureInfo.InvariantCulture),
        _ => null,
    };

    private static int CountElements(ModelTreeNode node) =>
        node.Children.Sum(child => child switch
        {
            TypeGroupNode group => group.Elements.Count + group.Elements.Sum(CountElements),
            _ => CountElements(child),
        });
}
