
namespace Plumb.App.Platform;

public interface IFilePickerService
{
    /// <returns>The local path of the chosen file, or null if the user cancelled.</returns>
    Task<string?> PickIfcFileAsync();

    /// <returns>The local path of the chosen <c>.plumb</c> folder, or null if the user cancelled.</returns>
    Task<string?> PickPackageAsync();
}
