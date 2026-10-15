using System.Collections.Generic;

namespace Plumb.Core.Search
{
    /// <summary>A question about a model's elements. An element matches when it satisfies every condition.</summary>
    public sealed record ElementQuery(IReadOnlyList<Condition> Conditions);
}
