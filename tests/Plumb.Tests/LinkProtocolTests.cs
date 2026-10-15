using Plumb.Core.Link;

namespace Plumb.Tests;

[TestFixture]
public sealed class LinkProtocolTests
{
    private static IEnumerable<string> SharedExamples() =>
        File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, "link-examples.txt"))
            .Where(line => line.Length > 0);

    [TestCaseSource(nameof(SharedExamples))]
    public void EveryExampleTheViewerAlsoChecksReadsAndWritesBackUnchanged(string line)
    {
        var message = LinkProtocol.Parse(line);

        Assert.That(message, Is.Not.TypeOf<LinkMessage.Unknown>());
        Assert.That(LinkProtocol.Format(message), Is.EqualTo(line));
    }

    [Test]
    public void HelloCarriesTheVersionAndToken()
    {
        Assert.That(LinkProtocol.Parse("hello 1 abc"), Is.EqualTo(new LinkMessage.Hello(1, "abc")));
    }

    [Test]
    public void SelectCarriesTheGlobalId()
    {
        Assert.That(LinkProtocol.Parse("select 2O2Fr$t4X7Zf8NOew3FNql"), Is.EqualTo(new LinkMessage.Select("2O2Fr$t4X7Zf8NOew3FNql")));
    }

    [Test]
    public void ClearHasNoArgument()
    {
        Assert.That(LinkProtocol.Parse("clear"), Is.EqualTo(new LinkMessage.Clear()));
    }

    [TestCase("")]
    [TestCase("frame 2O2Fr$t4X7Zf8NOew3FNql")]
    [TestCase("select")]
    [TestCase("select ")]
    [TestCase("select two words")]
    [TestCase("clear now")]
    [TestCase("hello one abc")]
    [TestCase("hello 1")]
    [TestCase("hello 1 abc extra")]
    [TestCase("SELECT 2O2Fr$t4X7Zf8NOew3FNql")]
    public void AnythingElseIsUnknownSoANewerPeerCanAddMessages(string line)
    {
        Assert.That(LinkProtocol.Parse(line), Is.EqualTo(new LinkMessage.Unknown(line)));
    }

    [Test]
    public void ALineEndingIsNotPartOfTheMessage()
    {
        Assert.That(LinkProtocol.Parse("clear\r"), Is.EqualTo(new LinkMessage.Clear()));
    }

    [TestCase("two words")]
    [TestCase("line\nbreak")]
    [TestCase("")]
    public void AGlobalIdThatWouldBreakTheLineCannotBeWritten(string globalId)
    {
        Assert.That(() => LinkProtocol.Format(new LinkMessage.Select(globalId)), Throws.ArgumentException);
    }

    [Test]
    public void AnUnknownMessageCannotBeWritten()
    {
        Assert.That(() => LinkProtocol.Format(new LinkMessage.Unknown("frame x")), Throws.ArgumentException);
    }
}
