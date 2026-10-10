using Plumb.App.Screens.Failed;
using Plumb.App.Screens.Importing;
using Plumb.App.Screens.Model;
using Plumb.App.Screens.Start;
using Plumb.App.Shell;

namespace Plumb.Tests;

[TestFixture]
public sealed class ScreenViewModelTests
{
    [Test]
    public void TheWindowShowsEmptyImportingFailedOrLoaded()
    {
        var states = typeof(ScreenViewModel).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(ScreenViewModel)));

        Assert.That(states, Is.EquivalentTo(new[]
        {
            typeof(EmptyStateViewModel),
            typeof(ImportingViewModel),
            typeof(FailedViewModel),
            typeof(LoadedViewModel),
        }));
    }
}
