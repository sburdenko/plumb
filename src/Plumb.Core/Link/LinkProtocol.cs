using System;
using System.Globalization;

namespace Plumb.Core.Link
{
    /// <summary>
    /// Reads and writes <see cref="LinkMessage"/> lines: a lower-case word, then its arguments separated by single
    /// spaces. The viewer has its own copy of these rules; <c>tests/link-examples.txt</c> keeps the two in step.
    /// </summary>
    public static class LinkProtocol
    {
        public const int Version = 1;

        private const char Separator = ' ';

        public static LinkMessage Parse(string line)
        {
            var words = line.TrimEnd('\r').Split(Separator);
            if (Array.IndexOf(words, string.Empty) >= 0)
            {
                return new LinkMessage.Unknown(line);
            }

            return (words[0], words.Length) switch
            {
                ("hello", 3) when TryParseVersion(words[1], out var version) => new LinkMessage.Hello(version, words[2]),
                ("select", 2) => new LinkMessage.Select(words[1]),
                ("clear", 1) => new LinkMessage.Clear(),
                _ => new LinkMessage.Unknown(line),
            };
        }

        /// <exception cref="ArgumentException">An argument is empty or contains white space, or the message is
        /// <see cref="LinkMessage.Unknown"/>.</exception>
        public static string Format(LinkMessage message) => message switch
        {
            LinkMessage.Hello hello => Line("hello", hello.Version.ToString(CultureInfo.InvariantCulture), Word(hello.Token)),
            LinkMessage.Select select => Line("select", Word(select.GlobalId)),
            LinkMessage.Clear => "clear",
            _ => throw new ArgumentException($"{message} cannot be sent.", nameof(message)),
        };

        private static string Line(params string[] words) => string.Join(Separator.ToString(), words);

        private static string Word(string value)
        {
            if (value.Length == 0 || Array.Exists(value.ToCharArray(), char.IsWhiteSpace))
            {
                throw new ArgumentException($"'{value}' must be one word to fit on a link line.", nameof(value));
            }

            return value;
        }

        private static bool TryParseVersion(string text, out int version) =>
            int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out version);
    }
}
