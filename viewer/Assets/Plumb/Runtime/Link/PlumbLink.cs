using System;

namespace Plumb.Viewer
{
    public enum LinkState
    {
        /// <summary>Started without an invitation, by hand or for a screenshot or benchmark.</summary>
        NotLinked,
        Connecting,
        Linked,

        /// <summary>Plumb could not be reached or went away; the viewer keeps working on its own.</summary>
        Lost,
    }

    /// <summary>
    /// The viewer's side of the link to the Plumb that started it. Read and written from the main thread: messages
    /// from Plumb wait in a queue until <see cref="TryReceive"/> takes them in <c>Update</c>.
    /// </summary>
    public abstract class PlumbLink : IDisposable
    {
        public abstract LinkState State { get; }

        public static PlumbLink For(ViewerArguments arguments) =>
            arguments.LinkPort is int port ? new SocketLink(port, arguments.LinkToken) : (PlumbLink)new NoLink();

        /// <summary>Takes the next <see cref="LinkMessage.Select"/> or <see cref="LinkMessage.Clear"/> from Plumb.</summary>
        public abstract bool TryReceive(out LinkMessage message);

        /// <summary>Tells Plumb the person selected this element here, or nothing when <paramref name="globalId"/> is null.</summary>
        public abstract void Share(string globalId);

        public abstract void Dispose();
    }
}
