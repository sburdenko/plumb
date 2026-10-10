using Plumb.App.ViewModels;

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
