using System;
using System.Collections.Generic;

namespace Plumb.Core.Clashes
{
    /// <summary>
    /// A bounding volume hierarchy over a fixed list of boxes: each node holds a box around everything below it, so
    /// a query skips whole branches that cannot overlap. Built by splitting the boxes at the median of their
    /// centres along the longest side, which keeps the tree balanced.
    /// </summary>
    public sealed class BoxTree
    {
        private const int LeafSize = 4;

        private readonly IReadOnlyList<Box> _boxes;
        private readonly int[] _items;
        private readonly List<Node> _nodes = new List<Node>();

        private BoxTree(IReadOnlyList<Box> boxes)
        {
            _boxes = boxes;
            _items = new int[boxes.Count];
            for (var i = 0; i < _items.Length; i++)
            {
                _items[i] = i;
            }

            if (_items.Length > 0)
            {
                Split(0, _items.Length);
            }
        }

        public static BoxTree Build(IReadOnlyList<Box> boxes) => new BoxTree(boxes);

        /// <summary>Adds to <paramref name="hits"/> the index of every box that overlaps <paramref name="box"/>.</summary>
        public void Query(Box box, List<int> hits)
        {
            if (_nodes.Count == 0)
            {
                return;
            }

            var pending = new Stack<int>();
            pending.Push(0);
            while (pending.Count > 0)
            {
                var node = _nodes[pending.Pop()];
                if (!node.Bounds.Overlaps(box))
                {
                    continue;
                }

                if (node.Count > 0)
                {
                    for (var i = node.Start; i < node.Start + node.Count; i++)
                    {
                        if (_boxes[_items[i]].Overlaps(box))
                        {
                            hits.Add(_items[i]);
                        }
                    }
                }
                else
                {
                    pending.Push(node.Start);
                    pending.Push(node.Right);
                }
            }
        }

        /// <returns>The index of the node made for items [start, start + count).</returns>
        private int Split(int start, int count)
        {
            var bounds = Box.Empty;
            var centres = Box.Empty;
            for (var i = start; i < start + count; i++)
            {
                bounds = bounds.Union(_boxes[_items[i]]);
                centres = centres.Include(_boxes[_items[i]].Center);
            }

            var index = _nodes.Count;
            _nodes.Add(new Node(bounds, start, count, right: -1));
            if (count <= LeafSize)
            {
                return index;
            }

            var axis = LongestAxis(centres);
            Array.Sort(_items, start, count, Comparer<int>.Create((x, y) => Along(x, axis).CompareTo(Along(y, axis))));
            var half = count / 2;
            var left = Split(start, half);
            var right = Split(start + half, count - half);
            _nodes[index] = new Node(bounds, left, count: 0, right);
            return index;
        }

        private float Along(int item, int axis)
        {
            var centre = _boxes[item].Center;
            return axis == 0 ? centre.X : axis == 1 ? centre.Y : centre.Z;
        }

        private static int LongestAxis(Box box)
        {
            var size = box.Size;
            return size.X >= size.Y && size.X >= size.Z ? 0 : size.Y >= size.Z ? 1 : 2;
        }

        /// <summary>A leaf when <see cref="Count"/> is positive: items [Start, Start + Count). Otherwise the children
        /// are nodes <see cref="Start"/> and <see cref="Right"/>.</summary>
        private readonly struct Node
        {
            public Node(Box bounds, int start, int count, int right)
            {
                Bounds = bounds;
                Start = start;
                Count = count;
                Right = right;
            }

            public Box Bounds { get; }

            public int Start { get; }

            public int Count { get; }

            public int Right { get; }
        }
    }
}
