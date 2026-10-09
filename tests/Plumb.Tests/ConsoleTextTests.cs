using System.Text;
using Plumb.Geometry;

namespace Plumb.Tests;

[TestFixture]
public sealed class ConsoleTextTests
{
    private const string Line = "[error] [SYN001] Unable to parse input file 'C:\\models\\junk.ifc'";

    [Test]
    public void DecodesUtf8()
    {
        Assert.That(ConsoleText.Decode(Encoding.UTF8.GetBytes(Line)), Is.EqualTo(Line));
    }

    [Test]
    public void DecodesUtf16AsWrittenByIfcConvertOnWindows()
    {
        Assert.That(ConsoleText.Decode(Encoding.Unicode.GetBytes(Line + "\r\n")), Is.EqualTo(Line + "\r\n"));
    }

    [Test]
    public void DecodesUtf16WithByteOrderMark()
    {
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(Line)).ToArray();

        Assert.That(ConsoleText.Decode(bytes), Is.EqualTo(Line));
    }

    [Test]
    public void KeepsNonAsciiUtf8()
    {
        const string text = "Fläche 12 m²";

        Assert.That(ConsoleText.Decode(Encoding.UTF8.GetBytes(text)), Is.EqualTo(text));
    }

    [Test]
    public void EmptyOutputIsEmpty()
    {
        Assert.That(ConsoleText.Decode([]), Is.Empty);
    }
}
