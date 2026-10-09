namespace Plumb.App.Services;

public interface IFilePickerService
{
    /// <returns>The local path of the chosen file, or null if the user cancelled.</returns>
    Task<string?> PickIfcFileAsync();
}
