using System.Collections.Generic;

namespace Plumb.Core.Tree
{
    /// <summary>
    /// Immutable node of the model tree: either an <see cref="ElementNode"/> or a <see cref="TypeGroupNode"/>.
    /// </summary>
    public abstract class ModelTreeNode
    {
        private protected ModelTreeNode()
        {
        }

        public abstract string Label { get; }

        public abstract IReadOnlyList<ModelTreeNode> Children { get; }
    }
}
