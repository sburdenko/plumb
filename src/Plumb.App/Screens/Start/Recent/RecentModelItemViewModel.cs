using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Plumb.App.Shell;

namespace Plumb.App.Screens.Start.Recent;

/// <summary>One row of the recent models: name, folder, schema, size and age, with open, pin, reveal and remove.</summary>
public sealed partial class RecentModelItemViewModel : ViewModelBase
{
    private readonly RecentModel _model;
    private readonly OpenTarget _target;
    private readonly RecentModelsViewModel _list;

    public RecentModelItemViewModel(RecentModel model, OpenTarget target, string lastOpened, RecentModelsViewModel list)
    {
        _model = model;
        _target = target;
        _list = list;
        LastOpened = lastOpened;
    }

    public string Title => _model.Name;

    public string Location => HomeRelative(_model.Folder);

    public string Schema => _model.Schema;

    public string Elements => _model.ElementCount == 1
        ? "1 element"
        : $"{_model.ElementCount.ToString("N0", CultureInfo.InvariantCulture)} elements";

    public string LastOpened { get; }

    public bool IsPinned => _model.IsPinned;

    public bool IsMissing => _target is OpenTarget.Missing;

    public string PinTip => IsPinned ? "Unpin" : "Pin to the top";

    public string RevealLabel => _list.RevealLabel;

    private string? PathToOpen => _target switch
    {
        OpenTarget.Package package => package.Path,
        OpenTarget.Source source => source.Path,
        _ => null,
    };

    private bool CanUseFiles() => PathToOpen != null;

    [RelayCommand(CanExecute = nameof(CanUseFiles))]
    private Task OpenAsync() => _list.OpenAsync(PathToOpen!);

    [RelayCommand(CanExecute = nameof(CanUseFiles))]
    private void Reveal() => _list.Reveal(PathToOpen!);

    [RelayCommand]
    private void TogglePin() => _list.TogglePin(_model);

    [RelayCommand]
    private void Remove() => _list.Remove(_model);

    private static string HomeRelative(string folder)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return home.Length > 0 && folder.StartsWith(home + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? "~" + folder[home.Length..]
            : folder;
    }
}
