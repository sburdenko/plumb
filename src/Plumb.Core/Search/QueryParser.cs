using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Plumb.Core.Search
{
    /// <summary>
    /// Reads the text of a search box. Conditions are separated by <c>;</c>. A condition with an operator is about a
    /// property: <c>Name = value</c> (equals), <c>Name ~ value</c> (contains), <c>Name &gt; 10</c>, <c>&gt;=</c>,
    /// <c>&lt;</c>, <c>&lt;=</c>. Anything else is text to look for in names, classes, GlobalIds and tags.
    /// Double quotes keep <c>;</c> and operators inside a name or value.
    /// </summary>
    public static class QueryParser
    {
        private static readonly string[] Operators = { ">=", "<=", "=", "~", ">", "<" };

        public static ParseResult Parse(string? text)
        {
            var segments = Split(text ?? string.Empty);
            if (segments == null)
            {
                return new ParseResult.Invalid("A quote is not closed.");
            }

            var conditions = new List<Condition>();
            foreach (var segment in segments)
            {
                if (segment.Trim().Length == 0)
                {
                    continue;
                }

                var result = ParseCondition(segment);
                if (!(result is ParseResult.Parsed parsed))
                {
                    return result;
                }

                conditions.AddRange(parsed.Query.Conditions);
            }

            return new ParseResult.Parsed(new ElementQuery(conditions));
        }

        private static ParseResult ParseCondition(string segment)
        {
            var position = FindOperator(segment, out var op);
            if (position < 0)
            {
                return Single(new Condition.Text(Unquote(segment)));
            }

            var name = Unquote(segment.Substring(0, position));
            var value = Unquote(segment.Substring(position + op.Length));
            if (name.Length == 0)
            {
                return new ParseResult.Invalid($"Write a property name before {op}.");
            }

            switch (op)
            {
                case "=":
                case "~":
                    if (value.Length == 0)
                    {
                        return new ParseResult.Invalid($"Write a value after {op}.");
                    }

                    return Single(op == "="
                        ? new Condition.PropertyEquals(name, value)
                        : new Condition.PropertyContains(name, value));
                default:
                    if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !IsFinite(number))
                    {
                        return new ParseResult.Invalid(value.Length == 0
                            ? $"Write a number after {op}."
                            : $"{op} needs a number, not \"{value}\".");
                    }

                    return Single(new Condition.PropertyNumber(name, ComparisonOf(op), number));
            }
        }

        private static ParseResult Single(Condition condition) =>
            new ParseResult.Parsed(new ElementQuery(new[] { condition }));

        private static NumberComparison ComparisonOf(string op)
        {
            switch (op)
            {
                case ">=": return NumberComparison.GreaterOrEqual;
                case "<=": return NumberComparison.LessOrEqual;
                case ">": return NumberComparison.Greater;
                default: return NumberComparison.Less;
            }
        }

        /// <returns>The segments, or null when a quote is left open.</returns>
        private static List<string>? Split(string text)
        {
            var segments = new List<string>();
            var current = new StringBuilder();
            var quoted = false;
            foreach (var c in text)
            {
                if (c == '"')
                {
                    quoted = !quoted;
                }

                if (c == ';' && !quoted)
                {
                    segments.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            if (quoted)
            {
                return null;
            }

            segments.Add(current.ToString());
            return segments;
        }

        private static int FindOperator(string segment, out string op)
        {
            var quoted = false;
            for (var i = 0; i < segment.Length; i++)
            {
                if (segment[i] == '"')
                {
                    quoted = !quoted;
                    continue;
                }

                if (quoted)
                {
                    continue;
                }

                foreach (var candidate in Operators)
                {
                    if (string.CompareOrdinal(segment, i, candidate, 0, candidate.Length) == 0)
                    {
                        op = candidate;
                        return i;
                    }
                }
            }

            op = string.Empty;
            return -1;
        }

        private static string Unquote(string text)
        {
            var trimmed = text.Trim();
            return trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"'
                ? trimmed.Substring(1, trimmed.Length - 2).Trim()
                : trimmed;
        }

        private static bool IsFinite(double number) => !double.IsNaN(number) && !double.IsInfinity(number);
    }
}
