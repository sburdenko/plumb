using System.Net;
using System.Net.Sockets;
using System.Text;
using Plumb.App.Platform.Viewer;

namespace Plumb.Tests;

/// <summary>The viewer's end of a link, driven by a test over a real loopback socket.</summary>
internal sealed class FakeViewer : IDisposable
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    private readonly TcpClient _client = new();
    private StreamReader _reader = null!;
    private StreamWriter _writer = null!;

    public static async Task<FakeViewer> ConnectAsync(LinkInvitation invitation)
    {
        var viewer = new FakeViewer();
        await viewer._client.ConnectAsync(IPAddress.Loopback, invitation.Port).WaitAsync(Patience);
        var stream = viewer._client.GetStream();
        viewer._reader = new StreamReader(stream, new UTF8Encoding(false));
        viewer._writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
        return viewer;
    }

    public static async Task<FakeViewer> LinkAsync(LinkInvitation invitation)
    {
        var viewer = await ConnectAsync(invitation);
        await viewer.SendAsync($"hello 1 {invitation.Token}");
        return viewer;
    }

    public Task SendAsync(string line) => _writer.WriteLineAsync(line);

    /// <summary>The next line from Plumb, or null when Plumb closed the link.</summary>
    public async Task<string?> ReadAsync() => await _reader.ReadLineAsync().WaitAsync(Patience);

    public void Dispose() => _client.Dispose();
}
