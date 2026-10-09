using System;
using System.Collections.Generic;
using System.Linq;
using Plumb.Core.Model;

namespace Plumb.Core.Tree
{
    public static class ModelTreeBuilder
    {
        private static readonly HashSet<string> SpatialLevelTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "IfcSite",
            "IfcBuilding",
            "IfcBuildingStorey",
        };

        /// <summary>
        /// Builds Project &gt; Site &gt; Building &gt; Storey &gt; type groups &gt; elements from flat records.
        /// Spatial levels stay direct children; everything else is grouped by IFC type.
        /// Records whose parent is unknown become roots.
        /// </summary>
        public static IReadOnlyList<ModelTreeNode> Build(IReadOnlyList<ElementRecord> elements)
        {
            var knownIds = new HashSet<string>(elements.Select(e => e.GlobalId));
            var childrenByParent = elements
                .Where(e => e.ParentGlobalId != null && knownIds.Contains(e.ParentGlobalId))
                .ToLookup(e => e.ParentGlobalId!);

            return elements
                .Where(e => e.ParentGlobalId == null || !knownIds.Contains(e.ParentGlobalId))
                .Select(root => BuildElementNode(root, childrenByParent))
                .ToList();
        }

        public static bool IsSpatialLevel(string ifcType) => SpatialLevelTypes.Contains(ifcType);

        private static ElementNode BuildElementNode(ElementRecord element, ILookup<string, ElementRecord> childrenByParent)
        {
            var children = childrenByParent[element.GlobalId].ToList();

            var levels = children
                .Where(c => IsSpatialLevel(c.IfcType))
                .Select(c => (ModelTreeNode)BuildElementNode(c, childrenByParent));

            var groups = children
                .Where(c => !IsSpatialLevel(c.IfcType))
                .GroupBy(c => c.IfcType)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => (ModelTreeNode)new TypeGroupNode(
                    g.Key,
                    g.Select(c => BuildElementNode(c, childrenByParent)).ToList()));

            return new ElementNode(element, levels.Concat(groups).ToList());
        }
    }
}
