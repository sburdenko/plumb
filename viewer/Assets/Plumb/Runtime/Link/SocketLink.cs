using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// Connects to Plumb on a loopback port, says hello with the token Plumb gave, then reads Plumb's lines on a
    /// background thread. Remembers the selection both sides share, so a pick is sent once and what Plumb selected
    /// is never sent back.
    /// </summary>
    public sealed class SocketLink : PlumbLink
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        private readonly ConcurrentQueue<LinkMessage> _inbox = new ConcurrentQueue<LinkMessage>();
        private readonly object _writing = new object();
        private readonly TcpClient _client = new TcpClient();
        private readonly int _port;
        private readonly string _token;
        private volatile LinkState _state = LinkState.Connecting;
        private StreamWriter _writer;
        private string _shared;

        public SocketLink(int port, string token)
        {
            _port = port;
            _token = token;
            new Thread(Run) { IsBackground = true, Name = "Plumb link" }.Start();
        }

        public override LinkState State => _state;

        public override bool TryReceive(out LinkMessage message)
        {
            while (_inbox.TryDequeue(out message))
            {
                switch (message)
                {
                    case LinkMessage.Select select:
                        _shared = select.GlobalId;
                        return true;
                    case LinkMessage.Clear _:
                        _shared = null;
                        return true;
                }
            }

            return false;
        }

        public override void Share(string globalId)
        {
            if (globalId == _shared)
            {
                return;
            }

            _shared = globalId;
            if (_state == LinkState.Linked)
            {
                Send(globalId == null ? (LinkMessage)new LinkMessage.Clear() : new LinkMessage.Select(globalId));
            }
        }

        public override void Dispose() => _client.Close();

        private void Run()
        {
            try
            {
                _client.Connect(IPAddress.Loopback, _port);
                var stream = _client.GetStream();
                var reader = new StreamReader(stream, Utf8);
                _writer = new StreamWriter(stream, Utf8) { AutoFlush = true, NewLine = "\n" };
                Send(new LinkMessage.Hello(LinkProtocol.Version, _token));
                _state = LinkState.Linked;
                Debug.Log($"Linked to Plumb on port {_port}");

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    _inbox.Enqueue(LinkProtocol.Parse(line));
                }
            }
            catch (Exception ex) when (ex is IOException || ex is SocketException || ex is ObjectDisposedException || ex is ArgumentException)
            {
                Debug.Log($"The link to Plumb ended: {ex.Message}");
            }
            finally
            {
                _state = LinkState.Lost;
            }
        }

        private void Send(LinkMessage message)
        {
            try
            {
                lock (_writing)
                {
                    _writer.WriteLine(LinkProtocol.Format(message));
                }
            }
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
            {
                Debug.Log($"Cannot reach Plumb: {ex.Message}");
                _state = LinkState.Lost;
            }
        }
    }
}
