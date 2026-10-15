namespace Plumb.Core.Search
{
    /// <summary>What the text of a search box means: nothing to search, a query, or why it is not one yet.</summary>
    public abstract record ParseResult
    {
        private ParseResult()
        {
        }

        /// <summary>The box is empty or holds only separators.</summary>
        public sealed record Blank : ParseResult;

        public sealed record Parsed(ElementQuery Query) : ParseResult;

        /// <param name="Reason">A sentence for the person typing, naming what is missing.</param>
        public sealed record Invalid(string Reason) : ParseResult;
    }
}
