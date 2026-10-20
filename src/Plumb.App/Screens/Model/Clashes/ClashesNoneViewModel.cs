using System.Globalization;
using Plumb.Import;

namespace Plumb.App.Screens.Model.Clashes;

public sealed class ClashesNoneViewModel : ClashesStateViewModel
{
    public string Text =>
        $"No clashes. No element of the first group passes more than {(ClashService.Tolerance * 100).ToString("0.#", CultureInfo.InvariantCulture)} cm into one of the second.";
}
