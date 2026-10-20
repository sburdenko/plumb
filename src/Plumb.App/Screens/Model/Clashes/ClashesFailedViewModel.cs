namespace Plumb.App.Screens.Model.Clashes;

public sealed class ClashesFailedViewModel(string message) : ClashesStateViewModel
{
    public string Message { get; } = message;
}
