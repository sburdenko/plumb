using CommunityToolkit.Mvvm.ComponentModel;
using Plumb.Core.Model;
using Plumb.Core.Tree;

namespace Plumb.App.ViewModels;

public sealed partial class TreeNodeViewModel : ViewModelBase
{
    private TreeNodeViewModel(string label, ElementRecord? element, IReadOnlyList<TreeNodeViewModel> children, bool isExpanded)
    {
        Label = label;
        Element = element;
        Children = children;
        IsExpanded = isExpanded;
    }

    public string Label { get; }

    /// <summary>The element this node shows, or null for a type group.</summary>
    public ElementRecord? Element { get; }

    public IReadOnlyList<TreeNodeViewModel> Children { get; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <param name="expandAll">Expand every node (used for search results); otherwise only nodes above storeys.</param>
    public static TreeNodeViewModel From(ModelTreeNode node, bool expandAll)
    {
        var children = node.Children.Select(child => From(child, expandAll)).ToList();
        var element = (node as ElementNode)?.Element;
        return new TreeNodeViewModel(node.Label, element, children, expandAll || HasSpatialLevelChildren(node));
    }

    private static bool HasSpatialLevelChildren(ModelTreeNode node) =>
        node.Children.OfType<ElementNode>().Any(child => ModelTreeBuilder.IsSpatialLevel(child.Element.IfcType));
}
