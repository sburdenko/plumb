using Microsoft.Extensions.Logging.Abstractions;
using Plumb.Ifc;

namespace Plumb.Tests;

[SetUpFixture]
public sealed class TestSetup
{
    [OneTimeSetUp]
    public void ConfigureXbim() => XbimSetup.Configure(NullLoggerFactory.Instance);
}
