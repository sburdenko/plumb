using System;
using System.Globalization;

namespace Plumb.Viewer
{
    /// <summary>
    /// The viewer's copy of <c>Plumb.Core.Link.LinkProtocol</c>: a lower-case word, then its arguments separated by
    /// single spaces. <c>tests/link-examples.txt</c> holds lines both copies must read and write the same way.
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

            switch (words[0])
            {
                case "hello" when words.Length == 3 && TryParseVersion(words[1], out var version):
                    return new LinkMessage.Hello(version, words[2]);
                case "select" when words.Length == 2:
                    return new LinkMessage.Select(words[1]);
                case "clear" when words.Length == 1:
                    return new LinkMessage.Clear();
                default:
                    return new LinkMessage.Unknown(line);
            }
        }

        /// <exception cref="ArgumentException">An argument is empty or contains white space, or the message is
        /// <see cref="LinkMessage.Unknown"/>.</exception>
        public static string Format(LinkMessage message)
        {
            switch (message)
            {
                case LinkMessage.Hello hello:
                    return Line("hello", hello.Version.ToString(CultureInfo.InvariantCulture), Word(hello.Token));
                case LinkMessage.Select select:
                    return Line("select", Word(select.GlobalId));
                case LinkMessage.Clear _:
                    return "clear";
                default:
                    throw new ArgumentException($"{message} cannot be sent.", nameof(message));
            }
        }

        private static string Line(params string[] words) => string.Join(Separator.ToString(), words);

        private static string Word(string value)
        {
            if (string.IsNullOrEmpty(value) || Array.Exists(value.ToCharArray(), char.IsWhiteSpace))
            {
                throw new ArgumentException($"'{value}' must be one word to fit on a link line.", nameof(value));
            }

            return value;
        }

        private static bool TryParseVersion(string text, out int version) =>
            int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out version);
    }
}
