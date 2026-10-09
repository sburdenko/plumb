using Plumb.App.ViewModels;
using Plumb.Core.Geometry;

namespace Plumb.Tests;

[TestFixture]
public sealed class GeometryWarningTests
{
    [TestCase(GeometryError.ConverterMissing, "not found at /x", "3D geometry was not built: IfcConvert is not installed next to Plumb.")]
    [TestCase(GeometryError.ConverterFailed, "exit code 1: Unable to parse input file", "3D geometry was not built: IfcConvert could not convert this file. (exit code 1: Unable to parse input file)")]
    [TestCase(GeometryError.ConverterFailed, "", "3D geometry was not built: IfcConvert could not convert this file.")]
    [TestCase(GeometryError.Timeout, "stopped after 600 s", "3D geometry was not built: IfcConvert took too long and was stopped.")]
    [TestCase(GeometryError.FileMissing, "", "This package has no 3D geometry.")]
    [TestCase(GeometryError.Unknown, "needs 12 GB", "3D geometry was not built. (needs 12 GB)")]
    public void WordsAMessageForEveryError(GeometryError error, string detail, string expected)
    {
        Assert.That(GeometryWarning.For(new GeometryState.NotBuilt(error, detail)), Is.EqualTo(expected));
    }

    [Test]
    public void EveryErrorHasAMessage()
    {
        foreach (var error in Enum.GetValues<GeometryError>())
        {
            Assert.That(() => GeometryWarning.For(new GeometryState.NotBuilt(error, string.Empty)), Throws.Nothing, error.ToString());
        }
    }
}
