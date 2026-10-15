using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Plumb.Viewer.Tests
{
    public sealed class LinkProtocolTests
    {
        private static IEnumerable<string> SharedExamples() =>
            File.ReadAllLines(Path.Combine(Application.dataPath, "..", "..", "tests", "link-examples.txt"))
                .Where(line => line.Length > 0);

        [TestCaseSource(nameof(SharedExamples))]
        public void EveryExamplePlumbAlsoChecksReadsAndWritesBackUnchanged(string line)
        {
            var message = LinkProtocol.Parse(line);

            Assert.That(message, Is.Not.TypeOf<LinkMessage.Unknown>());
            Assert.That(LinkProtocol.Format(message), Is.EqualTo(line));
        }

        [Test]
        public void SelectCarriesTheGlobalId()
        {
            var message = (LinkMessage.Select)LinkProtocol.Parse("select 2O2Fr$t4X7Zf8NOew3FNql");

            Assert.That(message.GlobalId, Is.EqualTo("2O2Fr$t4X7Zf8NOew3FNql"));
        }

        [TestCase("")]
        [TestCase("select")]
        [TestCase("select two words")]
        [TestCase("clear now")]
        [TestCase("frame everything")]
        public void AnythingElseIsUnknown(string line)
        {
            Assert.That(LinkProtocol.Parse(line), Is.TypeOf<LinkMessage.Unknown>());
        }

        [Test]
        public void AGlobalIdThatWouldBreakTheLineCannotBeWritten()
        {
            Assert.That(() => LinkProtocol.Format(new LinkMessage.Select("two words")), Throws.InstanceOf<ArgumentException>());
        }
    }
}
