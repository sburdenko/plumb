namespace Plumb.Viewer
{
    /// <summary>
    /// One line between the viewer and Plumb, the viewer's copy of <c>Plumb.Core.Link.LinkMessage</c>. The viewer
    /// opens with <see cref="Hello"/>; after that either side sends <see cref="Select"/> or <see cref="Clear"/>.
    /// </summary>
    public abstract class LinkMessage
    {
        private LinkMessage()
        {
        }

        public sealed class Hello : LinkMessage
        {
            public Hello(int version, string token)
            {
                Version = version;
                Token = token;
            }

            public int Version { get; }

            public string Token { get; }
        }

        /// <summary>The sender's selection is now this element.</summary>
        public sealed class Select : LinkMessage
        {
            public Select(string globalId)
            {
                GlobalId = globalId;
            }

            public string GlobalId { get; }
        }

        /// <summary>The sender's selection is now empty.</summary>
        public sealed class Clear : LinkMessage
        {
        }

        /// <summary>A line this version does not understand; ignored, so a newer Plumb can add messages.</summary>
        public sealed class Unknown : LinkMessage
        {
            public Unknown(string line)
            {
                Line = line;
            }

            public string Line { get; }
        }
    }
}
