using System;
using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// Colours by IFC class for geometry the file left unstyled. Most IFC files from authoring tools carry no
    /// surface styles for services, and IfcConvert paints those one grey; a viewer shows them by class instead.
    /// </summary>
    public static class TypePalette
    {
        public static readonly Color Neutral = Graphite.Hex(0xB9B4AC);

        private const string ConverterDefault = "DefaultMaterial";

        private static readonly Color Wall = Graphite.Hex(0xD9D2C5);
        private static readonly Color CurtainWall = Graphite.Hex(0x8FB4C9);
        private static readonly Color Window = Graphite.Hex(0x6FB3D2);
        private static readonly Color Door = Graphite.Hex(0x9A7650);
        private static readonly Color Roof = Graphite.Hex(0xB8745A);
        private static readonly Color Slab = Graphite.Hex(0xA9B0B8);
        private static readonly Color Covering = Graphite.Hex(0xC8C3BA);
        private static readonly Color Frame = Graphite.Hex(0xC9B28A);
        private static readonly Color Steel = Graphite.Hex(0x98A5B3);
        private static readonly Color Stair = Graphite.Hex(0xB8AFA3);
        private static readonly Color Railing = Graphite.Hex(0x7F8C99);
        private static readonly Color Foundation = Graphite.Hex(0x8A8580);
        private static readonly Color Furniture = Graphite.Hex(0xA98FB0);
        private static readonly Color Terrain = Graphite.Hex(0x7FA36B);
        private static readonly Color Duct = Graphite.Hex(0x5B9BD5);
        private static readonly Color Pipe = Graphite.Hex(0x5DB38A);
        private static readonly Color Electric = Graphite.Hex(0xE0A838);
        private static readonly Color Terminal = Graphite.Hex(0xC9DCE8);
        private static readonly Color Controller = Graphite.Hex(0xD0605E);
        private static readonly Color Moving = Graphite.Hex(0xE07B39);
        private static readonly Color Energy = Graphite.Hex(0xC94F7C);
        private static readonly Color Storage = Graphite.Hex(0x8E7CC3);
        private static readonly Color Flow = Graphite.Hex(0x5CB5A5);

        // First match wins, so a specific class comes before the family it belongs to.
        private static readonly (string[] Needles, Color Colour)[] Rules =
        {
            (new[] { "CurtainWall" }, CurtainWall),
            (new[] { "Wall" }, Wall),
            (new[] { "Window" }, Window),
            (new[] { "Door" }, Door),
            (new[] { "Roof" }, Roof),
            (new[] { "Slab" }, Slab),
            (new[] { "Covering" }, Covering),
            (new[] { "Column", "Beam" }, Frame),
            (new[] { "Member", "Plate" }, Steel),
            (new[] { "Stair", "Ramp" }, Stair),
            (new[] { "Railing" }, Railing),
            (new[] { "Footing", "Pile" }, Foundation),
            (new[] { "Furnishing", "Furniture" }, Furniture),
            (new[] { "IfcSite", "Geographic", "Terrain" }, Terrain),
            (new[] { "Duct" }, Duct),
            (new[] { "Pipe" }, Pipe),
            (new[] { "Cable", "Conduit", "Electric", "Lamp", "Light" }, Electric),
            (new[] { "Terminal" }, Terminal),
            (new[] { "Valve", "Damper", "FlowController" }, Controller),
            (new[] { "Pump", "Fan", "MovingDevice" }, Moving),
            (new[] { "EnergyConversion", "Boiler", "Chiller" }, Energy),
            (new[] { "Storage", "Tank" }, Storage),
        };

        private static readonly (string[] Needles, Color Colour)[] FlowNameHints =
        {
            (new[] { "duct" }, Duct),
            (new[] { "pipe" }, Pipe),
            (new[] { "cable", "conduit", "tray" }, Electric),
        };

        /// <param name="name">The element's name, which tells ducts from pipes where the class is only "flow segment".</param>
        public static Color For(string ifcType, string name)
        {
            if (string.IsNullOrEmpty(ifcType))
            {
                return Neutral;
            }

            foreach (var rule in Rules)
            {
                if (ContainsAny(ifcType, rule.Needles))
                {
                    return rule.Colour;
                }
            }

            if (!ContainsAny(ifcType, new[] { "Flow", "Distribution" }))
            {
                return Neutral;
            }

            foreach (var hint in FlowNameHints)
            {
                if (name != null && ContainsAny(name, hint.Needles))
                {
                    return hint.Colour;
                }
            }

            return Flow;
        }

        /// <summary>
        /// True for the materials IfcConvert makes up when the file has no style: "DefaultMaterial", and a material
        /// named after the element's own class. A style that came from the file is named after the style.
        /// </summary>
        public static bool ReplacesMaterial(string materialName, string ifcType)
        {
            if (string.IsNullOrEmpty(materialName))
            {
                return false;
            }

            return string.Equals(materialName, ConverterDefault, StringComparison.Ordinal)
                || string.Equals(materialName, ifcType, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsAny(string text, string[] needles)
        {
            foreach (var needle in needles)
            {
                if (text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
