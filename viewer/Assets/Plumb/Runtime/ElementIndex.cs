using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// Looks up the elements listed in <c>elements.json</c> by IFC GlobalId, which is also the glTF node name.
    /// </summary>
    public sealed class ElementIndex
    {
        private readonly Dictionary<string, ElementInfo> _byId;

        private ElementIndex(Dictionary<string, ElementInfo> byId)
        {
            _byId = byId;
        }

        public int Count => _byId.Count;

        /// <exception cref="ArgumentException">The text is not a JSON array of elements.</exception>
        public static ElementIndex Parse(string json)
        {
            // JsonUtility cannot read a top-level array, so the array is wrapped in an object first.
            var wrapped = JsonUtility.FromJson<EntryList>("{\"items\":" + json + "}");
            if (wrapped?.items == null)
            {
                throw new ArgumentException("elements.json is not a list of elements.");
            }

            var byId = new Dictionary<string, ElementInfo>(StringComparer.Ordinal);
            foreach (var entry in wrapped.items)
            {
                if (!string.IsNullOrEmpty(entry.id))
                {
                    byId[entry.id] = new ElementInfo(entry.id, entry.type, Blank(entry.name), Blank(entry.storey));
                }
            }

            return new ElementIndex(byId);
        }

        public bool TryGet(string id, out ElementInfo element)
        {
            return _byId.TryGetValue(id ?? string.Empty, out element);
        }

        private static string Blank(string value) => string.IsNullOrEmpty(value) ? null : value;

        [Serializable]
        private sealed class EntryList
        {
            public Entry[] items;
        }

        [Serializable]
        private sealed class Entry
        {
            public string id;
            public string type;
            public string name;
            public string storey;
        }
    }
}
