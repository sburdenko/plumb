using System.Collections.Generic;

namespace Plumb.Core.Tree
{
    /// <summary>
    /// Groups sibling elements of one IFC type, e.g. "IfcWall (42)".
    /// </summary>
    public sealed class TypeGroupNode : ModelTreeNode
    {
        public TypeGroupNode(string ifcType, IReadOnlyList<ElementNode> elements)
        {
            IfcType = ifcType;
            Elements = elements;
        }

        public string IfcType { get; }

        public IReadOnlyList<ElementNode> Elements { get; }

        public override IReadOnlyList<ModelTreeNode> Children => Elements;

        public override string Label => $"{IfcType} ({Elements.Count})";

        public TypeGroupNode WithElements(IReadOnlyList<ElementNode> elements) => new TypeGroupNode(IfcType, elements);
    }
}
