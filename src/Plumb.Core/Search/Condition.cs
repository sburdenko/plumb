namespace Plumb.Core.Search
{
    /// <summary>One thing an element has to satisfy to match a query.</summary>
    public abstract record Condition
    {
        private Condition()
        {
        }

        /// <summary>The name, IFC class, GlobalId or tag (with or without its <c>#</c>) contains the term, ignoring case.</summary>
        public sealed record Text(string Term) : Condition;

        /// <summary>
        /// The element has a property of this name whose value equals the wanted one: ignoring case, and as numbers
        /// when both are numbers, so <c>4.570</c> finds <c>4.57</c>.
        /// </summary>
        public sealed record PropertyEquals(string Name, string Value) : Condition;

        /// <summary>The element has a property of this name whose value contains the text, ignoring case.</summary>
        public sealed record PropertyContains(string Name, string Value) : Condition;

        /// <summary>The element has a property of this name whose value is a number that passes the comparison.</summary>
        public sealed record PropertyNumber(string Name, NumberComparison Comparison, double Number) : Condition;
    }
}
