namespace Plumb.App.Screens.Model.Filter;

/// <summary>The text is not a query yet; the tree keeps showing the last result.</summary>
public sealed class FilterRejectedViewModel(string reason) : FilterStatusViewModel
{
    public override string Text => reason;
}
