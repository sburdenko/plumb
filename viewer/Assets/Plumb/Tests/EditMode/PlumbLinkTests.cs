using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;

namespace Plumb.Viewer.Tests
{
    public sealed class PlumbLinkTests
    {
        private const string Token = "3f2c9a1e7b6d4e0f8a5c2b1d9e7f6a3c";
        private const string Wall = "2O2Fr$t4X7Zf8NOew3FNql";
        private const string Door = "0K7w7JxHr9IggqQcRYZ6Jx";
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

        private TcpListener _plumb;
        private TcpClient _accepted;
        private StreamReader _fromViewer;
        private StreamWriter _toViewer;
        private PlumbLink _link;

        [SetUp]
        public void ListenAsPlumb()
        {
            _plumb = new TcpListener(IPAddress.Loopback, 0);
            _plumb.Start();
        }

        [TearDown]
        public void CloseEverything()
        {
            _link?.Dispose();
            _accepted?.Dispose();
            _plumb.Stop();
        }

        [Test]
        public void WithoutAnInvitationTheViewerIsNotLinked()
        {
            _link = PlumbLink.For(ViewerArguments.Parse(new[] { "viewer", "--package", "/m/a.plumb" }, linkToken: null));

            Assert.That(_link.State, Is.EqualTo(LinkState.NotLinked));
            Assert.That(_link.TryReceive(out _), Is.False);
            Assert.That(() => _link.Share(Wall), Throws.Nothing);
        }

        [Test]
        public void TheViewerSaysHelloWithItsToken()
        {
            Connect();

            Assert.That(_fromViewer.ReadLine(), Is.EqualTo($"hello 1 {Token}"));
            WaitUntil(() => _link.State == LinkState.Linked);
        }

        [Test]
        public void SelectionsFromPlumbAreReceivedInOrder()
        {
            ConnectAndGreet();

            _toViewer.WriteLine($"select {Wall}");
            _toViewer.WriteLine("frame everything");
            _toViewer.WriteLine("clear");

            Assert.That(NextMessage(), Is.TypeOf<LinkMessage.Select>().With.Property("GlobalId").EqualTo(Wall));
            Assert.That(NextMessage(), Is.TypeOf<LinkMessage.Clear>());
        }

        [Test]
        public void APickIsSentToPlumb()
        {
            ConnectAndGreet();

            _link.Share(Wall);
            _link.Share(null);

            Assert.That(_fromViewer.ReadLine(), Is.EqualTo($"select {Wall}"));
            Assert.That(_fromViewer.ReadLine(), Is.EqualTo("clear"));
        }

        [Test]
        public void WhatPlumbSelectedIsNotSentBack()
        {
            ConnectAndGreet();
            _toViewer.WriteLine($"select {Wall}");
            NextMessage();

            _link.Share(Wall);
            _link.Share(Door);

            Assert.That(_fromViewer.ReadLine(), Is.EqualTo($"select {Door}"));
        }

        [Test]
        public void ClosingPlumbLosesTheLink()
        {
            ConnectAndGreet();

            _accepted.Dispose();

            WaitUntil(() => _link.State == LinkState.Lost);
        }

        [Test]
        public void NoPlumbToConnectToLosesTheLink()
        {
            var port = ((IPEndPoint)_plumb.LocalEndpoint).Port;
            _plumb.Stop();

            _link = PlumbLink.For(Invited(port));

            WaitUntil(() => _link.State == LinkState.Lost);
        }

        private void Connect()
        {
            _link = PlumbLink.For(Invited(((IPEndPoint)_plumb.LocalEndpoint).Port));
            var accepting = _plumb.AcceptTcpClientAsync();
            Assert.That(accepting.Wait(Patience), Is.True, "the viewer did not connect");
            _accepted = accepting.Result;
            var stream = _accepted.GetStream();
            stream.ReadTimeout = (int)Patience.TotalMilliseconds;
            _fromViewer = new StreamReader(stream, new UTF8Encoding(false));
            _toViewer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
        }

        private void ConnectAndGreet()
        {
            Connect();
            _fromViewer.ReadLine();
            WaitUntil(() => _link.State == LinkState.Linked);
        }

        private LinkMessage NextMessage()
        {
            LinkMessage message = null;
            WaitUntil(() => _link.TryReceive(out message));
            return message;
        }

        private static ViewerArguments Invited(int port) =>
            ViewerArguments.Parse(new[] { "viewer", "--package", "/m/a.plumb", "--link", port.ToString() }, Token);

        private static void WaitUntil(Func<bool> condition)
        {
            var clock = Stopwatch.StartNew();
            while (!condition())
            {
                if (clock.Elapsed > Patience)
                {
                    Assert.Fail("Timed out waiting for the link.");
                }

                System.Threading.Thread.Sleep(5);
            }
        }
    }
}
