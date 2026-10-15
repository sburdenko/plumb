using System;
using System.Collections.Generic;
using System.Linq;
using Plumb.Core.Search;

namespace Plumb.Core.Tree
{
    /// <summary>
    /// Prunes a tree to the elements a search matched, keeping their ancestors so each match stays in its place.
    /// A matching element keeps its whole subtree.
    /// </summary>
    public static class ModelTreeFilter
    {
        public static IReadOnlyList<ModelTreeNode> Apply(IReadOnlyList<ModelTreeNode> roots, ElementMatches matches)
        {
            return roots.Select(node => Filter(node, matches)).OfType<ModelTreeNode>().ToList();
        }

        private static ModelTreeNode? Filter(ModelTreeNode node, ElementMatches matches) => node switch
        {
            ElementNode element => FilterElement(element, matches),
            TypeGroupNode group => FilterGroup(group, matches),
            _ => throw new ArgumentOutOfRangeException(nameof(node), node.GetType().Name, "Unknown tree node type."),
        };

        private static ElementNode? FilterElement(ElementNode node, ElementMatches matches)
        {
            if (matches.Contains(node.Element.GlobalId))
            {
                return node;
            }

            var children = node.Children.Select(child => Filter(child, matches)).OfType<ModelTreeNode>().ToList();
            return children.Count == 0 ? null : node.WithChildren(children);
        }

        private static TypeGroupNode? FilterGroup(TypeGroupNode group, ElementMatches matches)
        {
            var elements = group.Elements.Select(element => FilterElement(element, matches)).OfType<ElementNode>().ToList();
            return elements.Count == 0 ? null : group.WithElements(elements);
        }
    }
}
