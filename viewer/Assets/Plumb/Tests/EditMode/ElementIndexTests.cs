using System;
using NUnit.Framework;
using UnityEngine;

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

        [Test]
        public void FindsTheElementAboveAMeshNode()
        {
            var index = ElementIndex.Parse("[{\"id\":\"W1\",\"type\":\"IfcWall\",\"name\":\"Wall\",\"storey\":null}]");
            var root = new GameObject("Model").transform;
            var element = new GameObject("W1").transform;
            var mesh = new GameObject("mesh 0").transform;
            element.SetParent(root);
            mesh.SetParent(element);

            var found = index.TryFind(mesh, root, out var node, out var info);

            Assert.That(found, Is.True);
            Assert.That(node, Is.SameAs(element));
            Assert.That(info.Type, Is.EqualTo("IfcWall"));
            UnityEngine.Object.DestroyImmediate(root.gameObject);
        }

        [Test]
        public void FindsNothingForANodeThatIsNoElement()
        {
            var index = ElementIndex.Parse("[]");
            var root = new GameObject("Model").transform;
            var stray = new GameObject("stray").transform;
            stray.SetParent(root);

            Assert.That(index.TryFind(stray, root, out _, out _), Is.False);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
        }
    }
}
