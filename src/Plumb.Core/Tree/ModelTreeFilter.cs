using System;
using System.Collections.Generic;
using System.Linq;

namespace Plumb.Core.Tree
{
    /// <summary>
    /// Prunes a tree to nodes whose name, IFC type, GlobalId or tag contains the query, keeping their ancestors.
    /// Tags match with or without their leading <c>#</c>.
    /// A matching node keeps its whole subtree.
    /// </summary>
    public static class ModelTreeFilter
    {
        public static IReadOnlyList<ModelTreeNode> Apply(IReadOnlyList<ModelTreeNode> roots, string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return roots;
            }

            var term = query.Trim();
            return roots.Select(node => Filter(node, term)).OfType<ModelTreeNode>().ToList();
        }

        private static ModelTreeNode? Filter(ModelTreeNode node, string term) => node switch
        {
            ElementNode element => FilterElement(element, term),
            TypeGroupNode group => FilterGroup(group, term),
            _ => throw new ArgumentOutOfRangeException(nameof(node), node.GetType().Name, "Unknown tree node type."),
        };

        private static ElementNode? FilterElement(ElementNode node, string term)
        {
            var element = node.Element;
            if (Contains(element.Name, term) || Contains(element.IfcType, term) || Contains(element.GlobalId, term)
                || Contains(element.Tag == null ? null : "#" + element.Tag, term))
            {
                return node;
            }

            var children = node.Children.Select(child => Filter(child, term)).OfType<ModelTreeNode>().ToList();
            return children.Count == 0 ? null : node.WithChildren(children);
        }

        private static TypeGroupNode? FilterGroup(TypeGroupNode group, string term)
        {
            if (Contains(group.IfcType, term))
            {
                return group;
            }

            var elements = group.Elements.Select(element => FilterElement(element, term)).OfType<ElementNode>().ToList();
            return elements.Count == 0 ? null : group.WithElements(elements);
        }

        private static bool Contains(string? text, string term) =>
            text != null && text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
