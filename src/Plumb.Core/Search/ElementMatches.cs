using System.Collections;
using System.Collections.Generic;

namespace Plumb.Core.Search
{
    /// <summary>The GlobalIds of the elements that matched a query.</summary>
    public sealed class ElementMatches : IEnumerable<string>
    {
        private readonly HashSet<string> _globalIds;

        internal ElementMatches(HashSet<string> globalIds)
        {
            _globalIds = globalIds;
        }

        public int Count => _globalIds.Count;

        public bool Contains(string globalId) => _globalIds.Contains(globalId);

        public IEnumerator<string> GetEnumerator() => _globalIds.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
