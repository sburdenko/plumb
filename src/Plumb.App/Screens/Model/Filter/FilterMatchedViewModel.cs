using System.Globalization;

namespace Plumb.App.Screens.Model.Filter;

public sealed class FilterMatchedViewModel(int count) : FilterStatusViewModel
{
    public override string Text => count switch
    {
        0 => "No element matches",
        1 => "1 element matches",
        _ => $"{count.ToString("N0", CultureInfo.InvariantCulture)} elements match",
    };
}
