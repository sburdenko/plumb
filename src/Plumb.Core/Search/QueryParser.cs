using System;
using System.Collections.Generic;
using System.Linq;
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
            if (segments is null)
            {
                return new ParseResult.Invalid("A quote is not closed.");
            }

            var conditions = new List<Condition>();
            foreach (var segment in segments.Where(segment => segment.Trim().Length > 0))
            {
                switch (ReadCondition(segment))
                {
                    case ConditionRead.Read read:
                        conditions.Add(read.Condition);
                        break;
                    case ConditionRead.Rejected rejected:
                        return new ParseResult.Invalid(rejected.Reason);
                }
            }

            return conditions.Count == 0
                ? new ParseResult.Blank()
                : new ParseResult.Parsed(new ElementQuery(conditions));
        }

        private static ConditionRead ReadCondition(string segment)
        {
            if (FindOperator(segment) is not { } found)
            {
                return new ConditionRead.Read(new Condition.Text(Unquote(segment)));
            }

            var (position, op) = found;
            var name = Unquote(segment.Substring(0, position));
            var value = Unquote(segment.Substring(position + op.Length));
            if (name.Length == 0)
            {
                return new ConditionRead.Rejected($"Write a property name before {op}.");
            }

            return op switch
            {
                "=" or "~" when value.Length == 0 => new ConditionRead.Rejected($"Write a value after {op}."),
                "=" => new ConditionRead.Read(new Condition.PropertyEquals(name, value)),
                "~" => new ConditionRead.Read(new Condition.PropertyContains(name, value)),
                _ => ReadNumberCondition(name, op, value),
            };
        }

        private static ConditionRead ReadNumberCondition(string name, string op, string value) =>
            Numbers.TryParseFinite(value, out var number)
                ? new ConditionRead.Read(new Condition.PropertyNumber(name, ComparisonOf(op), number))
                : new ConditionRead.Rejected(value.Length == 0
                    ? $"Write a number after {op}."
                    : $"{op} needs a number, not \"{value}\".");

        private static NumberComparison ComparisonOf(string op) => op switch
        {
            ">=" => NumberComparison.GreaterOrEqual,
            "<=" => NumberComparison.LessOrEqual,
            ">" => NumberComparison.Greater,
            "<" => NumberComparison.Less,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Not a number comparison."),
        };

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

        /// <returns>The first operator outside quotes and where it starts, or null when there is none.</returns>
        private static (int Position, string Operator)? FindOperator(string segment)
        {
            var quoted = false;
            for (var i = 0; i < segment.Length; i++)
            {
                if (segment[i] == '"')
                {
                    quoted = !quoted;
                }
                else if (!quoted && Operators.FirstOrDefault(op => string.CompareOrdinal(segment, i, op, 0, op.Length) == 0) is { } op)
                {
                    return (i, op);
                }
            }

            return null;
        }

        private static string Unquote(string text)
        {
            var trimmed = text.Trim();
            return trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"'
                ? trimmed.Substring(1, trimmed.Length - 2).Trim()
                : trimmed;
        }

        /// <summary>One condition read from the text, or why it could not be.</summary>
        private abstract record ConditionRead
        {
            private ConditionRead()
            {
            }

            public sealed record Read(Condition Condition) : ConditionRead;

            public sealed record Rejected(string Reason) : ConditionRead;
        }
    }
}
