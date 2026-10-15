namespace Plumb.App.Platform.Viewer;

/// <summary>Where a starting viewer connects back to Plumb, and the token that proves Plumb started it.</summary>
public sealed record LinkInvitation(int Port, string Token)
{
    public const string PortArgument = "--link";

    /// <summary>An environment variable rather than an argument, so other processes cannot read it from a process list.</summary>
    public const string TokenVariable = "PLUMB_LINK_TOKEN";
}
