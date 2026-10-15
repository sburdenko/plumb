using Plumb.Core.Search;

namespace Plumb.Tests;

[TestFixture]
public sealed class QueryParserTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase(" ; ")]
    public void ABlankBoxHasNothingToSearch(string? text)
    {
        Assert.That(QueryParser.Parse(text), Is.TypeOf<ParseResult.Blank>());
    }

    [Test]
    public void PlainWordsAreOneTextCondition()
    {
        Assert.That(Parse("Basic Wall:Exterior - Brick"), Is.EqualTo(new Condition[] { new Condition.Text("Basic Wall:Exterior - Brick") }));
    }

    [Test]
    public void ATagIsText()
    {
        Assert.That(Parse(" #139117 "), Is.EqualTo(new Condition[] { new Condition.Text("#139117") }));
    }

    [Test]
    public void EqualsComparesAPropertyValue()
    {
        Assert.That(Parse("FireRating = REI 90"), Is.EqualTo(new Condition[] { new Condition.PropertyEquals("FireRating", "REI 90") }));
    }

    [Test]
    public void ATildeSearchesInsideAValue()
    {
        Assert.That(Parse("Size ~ 1150"), Is.EqualTo(new Condition[] { new Condition.PropertyContains("Size", "1150") }));
    }

    [TestCase("Area > 10", NumberComparison.Greater)]
    [TestCase("Area >= 10", NumberComparison.GreaterOrEqual)]
    [TestCase("Area < 10", NumberComparison.Less)]
    [TestCase("Area <= 10", NumberComparison.LessOrEqual)]
    public void NumbersAreComparedWithAnOperator(string text, NumberComparison comparison)
    {
        Assert.That(Parse(text), Is.EqualTo(new Condition[] { new Condition.PropertyNumber("Area", comparison, 10) }));
    }

    [Test]
    public void AnOperatorNeedsNoSpacesAndANameMayHaveSome()
    {
        Assert.That(Parse("Base Constraint=Level 1"), Is.EqualTo(new Condition[] { new Condition.PropertyEquals("Base Constraint", "Level 1") }));
    }

    [Test]
    public void NumbersUseADotAndAMinusSign()
    {
        Assert.That(Parse("Offset >= -1.25"), Is.EqualTo(new Condition[] { new Condition.PropertyNumber("Offset", NumberComparison.GreaterOrEqual, -1.25) }));
    }

    [Test]
    public void ASemicolonJoinsConditions()
    {
        Assert.That(Parse("wall; FireRating = REI 90 ; Area > 10"), Is.EqualTo(new Condition[]
        {
            new Condition.Text("wall"),
            new Condition.PropertyEquals("FireRating", "REI 90"),
            new Condition.PropertyNumber("Area", NumberComparison.Greater, 10),
        }));
    }

    [Test]
    public void QuotesKeepSemicolonsAndOperatorsInsideAName()
    {
        Assert.That(Parse("\"Rating; fire\" = \"a; b = c\""), Is.EqualTo(new Condition[] { new Condition.PropertyEquals("Rating; fire", "a; b = c") }));
    }

    [Test]
    public void AQuotedTextIsNotMistakenForAProperty()
    {
        Assert.That(Parse("\"a = b\""), Is.EqualTo(new Condition[] { new Condition.Text("a = b") }));
    }

    [TestCase("Area >", "number")]
    [TestCase("Area > abc", "number")]
    [TestCase("= 5", "name")]
    [TestCase("Area =", "value")]
    [TestCase("Area ~", "value")]
    [TestCase("wall; = 5", "name")]
    [TestCase("\"Area = 5", "quote")]
    public void AnUnfinishedConditionSaysWhatIsMissing(string text, string mentions)
    {
        var result = QueryParser.Parse(text);

        Assert.That(result, Is.TypeOf<ParseResult.Invalid>());
        Assert.That(((ParseResult.Invalid)result).Reason, Does.Contain(mentions));
    }

    private static IReadOnlyList<Condition> Parse(string? text)
    {
        var result = QueryParser.Parse(text);
        Assert.That(result, Is.TypeOf<ParseResult.Parsed>(), () => (result as ParseResult.Invalid)?.Reason ?? string.Empty);
        return ((ParseResult.Parsed)result).Query.Conditions;
    }
}
