using System.Collections.Generic;
using UnityEngine;

namespace Plumb.Viewer
{
    public static class RendererBounds
    {
        public static bool TryEncapsulate(IEnumerable<Renderer> renderers, out Bounds bounds)
        {
            bounds = default;
            var found = false;
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (found)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    found = true;
                }
            }

            return found;
        }
    }
}
