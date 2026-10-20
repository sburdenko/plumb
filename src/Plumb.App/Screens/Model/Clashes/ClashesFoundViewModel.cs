using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Plumb.App.Screens.Model.Clashes;

/// <summary>The clashes found, deepest first. Choosing one selects its first element everywhere.</summary>
public sealed partial class ClashesFoundViewModel(IReadOnlyList<ClashRowViewModel> rows, Action<string> selectElement)
    : ClashesStateViewModel
{
    public IReadOnlyList<ClashRowViewModel> Rows { get; } = rows;

    public string Summary => Rows.Count == 1 ? "1 clash" : $"{Rows.Count.ToString("N0", CultureInfo.InvariantCulture)} clashes";

    [ObservableProperty]
    public partial ClashRowViewModel? SelectedRow { get; set; }

    partial void OnSelectedRowChanged(ClashRowViewModel? value)
    {
        if (value != null)
        {
            selectElement(value.FirstId);
        }
    }
}
