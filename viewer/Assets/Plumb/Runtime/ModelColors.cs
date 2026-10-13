using System.Collections.Generic;
using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// Gives geometry the file left unstyled the colour of its IFC class. One material is made per colour, from
    /// the model's own material, so the renderers stay batched and a selection can still tint them.
    /// </summary>
    public static class ModelColors
    {
        private static readonly int BaseColor = Shader.PropertyToID("baseColorFactor");

        /// <returns>How many material slots were recoloured.</returns>
        public static int Apply(Transform model, ElementIndex index)
        {
            var made = new Dictionary<(Material, Color), Material>();
            var recoloured = 0;

            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                if (!index.TryFind(renderer.transform, model, out _, out var element))
                {
                    continue;
                }

                var slots = renderer.sharedMaterials;
                var changed = false;
                for (var i = 0; i < slots.Length; i++)
                {
                    if (slots[i] == null || !TypePalette.ReplacesMaterial(slots[i].name, element.Type))
                    {
                        continue;
                    }

                    var colour = TypePalette.For(element.Type, element.Name);
                    if (!made.TryGetValue((slots[i], colour), out var coloured))
                    {
                        coloured = new Material(slots[i]) { name = $"{slots[i].name} {ColorUtility.ToHtmlStringRGB(colour)}" };
                        coloured.SetColor(BaseColor, colour);
                        made[(slots[i], colour)] = coloured;
                    }

                    slots[i] = coloured;
                    changed = true;
                    recoloured++;
                }

                if (changed)
                {
                    renderer.sharedMaterials = slots;
                }
            }

            return recoloured;
        }
    }
}
