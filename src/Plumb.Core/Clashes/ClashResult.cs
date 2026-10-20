using System;
using System.Collections.Generic;

namespace Plumb.Core.Clashes
{
    /// <summary>What a clash search came to.</summary>
    public abstract record ClashResult
    {
        private ClashResult()
        {
        }

        /// <param name="Clashes">Each clashing pair once, deepest first; empty when nothing clashes.</param>
        public sealed record Found(IReadOnlyList<Clash> Clashes, TimeSpan Elapsed) : ClashResult;

        /// <param name="Message">A sentence for the person, saying why the search could not run.</param>
        public sealed record Failed(string Message) : ClashResult;

        public sealed record Cancelled : ClashResult;
    }

    /// <summary>The steps of a clash search, in order.</summary>
    public enum ClashStep
    {
        ReadingGeometry,
        Comparing,
    }
}
