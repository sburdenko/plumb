using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Plumb.Viewer.Tests
{
    public sealed class ColliderBuilderTests
    {
        private GameObject _root;

        [SetUp]
        public void CreateMeshes()
        {
            _root = new GameObject("Model");
            var mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.up, Vector3.right }, triangles = new[] { 0, 1, 2 } };
            for (var i = 0; i < 5; i++)
            {
                var child = new GameObject($"Element{i}");
                child.transform.SetParent(_root.transform);
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
            }
        }

        [TearDown]
        public void DestroyMeshes() => Object.DestroyImmediate(_root);

        [Test]
        public void ZeroBudgetStillAddsOneColliderPerStep()
        {
            var builder = new ColliderBuilder(_root.GetComponentsInChildren<MeshFilter>());

            builder.Advance(0);

            Assert.That(_root.GetComponentsInChildren<MeshCollider>(), Has.Length.EqualTo(1));
            Assert.That(builder.Progress, Is.EqualTo(0.2f).Within(0.001f));
        }

        [Test]
        public void EveryMeshGetsAColliderEventually()
        {
            var builder = new ColliderBuilder(_root.GetComponentsInChildren<MeshFilter>());

            for (var step = 0; step < 10 && !builder.Done; step++)
            {
                builder.Advance(0);
            }

            Assert.That(builder.Done, Is.True);
            Assert.That(_root.GetComponentsInChildren<MeshCollider>().Select(c => c.sharedMesh), Has.All.Not.Null);
            Assert.That(_root.GetComponentsInChildren<MeshCollider>(), Has.Length.EqualTo(5));
        }
    }
}
