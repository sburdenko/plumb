using System.Collections.Generic;
using Plumb.Core.Model;

namespace Plumb.Core.Tree
{
    /// <summary>
    /// A project, site, building, storey or element. Labelled by name, or by IFC type when unnamed.
    /// </summary>
    public sealed class ElementNode : ModelTreeNode
    {
        public ElementNode(ElementRecord element, IReadOnlyList<ModelTreeNode> children)
        {
            Element = element;
            Children = children;
        }

        public ElementRecord Element { get; }

        public override IReadOnlyList<ModelTreeNode> Children { get; }

        public override string Label =>
            string.IsNullOrWhiteSpace(Element.Name) ? Element.IfcType : Element.Name;

        public ElementNode WithChildren(IReadOnlyList<ModelTreeNode> children) => new ElementNode(Element, children);
    }
}
