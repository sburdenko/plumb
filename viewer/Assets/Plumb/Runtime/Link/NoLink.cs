namespace Plumb.Viewer
{
    public sealed class NoLink : PlumbLink
    {
        public override LinkState State => LinkState.NotLinked;

        public override bool TryReceive(out LinkMessage message)
        {
            message = null;
            return false;
        }

        public override void Share(string globalId)
        {
        }

        public override void Dispose()
        {
        }
    }
}
