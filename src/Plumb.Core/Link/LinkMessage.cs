namespace Plumb.Core.Link
{
    /// <summary>
    /// One line between Plumb and the 3D viewer it started. The viewer opens with <see cref="Hello"/>; after that
    /// either side sends <see cref="Select"/> or <see cref="Clear"/> whenever the person changes the selection.
    /// </summary>
    public abstract record LinkMessage
    {
        private LinkMessage()
        {
        }

        /// <param name="Version">The protocol version the viewer speaks; see <see cref="LinkProtocol.Version"/>.</param>
        /// <param name="Token">The token Plumb gave the viewer when it started it.</param>
        public sealed record Hello(int Version, string Token) : LinkMessage;

        /// <summary>The sender's selection is now this element.</summary>
        public sealed record Select(string GlobalId) : LinkMessage;

        /// <summary>The sender's selection is now empty.</summary>
        public sealed record Clear : LinkMessage;

        /// <summary>A line this version does not understand. It is ignored, so a newer peer can add messages.</summary>
        public sealed record Unknown(string Line) : LinkMessage;
    }
}
