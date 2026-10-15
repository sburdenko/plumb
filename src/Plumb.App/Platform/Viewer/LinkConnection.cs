using System.Net.Sockets;
using System.Text;
using Plumb.Core.Link;

namespace Plumb.App.Platform.Viewer;

/// <summary>One viewer's socket, read and written a line at a time.</summary>
internal sealed class LinkConnection : IDisposable
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private readonly TcpClient _client;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly Lock _writing = new();

    public LinkConnection(TcpClient client)
    {
        _client = client;
        var stream = client.GetStream();
        _reader = new StreamReader(stream, Utf8);
        _writer = new StreamWriter(stream, Utf8) { AutoFlush = true, NewLine = "\n" };
    }

    /// <summary>The next message, or null when the viewer closed the connection.</summary>
    public async Task<LinkMessage?> ReadAsync(CancellationToken cancellationToken) =>
        await _reader.ReadLineAsync(cancellationToken) is { } line ? LinkProtocol.Parse(line) : null;

    /// <exception cref="IOException">The viewer is gone.</exception>
    public void Send(LinkMessage message)
    {
        lock (_writing)
        {
            _writer.WriteLine(LinkProtocol.Format(message));
        }
    }

    public void Dispose() => _client.Dispose();
}
