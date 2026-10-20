using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Plumb.Core.Clashes;

namespace Plumb.App.Screens.Model.Clashes;

public sealed partial class ClashesSearchingViewModel : ClashesStateViewModel
{
    [ObservableProperty]
    public partial string Step { get; private set; } = Describe(ClashStep.ReadingGeometry);

    public void Show(ClashStep step) => Step = Describe(step);

    private static string Describe(ClashStep step) => step switch
    {
        ClashStep.ReadingGeometry => "Reading 3D geometry…",
        ClashStep.Comparing => "Comparing elements…",
        _ => throw new UnreachableException(),
    };
}
