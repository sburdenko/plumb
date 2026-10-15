using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Plumb.Core.Link;

namespace Plumb.App.Platform.Viewer;

/// <summary>
/// Starts the viewer with an invitation to connect back over a loopback socket, then keeps the two selections in
/// step. Plumb links to one viewer at a time: the one it started last.
/// </summary>
public sealed class ViewerLink : IViewerLink, IDisposable
{
    private static readonly TimeSpan HelloTimeout = TimeSpan.FromSeconds(10);

    private readonly IViewerStarter _starter;
    private readonly ILogger<ViewerLink> _logger;
    private readonly Action<Action> _deliver;
    private readonly CancellationTokenSource _closing = new();
    private readonly Lock _gate = new();
    private TcpListener? _listener;
    private ViewerSession _session = new ViewerSession.None();

    /// <param name="deliver">Runs a <see cref="SelectionChanged"/> notification on the thread that owns the window.</param>
    public ViewerLink(IViewerStarter starter, Action<Action> deliver, ILogger<ViewerLink> logger)
    {
        _starter = starter;
        _deliver = deliver;
        _logger = logger;
    }

    public event EventHandler<ViewerSelection>? SelectionChanged;

    internal ViewerSession Session
    {
        get
        {
            lock (_gate)
            {
                return _session;
            }
        }
    }

    public bool TryOpen(string packagePath, string? selectedId, [NotNullWhen(false)] out string? error)
    {
        // Held while the viewer starts, so its hello cannot arrive before Plumb knows the token it was given.
        lock (_gate)
        {
            if (_session is ViewerSession.Running running && running.PackagePath == packagePath && !running.Process.HasExited)
            {
                running.Process.BringToFront();
                Share(running, selectedId);
                error = null;
                return true;
            }

            var invitation = Invite();
            if (!_starter.TryStart(packagePath, invitation, out var process, out error))
            {
                return false;
            }

            Replace(invitation == null
                ? new ViewerSession.None()
                : new ViewerSession.Starting(packagePath, process, selectedId, invitation.Token));
            if (invitation == null)
            {
                process.Dispose();
            }

            return true;
        }
    }

    public void ShowSelection(string packagePath, string? globalId)
    {
        lock (_gate)
        {
            if (_session is ViewerSession.Running running && running.PackagePath == packagePath)
            {
                Share(running, globalId);
            }
        }
    }

    public void Dispose()
    {
        _closing.Cancel();
        lock (_gate)
        {
            _listener?.Stop();
            Replace(new ViewerSession.None());
        }

        _closing.Dispose();
    }

    private LinkInvitation? Invite()
    {
        if (_listener == null)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, port: 0);
                listener.Start();
                _listener = listener;
                _ = AcceptAsync(listener, _closing.Token);
            }
            catch (SocketException ex)
            {
                _logger.LogWarning(ex, "Cannot listen for the viewer; it will start without a link");
                return null;
            }
        }

        return new LinkInvitation(((IPEndPoint)_listener.LocalEndpoint).Port, NewToken());
    }

    private async Task AcceptAsync(TcpListener listener, CancellationToken closing)
    {
        while (true)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(closing);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
            {
                return;
            }
            catch (SocketException ex)
            {
                _logger.LogWarning(ex, "Stopped listening for the viewer");
                return;
            }

            _ = ServeAsync(new LinkConnection(client), closing);
        }
    }

    private async Task ServeAsync(LinkConnection connection, CancellationToken closing)
    {
        try
        {
            using (var waiting = CancellationTokenSource.CreateLinkedTokenSource(closing))
            {
                waiting.CancelAfter(HelloTimeout);
                if (await connection.ReadAsync(waiting.Token) is not LinkMessage.Hello hello || !TryAccept(connection, hello))
                {
                    return;
                }
            }

            while (await connection.ReadAsync(closing) is { } message)
            {
                Receive(connection, message);
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
        {
            _logger.LogDebug(ex, "Viewer connection ended");
        }
        finally
        {
            Disconnected(connection);
        }
    }

    private bool TryAccept(LinkConnection connection, LinkMessage.Hello hello)
    {
        lock (_gate)
        {
            if (_session is not ViewerSession.Starting starting || !SameToken(starting.Token, hello.Token))
            {
                _logger.LogWarning("Turned away a connection that Plumb did not invite");
                return false;
            }

            if (hello.Version != LinkProtocol.Version)
            {
                _logger.LogWarning("The viewer speaks link version {Version}; Plumb speaks {Expected}", hello.Version, LinkProtocol.Version);
                return false;
            }

            var linked = new ViewerSession.Linked(starting.PackagePath, starting.Process, starting.Selection, connection);
            Replace(linked);
            _logger.LogInformation("Linked to the viewer for {Package}", linked.PackagePath);
            if (linked.Selection != null)
            {
                Send(linked, new LinkMessage.Select(linked.Selection));
            }

            return true;
        }
    }

    private void Receive(LinkConnection connection, LinkMessage message)
    {
        string? globalId;
        switch (message)
        {
            case LinkMessage.Select select:
                globalId = select.GlobalId;
                break;
            case LinkMessage.Clear:
                globalId = null;
                break;
            default:
                return;
        }

        ViewerSelection report;
        lock (_gate)
        {
            if (_session is not ViewerSession.Linked linked || linked.Connection != connection || linked.Selection == globalId)
            {
                return;
            }

            _session = linked with { Selection = globalId };
            report = new ViewerSelection(linked.PackagePath, globalId);
        }

        Report(report);
    }

    private void Disconnected(LinkConnection connection)
    {
        lock (_gate)
        {
            if (_session is ViewerSession.Linked linked && linked.Connection == connection)
            {
                _logger.LogInformation("The viewer for {Package} closed the link", linked.PackagePath);
                Replace(new ViewerSession.None());
            }
        }

        connection.Dispose();
    }

    /// <summary>Must hold the gate.</summary>
    private void Share(ViewerSession.Running running, string? globalId)
    {
        if (running.Selection == globalId)
        {
            return;
        }

        var shared = running with { Selection = globalId };
        _session = shared;
        if (shared is ViewerSession.Linked linked)
        {
            Send(linked, globalId == null ? new LinkMessage.Clear() : new LinkMessage.Select(globalId));
        }
    }

    /// <summary>Must hold the gate.</summary>
    private void Send(ViewerSession.Linked linked, LinkMessage message)
    {
        try
        {
            linked.Connection.Send(message);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            _logger.LogInformation(ex, "The viewer for {Package} is gone", linked.PackagePath);
            Replace(new ViewerSession.None());
        }
    }

    /// <summary>Must hold the gate. Releases whatever the previous session held that the next one does not.</summary>
    private void Replace(ViewerSession next)
    {
        var previous = _session;
        _session = next;
        if (previous is ViewerSession.Linked linked && (next as ViewerSession.Linked)?.Connection != linked.Connection)
        {
            linked.Connection.Dispose();
        }

        if (previous is ViewerSession.Running running && (next as ViewerSession.Running)?.Process != running.Process)
        {
            running.Process.Dispose();
        }
    }

    private void Report(ViewerSelection selection) => _deliver(() => SelectionChanged?.Invoke(this, selection));

    private static string NewToken() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));

    private static bool SameToken(string expected, string offered) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(offered));
}
