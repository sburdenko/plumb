namespace Plumb.Core.Search
{
    /// <summary>What the text of a search box means: a query, or why it is not one yet.</summary>
    public abstract record ParseResult
    {
        private ParseResult()
        {
        }

        public sealed record Parsed(ElementQuery Query) : ParseResult;

        /// <param name="Reason">A sentence for the person typing, naming what is missing.</param>
        public sealed record Invalid(string Reason) : ParseResult;
    }
}
