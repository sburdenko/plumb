using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// Tints the renderers of one element through a property block, leaving glTFast's materials untouched.
    /// </summary>
    public sealed class SelectionHighlight
    {
        private static readonly int BaseColor = Shader.PropertyToID("baseColorFactor");
        private static readonly int Emissive = Shader.PropertyToID("emissiveFactor");
        private static readonly Color Tint = new Color(1f, 0.72f, 0.18f, 1f);
        private static readonly Color Glow = new Color(0.35f, 0.2f, 0f, 1f);

        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private Renderer[] _renderers = new Renderer[0];

        public bool HasSelection => _renderers.Length > 0;

        public void Show(Transform element)
        {
            Clear();
            _renderers = element.GetComponentsInChildren<Renderer>();
            _block.SetColor(BaseColor, Tint);
            _block.SetColor(Emissive, Glow);
            foreach (var renderer in _renderers)
            {
                renderer.SetPropertyBlock(_block);
            }
        }

        public void Clear()
        {
            foreach (var renderer in _renderers)
            {
                if (renderer != null)
                {
                    renderer.SetPropertyBlock(null);
                }
            }

            _renderers = new Renderer[0];
        }

        public bool TryGetBounds(out Bounds bounds)
        {
            return RendererBounds.TryEncapsulate(_renderers, out bounds);
        }
    }
}
