using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// Adds a MeshCollider to each mesh a few at a time, so a large model does not freeze the first frame.
    /// Elements become clickable as their colliders are added.
    /// </summary>
    public sealed class ColliderBuilder
    {
        private readonly Queue<MeshFilter> _pending;
        private readonly int _total;

        public ColliderBuilder(IEnumerable<MeshFilter> filters)
        {
            _pending = new Queue<MeshFilter>(filters);
            _total = _pending.Count;
        }

        public bool Done => _pending.Count == 0;

        public float Progress => _total == 0 ? 1f : 1f - (float)_pending.Count / _total;

        /// <summary>Adds colliders until the time budget is used; always adds at least one.</summary>
        public void Advance(double budgetMilliseconds)
        {
            var clock = Stopwatch.StartNew();
            do
            {
                if (_pending.Count == 0)
                {
                    return;
                }

                var filter = _pending.Dequeue();
                if (filter != null && filter.sharedMesh != null)
                {
                    filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                }
            }
            while (clock.Elapsed.TotalMilliseconds < budgetMilliseconds);
        }
    }
}
