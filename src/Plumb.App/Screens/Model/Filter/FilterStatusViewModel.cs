using Plumb.App.Shell;

namespace Plumb.App.Screens.Model.Filter;

/// <summary>What the tree filter says about the text in its box: how many elements match, or why it cannot search yet.</summary>
public abstract class FilterStatusViewModel : ViewModelBase
{
    public abstract string Text { get; }
}
