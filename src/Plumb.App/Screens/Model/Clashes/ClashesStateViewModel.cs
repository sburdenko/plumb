using Plumb.App.Shell;

namespace Plumb.App.Screens.Model.Clashes;

/// <summary>
/// What the clash panel shows under its two groups: nothing searched yet, a search under way, the clashes found,
/// none, a failure, or why this model cannot be searched at all.
/// </summary>
public abstract class ClashesStateViewModel : ViewModelBase;
