using NUnit.Framework;
using UnityEngine;

namespace Plumb.Viewer.Tests
{
    public sealed class TypePaletteTests
    {
        [Test]
        public void WallsOfEveryKindShareOneColour()
        {
            Assert.That(TypePalette.For("IfcWall", null), Is.EqualTo(TypePalette.For("IfcWallStandardCase", null)));
        }

        [Test]
        public void TheMainBuildingClassesAreToldApart()
        {
            var types = new[] { "IfcWall", "IfcSlab", "IfcRoof", "IfcDoor", "IfcWindow", "IfcColumn", "IfcStair", "IfcRailing" };

            var colours = new System.Collections.Generic.HashSet<Color>();
            foreach (var type in types)
            {
                colours.Add(TypePalette.For(type, null));
            }

            Assert.That(colours.Count, Is.EqualTo(types.Length));
        }

        [Test]
        public void ServicesAreToldApartByTheirClass()
        {
            Assert.That(TypePalette.For("IfcDuctSegment", null), Is.Not.EqualTo(TypePalette.For("IfcPipeSegment", null)));
            Assert.That(TypePalette.For("IfcDuctFitting", null), Is.EqualTo(TypePalette.For("IfcDuctSegment", null)));
        }

        [Test]
        public void AGenericFlowSegmentIsToldApartByItsName()
        {
            var duct = TypePalette.For("IfcFlowSegment", "Rectangular Duct:Mitered Elbows:566710");
            var pipe = TypePalette.For("IfcFlowSegment", "Pipe Types:Standard:230044");
            var plain = TypePalette.For("IfcFlowSegment", "Segment 12");

            Assert.That(duct, Is.EqualTo(TypePalette.For("IfcDuctSegment", null)));
            Assert.That(pipe, Is.EqualTo(TypePalette.For("IfcPipeSegment", null)));
            Assert.That(plain, Is.Not.EqualTo(duct));
            Assert.That(plain, Is.Not.EqualTo(pipe));
        }

        [Test]
        public void ANameDoesNotChangeAClassThatHasItsOwnColour()
        {
            Assert.That(TypePalette.For("IfcWall", "Duct shaft wall"), Is.EqualTo(TypePalette.For("IfcWall", null)));
        }

        [TestCase("IfcSomethingNew")]
        [TestCase("")]
        [TestCase(null)]
        public void UnknownClassesGetOneNeutralColour(string type)
        {
            Assert.That(TypePalette.For(type, null), Is.EqualTo(TypePalette.Neutral));
        }

        [TestCase("DefaultMaterial", "IfcWall", true)]
        [TestCase("IfcWall", "IfcWall", true)]
        [TestCase("ifcwall", "IfcWall", true)]
        [TestCase("IfcSurfaceStyleRendering-3791", "IfcWall", false)]
        [TestCase("Brick", "IfcWall", false)]
        [TestCase("IfcDoor", "IfcWall", false)]
        [TestCase(null, "IfcWall", false)]
        public void OnlyColoursTheConverterMadeUpAreReplaced(string material, string type, bool replaced)
        {
            Assert.That(TypePalette.ReplacesMaterial(material, type), Is.EqualTo(replaced));
        }
    }
}
