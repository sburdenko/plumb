using System;
using System.Collections.Generic;
using Plumb.Core.Model;

namespace Plumb.Core.Search
{
    /// <summary>
    /// Finds the elements of one model that satisfy a query. Properties are indexed by name the first time a
    /// property condition needs them, so a text-only search never pays for the index.
    /// </summary>
    public sealed class ElementSearch
    {
        private const double NumberTolerance = 1e-9;

        private readonly IReadOnlyList<ElementRecord> _elements;
        private readonly Lazy<Dictionary<string, List<PropertyRecord>>> _propertiesByName;

        public ElementSearch(IReadOnlyList<ElementRecord> elements, IReadOnlyList<PropertyRecord> properties)
        {
            _elements = elements;
            _propertiesByName = new Lazy<Dictionary<string, List<PropertyRecord>>>(() => IndexByName(properties));
        }

        public ElementMatches Find(ElementQuery query)
        {
            HashSet<string>? matches = null;
            foreach (var condition in query.Conditions)
            {
                var found = Evaluate(condition);
                if (matches is null)
                {
                    matches = found;
                }
                else
                {
                    matches.IntersectWith(found);
                }

                if (matches.Count == 0)
                {
                    break;
                }
            }

            return new ElementMatches(matches ?? AllGlobalIds());
        }

        private HashSet<string> Evaluate(Condition condition) => condition switch
        {
            Condition.Text text => MatchText(text.Term),
            Condition.PropertyEquals equals => MatchProperty(equals.Name, value => ValuesAreEqual(value, equals.Value)),
            Condition.PropertyContains contains => MatchProperty(contains.Name, value => Contains(value, contains.Value)),
            Condition.PropertyNumber number => MatchProperty(number.Name, value => Passes(value, number)),
            _ => throw new ArgumentOutOfRangeException(nameof(condition), condition.GetType().Name, "Unknown condition."),
        };

        private HashSet<string> MatchText(string term)
        {
            var matches = new HashSet<string>(StringComparer.Ordinal);
            foreach (var element in _elements)
            {
                if (Contains(element.Name, term) || Contains(element.IfcType, term) || Contains(element.GlobalId, term)
                    || Contains(element.Tag is null ? null : "#" + element.Tag, term))
                {
                    matches.Add(element.GlobalId);
                }
            }

            return matches;
        }

        private HashSet<string> MatchProperty(string name, Func<string, bool> valuePasses)
        {
            var matches = new HashSet<string>(StringComparer.Ordinal);
            if (!_propertiesByName.Value.TryGetValue(name, out var properties))
            {
                return matches;
            }

            foreach (var property in properties)
            {
                if (property.Value is { } value && valuePasses(value))
                {
                    matches.Add(property.GlobalId);
                }
            }

            return matches;
        }

        private HashSet<string> AllGlobalIds()
        {
            var all = new HashSet<string>(StringComparer.Ordinal);
            foreach (var element in _elements)
            {
                all.Add(element.GlobalId);
            }

            return all;
        }

        private static Dictionary<string, List<PropertyRecord>> IndexByName(IReadOnlyList<PropertyRecord> properties)
        {
            var byName = new Dictionary<string, List<PropertyRecord>>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in properties)
            {
                if (!byName.TryGetValue(property.Name, out var list))
                {
                    list = new List<PropertyRecord>();
                    byName[property.Name] = list;
                }

                list.Add(property);
            }

            return byName;
        }

        private static bool ValuesAreEqual(string stored, string wanted)
        {
            if (Numbers.TryParseFinite(stored, out var storedNumber) && Numbers.TryParseFinite(wanted, out var wantedNumber))
            {
                var scale = Math.Max(1d, Math.Max(Math.Abs(storedNumber), Math.Abs(wantedNumber)));
                return Math.Abs(storedNumber - wantedNumber) <= NumberTolerance * scale;
            }

            return string.Equals(stored.Trim(), wanted, StringComparison.OrdinalIgnoreCase);
        }

        private static bool Passes(string value, Condition.PropertyNumber condition) =>
            Numbers.TryParseFinite(value, out var number) && condition.Comparison switch
            {
                NumberComparison.Less => number < condition.Number,
                NumberComparison.LessOrEqual => number <= condition.Number,
                NumberComparison.Greater => number > condition.Number,
                NumberComparison.GreaterOrEqual => number >= condition.Number,
                _ => throw new ArgumentOutOfRangeException(nameof(condition), condition.Comparison, "Unknown comparison."),
            };

        private static bool Contains(string? text, string term) =>
            text is not null && text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
