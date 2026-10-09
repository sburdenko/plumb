using Microsoft.Extensions.Logging.Abstractions;
using Plumb.Import.Xbim;

namespace Plumb.Tests;

[SetUpFixture]
public sealed class TestSetup
{
    [OneTimeSetUp]
    public void ConfigureXbim() => XbimSetup.Configure(NullLoggerFactory.Instance);
}
