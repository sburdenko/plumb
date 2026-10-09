using NUnit.Framework;

namespace Plumb.Viewer.Tests
{
    public sealed class ViewerArgumentsTests
    {
        [Test]
        public void ReadsThePackagePath()
        {
            var arguments = ViewerArguments.Parse(new[] { "/Applications/Plumb Viewer", "--package", "/models/Duplex.plumb" });

            Assert.That(arguments.HasPackage, Is.True);
            Assert.That(arguments.PackagePath, Is.EqualTo("/models/Duplex.plumb"));
        }

        [Test]
        public void NoPackageWithoutTheFlag()
        {
            var arguments = ViewerArguments.Parse(new[] { "/Applications/Plumb Viewer" });

            Assert.That(arguments.HasPackage, Is.False);
            Assert.That(arguments.TakesScreenshot, Is.False);
        }

        [Test]
        public void FlagWithoutValueIsIgnored()
        {
            Assert.That(ViewerArguments.Parse(new[] { "viewer", "--package" }).HasPackage, Is.False);
        }

        [Test]
        public void ReadsSelectionAndScreenshot()
        {
            var arguments = ViewerArguments.Parse(new[] { "viewer", "--select", "2O2Fr$t4X7Zf8NOew3FNtn", "--screenshot", "/tmp/shot.png" });

            Assert.That(arguments.SelectId, Is.EqualTo("2O2Fr$t4X7Zf8NOew3FNtn"));
            Assert.That(arguments.TakesScreenshot, Is.True);
        }
    }
}
