using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Plumb.Core.Model;

namespace Plumb.Core.Search
{
    /// <summary>
    /// Finds the elements of one model that satisfy a query. Properties are indexed by name the first time a
    /// property condition needs them, so a text-only search never pays for the index.
    /// </summary>
    public sealed class ElementSearch
    {
        private const int CancellationCheckInterval = 4096;
        private const double NumberTolerance = 1e-9;

        private readonly IReadOnlyList<ElementRecord> _elements;
        private readonly Lazy<Dictionary<string, List<PropertyRecord>>> _propertiesByName;

        public ElementSearch(IReadOnlyList<ElementRecord> elements, IReadOnlyList<PropertyRecord> properties)
        {
            _elements = elements;
            _propertiesByName = new Lazy<Dictionary<string, List<PropertyRecord>>>(() => IndexByName(properties));
        }

        /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
        public ElementMatches Find(ElementQuery query, CancellationToken cancellationToken)
        {
            HashSet<string>? matches = null;
            foreach (var condition in query.Conditions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var found = Evaluate(condition, cancellationToken);
                if (matches == null)
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

        private HashSet<string> Evaluate(Condition condition, CancellationToken cancellationToken)
        {
            switch (condition)
            {
                case Condition.Text text:
                    return MatchText(text.Term, cancellationToken);
                case Condition.PropertyEquals equals:
                    return MatchProperty(equals.Name, value => ValuesAreEqual(value, equals.Value), cancellationToken);
                case Condition.PropertyContains contains:
                    return MatchProperty(contains.Name, value => Contains(value, contains.Value), cancellationToken);
                case Condition.PropertyNumber number:
                    return MatchProperty(number.Name, value => Passes(value, number), cancellationToken);
                default:
                    throw new ArgumentOutOfRangeException(nameof(condition), condition.GetType().Name, "Unknown condition.");
            }
        }

        private HashSet<string> MatchText(string term, CancellationToken cancellationToken)
        {
            var matches = new HashSet<string>(StringComparer.Ordinal);
            var checkedCount = 0;
            foreach (var element in _elements)
            {
                ThrowIfCancelled(++checkedCount, cancellationToken);
                if (Contains(element.Name, term) || Contains(element.IfcType, term) || Contains(element.GlobalId, term)
                    || Contains(element.Tag == null ? null : "#" + element.Tag, term))
                {
                    matches.Add(element.GlobalId);
                }
            }

            return matches;
        }

        private HashSet<string> MatchProperty(string name, Func<string, bool> valuePasses, CancellationToken cancellationToken)
        {
            var matches = new HashSet<string>(StringComparer.Ordinal);
            if (!_propertiesByName.Value.TryGetValue(name.Trim(), out var properties))
            {
                return matches;
            }

            var checkedCount = 0;
            foreach (var property in properties)
            {
                ThrowIfCancelled(++checkedCount, cancellationToken);
                if (property.Value != null && valuePasses(property.Value))
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
            if (TryNumber(stored, out var storedNumber) && TryNumber(wanted, out var wantedNumber))
            {
                var scale = Math.Max(1d, Math.Max(Math.Abs(storedNumber), Math.Abs(wantedNumber)));
                return Math.Abs(storedNumber - wantedNumber) <= NumberTolerance * scale;
            }

            return string.Equals(stored.Trim(), wanted.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool Passes(string value, Condition.PropertyNumber condition)
        {
            if (!TryNumber(value, out var number))
            {
                return false;
            }

            switch (condition.Comparison)
            {
                case NumberComparison.Less: return number < condition.Number;
                case NumberComparison.LessOrEqual: return number <= condition.Number;
                case NumberComparison.Greater: return number > condition.Number;
                case NumberComparison.GreaterOrEqual: return number >= condition.Number;
                default: throw new ArgumentOutOfRangeException(nameof(condition), condition.Comparison, "Unknown comparison.");
            }
        }

        private static bool TryNumber(string text, out double number) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
            && !double.IsNaN(number) && !double.IsInfinity(number);

        private static bool Contains(string? text, string term) =>
            text != null && text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;

        private static void ThrowIfCancelled(int checkedCount, CancellationToken cancellationToken)
        {
            if (checkedCount % CancellationCheckInterval == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
