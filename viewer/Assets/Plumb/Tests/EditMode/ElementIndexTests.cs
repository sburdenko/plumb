using System;
using NUnit.Framework;

namespace Plumb.Viewer.Tests
{
    public sealed class ElementIndexTests
    {
        private const string Json = @"[
  { ""id"": ""P"", ""type"": ""IfcProject"", ""name"": ""0001"", ""storey"": null },
  { ""id"": ""B"", ""type"": ""IfcBuilding"", ""name"": null, ""storey"": null },
  { ""id"": ""2O2Fr$t4X7Zf8NOew3FNtn"", ""type"": ""IfcWallStandardCase"", ""name"": ""Basic Wall:Exterior"", ""storey"": ""Level 1"" }
]";

        [Test]
        public void FindsAnElementByGlobalId()
        {
            var index = ElementIndex.Parse(Json);

            Assert.That(index.TryGet("2O2Fr$t4X7Zf8NOew3FNtn", out var wall), Is.True);
            Assert.That(wall.Type, Is.EqualTo("IfcWallStandardCase"));
            Assert.That(wall.Name, Is.EqualTo("Basic Wall:Exterior"));
            Assert.That(wall.Storey, Is.EqualTo("Level 1"));
        }

        [Test]
        public void MissingNameAndStoreyAreNull()
        {
            ElementIndex.Parse(Json).TryGet("B", out var building);

            Assert.That(building.Name, Is.Null);
            Assert.That(building.Storey, Is.Null);
        }

        [Test]
        public void UnknownIdIsNotFound()
        {
            Assert.That(ElementIndex.Parse(Json).TryGet("Node_17", out _), Is.False);
        }

        [Test]
        public void CountsEveryElement()
        {
            Assert.That(ElementIndex.Parse(Json).Count, Is.EqualTo(3));
        }

        [Test]
        public void NotAListIsRejected()
        {
            Assert.That(() => ElementIndex.Parse("{ \"formatVersion\": 1 }"), Throws.InstanceOf<ArgumentException>());
        }
    }
}
